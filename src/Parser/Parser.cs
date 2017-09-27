namespace ParseFive.Parser
{
    using System;
    using System.Collections.Generic;
    using Common;
    using Extensions;
    using Tokenizer;
    using TreeAdapters;
    using static Tokenizer.TokenType;
    using static Tokenizer.Tokenizer.MODE;
    using static Common.ForeignContent;
    using static Common.Doctype;
    using ATTRS = Common.HTML.ATTRS;
    using NS = Common.HTML.NAMESPACES;
    using MODE = Tokenizer.Tokenizer.MODE;
    using T = Common.HTML.TAG_NAMES;
    using Tokenizer = Tokenizer.Tokenizer;
    using Unicode = Common.Unicode;

    // ReSharper disable ArrangeThisQualifier

    public sealed class Parser
    {
        //Misc constants
        const string HIDDEN_INPUT_TYPE = "hidden";

        //Adoption agency loops iteration count
        const int AA_OUTER_LOOP_ITER = 8;
        const int AA_INNER_LOOP_ITER = 3;

        //Insertion modes
        const string INITIAL_MODE = "INITIAL_MODE";
        const string BEFORE_HTML_MODE = "BEFORE_HTML_MODE";
        const string BEFORE_HEAD_MODE = "BEFORE_HEAD_MODE";
        const string IN_HEAD_MODE = "IN_HEAD_MODE";
        const string AFTER_HEAD_MODE = "AFTER_HEAD_MODE";
        const string IN_BODY_MODE = "IN_BODY_MODE";
        const string TEXT_MODE = "TEXT_MODE";
        const string IN_TABLE_MODE = "IN_TABLE_MODE";
        const string IN_TABLE_TEXT_MODE = "IN_TABLE_TEXT_MODE";
        const string IN_CAPTION_MODE = "IN_CAPTION_MODE";
        const string IN_COLUMN_GROUP_MODE = "IN_COLUMN_GROUP_MODE";
        const string IN_TABLE_BODY_MODE = "IN_TABLE_BODY_MODE";
        const string IN_ROW_MODE = "IN_ROW_MODE";
        const string IN_CELL_MODE = "IN_CELL_MODE";
        const string IN_SELECT_MODE = "IN_SELECT_MODE";
        const string IN_SELECT_IN_TABLE_MODE = "IN_SELECT_IN_TABLE_MODE";
        const string IN_TEMPLATE_MODE = "IN_TEMPLATE_MODE";
        const string AFTER_BODY_MODE = "AFTER_BODY_MODE";
        const string IN_FRAMESET_MODE = "IN_FRAMESET_MODE";
        const string AFTER_FRAMESET_MODE = "AFTER_FRAMESET_MODE";
        const string AFTER_AFTER_BODY_MODE = "AFTER_AFTER_BODY_MODE";
        const string AFTER_AFTER_FRAMESET_MODE = "AFTER_AFTER_FRAMESET_MODE";

        //Insertion mode reset map
        static readonly IDictionary<string, string> INSERTION_MODE_RESET_MAP = new Dictionary<string, string>
        {
            [T.TR] = IN_ROW_MODE,
            [T.TBODY] = IN_TABLE_BODY_MODE,
            [T.THEAD] = IN_TABLE_BODY_MODE,
            [T.TFOOT] = IN_TABLE_BODY_MODE,
            [T.CAPTION] = IN_CAPTION_MODE,
            [T.COLGROUP] = IN_COLUMN_GROUP_MODE,
            [T.TABLE] = IN_TABLE_MODE,
            [T.BODY] = IN_BODY_MODE,
            [T.FRAMESET] = IN_FRAMESET_MODE,
        };



        //Template insertion mode switch map
        static readonly IDictionary<string, string> TEMPLATE_INSERTION_MODE_SWITCH_MAP = new Dictionary<string, string>
        {
            [T.CAPTION] = IN_TABLE_MODE,
            [T.COLGROUP] = IN_TABLE_MODE,
            [T.TBODY] = IN_TABLE_MODE,
            [T.TFOOT] = IN_TABLE_MODE,
            [T.THEAD] = IN_TABLE_MODE,
            [T.COL] = IN_COLUMN_GROUP_MODE,
            [T.TR] = IN_TABLE_BODY_MODE,
            [T.TD] = IN_ROW_MODE,
            [T.TH] = IN_ROW_MODE,
        };

        //Token handlers map for insertion modes
        static readonly IDictionary<string, IDictionary<TokenType, Action<Parser, Token>>> _ =
            new Dictionary<string, IDictionary<TokenType, Action<Parser, Token>>>
            {
                [INITIAL_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = TokenInInitialMode,
                    [NULL_CHARACTER_TOKEN] = TokenInInitialMode,
                    [WHITESPACE_CHARACTER_TOKEN] = IgnoreToken,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = DoctypeInInitialMode,
                    [START_TAG_TOKEN] = TokenInInitialMode,
                    [END_TAG_TOKEN] = TokenInInitialMode,
                    [EOF_TOKEN] = TokenInInitialMode,
                },

                [BEFORE_HTML_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = TokenBeforeHtml,
                    [NULL_CHARACTER_TOKEN] = TokenBeforeHtml,
                    [WHITESPACE_CHARACTER_TOKEN] = IgnoreToken,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagBeforeHtml,
                    [END_TAG_TOKEN] = EndTagBeforeHtml,
                    [EOF_TOKEN] = TokenBeforeHtml,
                },

                [BEFORE_HEAD_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = TokenBeforeHead,
                    [NULL_CHARACTER_TOKEN] = TokenBeforeHead,
                    [WHITESPACE_CHARACTER_TOKEN] = IgnoreToken,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagBeforeHead,
                    [END_TAG_TOKEN] = EndTagBeforeHead,
                    [EOF_TOKEN] = TokenBeforeHead,
                },

                [IN_HEAD_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = TokenInHead,
                    [NULL_CHARACTER_TOKEN] = TokenInHead,
                    [WHITESPACE_CHARACTER_TOKEN] = InsertCharacters,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInHead,
                    [END_TAG_TOKEN] = EndTagInHead,
                    [EOF_TOKEN] = TokenInHead,
                },

                [AFTER_HEAD_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = TokenAfterHead,
                    [NULL_CHARACTER_TOKEN] = TokenAfterHead,
                    [WHITESPACE_CHARACTER_TOKEN] = InsertCharacters,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagAfterHead,
                    [END_TAG_TOKEN] = EndTagAfterHead,
                    [EOF_TOKEN] = TokenAfterHead,
                },

                [IN_BODY_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = CharacterInBody,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = WhitespaceCharacterInBody,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInBody,
                    [END_TAG_TOKEN] = EndTagInBody,
                    [EOF_TOKEN] = EofInBody,
                },

                [TEXT_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = InsertCharacters,
                    [NULL_CHARACTER_TOKEN] = InsertCharacters,
                    [WHITESPACE_CHARACTER_TOKEN] = InsertCharacters,
                    [COMMENT_TOKEN] = IgnoreToken,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = IgnoreToken,
                    [END_TAG_TOKEN] = EndTagInText,
                    [EOF_TOKEN] = EofInText,
                },

                [IN_TABLE_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = CharacterInTable,
                    [NULL_CHARACTER_TOKEN] = CharacterInTable,
                    [WHITESPACE_CHARACTER_TOKEN] = CharacterInTable,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInTable,
                    [END_TAG_TOKEN] = EndTagInTable,
                    [EOF_TOKEN] = EofInBody,
                },

                [IN_TABLE_TEXT_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = CharacterInTableText,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = WhitespaceCharacterInTableText,
                    [COMMENT_TOKEN] = TokenInTableText,
                    [DOCTYPE_TOKEN] = TokenInTableText,
                    [START_TAG_TOKEN] = TokenInTableText,
                    [END_TAG_TOKEN] = TokenInTableText,
                    [EOF_TOKEN] = TokenInTableText,
                },

                [IN_CAPTION_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = CharacterInBody,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = WhitespaceCharacterInBody,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInCaption,
                    [END_TAG_TOKEN] = EndTagInCaption,
                    [EOF_TOKEN] = EofInBody,
                },

                [IN_COLUMN_GROUP_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = TokenInColumnGroup,
                    [NULL_CHARACTER_TOKEN] = TokenInColumnGroup,
                    [WHITESPACE_CHARACTER_TOKEN] = InsertCharacters,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInColumnGroup,
                    [END_TAG_TOKEN] = EndTagInColumnGroup,
                    [EOF_TOKEN] = EofInBody,
                },

                [IN_TABLE_BODY_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = CharacterInTable,
                    [NULL_CHARACTER_TOKEN] = CharacterInTable,
                    [WHITESPACE_CHARACTER_TOKEN] = CharacterInTable,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInTableBody,
                    [END_TAG_TOKEN] = EndTagInTableBody,
                    [EOF_TOKEN] = EofInBody,
                },

                [IN_ROW_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = CharacterInTable,
                    [NULL_CHARACTER_TOKEN] = CharacterInTable,
                    [WHITESPACE_CHARACTER_TOKEN] = CharacterInTable,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInRow,
                    [END_TAG_TOKEN] = EndTagInRow,
                    [EOF_TOKEN] = EofInBody,
                },

                [IN_CELL_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = CharacterInBody,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = WhitespaceCharacterInBody,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInCell,
                    [END_TAG_TOKEN] = EndTagInCell,
                    [EOF_TOKEN] = EofInBody,
                },

                [IN_SELECT_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = InsertCharacters,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = InsertCharacters,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInSelect,
                    [END_TAG_TOKEN] = EndTagInSelect,
                    [EOF_TOKEN] = EofInBody,
                },

                [IN_SELECT_IN_TABLE_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = InsertCharacters,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = InsertCharacters,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInSelectInTable,
                    [END_TAG_TOKEN] = EndTagInSelectInTable,
                    [EOF_TOKEN] = EofInBody,
                },

                [IN_TEMPLATE_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = CharacterInBody,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = WhitespaceCharacterInBody,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInTemplate,
                    [END_TAG_TOKEN] = EndTagInTemplate,
                    [EOF_TOKEN] = EofInTemplate,
                },

                [AFTER_BODY_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = TokenAfterBody,
                    [NULL_CHARACTER_TOKEN] = TokenAfterBody,
                    [WHITESPACE_CHARACTER_TOKEN] = WhitespaceCharacterInBody,
                    [COMMENT_TOKEN] = AppendCommentToRootHtmlElement,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagAfterBody,
                    [END_TAG_TOKEN] = EndTagAfterBody,
                    [EOF_TOKEN] = StopParsing,
                },

                [IN_FRAMESET_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = IgnoreToken,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = InsertCharacters,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagInFrameset,
                    [END_TAG_TOKEN] = EndTagInFrameset,
                    [EOF_TOKEN] = StopParsing,
                },

                [AFTER_FRAMESET_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = IgnoreToken,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = InsertCharacters,
                    [COMMENT_TOKEN] = AppendComment,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagAfterFrameset,
                    [END_TAG_TOKEN] = EndTagAfterFrameset,
                    [EOF_TOKEN] = StopParsing,
                },

                [AFTER_AFTER_BODY_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = TokenAfterAfterBody,
                    [NULL_CHARACTER_TOKEN] = TokenAfterAfterBody,
                    [WHITESPACE_CHARACTER_TOKEN] = WhitespaceCharacterInBody,
                    [COMMENT_TOKEN] = AppendCommentToDocument,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagAfterAfterBody,
                    [END_TAG_TOKEN] = TokenAfterAfterBody,
                    [EOF_TOKEN] = StopParsing,
                },

                [AFTER_AFTER_FRAMESET_MODE] = new Dictionary<TokenType, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = IgnoreToken,
                    [NULL_CHARACTER_TOKEN] = IgnoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = WhitespaceCharacterInBody,
                    [COMMENT_TOKEN] = AppendCommentToDocument,
                    [DOCTYPE_TOKEN] = IgnoreToken,
                    [START_TAG_TOKEN] = StartTagAfterAfterFrameset,
                    [END_TAG_TOKEN] = IgnoreToken,
                    [EOF_TOKEN] = StopParsing,
                },
            };

        readonly ITreeAdapter treeAdapter;
        Element pendingScript;
        string originalInsertionMode;
        Element headElement;
        Element formElement;
        OpenElementStack openElements;
        FormattingElementList activeFormattingElements;
        List<string> tmplInsertionModeStack;
        int tmplInsertionModeStackTop;
        string currentTmplInsertionMode;
        List<Token> pendingCharacterTokens;
        bool hasNonWhitespacePendingCharacterToken;
        bool framesetOk;
        bool skipNextNewLine;
        bool fosterParentingEnabled;

        Tokenizer tokenizer;
        bool stopped;
        string insertionMode;
        Node document;
        Node fragmentContext;

        internal class Location
        {
            public Node parent;
            public Element beforeElement;

            public Location(Node parent, Element beforeElement)
            {
                this.parent = parent;
                this.beforeElement = beforeElement;
            }
        }

        //Parser
        public Parser(ITreeAdapter treeAdapter = null)
        {
            //this.options = mergeOptions(DEFAULT_OPTIONS, options);

            this.treeAdapter = treeAdapter ?? new DefaultTreeAdapter();
            this.pendingScript = null;

            //TODO check Parsermixin
            //if (this.options.locationInfo)
            //    new LocationInfoParserMixin(this);
        }
        // API
        public Document Parse(string html)
        {
            var document = this.treeAdapter.CreateDocument();

            this.Bootstrap(document, null);
            this.tokenizer.Write(html, true);
            this.RunParsingLoop(null);

            return document;
        }

        public DocumentFragment ParseFragment(string html, Node fragmentContext)
        {
            //NOTE: use <template> element as a fragment context if context element was not provided,
            //so we will parse in "forgiving" manner
            if (!fragmentContext.IsTruthy())
                fragmentContext = this.treeAdapter.CreateElement(T.TEMPLATE, NS.HTML, new List<Attr>());

            //NOTE: create fake element which will be used as 'document' for fragment parsing.
            //This is important for jsdom there 'document' can't be recreated, therefore
            //fragment parsing causes messing of the main `document`.
            var documentMock = this.treeAdapter.CreateElement("documentmock", NS.HTML, new List<Attr>());

            this.Bootstrap(documentMock, fragmentContext);

            if (this.treeAdapter.GetTagName((Element) fragmentContext) == T.TEMPLATE)
                this.PushTmplInsertionMode(IN_TEMPLATE_MODE);

            this.InitTokenizerForFragmentParsing();
            this.InsertFakeRootElement();
            this.ResetInsertionMode();
            this.FindFormInFragmentContext();
            this.tokenizer.Write(html, true);
            this.RunParsingLoop(null);

            var rootElement = (Element) this.treeAdapter.GetFirstChild(documentMock);
            var fragment = this.treeAdapter.CreateDocumentFragment();

            this.AdoptNodes(rootElement, fragment);

            return fragment;
        }

        //Bootstrap parser
        void Bootstrap(Node document, Node fragmentContext)
        {
            this.tokenizer = new Tokenizer();

            this.stopped = false;

            this.insertionMode = INITIAL_MODE;
            this.originalInsertionMode = "";

            this.document = document;
            this.fragmentContext = fragmentContext;

            this.headElement = null;
            this.formElement = null;

            this.openElements = new OpenElementStack(this.document, this.treeAdapter);
            this.activeFormattingElements = new FormattingElementList(this.treeAdapter);

            this.tmplInsertionModeStack = new List<string>();
            this.tmplInsertionModeStackTop = -1;
            this.currentTmplInsertionMode = null;

            this.pendingCharacterTokens = new List<Token>();
            this.hasNonWhitespacePendingCharacterToken = false;

            this.framesetOk = true;
            this.skipNextNewLine = false;
            this.fosterParentingEnabled = false;
        }

        //Parsing loop
        void RunParsingLoop(Action<Element> scriptHandler)
        {
            while (!this.stopped)
            {
                this.SetupTokenizerCDataMode();

                var token = this.tokenizer.GetNextToken();

                if (token.Type == HIBERNATION_TOKEN)
                    break;

                if (this.skipNextNewLine)
                {
                    this.skipNextNewLine = false;

                    if (token.Type == WHITESPACE_CHARACTER_TOKEN && token.Chars[0] == '\n')
                    {
                        if (token.Chars.Length == 1)
                            continue;

                        token.Chars = token.Chars.Substring(1);
                    }
                }

                this.ProcessInputToken(token);

                if (scriptHandler.IsTruthy() && this.pendingScript.IsTruthy())
                    break;
            }
        }

        void RunParsingLoopForCurrentChunk(Action writeCallback, Action<Element> scriptHandler)
        {
            this.RunParsingLoop(scriptHandler);

            if (scriptHandler.IsTruthy() && this.pendingScript.IsTruthy())
            {
                var script = this.pendingScript;

                this.pendingScript = null;

                scriptHandler(script);

                return;
            }

            if (writeCallback.IsTruthy())
                writeCallback();
        }

        //Text parsing
        void SetupTokenizerCDataMode()
        {
            var current = this.GetAdjustedCurrentElement();

            this.tokenizer.AllowCData = current.IsTruthy() && current != this.document &&
                                        this.treeAdapter.GetNamespaceUri((Element)current) != NS.HTML && !this.IsIntegrationPoint((Element)current);
        }

        void SwitchToTextParsing(Token currentToken, string nextTokenizerState)
        {
            this.InsertElement(currentToken, NS.HTML);
            this.tokenizer.State = nextTokenizerState;
            this.originalInsertionMode = this.insertionMode;
            this.insertionMode = TEXT_MODE;
        }

        void SwitchToPlaintextParsing()
        {
            this.insertionMode = TEXT_MODE;
            this.originalInsertionMode = IN_BODY_MODE;
            this.tokenizer.State = PLAINTEXT;
        }

        //Fragment parsing
        Node GetAdjustedCurrentElement()
        {
            return (this.openElements.StackTop == 0 && this.fragmentContext.IsTruthy() ?
                this.fragmentContext :
                this.openElements.Current);
        }

        void FindFormInFragmentContext()
        {
            var node = (Element) this.fragmentContext;

            do
            {
                if (this.treeAdapter.GetTagName(node) == T.FORM)
                {
                    this.formElement = node;
                    break;
                }

                node = (Element) this.treeAdapter.GetParentNode(node);
            } while (node.IsTruthy());
        }

        void InitTokenizerForFragmentParsing()
        {
            if (this.treeAdapter.GetNamespaceUri((Element) this.fragmentContext) == NS.HTML)
            {
                var tn = this.treeAdapter.GetTagName((Element) this.fragmentContext);

                if (tn == T.TITLE || tn == T.TEXTAREA)
                    this.tokenizer.State = RCDATA;

                else if (tn == T.STYLE || tn == T.XMP || tn == T.IFRAME ||
                         tn == T.NOEMBED || tn == T.NOFRAMES || tn == T.NOSCRIPT)
                    this.tokenizer.State = RAWTEXT;

                else if (tn == T.SCRIPT)
                    this.tokenizer.State = SCRIPT_DATA;

                else if (tn == T.PLAINTEXT)
                    this.tokenizer.State = PLAINTEXT;
            }
        }

        //Tree mutation
        void SetDocumentType(DoctypeToken token)
        {
            this.treeAdapter.SetDocumentType((Document) this.document, token.Name, token.PublicId, token.SystemId);
        }

        void AttachElementToTree(Element element)
        {
            if (this.ShouldFosterParentOnInsertion())
                this.FosterParentElement(element);

            else
            {
                var parent = this.openElements.CurrentTmplContent ?? this.openElements.Current; //|| operator

                this.treeAdapter.AppendChild(parent, element);
            }
        }

        void AppendElement(Token token, string namespaceURI)
        {
            var element = this.treeAdapter.CreateElement(token.TagName, namespaceURI, token.Attrs);

            this.AttachElementToTree(element);
        }

        void InsertElement(Token token, string namespaceURI)
        {
            var element = this.treeAdapter.CreateElement(token.TagName, namespaceURI, token.Attrs);

            this.AttachElementToTree(element);
            this.openElements.Push(element);
        }

        void InsertFakeElement(string tagName)
        {
            var element = this.treeAdapter.CreateElement(tagName, NS.HTML, new List<Attr>());

            this.AttachElementToTree(element);
            this.openElements.Push(element);
        }

        void InsertTemplate(Token token)
        {
            var tmpl = (TemplateElement) this.treeAdapter.CreateElement(token.TagName, NS.HTML, token.Attrs);
            var content = this.treeAdapter.CreateDocumentFragment();

            this.treeAdapter.SetTemplateContent(tmpl, content);
            this.AttachElementToTree(tmpl);
            this.openElements.Push(tmpl);
        }

        void InsertTemplate(Token token, string s)
        {
            InsertTemplate(token);
        }

        void InsertFakeRootElement()
        {
            var element = this.treeAdapter.CreateElement(T.HTML, NS.HTML, new List<Attr>());

            this.treeAdapter.AppendChild(this.openElements.Current, element);
            this.openElements.Push(element);
        }

        void AppendCommentNode(Token token, Node parent)
        {
            var commentNode = this.treeAdapter.CreateCommentNode(token.Data);

            this.treeAdapter.AppendChild(parent, commentNode);
        }

        void InsertCharacters(Token token)
        {
            if (this.ShouldFosterParentOnInsertion())
                this.FosterParentText(token.Chars);

            else
            {
                var parent = this.openElements.CurrentTmplContent ?? this.openElements.Current; // || operator

                this.treeAdapter.InsertText(parent, token.Chars);
            }
        }

        void AdoptNodes(Element donor, Node recipient)
        {
            while (true)
            {
                var child = this.treeAdapter.GetFirstChild(donor);

                if (!child.IsTruthy())
                    break;

                this.treeAdapter.DetachNode(child);
                this.treeAdapter.AppendChild(recipient, child);
            }
        }

        //Token processing
        bool ShouldProcessTokenInForeignContent(Token token)
        {
            var current_ = this.GetAdjustedCurrentElement();

            if (!current_.IsTruthy() || current_ == this.document)
                return false;

            var current = (Element) current_;

            var ns = this.treeAdapter.GetNamespaceUri(current);

            if (ns == NS.HTML)
                return false;

            if (this.treeAdapter.GetTagName(current) == T.ANNOTATION_XML && ns == NS.MATHML &&
                token.Type == START_TAG_TOKEN && token.TagName == T.SVG)
                return false;

            var isCharacterToken = token.Type == CHARACTER_TOKEN ||
                                   token.Type == NULL_CHARACTER_TOKEN ||
                                   token.Type == WHITESPACE_CHARACTER_TOKEN;
            var isMathMLTextStartTag = token.Type == START_TAG_TOKEN &&
                                       token.TagName != T.MGLYPH &&
                                       token.TagName != T.MALIGNMARK;

            if ((isMathMLTextStartTag || isCharacterToken) && this.IsIntegrationPoint(current, NS.MATHML))
                return false;

            if ((token.Type == START_TAG_TOKEN || isCharacterToken) && this.IsIntegrationPoint(current, NS.HTML))
                return false;

            return token.Type != EOF_TOKEN;
        }

        void ProcessToken(Token token)
        {
            _[this.insertionMode][token.Type](this, token);
        }

        void ProcessTokenInBodyMode(Token token)
        {
            _[IN_BODY_MODE][token.Type](this, token);
        }

        void ProcessTokenInForeignContent(Token token)
        {
            if (token.Type == CHARACTER_TOKEN)
                CharacterInForeignContent(this, token);

            else if (token.Type == NULL_CHARACTER_TOKEN)
                NullCharacterInForeignContent(this, token);

            else if (token.Type == WHITESPACE_CHARACTER_TOKEN)
                InsertCharacters(this, token);

            else if (token.Type == COMMENT_TOKEN)
                AppendComment(this, token);

            else if (token.Type == START_TAG_TOKEN)
                StartTagInForeignContent(this, token);

            else if (token.Type == END_TAG_TOKEN)
                EndTagInForeignContent(this, token);
        }

        void ProcessInputToken(Token token)
        {
            if (this.ShouldProcessTokenInForeignContent(token))
                this.ProcessTokenInForeignContent(token);

            else
                this.ProcessToken(token);
        }

        //Integration points
        bool IsIntegrationPoint(Element element, string foreignNS)
        {
            var tn = this.treeAdapter.GetTagName(element);
            var ns = this.treeAdapter.GetNamespaceUri(element);
            var attrs = this.treeAdapter.GetAttrList(element);

            return ForeignContent.IsIntegrationPoint(tn, ns, attrs, foreignNS);
        }

        bool IsIntegrationPoint(Element element)
        {
            return IsIntegrationPoint(element, "");
        }

        //Active formatting elements reconstruction
        void ReconstructActiveFormattingElements()
        {
            var listLength = this.activeFormattingElements.Length;

            if (listLength.IsTruthy())
            {
                var unopenIdx = listLength;
                Entry entry;

                do
                {
                    unopenIdx--;
                    entry = this.activeFormattingElements[unopenIdx];

                    if (entry.Type == FormattingElementList.MARKER_ENTRY || this.openElements.Contains(entry.Element))
                    {
                        unopenIdx++;
                        break;
                    }
                } while (unopenIdx > 0);

                for (var i = unopenIdx; i < listLength; i++)
                {
                    entry = this.activeFormattingElements[i];
                    this.InsertElement(entry.Token, this.treeAdapter.GetNamespaceUri(entry.Element));
                    entry.Element = (Element) this.openElements.Current;
                }
            }
        }

        //Close elements
        void CloseTableCell()
        {
            this.openElements.GenerateImpliedEndTags();
            this.openElements.PopUntilTableCellPopped();
            this.activeFormattingElements.ClearToLastMarker();
            this.insertionMode = IN_ROW_MODE;
        }

        void ClosePElement()
        {
            this.openElements.GenerateImpliedEndTagsWithExclusion(T.P);
            this.openElements.PopUntilTagNamePopped(T.P);
        }

        //Insertion modes
        void ResetInsertionMode()
        {
            var last = false;
            for (var i = this.openElements.StackTop; i >= 0; i--)
            {
                var element = this.openElements[i];

                if (i == 0)
                {
                    last = true;

                    if (this.fragmentContext.IsTruthy())
                        element = (Element) this.fragmentContext;
                }

                var tn = this.treeAdapter.GetTagName(element);

                if (INSERTION_MODE_RESET_MAP.TryGetValue(tn, out var newInsertionMode))
                {
                    this.insertionMode = newInsertionMode;
                    break;
                }

                else if (!last && (tn == T.TD || tn == T.TH))
                {
                    this.insertionMode = IN_CELL_MODE;
                    break;
                }

                else if (!last && tn == T.HEAD)
                {
                    this.insertionMode = IN_HEAD_MODE;
                    break;
                }

                else if (tn == T.SELECT)
                {
                    this.ResetInsertionModeForSelect(i);
                    break;
                }

                else if (tn == T.TEMPLATE)
                {
                    this.insertionMode = this.currentTmplInsertionMode;
                    break;
                }

                else if (tn == T.HTML)
                {
                    this.insertionMode = this.headElement.IsTruthy() ? AFTER_HEAD_MODE : BEFORE_HEAD_MODE;
                    break;
                }

                else if (last)
                {
                    this.insertionMode = IN_BODY_MODE;
                    break;
                }
            }
        }

        void ResetInsertionModeForSelect(int selectIdx)
        {
            if (selectIdx > 0)
            {
                for (var i = selectIdx - 1; i > 0; i--)
                {
                    var ancestor = this.openElements[i];
                    var tn = this.treeAdapter.GetTagName(ancestor);

                    if (tn == T.TEMPLATE)
                        break;

                    else if (tn == T.TABLE)
                    {
                        this.insertionMode = IN_SELECT_IN_TABLE_MODE;
                        return;
                    }
                }
            }

            this.insertionMode = IN_SELECT_MODE;
        }

        void PushTmplInsertionMode(string mode)
        {
            this.tmplInsertionModeStack.Push(mode);
            this.tmplInsertionModeStackTop++;
            this.currentTmplInsertionMode = mode;
        }

        void PopTmplInsertionMode()
        {
            this.tmplInsertionModeStack.Pop();
            this.tmplInsertionModeStackTop--;
            this.currentTmplInsertionMode = this.tmplInsertionModeStackTop >= 0 && this.tmplInsertionModeStackTop < tmplInsertionModeStack.Count
                                          ? this.tmplInsertionModeStack[this.tmplInsertionModeStackTop]
                                          : null;
        }

        //Foster parenting
        bool IsElementCausesFosterParenting(Element element)
        {
            var tn = this.treeAdapter.GetTagName(element);

            return tn == T.TABLE || tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD || tn == T.TR;
        }

        bool ShouldFosterParentOnInsertion()
        {
            return this.fosterParentingEnabled && this.IsElementCausesFosterParenting((Element) this.openElements.Current);
        }

        Location FindFosterParentingLocation()
        {
            var location = new Location(null, null);

            for (var i = this.openElements.StackTop; i >= 0; i--)
            {
                var openElement = this.openElements[i];
                var tn = this.treeAdapter.GetTagName(openElement);
                var ns = this.treeAdapter.GetNamespaceUri(openElement);

                if (tn == T.TEMPLATE && ns == NS.HTML)
                {
                    location.parent = this.treeAdapter.GetTemplateContent(openElement);
                    break;
                }

                else if (tn == T.TABLE)
                {
                    location.parent = this.treeAdapter.GetParentNode(openElement);

                    if (location.parent.IsTruthy())
                        location.beforeElement = openElement;
                    else
                        location.parent = this.openElements[i - 1];

                    break;
                }
            }

            if (!location.parent.IsTruthy())
                location.parent = this.openElements[0];

            return location;
        }

        void FosterParentElement(Element element)
        {
            var location = this.FindFosterParentingLocation();

            if (location.beforeElement.IsTruthy())
                this.treeAdapter.InsertBefore(location.parent, element, location.beforeElement);
            else
                this.treeAdapter.AppendChild(location.parent, element);
        }

        void FosterParentText(string chars)
        {
            var location = this.FindFosterParentingLocation();

            if (location.beforeElement.IsTruthy())
                this.treeAdapter.InsertTextBefore(location.parent, chars, location.beforeElement);
            else
                this.treeAdapter.InsertText(location.parent, chars);
        }

        //Special elements
        bool IsSpecialElement(Element element)
        {
            var tn = this.treeAdapter.GetTagName(element);
            var ns = this.treeAdapter.GetNamespaceUri(element);

            return HTML.SPECIAL_ELEMENTS[ns].TryGetValue(tn, out var result) ? result : false;
        }

        //Adoption agency algorithm
        //(see: http://www.whatwg.org/specs/web-apps/current-work/multipage/tree-construction.html#adoptionAgency)
        //------------------------------------------------------------------

        //Steps 5-8 of the algorithm
        static Entry AaObtainFormattingElementEntry(Parser p, Token token)
        {
            var formattingElementEntry = p.activeFormattingElements.GetElementEntryInScopeWithTagName(token.TagName);

            if (formattingElementEntry.IsTruthy())
            {
                if (!p.openElements.Contains(formattingElementEntry.Element))
                {
                    p.activeFormattingElements.RemoveEntry(formattingElementEntry);
                    formattingElementEntry = null;
                }

                else if (!p.openElements.HasInScope(token.TagName))
                    formattingElementEntry = null;
            }

            else
                GenericEndTagInBody(p, token);

            return formattingElementEntry;
        }

        static Entry AaObtainFormattingElementEntry(Parser p, Token token, Entry formattingElementEntry)
        {
            return AaObtainFormattingElementEntry(p, token);
        }

        //Steps 9 and 10 of the algorithm
        static Element AaObtainFurthestBlock(Parser p, Entry formattingElementEntry)
        {
            Element furthestBlock = null;

            for (var i = p.openElements.StackTop; i >= 0; i--)
            {
                var element = p.openElements[i];

                if (element == formattingElementEntry.Element)
                    break;

                if (p.IsSpecialElement(element))
                    furthestBlock = element;
            }

            if (!furthestBlock.IsTruthy())
            {
                p.openElements.PopUntilElementPopped(formattingElementEntry.Element);
                p.activeFormattingElements.RemoveEntry(formattingElementEntry);
            }

            return furthestBlock;
        }

        //Step 13 of the algorithm
        static Element AaInnerLoop(Parser p, Element furthestBlock, Element formattingElement)
        {
            var lastElement = furthestBlock;
            var nextElement = p.openElements.GetCommonAncestor(furthestBlock);
            var element = nextElement;

            //for (var i = 0, element = nextElement; element != formattingElement; i++, element = nextElement)
            for (var i = 0; element != formattingElement; i++)
            {
                //NOTE: store next element for the next loop iteration (it may be deleted from the stack by step 9.5)
                nextElement = p.openElements.GetCommonAncestor(element);

                var elementEntry = p.activeFormattingElements.GetElementEntry(element);
                var counterOverflow = elementEntry.IsTruthy() && i >= AA_INNER_LOOP_ITER;
                var shouldRemoveFromOpenElements = !elementEntry.IsTruthy() || counterOverflow;

                if (shouldRemoveFromOpenElements)
                {
                    if (counterOverflow.IsTruthy())
                        p.activeFormattingElements.RemoveEntry(elementEntry);

                    p.openElements.Remove(element);
                }

                else
                {
                    element = AaRecreateElementFromEntry(p, elementEntry);

                    if (lastElement == furthestBlock)
                        p.activeFormattingElements.Bookmark = elementEntry;

                    p.treeAdapter.DetachNode(lastElement);
                    p.treeAdapter.AppendChild(element, lastElement);
                    lastElement = element;
                }
                element = nextElement;
            }

            return lastElement;
        }

        //Step 13.7 of the algorithm
        static Element AaRecreateElementFromEntry(Parser p, Entry elementEntry)
        {
            var ns = p.treeAdapter.GetNamespaceUri(elementEntry.Element);
            var newElement = p.treeAdapter.CreateElement(elementEntry.Token.TagName, ns, elementEntry.Token.Attrs);

            p.openElements.Replace(elementEntry.Element, newElement);
            elementEntry.Element = newElement;

            return newElement;
        }

        //Step 14 of the algorithm
        static void AaInsertLastNodeInCommonAncestor(Parser p, Element commonAncestor, Element lastElement)
        {
            if (p.IsElementCausesFosterParenting(commonAncestor))
                p.FosterParentElement(lastElement);

            else
            {
                Node commonAncestorNode = commonAncestor;
                var tn = p.treeAdapter.GetTagName(commonAncestor);
                var ns = p.treeAdapter.GetNamespaceUri(commonAncestor);

                if (tn == T.TEMPLATE && ns == NS.HTML)
                    commonAncestorNode = p.treeAdapter.GetTemplateContent(commonAncestor);

                p.treeAdapter.AppendChild(commonAncestorNode, lastElement);
            }
        }

        //Steps 15-19 of the algorithm
        static void AaReplaceFormattingElement(Parser p, Element furthestBlock, Entry formattingElementEntry)
        {
            var ns = p.treeAdapter.GetNamespaceUri(formattingElementEntry.Element);
            var token = formattingElementEntry.Token;
            var newElement = p.treeAdapter.CreateElement(token.TagName, ns, token.Attrs);

            p.AdoptNodes(furthestBlock, newElement);
            p.treeAdapter.AppendChild(furthestBlock, newElement);

            p.activeFormattingElements.InsertElementAfterBookmark(newElement, formattingElementEntry.Token);
            p.activeFormattingElements.RemoveEntry(formattingElementEntry);

            p.openElements.Remove(formattingElementEntry.Element);
            p.openElements.InsertAfter(furthestBlock, newElement);
        }

        //Algorithm entry point
        static void CallAdoptionAgency(Parser p, Token token)
        {
            Entry formattingElementEntry = null;

            for (var i = 0; i < AA_OUTER_LOOP_ITER; i++)
            {
                formattingElementEntry = AaObtainFormattingElementEntry(p, token, formattingElementEntry);

                if (!formattingElementEntry.IsTruthy())
                    break;

                var furthestBlock = AaObtainFurthestBlock(p, formattingElementEntry);

                if (!furthestBlock.IsTruthy())
                    break;

                p.activeFormattingElements.Bookmark = formattingElementEntry;

                var lastElement = AaInnerLoop(p, furthestBlock, formattingElementEntry.Element);
                var commonAncestor = p.openElements.GetCommonAncestor(formattingElementEntry.Element);

                p.treeAdapter.DetachNode(lastElement);
                AaInsertLastNodeInCommonAncestor(p, commonAncestor, lastElement);
                AaReplaceFormattingElement(p, furthestBlock, formattingElementEntry);
            }
        }


        //Generic token handlers
        //------------------------------------------------------------------
        static void IgnoreToken(Parser p, Token token)
        {
            //NOTE: do nothing =)
        }

        static void AppendComment(Parser p, Token token)
        {
            p.AppendCommentNode(token, p.openElements.CurrentTmplContent ?? p.openElements.Current); //|| operator
        }

        static void AppendCommentToRootHtmlElement(Parser p, Token token)
        {
            p.AppendCommentNode(token, p.openElements[0]);
        }

        static void AppendCommentToDocument(Parser p, Token token)
        {
            p.AppendCommentNode(token, p.document);
        }

        static void InsertCharacters(Parser p, Token token)
        {
            p.InsertCharacters(token);
        }

        static void StopParsing(Parser p, Token token)
        {
            p.stopped = true;
        }

        //12.2.5.4.1 The "initial" insertion mode
        //------------------------------------------------------------------
        static void DoctypeInInitialMode(Parser p, Token tokenObject)
        {
            var token = (DoctypeToken) tokenObject;
            p.SetDocumentType(token);

            var mode = token.ForceQuirks ?
                HTML.DOCUMENT_MODE.QUIRKS :
                GetDocumentMode(token.Name, token.PublicId, token.SystemId);

            p.treeAdapter.SetDocumentMode((Document) p.document, mode);

            p.insertionMode = BEFORE_HTML_MODE;
        }

        static void TokenInInitialMode(Parser p, Token token)
        {
            p.treeAdapter.SetDocumentMode((Document) p.document, HTML.DOCUMENT_MODE.QUIRKS);
            p.insertionMode = BEFORE_HTML_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.2 The "before html" insertion mode
        //------------------------------------------------------------------
        static void StartTagBeforeHtml(Parser p, Token token)
        {
            if (token.TagName == T.HTML)
            {
                p.InsertElement(token, NS.HTML);
                p.insertionMode = BEFORE_HEAD_MODE;
            }

            else
                TokenBeforeHtml(p, token);
        }

        static void EndTagBeforeHtml(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML || tn == T.HEAD || tn == T.BODY || tn == T.BR)
                TokenBeforeHtml(p, token);
        }

        static void TokenBeforeHtml(Parser p, Token token)
        {
            p.InsertFakeRootElement();
            p.insertionMode = BEFORE_HEAD_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.3 The "before head" insertion mode
        //------------------------------------------------------------------
        static void StartTagBeforeHead(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.HEAD)
            {
                p.InsertElement(token, NS.HTML);
                p.headElement = (Element) p.openElements.Current;
                p.insertionMode = IN_HEAD_MODE;
            }

            else
                TokenBeforeHead(p, token);
        }

        static void EndTagBeforeHead(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HEAD || tn == T.BODY || tn == T.HTML || tn == T.BR)
                TokenBeforeHead(p, token);
        }

        static void TokenBeforeHead(Parser p, Token token)
        {
            p.InsertFakeElement(T.HEAD);
            p.headElement = (Element) p.openElements.Current;
            p.insertionMode = IN_HEAD_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.4 The "in head" insertion mode
        //------------------------------------------------------------------
        static void StartTagInHead(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.BASE || tn == T.BASEFONT || tn == T.BGSOUND || tn == T.LINK || tn == T.META)
                p.AppendElement(token, NS.HTML);

            else if (tn == T.TITLE)
                p.SwitchToTextParsing(token, MODE.RCDATA);

            //NOTE: here we assume that we always act as an interactive user agent with enabled scripting, so we parse
            //<noscript> as a rawtext.
            else if (tn == T.NOSCRIPT || tn == T.NOFRAMES || tn == T.STYLE)
                p.SwitchToTextParsing(token, MODE.RAWTEXT);

            else if (tn == T.SCRIPT)
                p.SwitchToTextParsing(token, MODE.SCRIPT_DATA);

            else if (tn == T.TEMPLATE)
            {
                p.InsertTemplate(token, NS.HTML);
                p.activeFormattingElements.InsertMarker();
                p.framesetOk = false;
                p.insertionMode = IN_TEMPLATE_MODE;
                p.PushTmplInsertionMode(IN_TEMPLATE_MODE);
            }

            else if (tn != T.HEAD)
                TokenInHead(p, token);
        }

        static void EndTagInHead(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HEAD)
            {
                p.openElements.Pop();
                p.insertionMode = AFTER_HEAD_MODE;
            }

            else if (tn == T.BODY || tn == T.BR || tn == T.HTML)
                TokenInHead(p, token);

            else if (tn == T.TEMPLATE && p.openElements.TmplCount > 0)
            {
                p.openElements.GenerateImpliedEndTags();
                p.openElements.PopUntilTagNamePopped(T.TEMPLATE);
                p.activeFormattingElements.ClearToLastMarker();
                p.PopTmplInsertionMode();
                p.ResetInsertionMode();
            }
        }

        static void TokenInHead(Parser p, Token token)
        {
            p.openElements.Pop();
            p.insertionMode = AFTER_HEAD_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.6 The "after head" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterHead(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.BODY)
            {
                p.InsertElement(token, NS.HTML);
                p.framesetOk = false;
                p.insertionMode = IN_BODY_MODE;
            }

            else if (tn == T.FRAMESET)
            {
                p.InsertElement(token, NS.HTML);
                p.insertionMode = IN_FRAMESET_MODE;
            }

            else if (tn == T.BASE || tn == T.BASEFONT || tn == T.BGSOUND || tn == T.LINK || tn == T.META ||
                     tn == T.NOFRAMES || tn == T.SCRIPT || tn == T.STYLE || tn == T.TEMPLATE || tn == T.TITLE)
            {
                p.openElements.Push(p.headElement);
                StartTagInHead(p, token);
                p.openElements.Remove(p.headElement);
            }

            else if (tn != T.HEAD)
                TokenAfterHead(p, token);
        }

        static void EndTagAfterHead(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.BODY || tn == T.HTML || tn == T.BR)
                TokenAfterHead(p, token);

            else if (tn == T.TEMPLATE)
                EndTagInHead(p, token);
        }

        static void TokenAfterHead(Parser p, Token token)
        {
            p.InsertFakeElement(T.BODY);
            p.insertionMode = IN_BODY_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.7 The "in body" insertion mode
        //------------------------------------------------------------------
        static void WhitespaceCharacterInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertCharacters(token);
        }

        static void CharacterInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertCharacters(token);
            p.framesetOk = false;
        }

        static void HtmlStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.TmplCount == 0)
                p.treeAdapter.AdoptAttributes(p.openElements[0], token.Attrs);
        }

        static void BodyStartTagInBody(Parser p, Token token)
        {
            var bodyElement = p.openElements.TryPeekProperlyNestedBodyElement();

            if (bodyElement.IsTruthy() && p.openElements.TmplCount == 0)
            {
                p.framesetOk = false;
                p.treeAdapter.AdoptAttributes(bodyElement, token.Attrs);
            }
        }

        static void FramesetStartTagInBody(Parser p, Token token)
        {
            var bodyElement = p.openElements.TryPeekProperlyNestedBodyElement();

            if (p.framesetOk && bodyElement.IsTruthy())
            {
                p.treeAdapter.DetachNode(bodyElement);
                p.openElements.PopAllUpToHtmlElement();
                p.InsertElement(token, NS.HTML);
                p.insertionMode = IN_FRAMESET_MODE;
            }
        }

        static void AddressStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
        }

        static void NumberedHeaderStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            var tn = p.openElements.CurrentTagName;

            if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6)
                p.openElements.Pop();

            p.InsertElement(token, NS.HTML);
        }

        static void PreStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
            //NOTE: If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move
            //on to the next one. (Newlines at the start of pre blocks are ignored as an authoring convenience.)
            p.skipNextNewLine = true;
            p.framesetOk = false;
        }

        static void FormStartTagInBody(Parser p, Token token)
        {
            var inTemplate = p.openElements.TmplCount > 0;

            if (!p.formElement.IsTruthy() || inTemplate)
            {
                if (p.openElements.HasInButtonScope(T.P))
                    p.ClosePElement();

                p.InsertElement(token, NS.HTML);

                if (!inTemplate)
                    p.formElement = (Element) p.openElements.Current;
            }
        }

        static void ListItemStartTagInBody(Parser p, Token token)
        {
            p.framesetOk = false;

            var tn = token.TagName;

            for (var i = p.openElements.StackTop; i >= 0; i--)
            {
                var element = p.openElements[i];
                var elementTn = p.treeAdapter.GetTagName(element);
                string closeTn = null;

                if (tn == T.LI && elementTn == T.LI)
                    closeTn = T.LI;

                else if ((tn == T.DD || tn == T.DT) && (elementTn == T.DD || elementTn == T.DT))
                    closeTn = elementTn;

                if (closeTn.IsTruthy())
                {
                    p.openElements.GenerateImpliedEndTagsWithExclusion(closeTn);
                    p.openElements.PopUntilTagNamePopped(closeTn);
                    break;
                }

                if (elementTn != T.ADDRESS && elementTn != T.DIV && elementTn != T.P && p.IsSpecialElement(element))
                    break;
            }

            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
        }

        static void PlaintextStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
            p.tokenizer.State = MODE.PLAINTEXT;
        }

        static void ButtonStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInScope(T.BUTTON))
            {
                p.openElements.GenerateImpliedEndTags();
                p.openElements.PopUntilTagNamePopped(T.BUTTON);
            }

            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
            p.framesetOk = false;
        }

        static void AStartTagInBody(Parser p, Token token)
        {
            var activeElementEntry = p.activeFormattingElements.GetElementEntryInScopeWithTagName(T.A);

            if (activeElementEntry.IsTruthy())
            {
                CallAdoptionAgency(p, token);
                p.openElements.Remove(activeElementEntry.Element);
                p.activeFormattingElements.RemoveEntry(activeElementEntry);
            }

            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
            p.activeFormattingElements.PushElement((Element) p.openElements.Current, token);
        }

        static void BStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
            p.activeFormattingElements.PushElement((Element) p.openElements.Current, token);
        }

        static void NobrStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();

            if (p.openElements.HasInScope(T.NOBR))
            {
                CallAdoptionAgency(p, token);
                p.ReconstructActiveFormattingElements();
            }

            p.InsertElement(token, NS.HTML);
            p.activeFormattingElements.PushElement((Element) p.openElements.Current, token);
        }

        static void AppletStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
            p.activeFormattingElements.InsertMarker();
            p.framesetOk = false;
        }

        static void TableStartTagInBody(Parser p, Token token)
        {
            var mode = p.document is Document doc ? p.treeAdapter.GetDocumentMode(doc) : null;
            if (mode != HTML.DOCUMENT_MODE.QUIRKS && p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
            p.framesetOk = false;
            p.insertionMode = IN_TABLE_MODE;
        }

        static void AreaStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.AppendElement(token, NS.HTML);
            p.framesetOk = false;
        }

        static void InputStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.AppendElement(token, NS.HTML);

            var inputType = Tokenizer.GetTokenAttr(token, ATTRS.TYPE);

            if (!inputType.IsTruthy() || inputType.ToLowerCase() != HIDDEN_INPUT_TYPE)
                p.framesetOk = false;

        }

        static void ParamStartTagInBody(Parser p, Token token)
        {
            p.AppendElement(token, NS.HTML);
        }

        static void HrStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            if (p.openElements.CurrentTagName == T.MENUITEM)
                p.openElements.Pop();

            p.AppendElement(token, NS.HTML);
            p.framesetOk = false;
        }

        static void ImageStartTagInBody(Parser p, Token token)
        {
            token.TagName = T.IMG;
            AreaStartTagInBody(p, token);
        }

        static void TextareaStartTagInBody(Parser p, Token token)
        {
            p.InsertElement(token, NS.HTML);
            //NOTE: If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move
            //on to the next one. (Newlines at the start of textarea elements are ignored as an authoring convenience.)
            p.skipNextNewLine = true;
            p.tokenizer.State = MODE.RCDATA;
            p.originalInsertionMode = p.insertionMode;
            p.framesetOk = false;
            p.insertionMode = TEXT_MODE;
        }

        static void XmpStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.ReconstructActiveFormattingElements();
            p.framesetOk = false;
            p.SwitchToTextParsing(token, MODE.RAWTEXT);
        }

        static void IframeStartTagInBody(Parser p, Token token)
        {
            p.framesetOk = false;
            p.SwitchToTextParsing(token, MODE.RAWTEXT);
        }

        //NOTE: here we assume that we always act as an user agent with enabled plugins, so we parse
        //<noembed> as a rawtext.
        static void NoembedStartTagInBody(Parser p, Token token)
        {
            p.SwitchToTextParsing(token, MODE.RAWTEXT);
        }

        static void SelectStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
            p.framesetOk = false;

            if (p.insertionMode == IN_TABLE_MODE ||
                p.insertionMode == IN_CAPTION_MODE ||
                p.insertionMode == IN_TABLE_BODY_MODE ||
                p.insertionMode == IN_ROW_MODE ||
                p.insertionMode == IN_CELL_MODE)

                p.insertionMode = IN_SELECT_IN_TABLE_MODE;

            else
                p.insertionMode = IN_SELECT_MODE;
        }

        static void OptgroupStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.CurrentTagName == T.OPTION)
                p.openElements.Pop();

            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
        }

        static void RbStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInScope(T.RUBY))
                p.openElements.GenerateImpliedEndTags();

            p.InsertElement(token, NS.HTML);
        }

        static void RtStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInScope(T.RUBY))
                p.openElements.GenerateImpliedEndTagsWithExclusion(T.RTC);

            p.InsertElement(token, NS.HTML);
        }

        static void MenuitemStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.CurrentTagName == T.MENUITEM)
                p.openElements.Pop();

            // TODO needs clarification, see https://github.com/whatwg/html/pull/907/files#r73505877
            p.ReconstructActiveFormattingElements();

            p.InsertElement(token, NS.HTML);
        }

        static void MenuStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            if (p.openElements.CurrentTagName == T.MENUITEM)
                p.openElements.Pop();

            p.InsertElement(token, NS.HTML);
        }

        static void MathStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();

            AdjustTokenMathMlAttrs(token);
            AdjustTokenXmlAttrs(token);

            if (token.SelfClosing)
                p.AppendElement(token, NS.MATHML);
            else
                p.InsertElement(token, NS.MATHML);
        }

        static void SvgStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();

            AdjustTokenSvgAttrs(token);
            AdjustTokenXmlAttrs(token);

            if (token.SelfClosing)
                p.AppendElement(token, NS.SVG);
            else
                p.InsertElement(token, NS.SVG);
        }

        static void GenericStartTagInBody(Parser p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
        }

        //OPTIMIZATION: Integer comparisons are low-cost, so we can use very fast tag name.Length filters here.
        //It's faster than using dictionary.
        static void StartTagInBody(Parser p, Token token)
        {
            var tn = token.TagName;

            switch (tn.Length)
            {
                case 1:
                    if (tn == T.I || tn == T.S || tn == T.B || tn == T.U)
                        BStartTagInBody(p, token);

                    else if (tn == T.P)
                        AddressStartTagInBody(p, token);

                    else if (tn == T.A)
                        AStartTagInBody(p, token);

                    else
                        GenericStartTagInBody(p, token);

                    break;

                case 2:
                    if (tn == T.DL || tn == T.OL || tn == T.UL)
                        AddressStartTagInBody(p, token);

                    else if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6)
                        NumberedHeaderStartTagInBody(p, token);

                    else if (tn == T.LI || tn == T.DD || tn == T.DT)
                        ListItemStartTagInBody(p, token);

                    else if (tn == T.EM || tn == T.TT)
                        BStartTagInBody(p, token);

                    else if (tn == T.BR)
                        AreaStartTagInBody(p, token);

                    else if (tn == T.HR)
                        HrStartTagInBody(p, token);

                    else if (tn == T.RB)
                        RbStartTagInBody(p, token);

                    else if (tn == T.RT || tn == T.RP)
                        RtStartTagInBody(p, token);

                    else if (tn != T.TH && tn != T.TD && tn != T.TR)
                        GenericStartTagInBody(p, token);

                    break;

                case 3:
                    if (tn == T.DIV || tn == T.DIR || tn == T.NAV)
                        AddressStartTagInBody(p, token);

                    else if (tn == T.PRE)
                        PreStartTagInBody(p, token);

                    else if (tn == T.BIG)
                        BStartTagInBody(p, token);

                    else if (tn == T.IMG || tn == T.WBR)
                        AreaStartTagInBody(p, token);

                    else if (tn == T.XMP)
                        XmpStartTagInBody(p, token);

                    else if (tn == T.SVG)
                        SvgStartTagInBody(p, token);

                    else if (tn == T.RTC)
                        RbStartTagInBody(p, token);

                    else if (tn != T.COL)
                        GenericStartTagInBody(p, token);

                    break;

                case 4:
                    if (tn == T.HTML)
                        HtmlStartTagInBody(p, token);

                    else if (tn == T.BASE || tn == T.LINK || tn == T.META)
                        StartTagInHead(p, token);

                    else if (tn == T.BODY)
                        BodyStartTagInBody(p, token);

                    else if (tn == T.MAIN)
                        AddressStartTagInBody(p, token);

                    else if (tn == T.FORM)
                        FormStartTagInBody(p, token);

                    else if (tn == T.CODE || tn == T.FONT)
                        BStartTagInBody(p, token);

                    else if (tn == T.NOBR)
                        NobrStartTagInBody(p, token);

                    else if (tn == T.AREA)
                        AreaStartTagInBody(p, token);

                    else if (tn == T.MATH)
                        MathStartTagInBody(p, token);

                    else if (tn == T.MENU)
                        MenuStartTagInBody(p, token);

                    else if (tn != T.HEAD)
                        GenericStartTagInBody(p, token);

                    break;

                case 5:
                    if (tn == T.STYLE || tn == T.TITLE)
                        StartTagInHead(p, token);

                    else if (tn == T.ASIDE)
                        AddressStartTagInBody(p, token);

                    else if (tn == T.SMALL)
                        BStartTagInBody(p, token);

                    else if (tn == T.TABLE)
                        TableStartTagInBody(p, token);

                    else if (tn == T.EMBED)
                        AreaStartTagInBody(p, token);

                    else if (tn == T.INPUT)
                        InputStartTagInBody(p, token);

                    else if (tn == T.PARAM || tn == T.TRACK)
                        ParamStartTagInBody(p, token);

                    else if (tn == T.IMAGE)
                        ImageStartTagInBody(p, token);

                    else if (tn != T.FRAME && tn != T.TBODY && tn != T.TFOOT && tn != T.THEAD)
                        GenericStartTagInBody(p, token);

                    break;

                case 6:
                    if (tn == T.SCRIPT)
                        StartTagInHead(p, token);

                    else if (tn == T.CENTER || tn == T.FIGURE || tn == T.FOOTER || tn == T.HEADER || tn == T.HGROUP)
                        AddressStartTagInBody(p, token);

                    else if (tn == T.BUTTON)
                        ButtonStartTagInBody(p, token);

                    else if (tn == T.STRIKE || tn == T.STRONG)
                        BStartTagInBody(p, token);

                    else if (tn == T.APPLET || tn == T.OBJECT)
                        AppletStartTagInBody(p, token);

                    else if (tn == T.KEYGEN)
                        AreaStartTagInBody(p, token);

                    else if (tn == T.SOURCE)
                        ParamStartTagInBody(p, token);

                    else if (tn == T.IFRAME)
                        IframeStartTagInBody(p, token);

                    else if (tn == T.SELECT)
                        SelectStartTagInBody(p, token);

                    else if (tn == T.OPTION)
                        OptgroupStartTagInBody(p, token);

                    else
                        GenericStartTagInBody(p, token);

                    break;

                case 7:
                    if (tn == T.BGSOUND)
                        StartTagInHead(p, token);

                    else if (tn == T.DETAILS || tn == T.ADDRESS || tn == T.ARTICLE || tn == T.SECTION || tn == T.SUMMARY)
                        AddressStartTagInBody(p, token);

                    else if (tn == T.LISTING)
                        PreStartTagInBody(p, token);

                    else if (tn == T.MARQUEE)
                        AppletStartTagInBody(p, token);

                    else if (tn == T.NOEMBED)
                        NoembedStartTagInBody(p, token);

                    else if (tn != T.CAPTION)
                        GenericStartTagInBody(p, token);

                    break;

                case 8:
                    if (tn == T.BASEFONT)
                        StartTagInHead(p, token);

                    else if (tn == T.MENUITEM)
                        MenuitemStartTagInBody(p, token);

                    else if (tn == T.FRAMESET)
                        FramesetStartTagInBody(p, token);

                    else if (tn == T.FIELDSET)
                        AddressStartTagInBody(p, token);

                    else if (tn == T.TEXTAREA)
                        TextareaStartTagInBody(p, token);

                    else if (tn == T.TEMPLATE)
                        StartTagInHead(p, token);

                    else if (tn == T.NOSCRIPT)
                        NoembedStartTagInBody(p, token);

                    else if (tn == T.OPTGROUP)
                        OptgroupStartTagInBody(p, token);

                    else if (tn != T.COLGROUP)
                        GenericStartTagInBody(p, token);

                    break;

                case 9:
                    if (tn == T.PLAINTEXT)
                        PlaintextStartTagInBody(p, token);

                    else
                        GenericStartTagInBody(p, token);

                    break;

                case 10:
                    if (tn == T.BLOCKQUOTE || tn == T.FIGCAPTION)
                        AddressStartTagInBody(p, token);

                    else
                        GenericStartTagInBody(p, token);

                    break;

                default:
                    GenericStartTagInBody(p, token);
                    break;
            }
        }

        static void BodyEndTagInBody(Parser p)
        {
            if (p.openElements.HasInScope(T.BODY))
                p.insertionMode = AFTER_BODY_MODE;
        }

        static void BodyEndTagInBody(Parser p, Token token)
        {
            BodyEndTagInBody(p);
        }

        static void HtmlEndTagInBody(Parser p, Token token)
        {
            if (p.openElements.HasInScope(T.BODY))
            {
                p.insertionMode = AFTER_BODY_MODE;
                p.ProcessToken(token);
            }
        }

        static void AddressEndTagInBody(Parser p, Token token)
        {
            var tn = token.TagName;

            if (p.openElements.HasInScope(tn))
            {
                p.openElements.GenerateImpliedEndTags();
                p.openElements.PopUntilTagNamePopped(tn);
            }
        }

        static void FormEndTagInBody(Parser p)
        {
            var inTemplate = p.openElements.TmplCount > 0;
            var formElement = p.formElement;

            if (!inTemplate)
                p.formElement = null;

            if ((formElement.IsTruthy() || inTemplate) && p.openElements.HasInScope(T.FORM))
            {
                p.openElements.GenerateImpliedEndTags();

                if (inTemplate)
                    p.openElements.PopUntilTagNamePopped(T.FORM);

                else
                    p.openElements.Remove(formElement);
            }
        }

        static void FormEndTagInBody(Parser p, Token token)
        {
            FormEndTagInBody(p);
        }

        static void PEndTagInBody(Parser p)
        {
            if (!p.openElements.HasInButtonScope(T.P))
                p.InsertFakeElement(T.P);

            p.ClosePElement();
        }

        static void PEndTagInBody(Parser p, Token token)
        {
            PEndTagInBody(p);
        }

        static void LiEndTagInBody(Parser p)
        {
            if (p.openElements.HasInListItemScope(T.LI))
            {
                p.openElements.GenerateImpliedEndTagsWithExclusion(T.LI);
                p.openElements.PopUntilTagNamePopped(T.LI);
            }
        }

        static void LiEndTagInBody(Parser p, Token token)
        {
            LiEndTagInBody(p);
        }

        static void DdEndTagInBody(Parser p, Token token)
        {
            var tn = token.TagName;

            if (p.openElements.HasInScope(tn))
            {
                p.openElements.GenerateImpliedEndTagsWithExclusion(tn);
                p.openElements.PopUntilTagNamePopped(tn);
            }
        }

        static void NumberedHeaderEndTagInBody(Parser p)
        {
            if (p.openElements.HasNumberedHeaderInScope())
            {
                p.openElements.GenerateImpliedEndTags();
                p.openElements.PopUntilNumberedHeaderPopped();
            }
        }

        static void NumberedHeaderEndTagInBody(Parser p, Token token)
        {
            NumberedHeaderEndTagInBody(p);
        }

        static void AppletEndTagInBody(Parser p, Token token)
        {
            var tn = token.TagName;

            if (p.openElements.HasInScope(tn))
            {
                p.openElements.GenerateImpliedEndTags();
                p.openElements.PopUntilTagNamePopped(tn);
                p.activeFormattingElements.ClearToLastMarker();
            }
        }

        static void BrEndTagInBody(Parser p)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertFakeElement(T.BR);
            p.openElements.Pop();
            p.framesetOk = false;
        }

        static void BrEndTagInBody(Parser p, Token token)
        {
            BrEndTagInBody(p);
        }

        static void GenericEndTagInBody(Parser p, Token token)
        {
            var tn = token.TagName;

            for (var i = p.openElements.StackTop; i > 0; i--)
            {
                var element = p.openElements[i];

                if (p.treeAdapter.GetTagName(element) == tn)
                {
                    p.openElements.GenerateImpliedEndTagsWithExclusion(tn);
                    p.openElements.PopUntilElementPopped(element);
                    break;
                }

                if (p.IsSpecialElement(element))
                    break;
            }
        }

        //OPTIMIZATION: Integer comparisons are low-cost, so we can use very fast tag name.Length filters here.
        //It's faster than using dictionary.
        static void EndTagInBody(Parser p, Token token)
        {
            var tn = token.TagName;

            switch (tn.Length)
            {
                case 1:
                    if (tn == T.A || tn == T.B || tn == T.I || tn == T.S || tn == T.U)
                        CallAdoptionAgency(p, token);

                    else if (tn == T.P)
                        PEndTagInBody(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                case 2:
                    if (tn == T.DL || tn == T.UL || tn == T.OL)
                        AddressEndTagInBody(p, token);

                    else if (tn == T.LI)
                        LiEndTagInBody(p, token);

                    else if (tn == T.DD || tn == T.DT)
                        DdEndTagInBody(p, token);

                    else if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6)
                        NumberedHeaderEndTagInBody(p, token);

                    else if (tn == T.BR)
                        BrEndTagInBody(p, token);

                    else if (tn == T.EM || tn == T.TT)
                        CallAdoptionAgency(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                case 3:
                    if (tn == T.BIG)
                        CallAdoptionAgency(p, token);

                    else if (tn == T.DIR || tn == T.DIV || tn == T.NAV)
                        AddressEndTagInBody(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                case 4:
                    if (tn == T.BODY)
                        BodyEndTagInBody(p, token);

                    else if (tn == T.HTML)
                        HtmlEndTagInBody(p, token);

                    else if (tn == T.FORM)
                        FormEndTagInBody(p, token);

                    else if (tn == T.CODE || tn == T.FONT || tn == T.NOBR)
                        CallAdoptionAgency(p, token);

                    else if (tn == T.MAIN || tn == T.MENU)
                        AddressEndTagInBody(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                case 5:
                    if (tn == T.ASIDE)
                        AddressEndTagInBody(p, token);

                    else if (tn == T.SMALL)
                        CallAdoptionAgency(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                case 6:
                    if (tn == T.CENTER || tn == T.FIGURE || tn == T.FOOTER || tn == T.HEADER || tn == T.HGROUP)
                        AddressEndTagInBody(p, token);

                    else if (tn == T.APPLET || tn == T.OBJECT)
                        AppletEndTagInBody(p, token);

                    else if (tn == T.STRIKE || tn == T.STRONG)
                        CallAdoptionAgency(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                case 7:
                    if (tn == T.ADDRESS || tn == T.ARTICLE || tn == T.DETAILS || tn == T.SECTION || tn == T.SUMMARY)
                        AddressEndTagInBody(p, token);

                    else if (tn == T.MARQUEE)
                        AppletEndTagInBody(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                case 8:
                    if (tn == T.FIELDSET)
                        AddressEndTagInBody(p, token);

                    else if (tn == T.TEMPLATE)
                        EndTagInHead(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                case 10:
                    if (tn == T.BLOCKQUOTE || tn == T.FIGCAPTION)
                        AddressEndTagInBody(p, token);

                    else
                        GenericEndTagInBody(p, token);

                    break;

                default:
                    GenericEndTagInBody(p, token);
                    break;
            }
        }


        static void EofInBody(Parser p, Token token)
        {
            if (p.tmplInsertionModeStackTop > -1)
                EofInTemplate(p, token);

            else
                p.stopped = true;
        }

        //12.2.5.4.8 The "text" insertion mode
        //------------------------------------------------------------------
        static void EndTagInText(Parser p, Token token)
        {
            if (token.TagName == T.SCRIPT)
                p.pendingScript = (Element) p.openElements.Current;

            p.openElements.Pop();
            p.insertionMode = p.originalInsertionMode;
        }


        static void EofInText(Parser p, Token token)
        {
            p.openElements.Pop();
            p.insertionMode = p.originalInsertionMode;
            p.ProcessToken(token);
        }


        //12.2.5.4.9 The "in table" insertion mode
        //------------------------------------------------------------------
        static void CharacterInTable(Parser p, Token token)
        {
            var curTn = p.openElements.CurrentTagName;

            if (curTn == T.TABLE || curTn == T.TBODY || curTn == T.TFOOT || curTn == T.THEAD || curTn == T.TR)
            {
                p.pendingCharacterTokens = new List<Token>();
                p.hasNonWhitespacePendingCharacterToken = false;
                p.originalInsertionMode = p.insertionMode;
                p.insertionMode = IN_TABLE_TEXT_MODE;
                p.ProcessToken(token);
            }

            else
                TokenInTable(p, token);
        }

        static void CaptionStartTagInTable(Parser p, Token token)
        {
            p.openElements.ClearBackToTableContext();
            p.activeFormattingElements.InsertMarker();
            p.InsertElement(token, NS.HTML);
            p.insertionMode = IN_CAPTION_MODE;
        }

        static void ColgroupStartTagInTable(Parser p, Token token)
        {
            p.openElements.ClearBackToTableContext();
            p.InsertElement(token, NS.HTML);
            p.insertionMode = IN_COLUMN_GROUP_MODE;
        }

        static void ColStartTagInTable(Parser p, Token token)
        {
            p.openElements.ClearBackToTableContext();
            p.InsertFakeElement(T.COLGROUP);
            p.insertionMode = IN_COLUMN_GROUP_MODE;
            p.ProcessToken(token);
        }

        static void TbodyStartTagInTable(Parser p, Token token)
        {
            p.openElements.ClearBackToTableContext();
            p.InsertElement(token, NS.HTML);
            p.insertionMode = IN_TABLE_BODY_MODE;
        }

        static void TdStartTagInTable(Parser p, Token token)
        {
            p.openElements.ClearBackToTableContext();
            p.InsertFakeElement(T.TBODY);
            p.insertionMode = IN_TABLE_BODY_MODE;
            p.ProcessToken(token);
        }

        static void TableStartTagInTable(Parser p, Token token)
        {
            if (p.openElements.HasInTableScope(T.TABLE))
            {
                p.openElements.PopUntilTagNamePopped(T.TABLE);
                p.ResetInsertionMode();
                p.ProcessToken(token);
            }
        }

        static void InputStartTagInTable(Parser p, Token token)
        {
            var inputType = Tokenizer.GetTokenAttr(token, ATTRS.TYPE);

            if (inputType.IsTruthy() && inputType.ToLowerCase() == HIDDEN_INPUT_TYPE)
                p.AppendElement(token, NS.HTML);

            else
                TokenInTable(p, token);
        }

        static void FormStartTagInTable(Parser p, Token token)
        {
            if (!p.formElement.IsTruthy() && p.openElements.TmplCount == 0)
            {
                p.InsertElement(token, NS.HTML);
                p.formElement = (Element) p.openElements.Current;
                p.openElements.Pop();
            }
        }

        static void StartTagInTable(Parser p, Token token)
        {
            var tn = token.TagName;

            switch (tn.Length)
            {
                case 2:
                    if (tn == T.TD || tn == T.TH || tn == T.TR)
                        TdStartTagInTable(p, token);

                    else
                        TokenInTable(p, token);

                    break;

                case 3:
                    if (tn == T.COL)
                        ColStartTagInTable(p, token);

                    else
                        TokenInTable(p, token);

                    break;

                case 4:
                    if (tn == T.FORM)
                        FormStartTagInTable(p, token);

                    else
                        TokenInTable(p, token);

                    break;

                case 5:
                    if (tn == T.TABLE)
                        TableStartTagInTable(p, token);

                    else if (tn == T.STYLE)
                        StartTagInHead(p, token);

                    else if (tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD)
                        TbodyStartTagInTable(p, token);

                    else if (tn == T.INPUT)
                        InputStartTagInTable(p, token);

                    else
                        TokenInTable(p, token);

                    break;

                case 6:
                    if (tn == T.SCRIPT)
                        StartTagInHead(p, token);

                    else
                        TokenInTable(p, token);

                    break;

                case 7:
                    if (tn == T.CAPTION)
                        CaptionStartTagInTable(p, token);

                    else
                        TokenInTable(p, token);

                    break;

                case 8:
                    if (tn == T.COLGROUP)
                        ColgroupStartTagInTable(p, token);

                    else if (tn == T.TEMPLATE)
                        StartTagInHead(p, token);

                    else
                        TokenInTable(p, token);

                    break;

                default:
                    TokenInTable(p, token);
                    break;
            }

        }

        static void EndTagInTable(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.TABLE)
            {
                if (p.openElements.HasInTableScope(T.TABLE))
                {
                    p.openElements.PopUntilTagNamePopped(T.TABLE);
                    p.ResetInsertionMode();
                }
            }

            else if (tn == T.TEMPLATE)
                EndTagInHead(p, token);

            else if (tn != T.BODY && tn != T.CAPTION && tn != T.COL && tn != T.COLGROUP && tn != T.HTML &&
                     tn != T.TBODY && tn != T.TD && tn != T.TFOOT && tn != T.TH && tn != T.THEAD && tn != T.TR)
                TokenInTable(p, token);
        }

        static void TokenInTable(Parser p, Token token)
        {
            var savedFosterParentingState = p.fosterParentingEnabled;

            p.fosterParentingEnabled = true;
            p.ProcessTokenInBodyMode(token);
            p.fosterParentingEnabled = savedFosterParentingState;
        }


        //12.2.5.4.10 The "in table text" insertion mode
        //------------------------------------------------------------------
        static void WhitespaceCharacterInTableText(Parser p, Token token)
        {
            p.pendingCharacterTokens.Push(token);
        }

        static void CharacterInTableText(Parser p, Token token)
        {
            p.pendingCharacterTokens.Push(token);
            p.hasNonWhitespacePendingCharacterToken = true;
        }

        static void TokenInTableText(Parser p, Token token)
        {
            var i = 0;

            if (p.hasNonWhitespacePendingCharacterToken)
            {
                for (; i < p.pendingCharacterTokens.Count; i++)
                    TokenInTable(p, p.pendingCharacterTokens[i]);
            }

            else
            {
                for (; i < p.pendingCharacterTokens.Count; i++)
                    p.InsertCharacters(p.pendingCharacterTokens[i]);
            }

            p.insertionMode = p.originalInsertionMode;
            p.ProcessToken(token);
        }


        //12.2.5.4.11 The "in caption" insertion mode
        //------------------------------------------------------------------
        static void StartTagInCaption(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.CAPTION || tn == T.COL || tn == T.COLGROUP || tn == T.TBODY ||
                tn == T.TD || tn == T.TFOOT || tn == T.TH || tn == T.THEAD || tn == T.TR)
            {
                if (p.openElements.HasInTableScope(T.CAPTION))
                {
                    p.openElements.GenerateImpliedEndTags();
                    p.openElements.PopUntilTagNamePopped(T.CAPTION);
                    p.activeFormattingElements.ClearToLastMarker();
                    p.insertionMode = IN_TABLE_MODE;
                    p.ProcessToken(token);
                }
            }

            else
                StartTagInBody(p, token);
        }

        static void EndTagInCaption(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.CAPTION || tn == T.TABLE)
            {
                if (p.openElements.HasInTableScope(T.CAPTION))
                {
                    p.openElements.GenerateImpliedEndTags();
                    p.openElements.PopUntilTagNamePopped(T.CAPTION);
                    p.activeFormattingElements.ClearToLastMarker();
                    p.insertionMode = IN_TABLE_MODE;

                    if (tn == T.TABLE)
                        p.ProcessToken(token);
                }
            }

            else if (tn != T.BODY && tn != T.COL && tn != T.COLGROUP && tn != T.HTML && tn != T.TBODY &&
                     tn != T.TD && tn != T.TFOOT && tn != T.TH && tn != T.THEAD && tn != T.TR)
                EndTagInBody(p, token);
        }


        //12.2.5.4.12 The "in column group" insertion mode
        //------------------------------------------------------------------
        static void StartTagInColumnGroup(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.COL)
                p.AppendElement(token, NS.HTML);

            else if (tn == T.TEMPLATE)
                StartTagInHead(p, token);

            else
                TokenInColumnGroup(p, token);
        }

        static void EndTagInColumnGroup(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.COLGROUP)
            {
                if (p.openElements.CurrentTagName == T.COLGROUP)
                {
                    p.openElements.Pop();
                    p.insertionMode = IN_TABLE_MODE;
                }
            }

            else if (tn == T.TEMPLATE)
                EndTagInHead(p, token);

            else if (tn != T.COL)
                TokenInColumnGroup(p, token);
        }

        static void TokenInColumnGroup(Parser p, Token token)
        {
            if (p.openElements.CurrentTagName == T.COLGROUP)
            {
                p.openElements.Pop();
                p.insertionMode = IN_TABLE_MODE;
                p.ProcessToken(token);
            }
        }

        //12.2.5.4.13 The "in table body" insertion mode
        //------------------------------------------------------------------
        static void StartTagInTableBody(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.TR)
            {
                p.openElements.ClearBackToTableBodyContext();
                p.InsertElement(token, NS.HTML);
                p.insertionMode = IN_ROW_MODE;
            }

            else if (tn == T.TH || tn == T.TD)
            {
                p.openElements.ClearBackToTableBodyContext();
                p.InsertFakeElement(T.TR);
                p.insertionMode = IN_ROW_MODE;
                p.ProcessToken(token);
            }

            else if (tn == T.CAPTION || tn == T.COL || tn == T.COLGROUP ||
                     tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD)
            {

                if (p.openElements.HasTableBodyContextInTableScope())
                {
                    p.openElements.ClearBackToTableBodyContext();
                    p.openElements.Pop();
                    p.insertionMode = IN_TABLE_MODE;
                    p.ProcessToken(token);
                }
            }

            else
                StartTagInTable(p, token);
        }

        static void EndTagInTableBody(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD)
            {
                if (p.openElements.HasInTableScope(tn))
                {
                    p.openElements.ClearBackToTableBodyContext();
                    p.openElements.Pop();
                    p.insertionMode = IN_TABLE_MODE;
                }
            }

            else if (tn == T.TABLE)
            {
                if (p.openElements.HasTableBodyContextInTableScope())
                {
                    p.openElements.ClearBackToTableBodyContext();
                    p.openElements.Pop();
                    p.insertionMode = IN_TABLE_MODE;
                    p.ProcessToken(token);
                }
            }

            else if (tn != T.BODY && tn != T.CAPTION && tn != T.COL && tn != T.COLGROUP ||
                     tn != T.HTML && tn != T.TD && tn != T.TH && tn != T.TR)
                EndTagInTable(p, token);
        }

        //12.2.5.4.14 The "in row" insertion mode
        //------------------------------------------------------------------
        static void StartTagInRow(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.TH || tn == T.TD)
            {
                p.openElements.ClearBackToTableRowContext();
                p.InsertElement(token, NS.HTML);
                p.insertionMode = IN_CELL_MODE;
                p.activeFormattingElements.InsertMarker();
            }

            else if (tn == T.CAPTION || tn == T.COL || tn == T.COLGROUP || tn == T.TBODY ||
                     tn == T.TFOOT || tn == T.THEAD || tn == T.TR)
            {
                if (p.openElements.HasInTableScope(T.TR))
                {
                    p.openElements.ClearBackToTableRowContext();
                    p.openElements.Pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                    p.ProcessToken(token);
                }
            }

            else
                StartTagInTable(p, token);
        }

        static void EndTagInRow(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.TR)
            {
                if (p.openElements.HasInTableScope(T.TR))
                {
                    p.openElements.ClearBackToTableRowContext();
                    p.openElements.Pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                }
            }

            else if (tn == T.TABLE)
            {
                if (p.openElements.HasInTableScope(T.TR))
                {
                    p.openElements.ClearBackToTableRowContext();
                    p.openElements.Pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                    p.ProcessToken(token);
                }
            }

            else if (tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD)
            {
                if (p.openElements.HasInTableScope(tn) || p.openElements.HasInTableScope(T.TR))
                {
                    p.openElements.ClearBackToTableRowContext();
                    p.openElements.Pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                    p.ProcessToken(token);
                }
            }

            else if (tn != T.BODY && tn != T.CAPTION && tn != T.COL && tn != T.COLGROUP ||
                     tn != T.HTML && tn != T.TD && tn != T.TH)
                EndTagInTable(p, token);
        }


        //12.2.5.4.15 The "in cell" insertion mode
        //------------------------------------------------------------------
        static void StartTagInCell(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.CAPTION || tn == T.COL || tn == T.COLGROUP || tn == T.TBODY ||
                tn == T.TD || tn == T.TFOOT || tn == T.TH || tn == T.THEAD || tn == T.TR)
            {

                if (p.openElements.HasInTableScope(T.TD) || p.openElements.HasInTableScope(T.TH))
                {
                    p.CloseTableCell();
                    p.ProcessToken(token);
                }
            }

            else
                StartTagInBody(p, token);
        }

        static void EndTagInCell(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.TD || tn == T.TH)
            {
                if (p.openElements.HasInTableScope(tn))
                {
                    p.openElements.GenerateImpliedEndTags();
                    p.openElements.PopUntilTagNamePopped(tn);
                    p.activeFormattingElements.ClearToLastMarker();
                    p.insertionMode = IN_ROW_MODE;
                }
            }

            else if (tn == T.TABLE || tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD || tn == T.TR)
            {
                if (p.openElements.HasInTableScope(tn))
                {
                    p.CloseTableCell();
                    p.ProcessToken(token);
                }
            }

            else if (tn != T.BODY && tn != T.CAPTION && tn != T.COL && tn != T.COLGROUP && tn != T.HTML)
                EndTagInBody(p, token);
        }

        //12.2.5.4.16 The "in select" insertion mode
        //------------------------------------------------------------------
        static void StartTagInSelect(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.OPTION)
            {
                if (p.openElements.CurrentTagName == T.OPTION)
                    p.openElements.Pop();

                p.InsertElement(token, NS.HTML);
            }

            else if (tn == T.OPTGROUP)
            {
                if (p.openElements.CurrentTagName == T.OPTION)
                    p.openElements.Pop();

                if (p.openElements.CurrentTagName == T.OPTGROUP)
                    p.openElements.Pop();

                p.InsertElement(token, NS.HTML);
            }

            else if (tn == T.INPUT || tn == T.KEYGEN || tn == T.TEXTAREA || tn == T.SELECT)
            {
                if (p.openElements.HasInSelectScope(T.SELECT))
                {
                    p.openElements.PopUntilTagNamePopped(T.SELECT);
                    p.ResetInsertionMode();

                    if (tn != T.SELECT)
                        p.ProcessToken(token);
                }
            }

            else if (tn == T.SCRIPT || tn == T.TEMPLATE)
                StartTagInHead(p, token);
        }

        static void EndTagInSelect(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.OPTGROUP)
            {
                var prevOpenElement = p.openElements[p.openElements.StackTop - 1];
                var prevOpenElementTn = // prevOpenElement && p.treeAdapter.getTagName(prevOpenElement)
                                        prevOpenElement.IsTruthy() ? p.treeAdapter.GetTagName(prevOpenElement) : null;

                if (p.openElements.CurrentTagName == T.OPTION && prevOpenElementTn == T.OPTGROUP)
                    p.openElements.Pop();

                if (p.openElements.CurrentTagName == T.OPTGROUP)
                    p.openElements.Pop();
            }

            else if (tn == T.OPTION)
            {
                if (p.openElements.CurrentTagName == T.OPTION)
                    p.openElements.Pop();
            }

            else if (tn == T.SELECT && p.openElements.HasInSelectScope(T.SELECT))
            {
                p.openElements.PopUntilTagNamePopped(T.SELECT);
                p.ResetInsertionMode();
            }

            else if (tn == T.TEMPLATE)
                EndTagInHead(p, token);
        }

        //12.2.5.4.17 The "in select in table" insertion mode
        //------------------------------------------------------------------
        static void StartTagInSelectInTable(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.CAPTION || tn == T.TABLE || tn == T.TBODY || tn == T.TFOOT ||
                tn == T.THEAD || tn == T.TR || tn == T.TD || tn == T.TH)
            {
                p.openElements.PopUntilTagNamePopped(T.SELECT);
                p.ResetInsertionMode();
                p.ProcessToken(token);
            }

            else
                StartTagInSelect(p, token);
        }

        static void EndTagInSelectInTable(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.CAPTION || tn == T.TABLE || tn == T.TBODY || tn == T.TFOOT ||
                tn == T.THEAD || tn == T.TR || tn == T.TD || tn == T.TH)
            {
                if (p.openElements.HasInTableScope(tn))
                {
                    p.openElements.PopUntilTagNamePopped(T.SELECT);
                    p.ResetInsertionMode();
                    p.ProcessToken(token);
                }
            }

            else
                EndTagInSelect(p, token);
        }

        //12.2.5.4.18 The "in template" insertion mode
        //------------------------------------------------------------------
        static void StartTagInTemplate(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.BASE || tn == T.BASEFONT || tn == T.BGSOUND || tn == T.LINK || tn == T.META ||
                tn == T.NOFRAMES || tn == T.SCRIPT || tn == T.STYLE || tn == T.TEMPLATE || tn == T.TITLE)
                StartTagInHead(p, token);

            else
            {
                var newInsertionMode = TEMPLATE_INSERTION_MODE_SWITCH_MAP.TryGetValue(tn, out var result) ? result : IN_BODY_MODE; // || operator

                p.PopTmplInsertionMode();
                p.PushTmplInsertionMode(newInsertionMode);
                p.insertionMode = newInsertionMode;
                p.ProcessToken(token);
            }
        }

        static void EndTagInTemplate(Parser p, Token token)
        {
            if (token.TagName == T.TEMPLATE)
                EndTagInHead(p, token);
        }

        static void EofInTemplate(Parser p, Token token)
        {
            if (p.openElements.TmplCount > 0)
            {
                p.openElements.PopUntilTagNamePopped(T.TEMPLATE);
                p.activeFormattingElements.ClearToLastMarker();
                p.PopTmplInsertionMode();
                p.ResetInsertionMode();
                p.ProcessToken(token);
            }

            else
                p.stopped = true;
        }


        //12.2.5.4.19 The "after body" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterBody(Parser p, Token token)
        {
            if (token.TagName == T.HTML)
                StartTagInBody(p, token);

            else
                TokenAfterBody(p, token);
        }

        static void EndTagAfterBody(Parser p, Token token)
        {
            if (token.TagName == T.HTML)
            {
                if (!p.fragmentContext.IsTruthy())
                    p.insertionMode = AFTER_AFTER_BODY_MODE;
            }

            else
                TokenAfterBody(p, token);
        }

        static void TokenAfterBody(Parser p, Token token)
        {
            p.insertionMode = IN_BODY_MODE;
            p.ProcessToken(token);
        }

        //12.2.5.4.20 The "in frameset" insertion mode
        //------------------------------------------------------------------
        static void StartTagInFrameset(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.FRAMESET)
                p.InsertElement(token, NS.HTML);

            else if (tn == T.FRAME)
                p.AppendElement(token, NS.HTML);

            else if (tn == T.NOFRAMES)
                StartTagInHead(p, token);
        }

        static void EndTagInFrameset(Parser p, Token token)
        {
            if (token.TagName == T.FRAMESET && !p.openElements.IsRootHtmlElementCurrent())
            {
                p.openElements.Pop();

                if (!p.fragmentContext.IsTruthy() && p.openElements.CurrentTagName != T.FRAMESET)
                    p.insertionMode = AFTER_FRAMESET_MODE;
            }
        }

        //12.2.5.4.21 The "after frameset" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterFrameset(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.NOFRAMES)
                StartTagInHead(p, token);
        }

        static void EndTagAfterFrameset(Parser p, Token token)
        {
            if (token.TagName == T.HTML)
                p.insertionMode = AFTER_AFTER_FRAMESET_MODE;
        }

        //12.2.5.4.22 The "after after body" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterAfterBody(Parser p, Token token)
        {
            if (token.TagName == T.HTML)
                StartTagInBody(p, token);

            else
                TokenAfterAfterBody(p, token);
        }

        static void TokenAfterAfterBody(Parser p, Token token)
        {
            p.insertionMode = IN_BODY_MODE;
            p.ProcessToken(token);
        }

        //12.2.5.4.23 The "after after frameset" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterAfterFrameset(Parser p, Token token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.NOFRAMES)
                StartTagInHead(p, token);
        }


        //12.2.5.5 The rules for parsing tokens in foreign content
        //------------------------------------------------------------------
        static void NullCharacterInForeignContent(Parser p, Token token)
        {
            token.Chars = Unicode.ReplacementCharacter.ToString();
            p.InsertCharacters(token);
        }

        static void CharacterInForeignContent(Parser p, Token token)
        {
            p.InsertCharacters(token);
            p.framesetOk = false;
        }

        static void StartTagInForeignContent(Parser p, Token token)
        {
            if (CausesExit(token) && !p.fragmentContext.IsTruthy())
            {
                while (p.treeAdapter.GetNamespaceUri((Element) p.openElements.Current) != NS.HTML && !p.IsIntegrationPoint((Element) p.openElements.Current))
                    p.openElements.Pop();

                p.ProcessToken(token);
            }

            else
            {
                var current = (Element) p.GetAdjustedCurrentElement();
                var currentNs = p.treeAdapter.GetNamespaceUri(current);

                if (currentNs == NS.MATHML)
                    AdjustTokenMathMlAttrs(token);

                else if (currentNs == NS.SVG)
                {
                    AdjustTokenSvgTagName(token);
                    AdjustTokenSvgAttrs(token);
                }

                AdjustTokenXmlAttrs(token);

                if (token.SelfClosing)
                    p.AppendElement(token, currentNs);
                else
                    p.InsertElement(token, currentNs);
            }
        }

        static void EndTagInForeignContent(Parser p, Token token)
        {
            for (var i = p.openElements.StackTop; i > 0; i--)
            {
                var element = p.openElements[i];

                if (p.treeAdapter.GetNamespaceUri(element) == NS.HTML)
                {
                    p.ProcessToken(token);
                    break;
                }

                if (p.treeAdapter.GetTagName(element).ToLowerCase() == token.TagName)
                {
                    p.openElements.PopUntilElementPopped(element);
                    break;
                }
            }
        }
    }
}
