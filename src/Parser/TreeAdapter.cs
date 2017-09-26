namespace ParseFive
{
    using System.Collections.Generic;

    public interface ITreeAdapter
    {
        // Node construction

        Document CreateDocument();
        DocumentFragment CreateDocumentFragment();
        Element CreateElement(string tagName, string namespaceUri, List<Attr> attrs);
        Comment CreateCommentNode(string data);
        Text CreateTextNode(string value);

        // Tree mutation

        void AppendChild(Node parentNode, Node newNode);
        void InsertBefore(Node parentNode, Node newNode, Node referenceNode);
        void SetTemplateContent(Node templateElement, Node contentElement);
        Node GetTemplateContent(Node templateElement);
        void SetDocumentType(Document document, string name, string publicId, string systemId);
        void SetDocumentMode(Document document, string mode);
        string GetDocumentMode(Document document);
        void DetachNode(Node node);
        void InsertText(Node parentNode, string text);
        void InsertTextBefore(Node parentNode, string text, Node referenceNode);
        void AdoptAttributes(Element recipient, List<Attr> attrs);

        // Tree traversing

        Node GetFirstChild(Node node);
        List<Node> GetChildNodes(Node node);
        Node GetParentNode(Node node);
        List<Attr> GetAttrList(Element element);

        // Node data

        string GetTagName(Element element);
        string GetNamespaceUri(Element element);
        string GetTextNodeContent(Text textNode);
        string GetCommentNodeContent(Comment commentNode);
        string GetDocumentTypeNodeName(DocumentType doctypeNode);
        string GetDocumentTypeNodePublicId(DocumentType doctypeNode);
        string GetDocumentTypeNodeSystemId(DocumentType doctypeNode);

        // Node types

        bool IsTextNode(Node node);
        bool IsCommentNode(Node node);
        bool IsDocumentTypeNode(Node node);
        bool IsElementNode(Node node);
    }
}
