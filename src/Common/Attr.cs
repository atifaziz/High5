public class Attr
{
    public string prefix;
    public string name;
    public string @namespace;
    public string value;

    public Attr(string name, string value)
    {
        this.prefix = "";
        this.name = name;
        this.@namespace = "";
        this.value = value;
    }
}