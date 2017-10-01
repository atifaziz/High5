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
    using System;

    public interface ITreeAdapter<TNode,
                                  TDocument, TDocumentFragment,
                                  TElement, TAttr, TTemplateElement,
                                  TComment, TText>
        where TDocument         : TNode
        where TDocumentFragment : TNode
        where TElement          : TNode
        where TTemplateElement  : TElement
        where TComment          : TNode
        where TText             : TNode
    {
        // Node construction

        TDocument CreateDocument();
        TDocumentFragment CreateDocumentFragment();
        TElement CreateElement(string tagName, string namespaceUri, ArraySegment<TAttr> attrs);
        TAttr CreateAttribute(string ns, string prefix, string name, string value);
        TComment CreateCommentNode(string data);
        TText CreateTextNode(string value);

        // Tree mutation

        void AppendChild(TNode parentNode, TNode newNode);
        void InsertBefore(TNode parentNode, TNode newNode, TNode referenceNode);
        void SetTemplateContent(TTemplateElement templateElement, TNode contentElement);
        TNode GetTemplateContent(TTemplateElement templateElement);
        void SetDocumentType(TDocument document, string name, string publicId, string systemId);
        void SetDocumentMode(TDocument document, string mode);
        string GetDocumentMode(TDocument document);
        void DetachNode(TNode node);
        void InsertText(TNode parentNode, string text);
        void InsertTextBefore(TNode parentNode, string text, TNode referenceNode);
        void AdoptAttributes(TElement recipient, ArraySegment<TAttr> attrs);

        // Tree traversing

        TNode GetFirstChild(TNode node);
        TNode GetParentNode(TNode node);
        int GetAttrListCount(TElement element);
        int GetAttrList(TElement element, ArraySegment<TAttr> buffer);
        string GetAttrName(TAttr attr);
        string GetAttrValue(TAttr attr);

        // Node data

        string GetTagName(TElement element);
        string GetNamespaceUri(TElement element);
    }
}
