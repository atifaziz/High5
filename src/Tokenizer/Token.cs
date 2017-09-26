namespace ParseFive.Tokenizer
{
    using Extensions;

    public class Token
    {
        public string type { get; set; }
        public string tagName { get; set; }
        public List<Attr> attrs { get; set; }
        public bool selfClosing { get; set; }
        public string data { get; set; }
        public string name { get; set; }
        public bool forceQuirks { get; set; }
        public string publicId { get; set; }
        public string systemId { get; set; }
        public string chars { get; set; }

        public Token(string type, string tagName, bool selfClosing, List<Attr> attrs) //START TAG
        {
            this.type = type;
            this.tagName = tagName;
            this.selfClosing = selfClosing;
            this.attrs = attrs;
        }

        public Token(string type, char ch)
        {
            this.type = type;
            this.chars = ch.ToString();
        }

        public Token(string type, string tagName, List<Attr> attrs) //end tag
        {
            this.type = type;
            this.tagName = tagName;
            this.attrs = attrs;
        }

        public Token(string type, string data) //Comment
        {
            this.type = type;
            this.data = data;
        }

        public Token(string type, string name, bool forceQuirks, string publicId, string systemId) //Doctype
        {
            this.type = type;
            this.name = name;
            this.forceQuirks = forceQuirks;
            this.publicId = publicId;
            this.systemId = systemId;
        }

        public Token(string type) //Hibernation && EOF
        {
            this.type = type;
        }
    }
}