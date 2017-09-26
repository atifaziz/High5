using System.Collections.Generic;

public abstract class Node
{
    public Node ParentNode { get; internal set; }
    public List<Node> ChildNodes { get; } = new List<Node>();
}

public class Document : Node
{
    public string Mode { get; internal set; }
}
public class DocumentFragment : Node {}
public class Element : Node
{
    public string TagName { get; }
    public string NamespaceUri { get; }
    public List<Attr> Attributes { get; }

    public Element(string tagName, string namespaceUri, List<Attr> attributes)
    {
        TagName = tagName;
        NamespaceUri = namespaceUri;
        Attributes = attributes;
    }
}
public class Comment : Node
{
    public string Data { get; }
    public Comment(string data) => Data = data;
}
public class Text : Node
{
    public string Value { get; internal set; }
    public Text(string value) => Value = value;
}

public class DocumentType : Node
{
    public string Name     { get; internal set; }
    public string PublicId { get; internal set; }
    public string SystemId { get; internal set; }

    public DocumentType(string name, string publicId, string systemId)
    {
        Name = name;
        PublicId = publicId;
        SystemId = systemId;
    }
}

public class TemplateElement : Element
{
    public Node content;

    public TemplateElement(string tagName, string namespaceUri, List<Attr> attributes) : base(tagName, namespaceUri, attributes)
    {
    }
}
