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
        void setDocumentType(object document, string name, string publicId, string systemId);
        void setDocumentMode(Node document, string mode);
        string getDocumentMode(Node document);
        void detachNode(object node);
        void insertText(object parent, string chars);
        void insertTextBefore(object parentNode, object text, Element referenceNode);
        void adoptAttributes(object recipient, List<Attr> attrs);

        // Tree traversing

        Element getFirstChild(Element node);
        Node getChildNodes(Node node);
        Node getParentNode(Node node);
        List<Attr> getAttrList(Element element);

        // Node data

        string getTagName(Element element);
        string getNamespaceURI(Element element);
        string getTextNodeContent(Node textNode);
        string getCommentNodeContent(Node commentNode);
        string getDocumentTypeNodeName(Node doctypeNode);
        string getDocumentTypeNodePublicId(Node doctypeNode);
        string getDocumentTypeNodeSystemId(Node doctypeNode);

        // Node types

        bool isTextNode(Node node);
        bool isCommentNode(Node node);
        bool isDocumentTypeNode(Node node);
        bool isElementNode(Node node);
    }
}
