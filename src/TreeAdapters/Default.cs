using System;
using System.Collections.Generic;
using System.Text;

namespace ParseFive.TreeAdapters
{
    using System.Linq;
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
            new Element(tagName, namespaceURI, attrs);

        public Comment createCommentNode(string data) => new Comment(data);

        public Text createTextNode(string value) => new Text(value);

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
            var doctypeNode = document.ChildNodes.OfType<DocumentType>().FirstOrDefault();

            if (doctypeNode.IsTruthy())
            {
                doctypeNode.Name = name;
                doctypeNode.PublicId = publicId;
                doctypeNode.SystemId = systemId;
            }
            else
            {
                appendChild(document, new DocumentType(name, publicId, systemId));
            }
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

        //Tree traversing
        public Node getFirstChild(Node node)
        {
            return node.ChildNodes[0];
        }

        public List<Node> getChildNodes(Node node)
        {
            return node.ChildNodes;
        }

        public Node getParentNode(Node node)
        {
            return node.ParentNode;
        }

        public List<Attr> getAttrList(Element element)
        {
            return element.Attributes;
        }

        //Node data
        public string getTagName(Element element)
        {
            return element.TagName;
        }

        public string getNamespaceURI(Element element)
        {
            return element.NamespaceUri;
        }

        public string getTextNodeContent(Text textNode)
        {
            return textNode.Value;
        }

        public string getCommentNodeContent(Comment commentNode)
        {
            return commentNode.Data;
        }

        public string getDocumentTypeNodeName(DocumentType doctypeNode)
        {
            return doctypeNode.Name;
        }

        public string getDocumentTypeNodePublicId(DocumentType doctypeNode)
        {
            return doctypeNode.PublicId;
        }

        public string getDocumentTypeNodeSystemId(DocumentType doctypeNode)
        {
            return doctypeNode.SystemId;
        }

        //Node types
        public bool isTextNode(Node node)
        {
            return node is Text;
        }

        public bool isCommentNode(Node node)
        {
            return node is Comment;
        }

        public bool isDocumentTypeNode(Node node)
        {
            return node is Document;
        }

        public bool isElementNode(Node node)
        {
            return node is Element;
        }
    }
}
