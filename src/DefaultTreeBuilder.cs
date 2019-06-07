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
    using IDefaultTreeBuilder = IDocumentTreeBuilder<HtmlNode, HtmlNode,
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
            ? new HtmlTemplateElement(namespaceUri, attributes)
            : new HtmlElement(tagName, namespaceUri, attributes);

        public HtmlAttribute CreateAttribute(string ns, string prefix, string name, string value) =>
            ns == HTML.NAMESPACES.HTML && HtmlAttributeName.CommonNames.TryGetValue(name, out var an)
            ? new HtmlAttribute(an, value)
            : new HtmlAttribute(ns, prefix, name, value);

        public HtmlComment CreateCommentNode(string data) => new HtmlComment(data);

        public void AppendChild(HtmlNode parentNode, HtmlNode newNode)
        {
            parentNode.AddChildNode(newNode);
        }

        public void InsertBefore(HtmlNode parentNode, HtmlNode newNode, HtmlNode referenceNode)
        {
            var i = parentNode.ChildNodes.IndexOf(referenceNode);
            parentNode.InsertChildNode(i, newNode);
        }

        public void SetTemplateContent(HtmlTemplateElement templateElement, HtmlDocumentFragment content) =>
            templateElement.Content = content;

        public HtmlDocumentFragment GetTemplateContent(HtmlTemplateElement templateElement) =>
            templateElement.Content;

        public HtmlNode SetDocumentType(HtmlDocument document, string name, string publicId, string systemId)
        {
            var doctypeNode = document.ChildNodes.OfType<HtmlDocumentType>().FirstOrDefault();

            if (doctypeNode != null)
            {
                doctypeNode.Name = name;
                doctypeNode.PublicId = publicId;
                doctypeNode.SystemId = systemId;
                return null;
            }
            else
            {
                var documentType = new HtmlDocumentType(name, publicId, systemId);
                AppendChild(document, documentType);
                return documentType;
            }
        }

        public void DetachNode(HtmlNode parentNode, HtmlNode node)
        {
            if (parentNode == null)
                return;
            var i = parentNode.ChildNodes.IndexOf(node);
            parentNode.RemoveChildNodeAt(i);
        }

        static HtmlText CreateTextNode(string value) => new HtmlText(value);

        public HtmlNode InsertText(HtmlNode parentNode, string text)
        {
            if (parentNode.ChildNodes.Count > 0)
            {
                if (parentNode.ChildNodes[parentNode.ChildNodes.Count - 1] is HtmlText tn)
                {
                    tn.Value += text;
                    return null;
                }
            }

            var textNode = CreateTextNode(text);
            AppendChild(parentNode, textNode);
            return textNode;
        }

        public HtmlNode InsertTextBefore(HtmlNode parentNode, string text, HtmlNode referenceNode)
        {
            var idx = parentNode.ChildNodes.IndexOf(referenceNode) - 1;
            var prevNode = 0 <= idx && idx < parentNode.ChildNodes.Count ? parentNode.ChildNodes[idx] : null;

            if (prevNode is HtmlText textNode)
            {
                textNode.Value += text;
                return null;
            }
            else
            {
                textNode = CreateTextNode(text);
                InsertBefore(parentNode, textNode, referenceNode);
                return textNode;
            }
        }

        public void AdoptAttributes(HtmlElement recipient, ArraySegment<HtmlAttribute> attributes)
        {
            var recipientAttributes = recipient.Attributes;

            HashSet<string> recipientAttrsMap = null;
            if (recipientAttributes.Count > 0)
            {
                recipientAttrsMap = new HashSet<string>();

                foreach (var attr in recipientAttributes)
                    recipientAttrsMap.Add(attr.Name);
            }

            for (var i = attributes.Offset; i < attributes.Count; i++)
            {
                var attr = attributes.Array[i];
                if (recipientAttrsMap == null || !recipientAttrsMap.Contains(attr.Name))
                    recipient.AttributesPush(attr);
            }
        }

        // Tree traversing

        public HtmlNode GetFirstChild(HtmlNode node) =>
            node.ChildNodes.Count > 0 ? node.ChildNodes[0] : null;

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
