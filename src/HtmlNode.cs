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
    using Extensions;
    using Microsoft.Extensions.Internal;

    public abstract class HtmlNode
    {
        public HtmlNode ParentNode { get; internal set; }
        public IList<HtmlNode> ChildNodes { get; } = new List<HtmlNode>();

        public HtmlNode FirstChild => ChildNodes.Count > 0 ? ChildNodes[0] : null;
        public HtmlNode LastChild  => ChildNodes.Count > 0 ? ChildNodes.GetLastItem() : null;

        public HtmlNode PreviousSibling => GetSibling(-1, (i, _    ) => i >= 0   );
        public HtmlNode NextSibling     => GetSibling(+1, (i, count) => i < count);

        HtmlNode GetSibling(int offset, Func<int, int, bool> predicate)
        {
            var siblings = ParentNode?.ChildNodes;
            var index = siblings?.IndexOf(this);
            return index is int i && predicate(i + offset, siblings.Count)
                 ? siblings[i + offset]
                 : null;
        }

        public IEnumerable<HtmlNode> NodesBeforeSelf()
        {
            return _(); IEnumerable<HtmlNode> _()
            {
                var node = this;
                while ((node = node.PreviousSibling) != null)
                    yield return node;
            }
        }
    }

    public class HtmlDocument : HtmlNode
    {
        public string Mode { get; internal set; }
    }

    public class HtmlDocumentFragment : HtmlNode {}

    public class HtmlDocumentType : HtmlNode
    {
        public string Name     { get; internal set; }
        public string PublicId { get; internal set; }
        public string SystemId { get; internal set; }

        public HtmlDocumentType(string name, string publicId, string systemId)
        {
            Name = name;
            PublicId = publicId;
            SystemId = systemId;
        }
    }

    public struct HtmlAttribute : IEquatable<HtmlAttribute>
    {
        public string NamespaceUri { get; }
        public string Prefix       { get; }
        public string Name         { get; }
        public string Value        { get; }

        public HtmlAttribute(string name, string value) :
            this(null, null, name, value) {}

        public HtmlAttribute(string namespaceUri, string prefix, string name, string value)
        {
            NamespaceUri = namespaceUri;
            Prefix       = prefix;
            Name         = name;
            Value        = value;
        }

        public bool Equals(HtmlAttribute other) =>
            string.Equals(NamespaceUri, other.NamespaceUri)
            && string.Equals(Prefix, other.Prefix, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Value, other.Value);

        public override bool Equals(object obj) =>
            obj is HtmlAttribute attribute && Equals(attribute);

        public override int GetHashCode()
        {
            var hashCode = HashCodeCombiner.Start();
            hashCode.Add(NamespaceUri);
            hashCode.Add(Prefix, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Name, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(Value);
            return hashCode;
        }
    }

    public class HtmlElement : HtmlNode
    {
        IList<HtmlAttribute> _attrs;

        public string TagName { get; }
        public string NamespaceUri { get; }

        static readonly HtmlAttribute[] ZeroAttrs = new HtmlAttribute[0];

        public IList<HtmlAttribute> Attributes
        {
            get => _attrs ?? ZeroAttrs;
            private set => _attrs = value;
        }

        public HtmlElement(string tagName, string namespaceUri, IList<HtmlAttribute> attributes)
        {
            TagName = tagName;
            NamespaceUri = namespaceUri;
            Attributes = attributes;
        }

        internal void AttributesPush(HtmlAttribute attr)
        {
            // TODO remove ugly hack
            if (_attrs is null || _attrs is HtmlAttribute[] a && a.Length == 0)
                _attrs = new List<HtmlAttribute>();
            _attrs.Push(attr);
        }
    }

    public class HtmlTemplateElement : HtmlElement
    {
        public HtmlDocumentFragment Content { get; internal set; }

        public HtmlTemplateElement(string namespaceUri, IList<HtmlAttribute> attributes) :
            base(HTML.TAG_NAMES.TEMPLATE, namespaceUri, attributes) {}
    }

    public class HtmlComment : HtmlNode
    {
        public string Data { get; }
        public HtmlComment(string data) => Data = data;
    }

    public class HtmlText : HtmlNode
    {
        public string Value { get; internal set; }
        public HtmlText(string value) => Value = value;
    }
}
