#region Copyright (c) 2017 Atif Aziz, Adrian Guerra
//
// Portions Copyright (c) 2013 Ivan Nikulin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace ParseFive.Tokenizer
{
    using System.Collections.Generic;
    using Extensions;
    using CP = Common.Unicode.CodePoints;
    using static Truthiness;

    sealed class Preprocessor
    {
        const int DefaultBufferWaterline = 1 << 16;

        string html;

        int pos;
        int lastGapPos;
        int lastCharPos;

        Stack<int> gapStack;

        bool skipNextNewLine;

        bool lastChunkWritten;
        readonly int bufferWaterline;

        public bool EndOfChunkHit { get; private set; }

        public Preprocessor()
        {
            this.html = null;

            this.pos = -1;
            this.lastGapPos = -1;
            this.lastCharPos = -1;

            this.gapStack = new Stack<int>();

            this.skipNextNewLine = false;

            this.lastChunkWritten = false;
            this.EndOfChunkHit = false;
            this.bufferWaterline = DefaultBufferWaterline;
        }

        public void DropParsedChunk()
        {
            if (this.pos > this.bufferWaterline)
            {
                this.lastCharPos -= this.pos;
                this.html = this.html.Substring(this.pos);
                this.pos = 0;
                this.lastGapPos = -1;
                this.gapStack = new Stack<int>();
            }
        }

        void AddGap()
        {
            this.gapStack.Push(this.lastGapPos);
            this.lastGapPos = this.pos;
        }

        int ProcessHighRangeCodePoint(int cp)
        {
            //NOTE: try to peek a surrogate pair
            if (this.pos != this.lastCharPos)
            {
                var nextCp = this.html.CharCodeAt(this.pos + 1);

                if (IsSurrogatePair(cp, nextCp))
                {
                    //NOTE: we have a surrogate pair. Peek pair character and recalculate code point.
                    this.pos++;
                    cp = GetSurrogatePairCodePoint(cp, nextCp);

                    //NOTE: add gap that should be avoided during retreat
                    AddGap();
                }
            }

            // NOTE: we've hit the end of chunk, stop processing at this point
            else if (!this.lastChunkWritten)
            {
                this.EndOfChunkHit = true;
                return CP.EOF;
            }
            return cp;

            bool IsSurrogatePair(int cp1, int cp2) =>
                cp1 >= 0xD800 && cp1 <= 0xDBFF && cp2 >= 0xDC00 && cp2 <= 0xDFFF;

            int GetSurrogatePairCodePoint(int cp1, int cp2) =>
                (cp1 - 0xD800) * 0x400 + 0x2400 + cp2;
        }

        public void Write(string chunk, bool isLastChunk)
        {
            if (IsTruthy(this.html))
                this.html += chunk;
            else
                this.html = chunk;

            this.lastCharPos = this.html.Length - 1;
            this.EndOfChunkHit = false;
            this.lastChunkWritten = isLastChunk;
        }

        public void InsertHtmlAtCurrentPos(string chunk)
        {
            this.html = this.html.Substring(0, this.pos + 1) +
                        chunk +
                        this.html.Substring(this.pos + 1, this.html.Length);

            this.lastCharPos = this.html.Length - 1;
            this.EndOfChunkHit = false;
        }

        public int Advance()
        {
            this.pos++;

            if (this.pos > this.lastCharPos)
            {
                if (!this.lastChunkWritten)
                    this.EndOfChunkHit = true;

                return CP.EOF;
            }

            var cp = this.html.CharCodeAt(this.pos);

            //NOTE: any U+000A LINE FEED (LF) characters that immediately follow a U+000D CARRIAGE RETURN (CR) character
            //must be ignored.
            if (this.skipNextNewLine && cp == CP.LINE_FEED)
            {
                this.skipNextNewLine = false;
                AddGap();
                return Advance();
            }

            //NOTE: all U+000D CARRIAGE RETURN (CR) characters must be converted to U+000A LINE FEED (LF) characters
            if (cp == CP.CARRIAGE_RETURN)
            {
                this.skipNextNewLine = true;
                return CP.LINE_FEED;
            }

            this.skipNextNewLine = false;

            //OPTIMIZATION: first perform check if the code point in the allowed range that covers most common
            //HTML input (e.g. ASCII codes) to avoid performance-cost operations for high-range code points.
            return cp >= 0xD800 ? ProcessHighRangeCodePoint(cp) : cp;
        }


        public void Retreat()
        {
            if (this.pos == this.lastGapPos)
            {
                this.lastGapPos = this.gapStack.Pop();
                this.pos--;
            }

            this.pos--;
        }
    }
}
