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

namespace High5
{
    using System;
    using System.Text;

    /// <summary>
    /// Represents a Unicode code point.
    /// </summary>

    struct CodePoint : IEquatable<CodePoint>, IComparable<CodePoint>
    {
        readonly int _cp;

        public CodePoint(int cp) => _cp = cp;

        public void AppendTo(StringBuilder sb)
        {
            var cp = _cp;
            if (cp <= 0xFFFF)
            {
                sb.Append((char) cp);
                return;
            }

            cp -= 0x10000;
            sb.Append((char) ((int) (((uint) cp) >> 10) & 0x3FF | 0xD800))
              .Append((char) (0xDC00 | cp & 0x3FF));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        // Equality

        public bool Equals(CodePoint other) => _cp == other._cp;
        public override bool Equals(object obj) => obj is CodePoint point && Equals(point);
        public override int GetHashCode() => _cp;
        public static bool operator ==(CodePoint left, CodePoint right) => left.Equals(right);
        public static bool operator !=(CodePoint left, CodePoint right) => !left.Equals(right);

        // Comparison

        public int CompareTo(CodePoint other) => _cp.CompareTo(other._cp);
        public static bool operator <(CodePoint left, CodePoint right) => left.CompareTo(right) < 0;
        public static bool operator >(CodePoint left, CodePoint right) => left.CompareTo(right) > 0;
        public static bool operator <=(CodePoint left, CodePoint right) => left.CompareTo(right) <= 0;
        public static bool operator >=(CodePoint left, CodePoint right) => left.CompareTo(right) >= 0;

        // Conversion

        public static implicit operator CodePoint(int cp) => new CodePoint(cp);
        public static explicit operator int(CodePoint cp) => cp._cp;
    }
}
