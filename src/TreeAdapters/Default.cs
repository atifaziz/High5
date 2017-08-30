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

        public Element createElement(string tagName, string namespaceURI, List<Attr> attrs) => tagName == "template" ? 
            new TemplateElement(tagName, namespaceURI, attrs) : new Element(tagName, namespaceURI, attrs);

        public Comment createCommentNode(string data) => new Comment(data);

        public Text createTextNode(string value) => new Text(value);

        public void appendChild(Node parentNode, Node newNode)
        {
            parentNode.ChildNodes.Add(newNode);
            newNode.ParentNode = parentNode;
        }

        public void insertBefore(Node parentNode, Node newNode, Node referenceNode)
        {
            var i = parentNode.ChildNodes.IndexOf(referenceNode);
            parentNode.ChildNodes.Insert(i, newNode);
        }

        public void setTemplateContent(Node templateElement, Node contentElement)
        {
            ((TemplateElement)templateElement).content = contentElement;
        }

        public Node getTemplateContent(Node templateElement)
        {
            return ((TemplateElement)templateElement).content;
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

        public void insertText(Node parentNode, string text)
        {
            if (parentNode.ChildNodes.length.IsTruthy())
            {
                if (parentNode.ChildNodes[parentNode.ChildNodes.length - 1] is Text tn)
                {
                    tn.Value += text;
                    return;
                }
            }

            appendChild(parentNode, createTextNode(text));
        }

        public void insertTextBefore(Node parentNode, string text, Node referenceNode)
        {
            var idx = parentNode.ChildNodes.IndexOf(referenceNode) - 1;
            var prevNode = 0 <= idx && idx < parentNode.ChildNodes.Count() ? parentNode.ChildNodes[idx] : null;

            if (prevNode.IsTruthy() && isTextNode(prevNode))
                ((Text)prevNode).Value += text;
            else
                insertBefore(parentNode, createTextNode(text), referenceNode);
        }

        public void adoptAttributes(Element recipient, List<Attr> attrs)
        {
            var recipientAttrsMap = new HashSet<string>();

            foreach (var attr in recipient.Attributes)
                recipientAttrsMap.Add(attr.name);

            foreach (var attr in attrs) {
                if (!recipientAttrsMap.Contains(attr.name))
                    recipient.Attributes.push(attr);
            }
        }

        //Tree traversing
        public Node getFirstChild(Node node)
        {
            return node.ChildNodes.Count() > 0 ? node.ChildNodes[0] : null;
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
