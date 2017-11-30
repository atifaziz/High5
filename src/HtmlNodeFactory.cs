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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MoreLinq;

    public static class HtmlNodeFactory
    {
        public static HtmlAttribute Attribute(string name, string value) =>
            new HtmlAttribute(name, value);

        public static IEnumerable<HtmlAttribute> Attributes(params (string Name, string Value)[] attributes) =>
            from a in attributes
            select Attribute(a.Name, a.Value);

        public static HtmlDocument Document(HtmlNode first, params HtmlNode[] children) =>
            Document(Enumerable.Repeat(first, 1).Concat(children));

        public static HtmlDocument Document(IEnumerable<HtmlNode> children) =>
            AddingChildren(new HtmlDocument(), children);

        public static HtmlDocumentFragment DocumentFragment(params HtmlNode[] children) =>
            DocumentFragment((IEnumerable<HtmlNode>) children);

        public static HtmlDocumentFragment DocumentFragment(IEnumerable<HtmlNode> children) =>
            AddingChildren(new HtmlDocumentFragment(), children);

        public static HtmlElement Element(string tagName) =>
            Element(tagName, new HtmlNode[] {});

        public static HtmlElement Element(string tagName, string text) =>
            Element(tagName, Text(text));

        public static HtmlElement Element(string tagName, IEnumerable<HtmlAttribute> attributes, string text) =>
            Element(tagName, attributes, Text(text));

        public static HtmlElement Element(string tagName, params HtmlNode[] children) =>
            Element(tagName, (IEnumerable<HtmlNode>) children);

        public static HtmlElement Element(string tagName, IEnumerable<HtmlNode> children) =>
            Element(tagName, null, children);

        public static HtmlElement Element(string tagName, params HtmlAttribute[] attributes) =>
            Element(tagName, attributes, EmptyArray<HtmlNode>.Value);

        public static HtmlElement Element(string tagName, IEnumerable<HtmlAttribute> attributes, params HtmlNode[] children) =>
            Element(tagName, attributes, (IEnumerable<HtmlNode>) children);

        public static HtmlElement Element(string tagName, IEnumerable<HtmlAttribute> attributes, IEnumerable<HtmlNode> children)
        {
            if (HTML.TAG_NAMES.TEMPLATE.Equals(tagName))
                throw new ArgumentException(nameof(tagName));

            return AddingChildren(new HtmlElement(tagName, HTML.NAMESPACES.HTML, attributes), ChildrenAndGrandChildren());

            IEnumerable<HtmlNode> ChildrenAndGrandChildren()
            {
                foreach (var child in children ?? EmptyArray<HtmlNode>.Value)
                {
                    if (child is HtmlDocumentFragment documentFragment)
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

        public static HtmlTemplateElement Template(HtmlDocumentFragment content) =>
            Template(null, content);

        public static HtmlTemplateElement Template(IEnumerable<HtmlAttribute> attributes, HtmlDocumentFragment content) =>
            new HtmlTemplateElement(HTML.NAMESPACES.HTML, attributes, content);

        public static HtmlText Text(string value) =>
            new HtmlText(value);

        public static IEnumerable<HtmlNode> MergeText(params HtmlNode[] nodes) =>
            MergeText((IEnumerable<HtmlNode>) nodes);

        public static IEnumerable<HtmlNode> MergeText(IEnumerable<HtmlNode> nodes) =>
            from g in nodes.GroupAdjacent(node => node is HtmlText, (k, g) => new KeyValuePair<bool, IEnumerable<HtmlNode>>(k, g))
            select g.Key
                 ? from text in new[] { Text(string.Concat(from text in g.Value.Cast<HtmlText>() select text.Value)) }
                   where text.Value.Length > 0
                   select text
                 : g.Value
            into ns
            from node in ns
            select node;

        public static HtmlComment Comment(string data) =>
            new HtmlComment(data);

        static T AddingChildren<T>(T node, IEnumerable<HtmlNode> children)
            where T : HtmlNode
        {
            foreach (var child in children)
                node.AddChildNode(child);
            return node;
        }
    }
}
