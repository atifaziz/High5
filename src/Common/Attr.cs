public class Attr
{
    public string Prefix { get; internal set; }
    public string Name { get; internal set; }
    public string Namespace { get; internal set; }
    public string Value { get; internal set; }

    public Attr(string name, string value)
    {
        this.Prefix = "";
        this.Name = name;
        this.Namespace = "";
        this.Value = value;
    }
}