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
    using System.Linq;
    using Microsoft.Extensions.Internal;

    public abstract class HtmlNode
    {
        ReadOnlyCollection<HtmlNode> _childNodes = new ReadOnlyCollection<HtmlNode>();

        public ReadOnlyCollection<HtmlNode> ChildNodes => _childNodes;

        public override string ToString() => this.Serialize();

        internal void AddChildNode(HtmlNode node) => _childNodes.Add(node);
        internal void InsertChildNode(int index, HtmlNode node) => _childNodes.Insert(index, node);
        internal void RemoveChildNodeAt(int index) => _childNodes.RemoveAt(index);

        public HtmlNode FirstChild => ChildNodes.Count > 0 ? ChildNodes[0] : null;
        public HtmlNode LastChild  => ChildNodes.Count > 0 ? ChildNodes[ChildNodes.Count - 1] : null;

        public IEnumerable<HtmlNode> DescendantNodes()
        {
            foreach (var child in ChildNodes)
            {
                yield return child;
                foreach (var descendant in child.DescendantNodes())
                    yield return descendant;
            }
        }

        public IEnumerable<HtmlNode> DescendantNodesAndSelf() =>
            Enumerable.Repeat(this, 1).Concat(DescendantNodes());
    }

    public static partial class HtmlNodeExtensions
    {
        public static IEnumerable<HtmlElement> Elements(this IEnumerable<HtmlNode> nodes) =>
            nodes?.OfType<HtmlElement>()
            ?? throw new ArgumentNullException(nameof(nodes));

        public static IEnumerable<HtmlElement> Elements(this HtmlNode node) =>
            node?.ChildNodes.Elements()
            ?? throw new ArgumentNullException(nameof(node));

        public static IEnumerable<HtmlElement> Descendants(this HtmlNode node) =>
            node?.DescendantNodes().Elements()
            ?? throw new ArgumentNullException(nameof(node));

        public static IEnumerable<HtmlElement> DescendantsAndSelf(this HtmlElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return Enumerable.Repeat(element, 1)
                             .Concat(element.DescendantNodes())
                             .Elements();
        }
    }

    public sealed class HtmlDocument : HtmlNode {}

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

    sealed partial class HtmlAttributeName : IEquatable<HtmlAttributeName>
    {
        public string NamespaceUri { get; }
        public string Prefix       { get; }
        public string Name         { get; }

        public HtmlAttributeName(string name) :
            this(null, null, name) {}

        public HtmlAttributeName(string namespaceUri, string prefix, string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (name.Length == 0) throw new ArgumentException(null, nameof(name));

            NamespaceUri = namespaceUri ?? HTML.NAMESPACES.HTML;
            Prefix       = prefix ?? string.Empty;
            Name         = name;
        }

        public bool Equals(HtmlAttributeName other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (!string.Equals(NamespaceUri, other.NamespaceUri))
                return false;
            var comparison
                =    NamespaceUri == HTML.NAMESPACES.HTML
                  || NamespaceUri == HTML.NAMESPACES.MATHML
                  || NamespaceUri == HTML.NAMESPACES.SVG
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            return string.Equals(Prefix, other.Prefix, comparison)
                && string.Equals(Name, other.Name, comparison);
        }

        public override bool Equals(object obj) =>
            obj is HtmlAttributeName other && Equals(other);

        public override int GetHashCode()
        {
            var hash = HashCodeCombiner.Start();
            hash.Add(NamespaceUri);
            var comparer
                =    NamespaceUri == HTML.NAMESPACES.HTML
                  || NamespaceUri == HTML.NAMESPACES.MATHML
                  || NamespaceUri == HTML.NAMESPACES.SVG
                  ? StringComparer.OrdinalIgnoreCase
                  : StringComparer.Ordinal;
            hash.Add(Prefix, comparer);
            hash.Add(Name, comparer);
            return hash.CombinedHash;
        }

        public override string ToString() =>
            Prefix.Length == 0 && NamespaceUri.Length == 0 ? Name
            : Prefix.Length > 0 ? $"{Prefix}:Name ({NamespaceUri})"
            : Name + " (" + NamespaceUri + ")";
    }

    public struct HtmlAttribute : IEquatable<HtmlAttribute>
    {
        readonly HtmlAttributeName _name;

        public string NamespaceUri => _name?.NamespaceUri;
        public string Prefix       => _name?.Prefix;
        public string Name         => _name?.Name;
        public string Value        { get; }

        internal HtmlAttribute(string name, string value) :
            this(null, null, name, value) {}

        internal HtmlAttribute(string namespaceUri, string prefix, string name, string value) :
            this(new HtmlAttributeName(namespaceUri, prefix, name), value) {}

        internal HtmlAttribute(HtmlAttributeName name, string value)
        {
            _name = name;
            Value = value;
        }

        public bool Equals(HtmlAttribute other) =>
            _name?.Equals(other._name) == true && string.Equals(Value, other.Value);

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

        HtmlElement(string tagName, string namespaceUri)
        {
            TagName = tagName;
            NamespaceUri = namespaceUri;
        }

        internal HtmlElement(string tagName, string namespaceUri, IEnumerable<HtmlAttribute> attributes) :
            this(tagName, namespaceUri)
        {
            foreach (var attribute in attributes ?? Enumerable.Empty<HtmlAttribute>())
                _attrs.Add(attribute);
        }

        internal HtmlElement(string tagName, string namespaceUri, ArraySegment<HtmlAttribute> attributes) :
            this(tagName, namespaceUri)
        {
            if (attributes.Count > 0)
            {
                for (var i = attributes.Offset; i < attributes.Offset + attributes.Count; i++)
                    _attrs.Add(attributes.Array[i]);
            }
        }

        public string OuterHtml => this.Serialize();
        public string InnerHtml => this.SerializeChildNodes();

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

        public override string ToString() => Value;
    }

    partial class HtmlNodeExtensions
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
