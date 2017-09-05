using System;
using System.Collections.Generic;
using System.Text;
using ParseFive.Extensions;
using ɑ = ParseFive.Common.Unicode.CODE_POINTS;

namespace ParseFive.Tokenizer
{
    using Compatibility;

    class Preprocessor
    {
        const int DEFAULT_BUFFER_WATERLINE = 1 << 16;

        string html;

        int pos;
        int lastGapPos;
        int lastCharPos;

        Stack<int> gapStack;

        bool skipNextNewLine;

        bool lastChunkWritten;
        public bool endOfChunkHit;
        int bufferWaterline;


        bool isSurrogatePair(int cp1, int cp2)
        {
            return cp1 >= 0xD800 && cp1 <= 0xDBFF && cp2 >= 0xDC00 && cp2 <= 0xDFFF;
        }

        int getSurrogatePairCodePoint(int cp1, int cp2)
        {
            return (cp1 - 0xD800) * 0x400 + 0x2400 + cp2;
        }

        public Preprocessor()
        {
            this.html = null;

            this.pos = -1;
            this.lastGapPos = -1;
            this.lastCharPos = -1;

            this.gapStack = new Stack<int>();

            this.skipNextNewLine = false;

            this.lastChunkWritten = false;
            this.endOfChunkHit = false;
            this.bufferWaterline = DEFAULT_BUFFER_WATERLINE;
        }

        public void dropParsedChunk()
        {
            if (this.pos > this.bufferWaterline)
            {
                this.lastCharPos -= this.pos;
                this.html = this.html.substring(this.pos);
                this.pos = 0;
                this.lastGapPos = -1;
                this.gapStack = new Stack<int>();
            }
        }

        private void addGap()
        {
            this.gapStack.Push(this.lastGapPos);
            this.lastGapPos = this.pos;
        }

        private int processHighRangeCodePoint(int cp)
        {
            //NOTE: try to peek a surrogate pair
            if (this.pos != this.lastCharPos)
            {
                var nextCp = this.html.charCodeAt(this.pos + 1);

                if (isSurrogatePair(cp, nextCp))
                {
                    //NOTE: we have a surrogate pair. Peek pair character and recalculate code point.
                    this.pos++;
                    cp = getSurrogatePairCodePoint(cp, nextCp);

                    //NOTE: add gap that should be avoided during retreat
                    this.addGap();
                }
            }

            // NOTE: we've hit the end of chunk, stop processing at this point
            else if (!this.lastChunkWritten)
            {
                this.endOfChunkHit = true;
                return ɑ.EOF;
            }
            return cp;
        }

        public void write(string chunk, bool isLastChunk)
        {
            if (this.html.isTruthy())
                this.html += chunk;

            else
                this.html = chunk;

            this.lastCharPos = this.html.Length - 1;
            this.endOfChunkHit = false;
            this.lastChunkWritten = isLastChunk;
        }

        public void insertHtmlAtCurrentPos(string chunk)
        {
            this.html = this.html.substring(0, this.pos + 1) +
                        chunk +
                        this.html.substring(this.pos + 1, this.html.Length);

            this.lastCharPos = this.html.Length - 1;
            this.endOfChunkHit = false;
        }

        public int advance()
        {
            this.pos++;

            if (this.pos > this.lastCharPos)
            {
                if (!this.lastChunkWritten)
                    this.endOfChunkHit = true;

                return ɑ.EOF;
            }

            var cp = this.html.charCodeAt(this.pos);

            //NOTE: any U+000A LINE FEED (LF) characters that immediately follow a U+000D CARRIAGE RETURN (CR) character
            //must be ignored.
            if (this.skipNextNewLine && cp == ɑ.LINE_FEED)
            {
                this.skipNextNewLine = false;
                this.addGap();
                return this.advance();
            }

            //NOTE: all U+000D CARRIAGE RETURN (CR) characters must be converted to U+000A LINE FEED (LF) characters
            if (cp == ɑ.CARRIAGE_RETURN)
            {
                this.skipNextNewLine = true;
                return ɑ.LINE_FEED;
            }

            this.skipNextNewLine = false;

            //OPTIMIZATION: first perform check if the code point in the allowed range that covers most common
            //HTML input (e.g. ASCII codes) to avoid performance-cost operations for high-range code points.
            return cp >= 0xD800 ? this.processHighRangeCodePoint(cp) : cp;
        }


        public void retreat()
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
