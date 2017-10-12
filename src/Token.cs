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
        public string TagName { get; set; }
        public List<Attr> Attrs { get; set; }

        protected TagToken(TokenType type, string tagName, List<Attr> attrs) :
            base(type)
        {
            this.TagName = tagName;
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
        public string Name { get; set; }
        public bool ForceQuirks { get; set; }
        public string PublicId { get; set; }
        public string SystemId { get; set; }

        public DoctypeToken(string name,
                            bool forceQuirks = false,
                            string publicId = null,
                            string systemId = null) :
            base(DOCTYPE_TOKEN)
        {
            this.Name = name;
            this.ForceQuirks = forceQuirks;
            this.PublicId = publicId;
            this.SystemId = systemId;
        }
    }

    sealed class CommentToken : Token
    {
        public string Data { get; set; }

        public CommentToken(string data) :
            base(COMMENT_TOKEN)
        {
            this.Data = data;
        }
    }

    sealed class CharacterToken : Token
    {
        public string Chars { get; set; }

        public CharacterToken(TokenType type, char ch) :
            base(type)
        {
            Debug.Assert(type == CHARACTER_TOKEN
                      || type == WHITESPACE_CHARACTER_TOKEN
                      || type == NULL_CHARACTER_TOKEN);
            this.Chars = ch.ToString();
        }
    }
}