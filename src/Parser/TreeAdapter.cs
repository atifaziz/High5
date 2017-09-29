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

namespace ParseFive
{
    using System.Collections.Generic;

    public interface ITreeAdapter
    {
        // Node construction

        Document CreateDocument();
        DocumentFragment CreateDocumentFragment();
        Element CreateElement(string tagName, string namespaceUri, IList<Attr> attrs);
        Comment CreateCommentNode(string data);
        Text CreateTextNode(string value);

        // Tree mutation

        void AppendChild(Node parentNode, Node newNode);
        void InsertBefore(Node parentNode, Node newNode, Node referenceNode);
        void SetTemplateContent(TemplateElement templateElement, Node contentElement);
        Node GetTemplateContent(TemplateElement templateElement);
        void SetDocumentType(Document document, string name, string publicId, string systemId);
        void SetDocumentMode(Document document, string mode);
        string GetDocumentMode(Document document);
        void DetachNode(Node node);
        void InsertText(Node parentNode, string text);
        void InsertTextBefore(Node parentNode, string text, Node referenceNode);
        void AdoptAttributes(Element recipient, IList<Attr> attrs);

        // Tree traversing

        Node GetFirstChild(Node node);
        Node GetParentNode(Node node);
        IList<Attr> GetAttrList(Element element);

        // Node data

        string GetTagName(Element element);
        string GetNamespaceUri(Element element);
    }
}
