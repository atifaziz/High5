public class Attr
{
    public string prefix { get; internal set; }
    public string name { get; internal set; }
    public string @namespace { get; internal set; }
    public string value { get; internal set; }

    public Attr(string name, string value)
    {
        this.prefix = "";
        this.name = name;
        this.@namespace = "";
        this.value = value;
    }
}