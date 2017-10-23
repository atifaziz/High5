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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using static TokenType;

    abstract class Token
    {
        public TokenType Type { get; }

        protected Token(TokenType type) =>
            this.Type = type;
    }

    sealed class EofToken : Token
    {
        public static readonly EofToken Instance = new EofToken();

        EofToken() :
            base(EOF_TOKEN) { }
    }

    sealed class HibernationToken : Token
    {
        public static readonly HibernationToken Instance = new HibernationToken();

        HibernationToken() :
            base(HIBERNATION_TOKEN) { }
    }

    abstract class TagToken : Token
    {
        readonly StringBuilder _tagName;
        string _cachedString;

        public string TagName
        {
            get => _cachedString ?? (_cachedString = _tagName.ToString());
            set { _cachedString = value; _tagName.Length = 0; _tagName.Append(value); }
        }

        public List<Attr> Attrs { get; set; }

        protected TagToken(TokenType type, string tagName, List<Attr> attrs) :
            base(type)
        {
            _tagName = new StringBuilder(tagName);
            this.Attrs = attrs;
        }

        public ArraySegment<T> CopyAttrsTo<T>(Array<T> attrs, Func<string, string, string, string, T> attrFactory)
        {
            attrs.Capacity = Attrs.Count;
            var i = 0;
            foreach (var attr in Attrs)
                attrs[i++] = attrFactory(attr.NamespaceUri, attr.Prefix, attr.Name, attr.Value);
            return attrs.ToArraySegment();
        }

        void InvalidateCache()
        {
            if (_cachedString == null)
                return;
            _cachedString = null;
        }

        public void Append(string str)
        {
            InvalidateCache();
            _tagName.Append(str);
        }

        public void Append(CodePoint cp)
        {
            InvalidateCache();
            cp.AppendTo(_tagName);
        }

        public override string ToString() => TagName;
    }

    sealed class StartTagToken : TagToken
    {
        public bool SelfClosing { get; set; }

        public StartTagToken(string tagName, bool selfClosing, List<Attr> attrs) :
            base(START_TAG_TOKEN, tagName, attrs)
        {
            this.SelfClosing = selfClosing;
        }
    }

    sealed class EndTagToken : TagToken
    {
        public EndTagToken(string tagName, List<Attr> attrs) :
            base(END_TAG_TOKEN, tagName, attrs) { }
    }

    sealed class DoctypeToken : Token
    {
        public string Name => _nameBuilder.ToString();
        public bool ForceQuirks { get; set; }
        public string PublicId => _publicIdBuilder?.ToString();
        public string SystemId => _systemIdBuilder?.ToString();

        readonly StringBuilder _nameBuilder;
        StringBuilder _publicIdBuilder;
        StringBuilder _systemIdBuilder;

        public DoctypeToken(string name,
                            bool forceQuirks = false,
                            string publicId = null,
                            string systemId = null) :
            base(DOCTYPE_TOKEN)
        {
            this._nameBuilder = new StringBuilder(name);
            this.ForceQuirks = forceQuirks;
            this._publicIdBuilder = publicId != null ? new StringBuilder(publicId) : null;
            this._systemIdBuilder = systemId != null ? new StringBuilder(systemId) : null;
        }

        StringBuilder PublicIdBuilder => _publicIdBuilder ?? (_publicIdBuilder = new StringBuilder());
        StringBuilder SystemIdBuilder => _systemIdBuilder ?? (_systemIdBuilder = new StringBuilder());

        public void AppendToName(CodePoint cp) => cp.AppendTo(_nameBuilder);
        public void ClearPublicId() => PublicIdBuilder.Length = 0;
        public void AppendToPublicId(CodePoint cp) => cp.AppendTo(PublicIdBuilder);
        public void ClearSystemId() => SystemIdBuilder.Length = 0;
        public void AppendToSystemId(CodePoint cp) => cp.AppendTo(SystemIdBuilder);
    }

    sealed class CommentToken : Token
    {
        readonly StringBuilder _data;
        string _cachedString;

        public string Data => _cachedString ?? (_cachedString = _data.ToString());

        public CommentToken(string data) :
            base(COMMENT_TOKEN) => _data = new StringBuilder(data);

        void InvalidateCache()
        {
            if (_cachedString == null)
                return;
            _cachedString = null;
        }

        public void Append(string str)
        {
            InvalidateCache();
            _data.Append(str);
        }

        public void Append(char ch)
        {
            InvalidateCache();
            _data.Append(ch);
        }

        public void Append(CodePoint cp)
        {
            InvalidateCache();
            cp.AppendTo(_data);
        }

        public override string ToString() => Data;
    }

    sealed class CharacterToken : Token
    {
        readonly StringBuilder _chars;
        string _cachedString;

        public char this[int index] => _chars[index];
        public int Length => _chars.Length;
        public string Chars => _cachedString ?? (_cachedString = _chars.ToString());

        public CharacterToken(TokenType type, CodePoint cp) :
            base(type)
        {
            Debug.Assert(type == CHARACTER_TOKEN
                      || type == WHITESPACE_CHARACTER_TOKEN
                      || type == NULL_CHARACTER_TOKEN);
            _chars = new StringBuilder();
            Append(cp);
        }

        void InvalidateCache()
        {
            if (_cachedString == null)
                return;
            _cachedString = null;
        }

        public void Append(char ch)
        {
            InvalidateCache();
            _chars.Append(ch);
        }

        public void Append(CodePoint cp)
        {
            InvalidateCache();
            cp.AppendTo(_chars);
        }

        public void Remove(int index, int length)
        {
            if (length == 0)
                return;
            InvalidateCache();
            _chars.Remove(index, length);
        }

        public void Clear()
        {
            if (_chars.Length == 0)
                return;
            InvalidateCache();
            _chars.Length = 0;
        }

        public override string ToString() => Chars;
    }
}
