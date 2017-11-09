#region Copyright (c) 2017 Atif Aziz
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
    using System.Collections.Generic;
    using System.Linq;
    using MoreLinq;

    public static class HNodeFactory
    {
        public static HAttribute Attribute(string name, string value) =>
            new HAttribute(name, value);

        public static IEnumerable<HAttribute> Attributes(params (string Name, string Value)[] attributes) =>
            from a in attributes select Attribute(a.Name, a.Value);

        public static HElement Html(params HNode[] children) => Element("html", children);
        public static HElement Title(string text) => Element("title", Text(text));
        public static HElement Title(IEnumerable<HAttribute> attributes, string text) => Element("title", Text(text));
        public static HElement Link(params HAttribute[] attributes) => Element("link", attributes);
        public static HElement Script(IEnumerable<HAttribute> attributes, params HNode[] children) => Element("script", attributes, children);

        public static HDocumentFragment DocumentFragment(params HNode[] children) =>
            DocumentFragment((IEnumerable<HNode>) children);

        public static HDocumentFragment DocumentFragment(IEnumerable<HNode> children) =>
            new HDocumentFragment(children);

        public static HElement Element(string tagName) =>
            Element(tagName, new HNode[] {});

        public static HElement Element(string tagName, params HNode[] children) =>
            Element(tagName, null, children);

        public static HElement Element(string tagName, params HAttribute[] attributes) =>
            Element(tagName, attributes, null);

        public static HElement Element(string tagName, IEnumerable<HAttribute> attributes, params HNode[] children)
        {
            return new HElement(tagName, null, attributes, Children());

            IEnumerable<HNode> Children()
            {
                foreach (var child in children)
                {
                    if (child is HDocumentFragment documentFragment)
                    {
                        foreach (var grandChild in documentFragment.ChildNodes)
                            yield return grandChild;
                    }
                    else
                    {
                        yield return child;
                    }
                }
            }
        }

        public static IEnumerable<HNode> MergeText(params HNode[] nodes) =>
            MergeText((IEnumerable<HNode>) nodes);

        public static IEnumerable<HNode> MergeText(IEnumerable<HNode> nodes) =>
            from g in nodes.GroupAdjacent(node => node is HText, (k, g) => new KeyValuePair<bool, IEnumerable<HNode>>(k, g))
            select g.Key
                 ? from text in new[] { Text(string.Concat(from text in g.Value.Cast<HText>() select text.Value)) }
                   where text.Value.Length > 0
                   select text
                 : g.Value
            into ns
            from node in ns
            select node;

        public static HText Text(string value) =>
            new HText(value);
    }

    public abstract class HNode
    {
        static readonly HNode[] ZeroNodes = new HNode[0];

        public IEnumerable<HNode> ChildNodes { get; }

        protected HNode(IEnumerable<HNode> children) =>
            ChildNodes = children ?? ZeroNodes;
    }

    public sealed class HAttribute
    {
        public string Name { get; }
        public string Value { get; }

        public HAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public sealed class HElement : HNode
    {
        public static readonly HAttribute[] ZeroAttributes = new HAttribute[0];

        public string TagName { get; }
        public string NamespaceUri { get; }
        public IEnumerable<HAttribute> Attributes { get; }

        public HElement(string tagName,
                       IEnumerable<HAttribute> attributes,
                       IEnumerable<HNode> children) :
            this(tagName, null, attributes, children) {}

        public HElement(string tagName, string namespaceUri,
                       IEnumerable<HAttribute> attributes,
                       IEnumerable<HNode> children) :
            base(children)
        {
            TagName = tagName;
            NamespaceUri = namespaceUri;
            Attributes = attributes ?? ZeroAttributes;
        }
    }

    public sealed class HText : HNode
    {
        public string Value { get; }

        public HText(string value) :
            base(Enumerable.Empty<HNode>()) => Value = value;
    }

    public sealed class HComment : HNode
    {
        public string Data { get; }

        public HComment(string data) :
            base(Enumerable.Empty<HNode>()) => Data = data;
    }

    public sealed class HDocument : HNode
    {
        public HDocument(IEnumerable<HNode> children) : base(children) {}
    }

    public sealed class HDocumentFragment : HNode
    {
        public HDocumentFragment(IEnumerable<HNode> children) : base(children) {}
    }
}
