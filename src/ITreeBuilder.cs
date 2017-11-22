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

    public interface ITreeBuilder<TNode,
                                  TDocumentFragment,
                                  TElement, TAttribute, TTemplateElement,
                                  TComment>
        where TDocumentFragment : TNode
        where TElement          : TNode
        where TTemplateElement  : TElement
        where TComment          : TNode
    {
        // Node construction

        TDocumentFragment CreateDocumentFragment();
        TElement CreateElement(string tagName, string namespaceUri, ArraySegment<TAttribute> attributes);
        TAttribute CreateAttribute(string ns, string prefix, string name, string value);
        TComment CreateCommentNode(string data);

        // Tree mutation

        void AppendChild(TNode parentNode, TNode newNode);
        void InsertBefore(TNode parentNode, TNode newNode, TNode referenceNode);
        void SetTemplateContent(TTemplateElement templateElement, TDocumentFragment content);
        TDocumentFragment GetTemplateContent(TTemplateElement templateElement);
        void DetachNode(TNode parentNode, TNode node);
        TNode InsertText(TNode parentNode, string text);
        TNode InsertTextBefore(TNode parentNode, string text, TNode referenceNode);
        void AdoptAttributes(TElement recipient, ArraySegment<TAttribute> attributes);

        // Tree traversing

        TNode GetFirstChild(TNode node);
        int GetAttributeCount(TElement element);
        int ListAttributes(TElement element, ArraySegment<TAttribute> attributes);
        string GetAttributeName(TAttribute attribute);
        string GetAttributeValue(TAttribute attribute);

        // Node data

        string GetTagName(TElement element);
        string GetNamespaceUri(TElement element);
    }

    public interface IDocumentTreeBuilder<TNode,
                                          TDocument, TDocumentFragment,
                                          TElement, TAttribute, TTemplateElement,
                                          TComment> :
        ITreeBuilder<TNode, TDocumentFragment,
                     TElement, TAttribute, TTemplateElement,
                     TComment>
        where TDocumentFragment : TNode
        where TElement          : TNode
        where TTemplateElement  : TElement
        where TComment          : TNode
    {
        TDocument CreateDocument();
        TNode SetDocumentType(TDocument document, string name, string publicId, string systemId);
        void SetDocumentMode(TDocument document, string mode);
        string GetDocumentMode(TDocument document);
    }
}
