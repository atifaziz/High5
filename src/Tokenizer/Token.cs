namespace ParseFive.Tokenizer
{
    using System.Collections.Generic;

    sealed class Token
    {
        public TokenType Type { get; }
        public string TagName { get; set; }
        public List<Attr> Attrs { get; set; }
        public bool SelfClosing { get; set; }
        public string Data { get; set; }
        public string Name { get; set; }
        public bool ForceQuirks { get; set; }
        public string PublicId { get; set; }
        public string SystemId { get; set; }
        public string Chars { get; set; }

        public Token(TokenType type, string tagName, bool selfClosing, List<Attr> attrs) //START TAG
        {
            this.Type = type;
            this.TagName = tagName;
            this.SelfClosing = selfClosing;
            this.Attrs = attrs;
        }

        public Token(TokenType type, char ch)
        {
            this.Type = type;
            this.Chars = ch.ToString();
        }

        public Token(TokenType type, string tagName, List<Attr> attrs) //end tag
        {
            this.Type = type;
            this.TagName = tagName;
            this.Attrs = attrs;
        }

        public Token(TokenType type, string data) //Comment
        {
            this.Type = type;
            this.Data = data;
        }

        public Token(TokenType type, string name, bool forceQuirks, string publicId, string systemId) //Doctype
        {
            this.Type = type;
            this.Name = name;
            this.ForceQuirks = forceQuirks;
            this.PublicId = publicId;
            this.SystemId = systemId;
        }

        public Token(TokenType type) //Hibernation && EOF
        {
            this.Type = type;
        }
    }
}