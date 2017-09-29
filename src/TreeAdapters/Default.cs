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

namespace ParseFive.TreeAdapters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Parser;
    using Extensions;
    using Attr = System.ValueTuple<string, string, string, string>;

    public sealed class DefaultTreeAdapter : ITreeAdapter<Node, Document, DocumentFragment, Element, Attr, TemplateElement, Comment, Text>
    {
        public static DefaultTreeAdapter Instance = new DefaultTreeAdapter();

        public Parser<Node, Document, DocumentFragment, Element, Attr, TemplateElement, Comment, Text>
            CreateParser() => new Parser<Node, Document, DocumentFragment, Element, Attr, TemplateElement, Comment, Text>(this);

        public Document CreateDocument() => new Document();

        public DocumentFragment CreateDocumentFragment() => new DocumentFragment();

        public Element CreateElement(string tagName, string namespaceUri, ArraySegment<Attr> attrs) =>
            tagName == "template"
            ? new TemplateElement(tagName, namespaceUri, attrs.ToList())
            : new Element(tagName, namespaceUri, attrs.ToList());

        public Attr CreateAttribute(string ns, string prefix, string name, string value) =>
            (ns, prefix, name, value);

        public Comment CreateCommentNode(string data) => new Comment(data);

        public Text CreateTextNode(string value) => new Text(value);

        public void AppendChild(Node parentNode, Node newNode)
        {
            parentNode.ChildNodes.Add(newNode);
            newNode.ParentNode = parentNode;
        }

        public void InsertBefore(Node parentNode, Node newNode, Node referenceNode)
        {
            var i = parentNode.ChildNodes.IndexOf(referenceNode);
            parentNode.ChildNodes.Insert(i, newNode);
            newNode.ParentNode = parentNode;
        }

        public void SetTemplateContent(TemplateElement templateElement, Node contentElement)
        {
            (templateElement).Content = contentElement;
        }

        public Node GetTemplateContent(TemplateElement templateElement)
        {
            return templateElement.Content;
        }

        public void SetDocumentType(Document document, string name, string publicId, string systemId)
        {
            var doctypeNode = document.ChildNodes.OfType<DocumentType>().FirstOrDefault();

            if (doctypeNode != null)
            {
                doctypeNode.Name = name;
                doctypeNode.PublicId = publicId;
                doctypeNode.SystemId = systemId;
            }
            else
            {
                AppendChild(document, new DocumentType(name, publicId, systemId));
            }
        }

        public void SetDocumentMode(Document document, string mode) =>
            document.Mode = mode;

        public string GetDocumentMode(Document document) =>
            document.Mode;

        public void DetachNode(Node node)
        {
            if (node.ParentNode == null)
                return;
            var i = node.ParentNode.ChildNodes.IndexOf(node);
            node.ParentNode.ChildNodes.RemoveAt(i);
            node.ParentNode = null;
        }

        public void InsertText(Node parentNode, string text)
        {
            if (parentNode.ChildNodes.Count > 0)
            {
                if (parentNode.ChildNodes[parentNode.ChildNodes.Count - 1] is Text tn)
                {
                    tn.Value += text;
                    return;
                }
            }

            AppendChild(parentNode, CreateTextNode(text));
        }

        public void InsertTextBefore(Node parentNode, string text, Node referenceNode)
        {
            var idx = parentNode.ChildNodes.IndexOf(referenceNode) - 1;
            var prevNode = 0 <= idx && idx < parentNode.ChildNodes.Count() ? parentNode.ChildNodes[idx] : null;

            if (prevNode is Text textNode)
                textNode.Value += text;
            else
                InsertBefore(parentNode, CreateTextNode(text), referenceNode);
        }

        public void AdoptAttributes(Element recipient, ArraySegment<Attr> attrs)
        {
            var recipientAttrsMap = new HashSet<string>();

            foreach (var attr in recipient.Attributes)
                recipientAttrsMap.Add(attr.Name);

            foreach (var attr in attrs) {
                var (_, _, name, _) = attr;
                if (!recipientAttrsMap.Contains(name))
                    recipient.AttributesPush(attr);
            }
        }

        //Tree traversing
        public Node GetFirstChild(Node node)
        {
            return node.ChildNodes.Count() > 0 ? node.ChildNodes[0] : null;
        }

        public Node GetParentNode(Node node)
        {
            return node.ParentNode;
        }

        public int GetAttrListCount(Element element) =>
            element.Attributes.Count;


        public int GetAttrList(Element element, ArraySegment<Attr> buffer)
        {
            var cc = 0;
            var bi = 0;
            for (var i = buffer.Offset; bi < buffer.Count && i < Math.Min(element.Attributes.Count, buffer.Count); i++)
            {
                var attr = element.Attributes[i];
                buffer.Array[bi++] = (attr.Namespace, attr.Prefix, attr.Name, attr.Value);
                cc++;
            }
            return cc;
        }

        public string GetAttrName(Attr attr)
        {
            var (_, _, name, _) = attr;
            return name;
        }

        public string GetAttrValue(Attr attr)
        {
            var (_, _, _, value) = attr;
            return value;
        }

        //Node data
        public string GetTagName(Element element)
        {
            return element.TagName;
        }

        public string GetNamespaceUri(Element element)
        {
            return element.NamespaceUri;
        }
    }
}
