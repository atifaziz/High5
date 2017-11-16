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

using System.Linq;

namespace High5
{
    using System;
    using System.Collections.Generic;
    using Extensions;
    using Microsoft.Extensions.Internal;

    public abstract class HtmlNode
    {
        static readonly IReadOnlyList<HtmlNode> ZeroNodes = new HtmlNode[0];

        List<HtmlNode> _childNodeList;

        internal List<HtmlNode> ChildNodeList => _childNodeList ?? (_childNodeList = new List<HtmlNode>());

        public IReadOnlyList<HtmlNode> ChildNodes => _childNodeList ?? ZeroNodes;
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
        List<HtmlAttribute> _attrs;

        public string TagName { get; }
        public string NamespaceUri { get; }

        static readonly IReadOnlyList<HtmlAttribute> ZeroAttrs = new HtmlAttribute[0];

        internal List<HtmlAttribute> AttributeList => _attrs ?? (_attrs = new List<HtmlAttribute>());
        public IReadOnlyList<HtmlAttribute> Attributes => _attrs ?? ZeroAttrs;

        internal HtmlElement(string tagName, string namespaceUri, IEnumerable<HtmlAttribute> attributes)
        {
            TagName = tagName;
            NamespaceUri = namespaceUri;
            _attrs = attributes?.ToList();
        }

        internal HtmlElement(string tagName, string namespaceUri, ArraySegment<HtmlAttribute> attributes)
        {
            TagName = tagName;
            NamespaceUri = namespaceUri;
            if (attributes.Count > 0)
            {
                var list = new List<HtmlAttribute>(attributes.Count);
                for (var i = attributes.Offset; i < attributes.Offset + attributes.Count; i++)
                    list.Add(attributes.Array[i]);
                _attrs = list;
            }
        }

        internal void AttributesPush(HtmlAttribute attr) =>
            AttributeList.Push(attr);
    }

    public class HtmlTemplateElement : HtmlElement
    {
        public HtmlDocumentFragment Content { get; internal set; }

        internal HtmlTemplateElement(string namespaceUri,
                                     IEnumerable<HtmlAttribute> attributes,
                                     HtmlDocumentFragment content) :
            base(HTML.TAG_NAMES.TEMPLATE, namespaceUri, attributes) => Content = content;

        internal HtmlTemplateElement(string namespaceUri, ArraySegment<HtmlAttribute> attributes) :
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
