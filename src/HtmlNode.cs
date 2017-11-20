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
    using Microsoft.Extensions.Internal;

    public abstract class HtmlNode
    {
        ReadOnlyCollection<HtmlNode> _childNodes = new ReadOnlyCollection<HtmlNode>();

        public ReadOnlyCollection<HtmlNode> ChildNodes => _childNodes;

        internal void AddChildNode(HtmlNode node) => _childNodes.Add(node);
        internal void InsertChildNode(int index, HtmlNode node) => _childNodes.Insert(index, node);
        internal void RemoveChildNodeAt(int index) => _childNodes.RemoveAt(index);
    }

    public sealed class HtmlDocument : HtmlNode
    {
        public string Mode { get; internal set; }
    }

    public sealed class HtmlDocumentFragment : HtmlNode {}

    public sealed class HtmlDocumentType : HtmlNode
    {
        public string Name     { get; internal set; }
        public string PublicId { get; internal set; }
        public string SystemId { get; internal set; }

        internal HtmlDocumentType(string name, string publicId, string systemId)
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

        public void Deconstruct(out string name, out string value)
        {
            name = Name;
            value = Value;
        }

        public void Deconstruct(out string namespaceUri, out string name, out string value)
        {
            namespaceUri = NamespaceUri;
            Deconstruct(out name, out value);
        }

        public void Deconstruct(out string namespaceUri, out string prefix, out string name, out string value)
        {
            prefix = Prefix;
            Deconstruct(out namespaceUri, out name, out value);
        }
    }

    public class HtmlElement : HtmlNode
    {
        ReadOnlyCollection<HtmlAttribute> _attrs = new ReadOnlyCollection<HtmlAttribute>();

        public string TagName { get; }
        public string NamespaceUri { get; }

        public ReadOnlyCollection<HtmlAttribute> Attributes => _attrs;

        internal HtmlElement(string tagName, string namespaceUri, IEnumerable<HtmlAttribute> attributes)
        {
            TagName = tagName;
            NamespaceUri = namespaceUri;
            foreach (var attribute in attributes ?? Enumerable.Empty<HtmlAttribute>())
                _attrs.Add(attribute);
        }

        internal HtmlElement(string tagName, string namespaceUri, ArraySegment<HtmlAttribute> attributes)
        {
            TagName = tagName;
            NamespaceUri = namespaceUri;
            if (attributes.Count > 0)
            {
                for (var i = attributes.Offset; i < attributes.Offset + attributes.Count; i++)
                    _attrs.Add(attributes.Array[i]);
            }
        }

        internal void AttributesPush(HtmlAttribute attr) =>
            _attrs.Add(attr);
    }

    public sealed class HtmlTemplateElement : HtmlElement
    {
        public HtmlDocumentFragment Content { get; internal set; }

        internal HtmlTemplateElement(string namespaceUri,
                                     IEnumerable<HtmlAttribute> attributes,
                                     HtmlDocumentFragment content) :
            base(HTML.TAG_NAMES.TEMPLATE, namespaceUri, attributes) => Content = content;

        internal HtmlTemplateElement(string namespaceUri, ArraySegment<HtmlAttribute> attributes) :
            base(HTML.TAG_NAMES.TEMPLATE, namespaceUri, attributes) {}
    }

    public sealed class HtmlComment : HtmlNode
    {
        public string Data { get; }
        public HtmlComment(string data) => Data = data;
    }

    public sealed class HtmlText : HtmlNode
    {
        public string Value { get; internal set; }
        public HtmlText(string value) => Value = value;
    }

    static class HtmlNodeExtensions
    {
        static readonly HtmlDocumentType HtmlDocType = new HtmlDocumentType("html", null, null);

        public static HtmlDocument WithHtmlDoctype(this HtmlDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return document.ChildNodes.Any(n => n is HtmlDocumentType)
                 ? document
                 : HtmlNodeFactory.Document(Enumerable.Repeat(HtmlDocType, 1)
                                                      .Concat(document.ChildNodes));
        }
    }
}
