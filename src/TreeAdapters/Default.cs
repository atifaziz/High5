using System;
using System.Collections.Generic;
using System.Text;

namespace ParseFive.TreeAdapters
{
    using Extensions;

    static class StockTreeAdapters
    {
        public static TreeAdapter defaultTreeAdapter = null;
    }

    public sealed class DefaultTreeAdapter : TreeAdapter
    {
        public static TreeAdapter Instance = new DefaultTreeAdapter();

        public Document createDocument() => new Document();

        public DocumentFragment createDocumentFragment() => new DocumentFragment();

        public Element createElement(string tagName, string namespaceURI, List<Attr> attrs) =>
            new Element();

        public Comment createCommentNode(string data) => new Comment();

        public Text createTextNode(string value) => new Text();

        public void appendChild(Node parentNode, Node newNode)
        {
            parentNode.ChildNodes.Add(newNode);
            newNode.ParentNode = parentNode;
        }

        public void insertBefore(Node parentNode, Element newNode, Element referenceNode)
        {
            var i = parentNode.ChildNodes.IndexOf(referenceNode);
            parentNode.ChildNodes.Insert(i, newNode);
        }

        public void setTemplateContent(object templateElement, object contentElement)
        {
            throw new NotImplementedException();
        }

        public Element getTemplateContent(Element templateElement)
        {
            throw new NotImplementedException();
        }

        public void setDocumentType(Document document, string name, string publicId, string systemId)
        {
            throw new NotImplementedException();
        }

        public void setDocumentMode(Document document, string mode) =>
            document.Mode = mode;

        public string getDocumentMode(Document document) =>
            document.Mode;

        public void detachNode(Node node)
        {
            if (node.ParentNode == null)
                return;
            var i = node.ParentNode.ChildNodes.IndexOf(node);
            node.ParentNode.ChildNodes.RemoveAt(i);
            node.ParentNode = null;
        }

        public void insertText(Node parent, string chars)
        {
            throw new NotImplementedException();
        }

        public void insertTextBefore(Node parentNode, string text, Element referenceNode)
        {
            throw new NotImplementedException();
        }

        public void adoptAttributes(Element recipient, List<Attr> attrs)
        {
            throw new NotImplementedException();
        }

        public Element getFirstChild(Node node)
        {
            throw new NotImplementedException();
        }

        public Node getChildNodes(Node node)
        {
            throw new NotImplementedException();
        }

        public Node getParentNode(Node node)
        {
            throw new NotImplementedException();
        }

        public List<Attr> getAttrList(Element element)
        {
            throw new NotImplementedException();
        }

        public string getTagName(Element element)
        {
            throw new NotImplementedException();
        }

        public string getNamespaceURI(Element element)
        {
            throw new NotImplementedException();
        }

        public string getTextNodeContent(Text textNode)
        {
            throw new NotImplementedException();
        }

        public string getCommentNodeContent(Comment commentNode)
        {
            throw new NotImplementedException();
        }

        public string getDocumentTypeNodeName(DocumentType doctypeNode)
        {
            throw new NotImplementedException();
        }

        public string getDocumentTypeNodePublicId(DocumentType doctypeNode)
        {
            throw new NotImplementedException();
        }

        public string getDocumentTypeNodeSystemId(DocumentType doctypeNode)
        {
            throw new NotImplementedException();
        }

        public bool isTextNode(Node node)
        {
            throw new NotImplementedException();
        }

        public bool isCommentNode(Node node)
        {
            throw new NotImplementedException();
        }

        public bool isDocumentTypeNode(Node node)
        {
            throw new NotImplementedException();
        }

        public bool isElementNode(Node node)
        {
            throw new NotImplementedException();
        }
    }
}
