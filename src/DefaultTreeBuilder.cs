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
    using IDefaultTreeBuilder = ITreeBuilder<HtmlNode,
                                             HtmlDocument, HtmlDocumentFragment,
                                             HtmlElement, HtmlAttribute, HtmlTemplateElement,
                                             HtmlComment>;

    public static class TreeBuilder
    {
        public static readonly IDefaultTreeBuilder Default = DefaultTreeBuilder.Instance;
    }

    sealed class DefaultTreeBuilder : IDefaultTreeBuilder
    {
        public static readonly DefaultTreeBuilder Instance = new DefaultTreeBuilder();

        public HtmlDocument CreateDocument() => new HtmlDocument();

        public HtmlDocumentFragment CreateDocumentFragment() => new HtmlDocumentFragment();

        public HtmlElement CreateElement(string tagName, string namespaceUri, ArraySegment<HtmlAttribute> attributes) =>
            tagName == "template"
            ? new HtmlTemplateElement(tagName, namespaceUri, attributes.ToList())
            : new HtmlElement(tagName, namespaceUri, attributes.ToList());

        public HtmlAttribute CreateAttribute(string ns, string prefix, string name, string value) =>
            new HtmlAttribute(ns, prefix, name, value);

        public HtmlComment CreateCommentNode(string data) => new HtmlComment(data);

        public void AppendChild(HtmlNode parentNode, HtmlNode newNode)
        {
            parentNode.ChildNodes.Add(newNode);
            newNode.ParentNode = parentNode;
        }

        public void InsertBefore(HtmlNode parentNode, HtmlNode newNode, HtmlNode referenceNode)
        {
            var i = parentNode.ChildNodes.IndexOf(referenceNode);
            parentNode.ChildNodes.Insert(i, newNode);
            newNode.ParentNode = parentNode;
        }

        public void SetTemplateContent(HtmlTemplateElement templateElement, HtmlNode contentElement) =>
            templateElement.Content = contentElement;

        public HtmlNode GetTemplateContent(HtmlTemplateElement templateElement) =>
            templateElement.Content;

        public void SetDocumentType(HtmlDocument document, string name, string publicId, string systemId)
        {
            var doctypeNode = document.ChildNodes.OfType<HtmlDocumentType>().FirstOrDefault();

            if (doctypeNode != null)
            {
                doctypeNode.Name = name;
                doctypeNode.PublicId = publicId;
                doctypeNode.SystemId = systemId;
            }
            else
            {
                AppendChild(document, new HtmlDocumentType(name, publicId, systemId));
            }
        }

        public void SetDocumentMode(HtmlDocument document, string mode) =>
            document.Mode = mode;

        public string GetDocumentMode(HtmlDocument document) =>
            document.Mode;

        public void DetachNode(HtmlNode node)
        {
            if (node.ParentNode == null)
                return;
            var i = node.ParentNode.ChildNodes.IndexOf(node);
            node.ParentNode.ChildNodes.RemoveAt(i);
            node.ParentNode = null;
        }

        static HtmlText CreateTextNode(string value) => new HtmlText(value);

        public void InsertText(HtmlNode parentNode, string text)
        {
            if (parentNode.ChildNodes.Count > 0)
            {
                if (parentNode.ChildNodes[parentNode.ChildNodes.Count - 1] is HtmlText tn)
                {
                    tn.Value += text;
                    return;
                }
            }

            AppendChild(parentNode, CreateTextNode(text));
        }

        public void InsertTextBefore(HtmlNode parentNode, string text, HtmlNode referenceNode)
        {
            var idx = parentNode.ChildNodes.IndexOf(referenceNode) - 1;
            var prevNode = 0 <= idx && idx < parentNode.ChildNodes.Count() ? parentNode.ChildNodes[idx] : null;

            if (prevNode is HtmlText textNode)
                textNode.Value += text;
            else
                InsertBefore(parentNode, CreateTextNode(text), referenceNode);
        }

        public void AdoptAttributes(HtmlElement recipient, ArraySegment<HtmlAttribute> attributes)
        {
            var recipientAttrsMap = new HashSet<string>();

            foreach (var attr in recipient.Attributes)
                recipientAttrsMap.Add(attr.Name);

            foreach (var attr in attributes)
            {
                if (!recipientAttrsMap.Contains(attr.Name))
                    recipient.AttributesPush(attr);
            }
        }

        // Tree traversing

        public HtmlNode GetFirstChild(HtmlNode node) =>
            node.ChildNodes.Any() ? node.ChildNodes[0] : null;

        public HtmlNode GetParentNode(HtmlNode node) =>
            node.ParentNode;

        public int GetAttributeCount(HtmlElement element) =>
            element.Attributes.Count;

        public int ListAttributes(HtmlElement element, ArraySegment<HtmlAttribute> attributes)
        {
            var lc = 0;
            var bi = 0;
            for (var i = attributes.Offset; bi < attributes.Count && i < Math.Min(element.Attributes.Count, attributes.Count); i++)
            {
                attributes.Array[bi++] = element.Attributes[i];
                lc++;
            }
            return lc;
        }

        public string GetAttributeName(HtmlAttribute attribute) => attribute.Name;
        public string GetAttributeValue(HtmlAttribute attribute) => attribute.Value;

        // Node data

        public string GetTagName(HtmlElement element) => element.TagName;
        public string GetNamespaceUri(HtmlElement element) => element.NamespaceUri;
    }
}
