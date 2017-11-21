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
    using IDefaultTree = ITree<HtmlNode, HtmlElement, HtmlText>;

    public static class Tree
    {
        public static readonly IDefaultTree Default = DefaultTree.Instance;
    }

    sealed class DefaultTree : IDefaultTree
    {
        public static readonly DefaultTree Instance = new DefaultTree();

        static T NonNullArg<T>(T input, string paramName) where T : class =>
            input ?? throw new ArgumentNullException(paramName);

        public IEnumerable<HtmlNode> GetChildNodes(HtmlNode node) =>
            NonNullArg(node, nameof(node)).ChildNodes;

        public bool TryGetElement(HtmlNode node, out HtmlElement element)
        {
            element = NonNullArg(node, nameof(node)) as HtmlElement;
            return element != null;
        }

        public string GetTagName(HtmlElement element) =>
            NonNullArg(element, nameof(element)).TagName;

        public string GetNamespaceUri(HtmlElement element) =>
            NonNullArg(element, nameof(element)).NamespaceUri;

        public HtmlNode GetTemplateContent(HtmlElement element) =>
            ((HtmlTemplateElement) NonNullArg(element, nameof(element))).Content;

        public int GetAttributeCount(HtmlElement element) =>
            NonNullArg(element, nameof(element)).Attributes.Count;

        public int ListAttributes(HtmlElement element,
                                  (string Namespace, string Name, string Value)[] attributes,
                                  int offset, int count)
        {
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            if (offset < 0 || offset > attributes.Length) throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
            if (attributes.Length - offset < count) throw new ArgumentException("Offset and length are out of bounds for the array or count is greater than the number of elements from index to the end of the array.");

            if (count == 0)
                return 0;

            var index = offset;
            foreach (var attribute in element.Attributes)
            {
                attributes[index++] = (attribute.NamespaceUri, attribute.Name, attribute.Value);
                if (--count == 0)
                    break;
            }

            return index - offset;
        }

        public bool TryGetText(HtmlNode node, out HtmlText text)
        {
            text = NonNullArg(node, nameof(node)) as HtmlText;
            return text != null;
        }

        public string GetTextContent(HtmlText text) =>
            NonNullArg(text, nameof(text)).Value;

        public bool TryGetCommentData(HtmlNode node, out string data)
        {
            data = NonNullArg(node, nameof(node)) is HtmlComment comment ? comment.Data : null;
            return data != null;
        }

        public bool TryGetDocumentTypeName(HtmlNode node, out string name)
        {
            name = NonNullArg(node, nameof(node)) is HtmlDocumentType dt ? dt.Name : null;
            return name != null;
        }
    }
}
