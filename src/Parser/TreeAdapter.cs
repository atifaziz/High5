namespace ParseFive
{
    using Extensions;

    public interface TreeAdapter
    {
        // Node construction

        Document createDocument();
        DocumentFragment createDocumentFragment();
        Element createElement(string tagName, string namespaceURI, List<Attr> attrs);
        Comment createCommentNode(string data);
        Text createTextNode(string value);

        // Tree mutation

        void appendChild(Node parentNode, Node newNode);
        void insertBefore(Node parentNode, Element newNode, Element referenceNode);
        void setTemplateContent(object templateElement, object contentElement);
        Element getTemplateContent(Element templateElement);
        void setDocumentType(Document document, string name, string publicId, string systemId);
        void setDocumentMode(Document document, string mode);
        string getDocumentMode(Document document);
        void detachNode(Node node);
        void insertText(Node parentNode, string text);
        void insertTextBefore(Node parentNode, string text, Element referenceNode);
        void adoptAttributes(Element recipient, List<Attr> attrs);

        // Tree traversing

        Node getFirstChild(Node node);
        List<Node> getChildNodes(Node node);
        Node getParentNode(Node node);
        List<Attr> getAttrList(Element element);

        // Node data

        string getTagName(Element element);
        string getNamespaceURI(Element element);
        string getTextNodeContent(Text textNode);
        string getCommentNodeContent(Comment commentNode);
        string getDocumentTypeNodeName(DocumentType doctypeNode);
        string getDocumentTypeNodePublicId(DocumentType doctypeNode);
        string getDocumentTypeNodeSystemId(DocumentType doctypeNode);

        // Node types

        bool isTextNode(Node node);
        bool isCommentNode(Node node);
        bool isDocumentTypeNode(Node node);
        bool isElementNode(Node node);
    }
}
