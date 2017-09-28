namespace ParseFive.TreeAdapters
{
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;

    static class StockTreeAdapters
    {
        public static ITreeAdapter defaultTreeAdapter = null;
    }

    public sealed class DefaultTreeAdapter : ITreeAdapter
    {
        public static ITreeAdapter Instance = new DefaultTreeAdapter();

        public Document CreateDocument() => new Document();

        public DocumentFragment CreateDocumentFragment() => new DocumentFragment();

        public Element CreateElement(string tagName, string namespaceURI, List<Attr> attrs) => tagName == "template" ? 
            new TemplateElement(tagName, namespaceURI, attrs) : new Element(tagName, namespaceURI, attrs);

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

            if (prevNode != null && IsTextNode(prevNode))
                ((Text)prevNode).Value += text;
            else
                InsertBefore(parentNode, CreateTextNode(text), referenceNode);
        }

        public void AdoptAttributes(Element recipient, List<Attr> attrs)
        {
            var recipientAttrsMap = new HashSet<string>();

            foreach (var attr in recipient.Attributes)
                recipientAttrsMap.Add(attr.Name);

            foreach (var attr in attrs) {
                if (!recipientAttrsMap.Contains(attr.Name))
                    recipient.Attributes.Push(attr);
            }
        }

        //Tree traversing
        public Node GetFirstChild(Node node)
        {
            return node.ChildNodes.Count() > 0 ? node.ChildNodes[0] : null;
        }

        public List<Node> GetChildNodes(Node node)
        {
            return node.ChildNodes;
        }

        public Node GetParentNode(Node node)
        {
            return node.ParentNode;
        }

        public List<Attr> GetAttrList(Element element)
        {
            return element.Attributes;
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

        public string GetTextNodeContent(Text textNode)
        {
            return textNode.Value;
        }

        public string GetCommentNodeContent(Comment commentNode)
        {
            return commentNode.Data;
        }

        public string GetDocumentTypeNodeName(DocumentType doctypeNode)
        {
            return doctypeNode.Name;
        }

        public string GetDocumentTypeNodePublicId(DocumentType doctypeNode)
        {
            return doctypeNode.PublicId;
        }

        public string GetDocumentTypeNodeSystemId(DocumentType doctypeNode)
        {
            return doctypeNode.SystemId;
        }

        //Node types
        public bool IsTextNode(Node node)
        {
            return node is Text;
        }

        public bool IsCommentNode(Node node)
        {
            return node is Comment;
        }

        public bool IsDocumentTypeNode(Node node)
        {
            return node is Document;
        }

        public bool IsElementNode(Node node)
        {
            return node is Element;
        }
    }
}
