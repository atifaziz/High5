using ParseFive.Extensions;

namespace ParseFive.Parser
{
    using System;
    using System.Collections.Generic;
    using Tokenizer;
    using TreeAdapters;
    using static Tokenizer.TokenType;
    using static Tokenizer.Tokenizer.MODE;
    using static Common.ForeignContent;
    using static Common.Doctype;
    using ATTRS = HTML.ATTRS;
    using NS = HTML.NAMESPACES;
    using MODE = Tokenizer.Tokenizer.MODE;
    using T = HTML.TAG_NAMES;
    using Tokenizer = Tokenizer.Tokenizer;
    using Unicode = Common.Unicode;

    // ReSharper disable InconsistentNaming
    // ReSharper disable ArrangeThisQualifier

    public class Parser
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
        static readonly IDictionary<string, IDictionary<string, Action<Parser, Token>>> _ =
            new Dictionary<string, IDictionary<string, Action<Parser, Token>>>
            {
                [INITIAL_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = tokenInInitialMode,
                    [NULL_CHARACTER_TOKEN] = tokenInInitialMode,
                    [WHITESPACE_CHARACTER_TOKEN] = ignoreToken,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = doctypeInInitialMode,
                    [START_TAG_TOKEN] = tokenInInitialMode,
                    [END_TAG_TOKEN] = tokenInInitialMode,
                    [EOF_TOKEN] = tokenInInitialMode,
                },

                [BEFORE_HTML_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = tokenBeforeHtml,
                    [NULL_CHARACTER_TOKEN] = tokenBeforeHtml,
                    [WHITESPACE_CHARACTER_TOKEN] = ignoreToken,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagBeforeHtml,
                    [END_TAG_TOKEN] = endTagBeforeHtml,
                    [EOF_TOKEN] = tokenBeforeHtml,
                },

                [BEFORE_HEAD_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = tokenBeforeHead,
                    [NULL_CHARACTER_TOKEN] = tokenBeforeHead,
                    [WHITESPACE_CHARACTER_TOKEN] = ignoreToken,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagBeforeHead,
                    [END_TAG_TOKEN] = endTagBeforeHead,
                    [EOF_TOKEN] = tokenBeforeHead,
                },

                [IN_HEAD_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = tokenInHead,
                    [NULL_CHARACTER_TOKEN] = tokenInHead,
                    [WHITESPACE_CHARACTER_TOKEN] = insertCharacters,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInHead,
                    [END_TAG_TOKEN] = endTagInHead,
                    [EOF_TOKEN] = tokenInHead,
                },

                [AFTER_HEAD_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = tokenAfterHead,
                    [NULL_CHARACTER_TOKEN] = tokenAfterHead,
                    [WHITESPACE_CHARACTER_TOKEN] = insertCharacters,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagAfterHead,
                    [END_TAG_TOKEN] = endTagAfterHead,
                    [EOF_TOKEN] = tokenAfterHead,
                },

                [IN_BODY_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = characterInBody,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = whitespaceCharacterInBody,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInBody,
                    [END_TAG_TOKEN] = endTagInBody,
                    [EOF_TOKEN] = eofInBody,
                },

                [TEXT_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = insertCharacters,
                    [NULL_CHARACTER_TOKEN] = insertCharacters,
                    [WHITESPACE_CHARACTER_TOKEN] = insertCharacters,
                    [COMMENT_TOKEN] = ignoreToken,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = ignoreToken,
                    [END_TAG_TOKEN] = endTagInText,
                    [EOF_TOKEN] = eofInText,
                },

                [IN_TABLE_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = characterInTable,
                    [NULL_CHARACTER_TOKEN] = characterInTable,
                    [WHITESPACE_CHARACTER_TOKEN] = characterInTable,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInTable,
                    [END_TAG_TOKEN] = endTagInTable,
                    [EOF_TOKEN] = eofInBody,
                },

                [IN_TABLE_TEXT_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = characterInTableText,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = whitespaceCharacterInTableText,
                    [COMMENT_TOKEN] = tokenInTableText,
                    [DOCTYPE_TOKEN] = tokenInTableText,
                    [START_TAG_TOKEN] = tokenInTableText,
                    [END_TAG_TOKEN] = tokenInTableText,
                    [EOF_TOKEN] = tokenInTableText,
                },

                [IN_CAPTION_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = characterInBody,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = whitespaceCharacterInBody,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInCaption,
                    [END_TAG_TOKEN] = endTagInCaption,
                    [EOF_TOKEN] = eofInBody,
                },

                [IN_COLUMN_GROUP_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = tokenInColumnGroup,
                    [NULL_CHARACTER_TOKEN] = tokenInColumnGroup,
                    [WHITESPACE_CHARACTER_TOKEN] = insertCharacters,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInColumnGroup,
                    [END_TAG_TOKEN] = endTagInColumnGroup,
                    [EOF_TOKEN] = eofInBody,
                },

                [IN_TABLE_BODY_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = characterInTable,
                    [NULL_CHARACTER_TOKEN] = characterInTable,
                    [WHITESPACE_CHARACTER_TOKEN] = characterInTable,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInTableBody,
                    [END_TAG_TOKEN] = endTagInTableBody,
                    [EOF_TOKEN] = eofInBody,
                },

                [IN_ROW_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = characterInTable,
                    [NULL_CHARACTER_TOKEN] = characterInTable,
                    [WHITESPACE_CHARACTER_TOKEN] = characterInTable,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInRow,
                    [END_TAG_TOKEN] = endTagInRow,
                    [EOF_TOKEN] = eofInBody,
                },

                [IN_CELL_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = characterInBody,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = whitespaceCharacterInBody,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInCell,
                    [END_TAG_TOKEN] = endTagInCell,
                    [EOF_TOKEN] = eofInBody,
                },

                [IN_SELECT_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = insertCharacters,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = insertCharacters,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInSelect,
                    [END_TAG_TOKEN] = endTagInSelect,
                    [EOF_TOKEN] = eofInBody,
                },

                [IN_SELECT_IN_TABLE_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = insertCharacters,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = insertCharacters,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInSelectInTable,
                    [END_TAG_TOKEN] = endTagInSelectInTable,
                    [EOF_TOKEN] = eofInBody,
                },

                [IN_TEMPLATE_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = characterInBody,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = whitespaceCharacterInBody,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInTemplate,
                    [END_TAG_TOKEN] = endTagInTemplate,
                    [EOF_TOKEN] = eofInTemplate,
                },

                [AFTER_BODY_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = tokenAfterBody,
                    [NULL_CHARACTER_TOKEN] = tokenAfterBody,
                    [WHITESPACE_CHARACTER_TOKEN] = whitespaceCharacterInBody,
                    [COMMENT_TOKEN] = appendCommentToRootHtmlElement,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagAfterBody,
                    [END_TAG_TOKEN] = endTagAfterBody,
                    [EOF_TOKEN] = stopParsing,
                },

                [IN_FRAMESET_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = ignoreToken,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = insertCharacters,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagInFrameset,
                    [END_TAG_TOKEN] = endTagInFrameset,
                    [EOF_TOKEN] = stopParsing,
                },

                [AFTER_FRAMESET_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = ignoreToken,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = insertCharacters,
                    [COMMENT_TOKEN] = appendComment,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagAfterFrameset,
                    [END_TAG_TOKEN] = endTagAfterFrameset,
                    [EOF_TOKEN] = stopParsing,
                },

                [AFTER_AFTER_BODY_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = tokenAfterAfterBody,
                    [NULL_CHARACTER_TOKEN] = tokenAfterAfterBody,
                    [WHITESPACE_CHARACTER_TOKEN] = whitespaceCharacterInBody,
                    [COMMENT_TOKEN] = appendCommentToDocument,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagAfterAfterBody,
                    [END_TAG_TOKEN] = tokenAfterAfterBody,
                    [EOF_TOKEN] = stopParsing,
                },

                [AFTER_AFTER_FRAMESET_MODE] = new Dictionary<string, Action<Parser, Token>>
                {
                    [CHARACTER_TOKEN] = ignoreToken,
                    [NULL_CHARACTER_TOKEN] = ignoreToken,
                    [WHITESPACE_CHARACTER_TOKEN] = whitespaceCharacterInBody,
                    [COMMENT_TOKEN] = appendCommentToDocument,
                    [DOCTYPE_TOKEN] = ignoreToken,
                    [START_TAG_TOKEN] = startTagAfterAfterFrameset,
                    [END_TAG_TOKEN] = ignoreToken,
                    [EOF_TOKEN] = stopParsing,
                },
            };

        readonly TreeAdapter treeAdapter;
        Element pendingScript;
        string originalInsertionMode;
        Element headElement;
        Element formElement;
        OpenElementStack openElements;
        FormattingElementList activeFormattingElements;
        Extensions.List<string> tmplInsertionModeStack;
        int tmplInsertionModeStackTop;
        string currentTmplInsertionMode;
        Extensions.List<Token> pendingCharacterTokens;
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
        public Parser(TreeAdapter treeAdapter = null)
        {
            //this.options = mergeOptions(DEFAULT_OPTIONS, options);

            this.treeAdapter = treeAdapter ?? new DefaultTreeAdapter();
            this.pendingScript = null;

            //TODO check Parsermixin
            //if (this.options.locationInfo)
            //    new LocationInfoParserMixin(this);
        }
        // API
        public Document parse(string html)
        {
            var document = this.treeAdapter.createDocument();

            this._bootstrap(document, null);
            this.tokenizer.Write(html, true);
            this._runParsingLoop(null);

            return document;
        }

        public DocumentFragment parseFragment(string html, Node fragmentContext)
        {
            //NOTE: use <template> element as a fragment context if context element was not provided,
            //so we will parse in "forgiving" manner
            if (!fragmentContext.IsTruthy())
                fragmentContext = this.treeAdapter.createElement(T.TEMPLATE, NS.HTML, new Extensions.List<Attr>());

            //NOTE: create fake element which will be used as 'document' for fragment parsing.
            //This is important for jsdom there 'document' can't be recreated, therefore
            //fragment parsing causes messing of the main `document`.
            var documentMock = this.treeAdapter.createElement("documentmock", NS.HTML, new Extensions.List<Attr>());

            this._bootstrap(documentMock, fragmentContext);

            if (this.treeAdapter.getTagName((Element) fragmentContext) == T.TEMPLATE)
                this._pushTmplInsertionMode(IN_TEMPLATE_MODE);

            this._initTokenizerForFragmentParsing();
            this._insertFakeRootElement();
            this._resetInsertionMode();
            this._findFormInFragmentContext();
            this.tokenizer.Write(html, true);
            this._runParsingLoop(null);

            var rootElement = (Element) this.treeAdapter.getFirstChild(documentMock);
            var fragment = this.treeAdapter.createDocumentFragment();

            this._adoptNodes(rootElement, fragment);

            return fragment;
        }

        //Bootstrap parser
        void _bootstrap(Node document, Node fragmentContext)
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

            this.tmplInsertionModeStack = new Extensions.List<string>();
            this.tmplInsertionModeStackTop = -1;
            this.currentTmplInsertionMode = null;

            this.pendingCharacterTokens = new Extensions.List<Token>();
            this.hasNonWhitespacePendingCharacterToken = false;

            this.framesetOk = true;
            this.skipNextNewLine = false;
            this.fosterParentingEnabled = false;
        }

        //Parsing loop
        void _runParsingLoop(Action<Element> scriptHandler)
        {
            while (!this.stopped)
            {
                this._setupTokenizerCDATAMode();

                var token = this.tokenizer.GetNextToken();

                if (token.type == HIBERNATION_TOKEN)
                    break;

                if (this.skipNextNewLine)
                {
                    this.skipNextNewLine = false;

                    if (token.type == WHITESPACE_CHARACTER_TOKEN && token.chars[0] == '\n')
                    {
                        if (token.chars.Length == 1)
                            continue;

                        token.chars = token.chars.substr(1);
                    }
                }

                this._processInputToken(token);

                if (scriptHandler.IsTruthy() && this.pendingScript.IsTruthy())
                    break;
            }
        }

        void runParsingLoopForCurrentChunk(Action writeCallback, Action<Element> scriptHandler)
        {
            this._runParsingLoop(scriptHandler);

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
        void _setupTokenizerCDATAMode()
        {
            var current = this._getAdjustedCurrentElement();

            this.tokenizer.AllowCData = current.IsTruthy() && current != this.document &&
                                        this.treeAdapter.getNamespaceURI((Element)current) != NS.HTML && !this._isIntegrationPoint((Element)current);
        }

        void _switchToTextParsing(Token currentToken, string nextTokenizerState)
        {
            this._insertElement(currentToken, NS.HTML);
            this.tokenizer.State = nextTokenizerState;
            this.originalInsertionMode = this.insertionMode;
            this.insertionMode = TEXT_MODE;
        }

        void switchToPlaintextParsing()
        {
            this.insertionMode = TEXT_MODE;
            this.originalInsertionMode = IN_BODY_MODE;
            this.tokenizer.State = PLAINTEXT;
        }

        //Fragment parsing
        Node _getAdjustedCurrentElement()
        {
            return (this.openElements.stackTop == 0 && this.fragmentContext.IsTruthy() ?
                this.fragmentContext :
                this.openElements.current);
        }

        void _findFormInFragmentContext()
        {
            var node = (Element) this.fragmentContext;

            do
            {
                if (this.treeAdapter.getTagName(node) == T.FORM)
                {
                    this.formElement = node;
                    break;
                }

                node = (Element) this.treeAdapter.getParentNode(node);
            } while (node.IsTruthy());
        }

        void _initTokenizerForFragmentParsing()
        {
            if (this.treeAdapter.getNamespaceURI((Element) this.fragmentContext) == NS.HTML)
            {
                var tn = this.treeAdapter.getTagName((Element) this.fragmentContext);

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
        void _setDocumentType(Token token)
        {
            this.treeAdapter.setDocumentType((Document) this.document, token.name, token.publicId, token.systemId);
        }

        void _attachElementToTree(Element element)
        {
            if (this._shouldFosterParentOnInsertion())
                this._fosterParentElement(element);

            else
            {
                var parent = this.openElements.currentTmplContent ?? this.openElements.current; //|| operator

                this.treeAdapter.appendChild(parent, element);
            }
        }

        void _appendElement(Token token, string namespaceURI)
        {
            var element = this.treeAdapter.createElement(token.tagName, namespaceURI, token.attrs);

            this._attachElementToTree(element);
        }

        void _insertElement(Token token, string namespaceURI)
        {
            var element = this.treeAdapter.createElement(token.tagName, namespaceURI, token.attrs);

            this._attachElementToTree(element);
            this.openElements.push(element);
        }

        void _insertFakeElement(string tagName)
        {
            var element = this.treeAdapter.createElement(tagName, NS.HTML, new Extensions.List<Attr>());

            this._attachElementToTree(element);
            this.openElements.push(element);
        }

        void _insertTemplate(Token token)
        {
            var tmpl = (TemplateElement) this.treeAdapter.createElement(token.tagName, NS.HTML, token.attrs);
            var content = this.treeAdapter.createDocumentFragment();

            this.treeAdapter.setTemplateContent(tmpl, content);
            this._attachElementToTree(tmpl);
            this.openElements.push(tmpl);
        }

        void _insertTemplate(Token token, string s)
        {
            _insertTemplate(token);
        }

        void _insertFakeRootElement()
        {
            var element = this.treeAdapter.createElement(T.HTML, NS.HTML, new Extensions.List<Attr>());

            this.treeAdapter.appendChild(this.openElements.current, element);
            this.openElements.push(element);
        }

        void _appendCommentNode(Token token, Node parent)
        {
            var commentNode = this.treeAdapter.createCommentNode(token.data);

            this.treeAdapter.appendChild(parent, commentNode);
        }

        void _insertCharacters(Token token)
        {
            if (this._shouldFosterParentOnInsertion())
                this._fosterParentText(token.chars);

            else
            {
                var parent = this.openElements.currentTmplContent ?? this.openElements.current; // || operator

                this.treeAdapter.insertText(parent, token.chars);
            }
        }

        void _adoptNodes(Element donor, Node recipient)
        {
            while (true)
            {
                var child = this.treeAdapter.getFirstChild(donor);

                if (!child.IsTruthy())
                    break;

                this.treeAdapter.detachNode(child);
                this.treeAdapter.appendChild(recipient, child);
            }
        }

        //Token processing
        bool _shouldProcessTokenInForeignContent(Token token)
        {
            var current_ = this._getAdjustedCurrentElement();

            if (!current_.IsTruthy() || current_ == this.document)
                return false;

            var current = (Element) current_;

            var ns = this.treeAdapter.getNamespaceURI(current);

            if (ns == NS.HTML)
                return false;

            if (this.treeAdapter.getTagName(current) == T.ANNOTATION_XML && ns == NS.MATHML &&
                token.type == START_TAG_TOKEN && token.tagName == T.SVG)
                return false;

            var isCharacterToken = token.type == CHARACTER_TOKEN ||
                                   token.type == NULL_CHARACTER_TOKEN ||
                                   token.type == WHITESPACE_CHARACTER_TOKEN;
            var isMathMLTextStartTag = token.type == START_TAG_TOKEN &&
                                       token.tagName != T.MGLYPH &&
                                       token.tagName != T.MALIGNMARK;

            if ((isMathMLTextStartTag || isCharacterToken) && this._isIntegrationPoint(current, NS.MATHML))
                return false;

            if ((token.type == START_TAG_TOKEN || isCharacterToken) && this._isIntegrationPoint(current, NS.HTML))
                return false;

            return token.type != EOF_TOKEN;
        }

        void _processToken(Token token)
        {
            _[this.insertionMode][token.type](this, token);
        }

        void _processTokenInBodyMode(Token token)
        {
            _[IN_BODY_MODE][token.type](this, token);
        }

        void _processTokenInForeignContent(Token token)
        {
            if (token.type == CHARACTER_TOKEN)
                characterInForeignContent(this, token);

            else if (token.type == NULL_CHARACTER_TOKEN)
                nullCharacterInForeignContent(this, token);

            else if (token.type == WHITESPACE_CHARACTER_TOKEN)
                insertCharacters(this, token);

            else if (token.type == COMMENT_TOKEN)
                appendComment(this, token);

            else if (token.type == START_TAG_TOKEN)
                startTagInForeignContent(this, token);

            else if (token.type == END_TAG_TOKEN)
                endTagInForeignContent(this, token);
        }

        void _processInputToken(Token token)
        {
            if (this._shouldProcessTokenInForeignContent(token))
                this._processTokenInForeignContent(token);

            else
                this._processToken(token);
        }

        //Integration points
        bool _isIntegrationPoint(Element element, string foreignNS)
        {
            var tn = this.treeAdapter.getTagName(element);
            var ns = this.treeAdapter.getNamespaceURI(element);
            var attrs = this.treeAdapter.getAttrList(element);

            return IsIntegrationPoint(tn, ns, attrs, foreignNS);
        }

        bool _isIntegrationPoint(Element element)
        {
            return _isIntegrationPoint(element, "");
        }

        //Active formatting elements reconstruction
        void _reconstructActiveFormattingElements()
        {
            int listLength = this.activeFormattingElements.Length;

            if (listLength.IsTruthy())
            {
                var unopenIdx = listLength;
                Entry entry;

                do
                {
                    unopenIdx--;
                    entry = this.activeFormattingElements[unopenIdx];

                    if (entry.Type == FormattingElementList.MARKER_ENTRY || this.openElements.contains(entry.Element))
                    {
                        unopenIdx++;
                        break;
                    }
                } while (unopenIdx > 0);

                for (var i = unopenIdx; i < listLength; i++)
                {
                    entry = this.activeFormattingElements[i];
                    this._insertElement(entry.Token, this.treeAdapter.getNamespaceURI(entry.Element));
                    entry.Element = (Element) this.openElements.current;
                }
            }
        }

        //Close elements
        void _closeTableCell()
        {
            this.openElements.generateImpliedEndTags();
            this.openElements.popUntilTableCellPopped();
            this.activeFormattingElements.ClearToLastMarker();
            this.insertionMode = IN_ROW_MODE;
        }

        void _closePElement()
        {
            this.openElements.generateImpliedEndTagsWithExclusion(T.P);
            this.openElements.popUntilTagNamePopped(T.P);
        }

        //Insertion modes
        void _resetInsertionMode()
        {
            bool last = false;
            for (int i = this.openElements.stackTop; i >= 0; i--)
            {
                var element = this.openElements.items[i];

                if (i == 0)
                {
                    last = true;

                    if (this.fragmentContext.IsTruthy())
                        element = (Element) this.fragmentContext;
                }

                var tn = this.treeAdapter.getTagName(element);

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
                    this._resetInsertionModeForSelect(i);
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

        void _resetInsertionModeForSelect(int selectIdx)
        {
            if (selectIdx > 0)
            {
                for (var i = selectIdx - 1; i > 0; i--)
                {
                    var ancestor = this.openElements.items[i];
                    var tn = this.treeAdapter.getTagName(ancestor);

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

        void _pushTmplInsertionMode(string mode)
        {
            this.tmplInsertionModeStack.push(mode);
            this.tmplInsertionModeStackTop++;
            this.currentTmplInsertionMode = mode;
        }

        void _popTmplInsertionMode()
        {
            this.tmplInsertionModeStack.pop();
            this.tmplInsertionModeStackTop--;
            this.currentTmplInsertionMode = this.tmplInsertionModeStackTop >= 0 && this.tmplInsertionModeStackTop < tmplInsertionModeStack.Count
                                          ? this.tmplInsertionModeStack[this.tmplInsertionModeStackTop]
                                          : null;
        }

        //Foster parenting
        bool _isElementCausesFosterParenting(Element element)
        {
            var tn = this.treeAdapter.getTagName(element);

            return tn == T.TABLE || tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD || tn == T.TR;
        }

        bool _shouldFosterParentOnInsertion()
        {
            return this.fosterParentingEnabled && this._isElementCausesFosterParenting((Element) this.openElements.current);
        }

        Location _findFosterParentingLocation()
        {
            var location = new Location(null, null);

            for (var i = this.openElements.stackTop; i >= 0; i--)
            {
                var openElement = this.openElements.items[i];
                var tn = this.treeAdapter.getTagName(openElement);
                var ns = this.treeAdapter.getNamespaceURI(openElement);

                if (tn == T.TEMPLATE && ns == NS.HTML)
                {
                    location.parent = this.treeAdapter.getTemplateContent(openElement);
                    break;
                }

                else if (tn == T.TABLE)
                {
                    location.parent = this.treeAdapter.getParentNode(openElement);

                    if (location.parent.IsTruthy())
                        location.beforeElement = openElement;
                    else
                        location.parent = this.openElements.items[i - 1];

                    break;
                }
            }

            if (!location.parent.IsTruthy())
                location.parent = this.openElements.items[0];

            return location;
        }

        void _fosterParentElement(Element element)
        {
            var location = this._findFosterParentingLocation();

            if (location.beforeElement.IsTruthy())
                this.treeAdapter.insertBefore(location.parent, element, location.beforeElement);
            else
                this.treeAdapter.appendChild(location.parent, element);
        }

        void _fosterParentText(string chars)
        {
            var location = this._findFosterParentingLocation();

            if (location.beforeElement.IsTruthy())
                this.treeAdapter.insertTextBefore(location.parent, chars, location.beforeElement);
            else
                this.treeAdapter.insertText(location.parent, chars);
        }

        //Special elements
        bool _isSpecialElement(Element element)
        {
            var tn = this.treeAdapter.getTagName(element);
            var ns = this.treeAdapter.getNamespaceURI(element);

            return HTML.SPECIAL_ELEMENTS[ns].TryGetValue(tn, out var result) ? result : false;
        }

        //Adoption agency algorithm
        //(see: http://www.whatwg.org/specs/web-apps/current-work/multipage/tree-construction.html#adoptionAgency)
        //------------------------------------------------------------------

        //Steps 5-8 of the algorithm
        static Entry aaObtainFormattingElementEntry(Parser p, Token token)
        {
            var formattingElementEntry = p.activeFormattingElements.GetElementEntryInScopeWithTagName(token.tagName);

            if (formattingElementEntry.IsTruthy())
            {
                if (!p.openElements.contains(formattingElementEntry.Element))
                {
                    p.activeFormattingElements.RemoveEntry(formattingElementEntry);
                    formattingElementEntry = null;
                }

                else if (!p.openElements.hasInScope(token.tagName))
                    formattingElementEntry = null;
            }

            else
                genericEndTagInBody(p, token);

            return formattingElementEntry;
        }

        static Entry aaObtainFormattingElementEntry(Parser p, Token token, Entry formattingElementEntry)
        {
            return aaObtainFormattingElementEntry(p, token);
        }

        //Steps 9 and 10 of the algorithm
        static Element aaObtainFurthestBlock(Parser p, Entry formattingElementEntry)
        {
            Element furthestBlock = null;

            for (var i = p.openElements.stackTop; i >= 0; i--)
            {
                var element = p.openElements.items[i];

                if (element == formattingElementEntry.Element)
                    break;

                if (p._isSpecialElement(element))
                    furthestBlock = element;
            }

            if (!furthestBlock.IsTruthy())
            {
                p.openElements.popUntilElementPopped(formattingElementEntry.Element);
                p.activeFormattingElements.RemoveEntry(formattingElementEntry);
            }

            return furthestBlock;
        }

        //Step 13 of the algorithm
        static Element aaInnerLoop(Parser p, Element furthestBlock, Element formattingElement)
        {
            var lastElement = furthestBlock;
            var nextElement = p.openElements.getCommonAncestor(furthestBlock);
            var element = nextElement;

            //for (var i = 0, element = nextElement; element != formattingElement; i++, element = nextElement)
            for (var i = 0; element != formattingElement; i++)
            {
                //NOTE: store next element for the next loop iteration (it may be deleted from the stack by step 9.5)
                nextElement = p.openElements.getCommonAncestor(element);

                var elementEntry = p.activeFormattingElements.GetElementEntry(element);
                var counterOverflow = elementEntry.IsTruthy() && i >= AA_INNER_LOOP_ITER;
                var shouldRemoveFromOpenElements = !elementEntry.IsTruthy() || counterOverflow;

                if (shouldRemoveFromOpenElements)
                {
                    if (counterOverflow.IsTruthy())
                        p.activeFormattingElements.RemoveEntry(elementEntry);

                    p.openElements.remove(element);
                }

                else
                {
                    element = aaRecreateElementFromEntry(p, elementEntry);

                    if (lastElement == furthestBlock)
                        p.activeFormattingElements.Bookmark = elementEntry;

                    p.treeAdapter.detachNode(lastElement);
                    p.treeAdapter.appendChild(element, lastElement);
                    lastElement = element;
                }
                element = nextElement;
            }

            return lastElement;
        }

        //Step 13.7 of the algorithm
        static Element aaRecreateElementFromEntry(Parser p, Entry elementEntry)
        {
            var ns = p.treeAdapter.getNamespaceURI(elementEntry.Element);
            var newElement = p.treeAdapter.createElement(elementEntry.Token.tagName, ns, elementEntry.Token.attrs);

            p.openElements.replace(elementEntry.Element, newElement);
            elementEntry.Element = newElement;

            return newElement;
        }

        //Step 14 of the algorithm
        static void aaInsertLastNodeInCommonAncestor(Parser p, Element commonAncestor, Element lastElement)
        {
            if (p._isElementCausesFosterParenting(commonAncestor))
                p._fosterParentElement(lastElement);

            else
            {
                Node commonAncestorNode = commonAncestor;
                var tn = p.treeAdapter.getTagName(commonAncestor);
                string ns = p.treeAdapter.getNamespaceURI(commonAncestor);

                if (tn == T.TEMPLATE && ns == NS.HTML)
                    commonAncestorNode = p.treeAdapter.getTemplateContent(commonAncestor);

                p.treeAdapter.appendChild(commonAncestorNode, lastElement);
            }
        }

        //Steps 15-19 of the algorithm
        static void aaReplaceFormattingElement(Parser p, Element furthestBlock, Entry formattingElementEntry)
        {
            string ns = p.treeAdapter.getNamespaceURI(formattingElementEntry.Element);
            Token token = formattingElementEntry.Token;
            Element newElement = p.treeAdapter.createElement(token.tagName, ns, token.attrs);

            p._adoptNodes(furthestBlock, newElement);
            p.treeAdapter.appendChild(furthestBlock, newElement);

            p.activeFormattingElements.InsertElementAfterBookmark(newElement, formattingElementEntry.Token);
            p.activeFormattingElements.RemoveEntry(formattingElementEntry);

            p.openElements.remove(formattingElementEntry.Element);
            p.openElements.insertAfter(furthestBlock, newElement);
        }

        //Algorithm entry point
        static void callAdoptionAgency(Parser p, Token token)
        {
            Entry formattingElementEntry = null;

            for (var i = 0; i < AA_OUTER_LOOP_ITER; i++)
            {
                formattingElementEntry = aaObtainFormattingElementEntry(p, token, formattingElementEntry);

                if (!formattingElementEntry.IsTruthy())
                    break;

                var furthestBlock = aaObtainFurthestBlock(p, formattingElementEntry);

                if (!furthestBlock.IsTruthy())
                    break;

                p.activeFormattingElements.Bookmark = formattingElementEntry;

                var lastElement = aaInnerLoop(p, furthestBlock, formattingElementEntry.Element);
                var commonAncestor = p.openElements.getCommonAncestor(formattingElementEntry.Element);

                p.treeAdapter.detachNode(lastElement);
                aaInsertLastNodeInCommonAncestor(p, commonAncestor, lastElement);
                aaReplaceFormattingElement(p, furthestBlock, formattingElementEntry);
            }
        }


        //Generic token handlers
        //------------------------------------------------------------------
        public static void ignoreToken(Parser p, Token token)
        {
            //NOTE: do nothing =)
        }

        public static void appendComment(Parser p, Token token)
        {
            p._appendCommentNode(token, p.openElements.currentTmplContent ?? p.openElements.current); //|| operator
        }

        public static void appendCommentToRootHtmlElement(Parser p, Token token)
        {
            p._appendCommentNode(token, p.openElements.items[0]);
        }

        public static void appendCommentToDocument(Parser p, Token token)
        {
            p._appendCommentNode(token, p.document);
        }

        public static void insertCharacters(Parser p, Token token)
        {
            p._insertCharacters(token);
        }

        public static void stopParsing(Parser p, Token token)
        {
            p.stopped = true;
        }

        //12.2.5.4.1 The "initial" insertion mode
        //------------------------------------------------------------------
        public static void doctypeInInitialMode(Parser p, Token token)
        {
            p._setDocumentType(token);

            var mode = token.forceQuirks ?
                HTML.DOCUMENT_MODE.QUIRKS :
                GetDocumentMode(token.name, token.publicId, token.systemId);

            p.treeAdapter.setDocumentMode((Document) p.document, mode);

            p.insertionMode = BEFORE_HTML_MODE;
        }

        public static void tokenInInitialMode(Parser p, Token token)
        {
            p.treeAdapter.setDocumentMode((Document) p.document, HTML.DOCUMENT_MODE.QUIRKS);
            p.insertionMode = BEFORE_HTML_MODE;
            p._processToken(token);
        }


        //12.2.5.4.2 The "before html" insertion mode
        //------------------------------------------------------------------
        public static void startTagBeforeHtml(Parser p, Token token)
        {
            if (token.tagName == T.HTML)
            {
                p._insertElement(token, NS.HTML);
                p.insertionMode = BEFORE_HEAD_MODE;
            }

            else
                tokenBeforeHtml(p, token);
        }

        public static void endTagBeforeHtml(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML || tn == T.HEAD || tn == T.BODY || tn == T.BR)
                tokenBeforeHtml(p, token);
        }

        public static void tokenBeforeHtml(Parser p, Token token)
        {
            p._insertFakeRootElement();
            p.insertionMode = BEFORE_HEAD_MODE;
            p._processToken(token);
        }


        //12.2.5.4.3 The "before head" insertion mode
        //------------------------------------------------------------------
        public static void startTagBeforeHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML)
                startTagInBody(p, token);

            else if (tn == T.HEAD)
            {
                p._insertElement(token, NS.HTML);
                p.headElement = (Element) p.openElements.current;
                p.insertionMode = IN_HEAD_MODE;
            }

            else
                tokenBeforeHead(p, token);
        }

        public static void endTagBeforeHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HEAD || tn == T.BODY || tn == T.HTML || tn == T.BR)
                tokenBeforeHead(p, token);
        }

        public static void tokenBeforeHead(Parser p, Token token)
        {
            p._insertFakeElement(T.HEAD);
            p.headElement = (Element) p.openElements.current;
            p.insertionMode = IN_HEAD_MODE;
            p._processToken(token);
        }


        //12.2.5.4.4 The "in head" insertion mode
        //------------------------------------------------------------------
        public static void startTagInHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML)
                startTagInBody(p, token);

            else if (tn == T.BASE || tn == T.BASEFONT || tn == T.BGSOUND || tn == T.LINK || tn == T.META)
                p._appendElement(token, NS.HTML);

            else if (tn == T.TITLE)
                p._switchToTextParsing(token, MODE.RCDATA);

            //NOTE: here we assume that we always act as an interactive user agent with enabled scripting, so we parse
            //<noscript> as a rawtext.
            else if (tn == T.NOSCRIPT || tn == T.NOFRAMES || tn == T.STYLE)
                p._switchToTextParsing(token, MODE.RAWTEXT);

            else if (tn == T.SCRIPT)
                p._switchToTextParsing(token, MODE.SCRIPT_DATA);

            else if (tn == T.TEMPLATE)
            {
                p._insertTemplate(token, NS.HTML);
                p.activeFormattingElements.InsertMarker();
                p.framesetOk = false;
                p.insertionMode = IN_TEMPLATE_MODE;
                p._pushTmplInsertionMode(IN_TEMPLATE_MODE);
            }

            else if (tn != T.HEAD)
                tokenInHead(p, token);
        }

        public static void endTagInHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HEAD)
            {
                p.openElements.pop();
                p.insertionMode = AFTER_HEAD_MODE;
            }

            else if (tn == T.BODY || tn == T.BR || tn == T.HTML)
                tokenInHead(p, token);

            else if (tn == T.TEMPLATE && p.openElements.tmplCount > 0)
            {
                p.openElements.generateImpliedEndTags();
                p.openElements.popUntilTagNamePopped(T.TEMPLATE);
                p.activeFormattingElements.ClearToLastMarker();
                p._popTmplInsertionMode();
                p._resetInsertionMode();
            }
        }

        public static void tokenInHead(Parser p, Token token)
        {
            p.openElements.pop();
            p.insertionMode = AFTER_HEAD_MODE;
            p._processToken(token);
        }


        //12.2.5.4.6 The "after head" insertion mode
        //------------------------------------------------------------------
        public static void startTagAfterHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML)
                startTagInBody(p, token);

            else if (tn == T.BODY)
            {
                p._insertElement(token, NS.HTML);
                p.framesetOk = false;
                p.insertionMode = IN_BODY_MODE;
            }

            else if (tn == T.FRAMESET)
            {
                p._insertElement(token, NS.HTML);
                p.insertionMode = IN_FRAMESET_MODE;
            }

            else if (tn == T.BASE || tn == T.BASEFONT || tn == T.BGSOUND || tn == T.LINK || tn == T.META ||
                     tn == T.NOFRAMES || tn == T.SCRIPT || tn == T.STYLE || tn == T.TEMPLATE || tn == T.TITLE)
            {
                p.openElements.push(p.headElement);
                startTagInHead(p, token);
                p.openElements.remove(p.headElement);
            }

            else if (tn != T.HEAD)
                tokenAfterHead(p, token);
        }

        public static void endTagAfterHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.BODY || tn == T.HTML || tn == T.BR)
                tokenAfterHead(p, token);

            else if (tn == T.TEMPLATE)
                endTagInHead(p, token);
        }

        public static void tokenAfterHead(Parser p, Token token)
        {
            p._insertFakeElement(T.BODY);
            p.insertionMode = IN_BODY_MODE;
            p._processToken(token);
        }


        //12.2.5.4.7 The "in body" insertion mode
        //------------------------------------------------------------------
        public static void whitespaceCharacterInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._insertCharacters(token);
        }

        public static void characterInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._insertCharacters(token);
            p.framesetOk = false;
        }

        public static void htmlStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.tmplCount == 0)
                p.treeAdapter.adoptAttributes(p.openElements.items[0], token.attrs);
        }

        public static void bodyStartTagInBody(Parser p, Token token)
        {
            var bodyElement = p.openElements.tryPeekProperlyNestedBodyElement();

            if (bodyElement.IsTruthy() && p.openElements.tmplCount == 0)
            {
                p.framesetOk = false;
                p.treeAdapter.adoptAttributes(bodyElement, token.attrs);
            }
        }

        public static void framesetStartTagInBody(Parser p, Token token)
        {
            var bodyElement = p.openElements.tryPeekProperlyNestedBodyElement();

            if (p.framesetOk && bodyElement.IsTruthy())
            {
                p.treeAdapter.detachNode(bodyElement);
                p.openElements.popAllUpToHtmlElement();
                p._insertElement(token, NS.HTML);
                p.insertionMode = IN_FRAMESET_MODE;
            }
        }

        public static void addressStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            p._insertElement(token, NS.HTML);
        }

        public static void numberedHeaderStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            var tn = p.openElements.currentTagName;

            if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6)
                p.openElements.pop();

            p._insertElement(token, NS.HTML);
        }

        public static void preStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            p._insertElement(token, NS.HTML);
            //NOTE: If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move
            //on to the next one. (Newlines at the start of pre blocks are ignored as an authoring convenience.)
            p.skipNextNewLine = true;
            p.framesetOk = false;
        }

        public static void formStartTagInBody(Parser p, Token token)
        {
            var inTemplate = p.openElements.tmplCount > 0;

            if (!p.formElement.IsTruthy() || inTemplate)
            {
                if (p.openElements.hasInButtonScope(T.P))
                    p._closePElement();

                p._insertElement(token, NS.HTML);

                if (!inTemplate)
                    p.formElement = (Element) p.openElements.current;
            }
        }

        public static void listItemStartTagInBody(Parser p, Token token)
        {
            p.framesetOk = false;

            var tn = token.tagName;

            for (var i = p.openElements.stackTop; i >= 0; i--)
            {
                var element = p.openElements.items[i];
                string elementTn = p.treeAdapter.getTagName(element);
                string closeTn = null;

                if (tn == T.LI && elementTn == T.LI)
                    closeTn = T.LI;

                else if ((tn == T.DD || tn == T.DT) && (elementTn == T.DD || elementTn == T.DT))
                    closeTn = elementTn;

                if (closeTn.IsTruthy())
                {
                    p.openElements.generateImpliedEndTagsWithExclusion(closeTn);
                    p.openElements.popUntilTagNamePopped(closeTn);
                    break;
                }

                if (elementTn != T.ADDRESS && elementTn != T.DIV && elementTn != T.P && p._isSpecialElement(element))
                    break;
            }

            if (p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            p._insertElement(token, NS.HTML);
        }

        public static void plaintextStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            p._insertElement(token, NS.HTML);
            p.tokenizer.State = MODE.PLAINTEXT;
        }

        public static void buttonStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInScope(T.BUTTON))
            {
                p.openElements.generateImpliedEndTags();
                p.openElements.popUntilTagNamePopped(T.BUTTON);
            }

            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
            p.framesetOk = false;
        }

        public static void aStartTagInBody(Parser p, Token token)
        {
            var activeElementEntry = p.activeFormattingElements.GetElementEntryInScopeWithTagName(T.A);

            if (activeElementEntry.IsTruthy())
            {
                callAdoptionAgency(p, token);
                p.openElements.remove(activeElementEntry.Element);
                p.activeFormattingElements.RemoveEntry(activeElementEntry);
            }

            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
            p.activeFormattingElements.PushElement((Element) p.openElements.current, token);
        }

        public static void bStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
            p.activeFormattingElements.PushElement((Element) p.openElements.current, token);
        }

        public static void nobrStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();

            if (p.openElements.hasInScope(T.NOBR))
            {
                callAdoptionAgency(p, token);
                p._reconstructActiveFormattingElements();
            }

            p._insertElement(token, NS.HTML);
            p.activeFormattingElements.PushElement((Element) p.openElements.current, token);
        }

        public static void appletStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
            p.activeFormattingElements.InsertMarker();
            p.framesetOk = false;
        }

        public static void tableStartTagInBody(Parser p, Token token)
        {
            var mode = p.document is Document doc ? p.treeAdapter.getDocumentMode(doc) : null;
            if (mode != HTML.DOCUMENT_MODE.QUIRKS && p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            p._insertElement(token, NS.HTML);
            p.framesetOk = false;
            p.insertionMode = IN_TABLE_MODE;
        }

        public static void areaStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._appendElement(token, NS.HTML);
            p.framesetOk = false;
        }

        public static void inputStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._appendElement(token, NS.HTML);

            var inputType = Tokenizer.GetTokenAttr(token, ATTRS.TYPE);

            if (!inputType.IsTruthy() || inputType.toLowerCase() != HIDDEN_INPUT_TYPE)
                p.framesetOk = false;

        }

        public static void paramStartTagInBody(Parser p, Token token)
        {
            p._appendElement(token, NS.HTML);
        }

        public static void hrStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            if (p.openElements.currentTagName == T.MENUITEM)
                p.openElements.pop();

            p._appendElement(token, NS.HTML);
            p.framesetOk = false;
        }

        public static void imageStartTagInBody(Parser p, Token token)
        {
            token.tagName = T.IMG;
            areaStartTagInBody(p, token);
        }

        public static void textareaStartTagInBody(Parser p, Token token)
        {
            p._insertElement(token, NS.HTML);
            //NOTE: If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move
            //on to the next one. (Newlines at the start of textarea elements are ignored as an authoring convenience.)
            p.skipNextNewLine = true;
            p.tokenizer.State = MODE.RCDATA;
            p.originalInsertionMode = p.insertionMode;
            p.framesetOk = false;
            p.insertionMode = TEXT_MODE;
        }

        public static void xmpStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            p._reconstructActiveFormattingElements();
            p.framesetOk = false;
            p._switchToTextParsing(token, MODE.RAWTEXT);
        }

        public static void iframeStartTagInBody(Parser p, Token token)
        {
            p.framesetOk = false;
            p._switchToTextParsing(token, MODE.RAWTEXT);
        }

        //NOTE: here we assume that we always act as an user agent with enabled plugins, so we parse
        //<noembed> as a rawtext.
        public static void noembedStartTagInBody(Parser p, Token token)
        {
            p._switchToTextParsing(token, MODE.RAWTEXT);
        }

        public static void selectStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
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

        public static void optgroupStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.currentTagName == T.OPTION)
                p.openElements.pop();

            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
        }

        public static void rbStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInScope(T.RUBY))
                p.openElements.generateImpliedEndTags();

            p._insertElement(token, NS.HTML);
        }

        public static void rtStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInScope(T.RUBY))
                p.openElements.generateImpliedEndTagsWithExclusion(T.RTC);

            p._insertElement(token, NS.HTML);
        }

        public static void menuitemStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.currentTagName == T.MENUITEM)
                p.openElements.pop();

            // TODO needs clarification, see https://github.com/whatwg/html/pull/907/files#r73505877
            p._reconstructActiveFormattingElements();

            p._insertElement(token, NS.HTML);
        }

        public static void menuStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(T.P))
                p._closePElement();

            if (p.openElements.currentTagName == T.MENUITEM)
                p.openElements.pop();

            p._insertElement(token, NS.HTML);
        }

        public static void mathStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();

            AdjustTokenMathMlAttrs(token);
            AdjustTokenXmlAttrs(token);

            if (token.selfClosing)
                p._appendElement(token, NS.MATHML);
            else
                p._insertElement(token, NS.MATHML);
        }

        public static void svgStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();

            AdjustTokenSvgAttrs(token);
            AdjustTokenXmlAttrs(token);

            if (token.selfClosing)
                p._appendElement(token, NS.SVG);
            else
                p._insertElement(token, NS.SVG);
        }

        public static void genericStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
        }

        //OPTIMIZATION: Integer comparisons are low-cost, so we can use very fast tag name.Length filters here.
        //It's faster than using dictionary.
        public static void startTagInBody(Parser p, Token token)
        {
            var tn = token.tagName;

            switch (tn.Length)
            {
                case 1:
                    if (tn == T.I || tn == T.S || tn == T.B || tn == T.U)
                        bStartTagInBody(p, token);

                    else if (tn == T.P)
                        addressStartTagInBody(p, token);

                    else if (tn == T.A)
                        aStartTagInBody(p, token);

                    else
                        genericStartTagInBody(p, token);

                    break;

                case 2:
                    if (tn == T.DL || tn == T.OL || tn == T.UL)
                        addressStartTagInBody(p, token);

                    else if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6)
                        numberedHeaderStartTagInBody(p, token);

                    else if (tn == T.LI || tn == T.DD || tn == T.DT)
                        listItemStartTagInBody(p, token);

                    else if (tn == T.EM || tn == T.TT)
                        bStartTagInBody(p, token);

                    else if (tn == T.BR)
                        areaStartTagInBody(p, token);

                    else if (tn == T.HR)
                        hrStartTagInBody(p, token);

                    else if (tn == T.RB)
                        rbStartTagInBody(p, token);

                    else if (tn == T.RT || tn == T.RP)
                        rtStartTagInBody(p, token);

                    else if (tn != T.TH && tn != T.TD && tn != T.TR)
                        genericStartTagInBody(p, token);

                    break;

                case 3:
                    if (tn == T.DIV || tn == T.DIR || tn == T.NAV)
                        addressStartTagInBody(p, token);

                    else if (tn == T.PRE)
                        preStartTagInBody(p, token);

                    else if (tn == T.BIG)
                        bStartTagInBody(p, token);

                    else if (tn == T.IMG || tn == T.WBR)
                        areaStartTagInBody(p, token);

                    else if (tn == T.XMP)
                        xmpStartTagInBody(p, token);

                    else if (tn == T.SVG)
                        svgStartTagInBody(p, token);

                    else if (tn == T.RTC)
                        rbStartTagInBody(p, token);

                    else if (tn != T.COL)
                        genericStartTagInBody(p, token);

                    break;

                case 4:
                    if (tn == T.HTML)
                        htmlStartTagInBody(p, token);

                    else if (tn == T.BASE || tn == T.LINK || tn == T.META)
                        startTagInHead(p, token);

                    else if (tn == T.BODY)
                        bodyStartTagInBody(p, token);

                    else if (tn == T.MAIN)
                        addressStartTagInBody(p, token);

                    else if (tn == T.FORM)
                        formStartTagInBody(p, token);

                    else if (tn == T.CODE || tn == T.FONT)
                        bStartTagInBody(p, token);

                    else if (tn == T.NOBR)
                        nobrStartTagInBody(p, token);

                    else if (tn == T.AREA)
                        areaStartTagInBody(p, token);

                    else if (tn == T.MATH)
                        mathStartTagInBody(p, token);

                    else if (tn == T.MENU)
                        menuStartTagInBody(p, token);

                    else if (tn != T.HEAD)
                        genericStartTagInBody(p, token);

                    break;

                case 5:
                    if (tn == T.STYLE || tn == T.TITLE)
                        startTagInHead(p, token);

                    else if (tn == T.ASIDE)
                        addressStartTagInBody(p, token);

                    else if (tn == T.SMALL)
                        bStartTagInBody(p, token);

                    else if (tn == T.TABLE)
                        tableStartTagInBody(p, token);

                    else if (tn == T.EMBED)
                        areaStartTagInBody(p, token);

                    else if (tn == T.INPUT)
                        inputStartTagInBody(p, token);

                    else if (tn == T.PARAM || tn == T.TRACK)
                        paramStartTagInBody(p, token);

                    else if (tn == T.IMAGE)
                        imageStartTagInBody(p, token);

                    else if (tn != T.FRAME && tn != T.TBODY && tn != T.TFOOT && tn != T.THEAD)
                        genericStartTagInBody(p, token);

                    break;

                case 6:
                    if (tn == T.SCRIPT)
                        startTagInHead(p, token);

                    else if (tn == T.CENTER || tn == T.FIGURE || tn == T.FOOTER || tn == T.HEADER || tn == T.HGROUP)
                        addressStartTagInBody(p, token);

                    else if (tn == T.BUTTON)
                        buttonStartTagInBody(p, token);

                    else if (tn == T.STRIKE || tn == T.STRONG)
                        bStartTagInBody(p, token);

                    else if (tn == T.APPLET || tn == T.OBJECT)
                        appletStartTagInBody(p, token);

                    else if (tn == T.KEYGEN)
                        areaStartTagInBody(p, token);

                    else if (tn == T.SOURCE)
                        paramStartTagInBody(p, token);

                    else if (tn == T.IFRAME)
                        iframeStartTagInBody(p, token);

                    else if (tn == T.SELECT)
                        selectStartTagInBody(p, token);

                    else if (tn == T.OPTION)
                        optgroupStartTagInBody(p, token);

                    else
                        genericStartTagInBody(p, token);

                    break;

                case 7:
                    if (tn == T.BGSOUND)
                        startTagInHead(p, token);

                    else if (tn == T.DETAILS || tn == T.ADDRESS || tn == T.ARTICLE || tn == T.SECTION || tn == T.SUMMARY)
                        addressStartTagInBody(p, token);

                    else if (tn == T.LISTING)
                        preStartTagInBody(p, token);

                    else if (tn == T.MARQUEE)
                        appletStartTagInBody(p, token);

                    else if (tn == T.NOEMBED)
                        noembedStartTagInBody(p, token);

                    else if (tn != T.CAPTION)
                        genericStartTagInBody(p, token);

                    break;

                case 8:
                    if (tn == T.BASEFONT)
                        startTagInHead(p, token);

                    else if (tn == T.MENUITEM)
                        menuitemStartTagInBody(p, token);

                    else if (tn == T.FRAMESET)
                        framesetStartTagInBody(p, token);

                    else if (tn == T.FIELDSET)
                        addressStartTagInBody(p, token);

                    else if (tn == T.TEXTAREA)
                        textareaStartTagInBody(p, token);

                    else if (tn == T.TEMPLATE)
                        startTagInHead(p, token);

                    else if (tn == T.NOSCRIPT)
                        noembedStartTagInBody(p, token);

                    else if (tn == T.OPTGROUP)
                        optgroupStartTagInBody(p, token);

                    else if (tn != T.COLGROUP)
                        genericStartTagInBody(p, token);

                    break;

                case 9:
                    if (tn == T.PLAINTEXT)
                        plaintextStartTagInBody(p, token);

                    else
                        genericStartTagInBody(p, token);

                    break;

                case 10:
                    if (tn == T.BLOCKQUOTE || tn == T.FIGCAPTION)
                        addressStartTagInBody(p, token);

                    else
                        genericStartTagInBody(p, token);

                    break;

                default:
                    genericStartTagInBody(p, token);
                    break;
            }
        }

        public static void bodyEndTagInBody(Parser p)
        {
            if (p.openElements.hasInScope(T.BODY))
                p.insertionMode = AFTER_BODY_MODE;
        }

        public static void bodyEndTagInBody(Parser p, Token token)
        {
            bodyEndTagInBody(p);
        }

        public static void htmlEndTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInScope(T.BODY))
            {
                p.insertionMode = AFTER_BODY_MODE;
                p._processToken(token);
            }
        }

        public static void addressEndTagInBody(Parser p, Token token)
        {
            var tn = token.tagName;

            if (p.openElements.hasInScope(tn))
            {
                p.openElements.generateImpliedEndTags();
                p.openElements.popUntilTagNamePopped(tn);
            }
        }

        public static void formEndTagInBody(Parser p)
        {
            var inTemplate = p.openElements.tmplCount > 0;
            var formElement = p.formElement;

            if (!inTemplate)
                p.formElement = null;

            if ((formElement.IsTruthy() || inTemplate) && p.openElements.hasInScope(T.FORM))
            {
                p.openElements.generateImpliedEndTags();

                if (inTemplate)
                    p.openElements.popUntilTagNamePopped(T.FORM);

                else
                    p.openElements.remove(formElement);
            }
        }

        public static void formEndTagInBody(Parser p, Token token)
        {
            formEndTagInBody(p);
        }

        public static void pEndTagInBody(Parser p)
        {
            if (!p.openElements.hasInButtonScope(T.P))
                p._insertFakeElement(T.P);

            p._closePElement();
        }

        static void pEndTagInBody(Parser p, Token token)
        {
            pEndTagInBody(p);
        }

        static void liEndTagInBody(Parser p)
        {
            if (p.openElements.hasInListItemScope(T.LI))
            {
                p.openElements.generateImpliedEndTagsWithExclusion(T.LI);
                p.openElements.popUntilTagNamePopped(T.LI);
            }
        }

        static void liEndTagInBody(Parser p, Token token)
        {
            liEndTagInBody(p);
        }

        static void ddEndTagInBody(Parser p, Token token)
        {
            var tn = token.tagName;

            if (p.openElements.hasInScope(tn))
            {
                p.openElements.generateImpliedEndTagsWithExclusion(tn);
                p.openElements.popUntilTagNamePopped(tn);
            }
        }

        static void numberedHeaderEndTagInBody(Parser p)
        {
            if (p.openElements.hasNumberedHeaderInScope())
            {
                p.openElements.generateImpliedEndTags();
                p.openElements.popUntilNumberedHeaderPopped();
            }
        }

        static void numberedHeaderEndTagInBody(Parser p, Token token)
        {
            numberedHeaderEndTagInBody(p);
        }

        static void appletEndTagInBody(Parser p, Token token)
        {
            var tn = token.tagName;

            if (p.openElements.hasInScope(tn))
            {
                p.openElements.generateImpliedEndTags();
                p.openElements.popUntilTagNamePopped(tn);
                p.activeFormattingElements.ClearToLastMarker();
            }
        }

        static void brEndTagInBody(Parser p)
        {
            p._reconstructActiveFormattingElements();
            p._insertFakeElement(T.BR);
            p.openElements.pop();
            p.framesetOk = false;
        }

        static void brEndTagInBody(Parser p, Token token)
        {
            brEndTagInBody(p);
        }

        static void genericEndTagInBody(Parser p, Token token)
        {
            var tn = token.tagName;

            for (var i = p.openElements.stackTop; i > 0; i--)
            {
                var element = p.openElements.items[i];

                if (p.treeAdapter.getTagName(element) == tn)
                {
                    p.openElements.generateImpliedEndTagsWithExclusion(tn);
                    p.openElements.popUntilElementPopped(element);
                    break;
                }

                if (p._isSpecialElement(element))
                    break;
            }
        }

        //OPTIMIZATION: Integer comparisons are low-cost, so we can use very fast tag name.Length filters here.
        //It's faster than using dictionary.
        public static void endTagInBody(Parser p, Token token)
        {
            var tn = token.tagName;

            switch (tn.Length)
            {
                case 1:
                    if (tn == T.A || tn == T.B || tn == T.I || tn == T.S || tn == T.U)
                        callAdoptionAgency(p, token);

                    else if (tn == T.P)
                        pEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 2:
                    if (tn == T.DL || tn == T.UL || tn == T.OL)
                        addressEndTagInBody(p, token);

                    else if (tn == T.LI)
                        liEndTagInBody(p, token);

                    else if (tn == T.DD || tn == T.DT)
                        ddEndTagInBody(p, token);

                    else if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6)
                        numberedHeaderEndTagInBody(p, token);

                    else if (tn == T.BR)
                        brEndTagInBody(p, token);

                    else if (tn == T.EM || tn == T.TT)
                        callAdoptionAgency(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 3:
                    if (tn == T.BIG)
                        callAdoptionAgency(p, token);

                    else if (tn == T.DIR || tn == T.DIV || tn == T.NAV)
                        addressEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 4:
                    if (tn == T.BODY)
                        bodyEndTagInBody(p, token);

                    else if (tn == T.HTML)
                        htmlEndTagInBody(p, token);

                    else if (tn == T.FORM)
                        formEndTagInBody(p, token);

                    else if (tn == T.CODE || tn == T.FONT || tn == T.NOBR)
                        callAdoptionAgency(p, token);

                    else if (tn == T.MAIN || tn == T.MENU)
                        addressEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 5:
                    if (tn == T.ASIDE)
                        addressEndTagInBody(p, token);

                    else if (tn == T.SMALL)
                        callAdoptionAgency(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 6:
                    if (tn == T.CENTER || tn == T.FIGURE || tn == T.FOOTER || tn == T.HEADER || tn == T.HGROUP)
                        addressEndTagInBody(p, token);

                    else if (tn == T.APPLET || tn == T.OBJECT)
                        appletEndTagInBody(p, token);

                    else if (tn == T.STRIKE || tn == T.STRONG)
                        callAdoptionAgency(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 7:
                    if (tn == T.ADDRESS || tn == T.ARTICLE || tn == T.DETAILS || tn == T.SECTION || tn == T.SUMMARY)
                        addressEndTagInBody(p, token);

                    else if (tn == T.MARQUEE)
                        appletEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 8:
                    if (tn == T.FIELDSET)
                        addressEndTagInBody(p, token);

                    else if (tn == T.TEMPLATE)
                        endTagInHead(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 10:
                    if (tn == T.BLOCKQUOTE || tn == T.FIGCAPTION)
                        addressEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                default:
                    genericEndTagInBody(p, token);
                    break;
            }
        }


        public static void eofInBody(Parser p, Token token)
        {
            if (p.tmplInsertionModeStackTop > -1)
                eofInTemplate(p, token);

            else
                p.stopped = true;
        }

        //12.2.5.4.8 The "text" insertion mode
        //------------------------------------------------------------------
        public static void endTagInText(Parser p, Token token)
        {
            if (token.tagName == T.SCRIPT)
                p.pendingScript = (Element) p.openElements.current;

            p.openElements.pop();
            p.insertionMode = p.originalInsertionMode;
        }


        public static void eofInText(Parser p, Token token)
        {
            p.openElements.pop();
            p.insertionMode = p.originalInsertionMode;
            p._processToken(token);
        }


        //12.2.5.4.9 The "in table" insertion mode
        //------------------------------------------------------------------
        public static void characterInTable(Parser p, Token token)
        {
            var curTn = p.openElements.currentTagName;

            if (curTn == T.TABLE || curTn == T.TBODY || curTn == T.TFOOT || curTn == T.THEAD || curTn == T.TR)
            {
                p.pendingCharacterTokens = new Extensions.List<Token>();
                p.hasNonWhitespacePendingCharacterToken = false;
                p.originalInsertionMode = p.insertionMode;
                p.insertionMode = IN_TABLE_TEXT_MODE;
                p._processToken(token);
            }

            else
                tokenInTable(p, token);
        }

        public static void captionStartTagInTable(Parser p, Token token)
        {
            p.openElements.clearBackToTableContext();
            p.activeFormattingElements.InsertMarker();
            p._insertElement(token, NS.HTML);
            p.insertionMode = IN_CAPTION_MODE;
        }

        public static void colgroupStartTagInTable(Parser p, Token token)
        {
            p.openElements.clearBackToTableContext();
            p._insertElement(token, NS.HTML);
            p.insertionMode = IN_COLUMN_GROUP_MODE;
        }

        public static void colStartTagInTable(Parser p, Token token)
        {
            p.openElements.clearBackToTableContext();
            p._insertFakeElement(T.COLGROUP);
            p.insertionMode = IN_COLUMN_GROUP_MODE;
            p._processToken(token);
        }

        public static void tbodyStartTagInTable(Parser p, Token token)
        {
            p.openElements.clearBackToTableContext();
            p._insertElement(token, NS.HTML);
            p.insertionMode = IN_TABLE_BODY_MODE;
        }

        public static void tdStartTagInTable(Parser p, Token token)
        {
            p.openElements.clearBackToTableContext();
            p._insertFakeElement(T.TBODY);
            p.insertionMode = IN_TABLE_BODY_MODE;
            p._processToken(token);
        }

        public static void tableStartTagInTable(Parser p, Token token)
        {
            if (p.openElements.hasInTableScope(T.TABLE))
            {
                p.openElements.popUntilTagNamePopped(T.TABLE);
                p._resetInsertionMode();
                p._processToken(token);
            }
        }

        public static void inputStartTagInTable(Parser p, Token token)
        {
            var inputType = Tokenizer.GetTokenAttr(token, ATTRS.TYPE);

            if (inputType.IsTruthy() && inputType.toLowerCase() == HIDDEN_INPUT_TYPE)
                p._appendElement(token, NS.HTML);

            else
                tokenInTable(p, token);
        }

        public static void formStartTagInTable(Parser p, Token token)
        {
            if (!p.formElement.IsTruthy() && p.openElements.tmplCount == 0)
            {
                p._insertElement(token, NS.HTML);
                p.formElement = (Element) p.openElements.current;
                p.openElements.pop();
            }
        }

        public static void startTagInTable(Parser p, Token token)
        {
            var tn = token.tagName;

            switch (tn.Length)
            {
                case 2:
                    if (tn == T.TD || tn == T.TH || tn == T.TR)
                        tdStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 3:
                    if (tn == T.COL)
                        colStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 4:
                    if (tn == T.FORM)
                        formStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 5:
                    if (tn == T.TABLE)
                        tableStartTagInTable(p, token);

                    else if (tn == T.STYLE)
                        startTagInHead(p, token);

                    else if (tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD)
                        tbodyStartTagInTable(p, token);

                    else if (tn == T.INPUT)
                        inputStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 6:
                    if (tn == T.SCRIPT)
                        startTagInHead(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 7:
                    if (tn == T.CAPTION)
                        captionStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 8:
                    if (tn == T.COLGROUP)
                        colgroupStartTagInTable(p, token);

                    else if (tn == T.TEMPLATE)
                        startTagInHead(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                default:
                    tokenInTable(p, token);
                    break;
            }

        }

        public static void endTagInTable(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.TABLE)
            {
                if (p.openElements.hasInTableScope(T.TABLE))
                {
                    p.openElements.popUntilTagNamePopped(T.TABLE);
                    p._resetInsertionMode();
                }
            }

            else if (tn == T.TEMPLATE)
                endTagInHead(p, token);

            else if (tn != T.BODY && tn != T.CAPTION && tn != T.COL && tn != T.COLGROUP && tn != T.HTML &&
                     tn != T.TBODY && tn != T.TD && tn != T.TFOOT && tn != T.TH && tn != T.THEAD && tn != T.TR)
                tokenInTable(p, token);
        }

        public static void tokenInTable(Parser p, Token token)
        {
            var savedFosterParentingState = p.fosterParentingEnabled;

            p.fosterParentingEnabled = true;
            p._processTokenInBodyMode(token);
            p.fosterParentingEnabled = savedFosterParentingState;
        }


        //12.2.5.4.10 The "in table text" insertion mode
        //------------------------------------------------------------------
        public static void whitespaceCharacterInTableText(Parser p, Token token)
        {
            p.pendingCharacterTokens.push(token);
        }

        public static void characterInTableText(Parser p, Token token)
        {
            p.pendingCharacterTokens.push(token);
            p.hasNonWhitespacePendingCharacterToken = true;
        }

        public static void tokenInTableText(Parser p, Token token)
        {
            var i = 0;

            if (p.hasNonWhitespacePendingCharacterToken)
            {
                for (; i < p.pendingCharacterTokens.length; i++)
                    tokenInTable(p, p.pendingCharacterTokens[i]);
            }

            else
            {
                for (; i < p.pendingCharacterTokens.length; i++)
                    p._insertCharacters(p.pendingCharacterTokens[i]);
            }

            p.insertionMode = p.originalInsertionMode;
            p._processToken(token);
        }


        //12.2.5.4.11 The "in caption" insertion mode
        //------------------------------------------------------------------
        public static void startTagInCaption(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.CAPTION || tn == T.COL || tn == T.COLGROUP || tn == T.TBODY ||
                tn == T.TD || tn == T.TFOOT || tn == T.TH || tn == T.THEAD || tn == T.TR)
            {
                if (p.openElements.hasInTableScope(T.CAPTION))
                {
                    p.openElements.generateImpliedEndTags();
                    p.openElements.popUntilTagNamePopped(T.CAPTION);
                    p.activeFormattingElements.ClearToLastMarker();
                    p.insertionMode = IN_TABLE_MODE;
                    p._processToken(token);
                }
            }

            else
                startTagInBody(p, token);
        }

        public static void endTagInCaption(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.CAPTION || tn == T.TABLE)
            {
                if (p.openElements.hasInTableScope(T.CAPTION))
                {
                    p.openElements.generateImpliedEndTags();
                    p.openElements.popUntilTagNamePopped(T.CAPTION);
                    p.activeFormattingElements.ClearToLastMarker();
                    p.insertionMode = IN_TABLE_MODE;

                    if (tn == T.TABLE)
                        p._processToken(token);
                }
            }

            else if (tn != T.BODY && tn != T.COL && tn != T.COLGROUP && tn != T.HTML && tn != T.TBODY &&
                     tn != T.TD && tn != T.TFOOT && tn != T.TH && tn != T.THEAD && tn != T.TR)
                endTagInBody(p, token);
        }


        //12.2.5.4.12 The "in column group" insertion mode
        //------------------------------------------------------------------
        public static void startTagInColumnGroup(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML)
                startTagInBody(p, token);

            else if (tn == T.COL)
                p._appendElement(token, NS.HTML);

            else if (tn == T.TEMPLATE)
                startTagInHead(p, token);

            else
                tokenInColumnGroup(p, token);
        }

        public static void endTagInColumnGroup(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.COLGROUP)
            {
                if (p.openElements.currentTagName == T.COLGROUP)
                {
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_MODE;
                }
            }

            else if (tn == T.TEMPLATE)
                endTagInHead(p, token);

            else if (tn != T.COL)
                tokenInColumnGroup(p, token);
        }

        public static void tokenInColumnGroup(Parser p, Token token)
        {
            if (p.openElements.currentTagName == T.COLGROUP)
            {
                p.openElements.pop();
                p.insertionMode = IN_TABLE_MODE;
                p._processToken(token);
            }
        }

        //12.2.5.4.13 The "in table body" insertion mode
        //------------------------------------------------------------------
        public static void startTagInTableBody(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.TR)
            {
                p.openElements.clearBackToTableBodyContext();
                p._insertElement(token, NS.HTML);
                p.insertionMode = IN_ROW_MODE;
            }

            else if (tn == T.TH || tn == T.TD)
            {
                p.openElements.clearBackToTableBodyContext();
                p._insertFakeElement(T.TR);
                p.insertionMode = IN_ROW_MODE;
                p._processToken(token);
            }

            else if (tn == T.CAPTION || tn == T.COL || tn == T.COLGROUP ||
                     tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD)
            {

                if (p.openElements.hasTableBodyContextInTableScope())
                {
                    p.openElements.clearBackToTableBodyContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_MODE;
                    p._processToken(token);
                }
            }

            else
                startTagInTable(p, token);
        }

        public static void endTagInTableBody(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD)
            {
                if (p.openElements.hasInTableScope(tn))
                {
                    p.openElements.clearBackToTableBodyContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_MODE;
                }
            }

            else if (tn == T.TABLE)
            {
                if (p.openElements.hasTableBodyContextInTableScope())
                {
                    p.openElements.clearBackToTableBodyContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_MODE;
                    p._processToken(token);
                }
            }

            else if (tn != T.BODY && tn != T.CAPTION && tn != T.COL && tn != T.COLGROUP ||
                     tn != T.HTML && tn != T.TD && tn != T.TH && tn != T.TR)
                endTagInTable(p, token);
        }

        //12.2.5.4.14 The "in row" insertion mode
        //------------------------------------------------------------------
        public static void startTagInRow(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.TH || tn == T.TD)
            {
                p.openElements.clearBackToTableRowContext();
                p._insertElement(token, NS.HTML);
                p.insertionMode = IN_CELL_MODE;
                p.activeFormattingElements.InsertMarker();
            }

            else if (tn == T.CAPTION || tn == T.COL || tn == T.COLGROUP || tn == T.TBODY ||
                     tn == T.TFOOT || tn == T.THEAD || tn == T.TR)
            {
                if (p.openElements.hasInTableScope(T.TR))
                {
                    p.openElements.clearBackToTableRowContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                    p._processToken(token);
                }
            }

            else
                startTagInTable(p, token);
        }

        public static void endTagInRow(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.TR)
            {
                if (p.openElements.hasInTableScope(T.TR))
                {
                    p.openElements.clearBackToTableRowContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                }
            }

            else if (tn == T.TABLE)
            {
                if (p.openElements.hasInTableScope(T.TR))
                {
                    p.openElements.clearBackToTableRowContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                    p._processToken(token);
                }
            }

            else if (tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD)
            {
                if (p.openElements.hasInTableScope(tn) || p.openElements.hasInTableScope(T.TR))
                {
                    p.openElements.clearBackToTableRowContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                    p._processToken(token);
                }
            }

            else if (tn != T.BODY && tn != T.CAPTION && tn != T.COL && tn != T.COLGROUP ||
                     tn != T.HTML && tn != T.TD && tn != T.TH)
                endTagInTable(p, token);
        }


        //12.2.5.4.15 The "in cell" insertion mode
        //------------------------------------------------------------------
        public static void startTagInCell(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.CAPTION || tn == T.COL || tn == T.COLGROUP || tn == T.TBODY ||
                tn == T.TD || tn == T.TFOOT || tn == T.TH || tn == T.THEAD || tn == T.TR)
            {

                if (p.openElements.hasInTableScope(T.TD) || p.openElements.hasInTableScope(T.TH))
                {
                    p._closeTableCell();
                    p._processToken(token);
                }
            }

            else
                startTagInBody(p, token);
        }

        public static void endTagInCell(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.TD || tn == T.TH)
            {
                if (p.openElements.hasInTableScope(tn))
                {
                    p.openElements.generateImpliedEndTags();
                    p.openElements.popUntilTagNamePopped(tn);
                    p.activeFormattingElements.ClearToLastMarker();
                    p.insertionMode = IN_ROW_MODE;
                }
            }

            else if (tn == T.TABLE || tn == T.TBODY || tn == T.TFOOT || tn == T.THEAD || tn == T.TR)
            {
                if (p.openElements.hasInTableScope(tn))
                {
                    p._closeTableCell();
                    p._processToken(token);
                }
            }

            else if (tn != T.BODY && tn != T.CAPTION && tn != T.COL && tn != T.COLGROUP && tn != T.HTML)
                endTagInBody(p, token);
        }

        //12.2.5.4.16 The "in select" insertion mode
        //------------------------------------------------------------------
        public static void startTagInSelect(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML)
                startTagInBody(p, token);

            else if (tn == T.OPTION)
            {
                if (p.openElements.currentTagName == T.OPTION)
                    p.openElements.pop();

                p._insertElement(token, NS.HTML);
            }

            else if (tn == T.OPTGROUP)
            {
                if (p.openElements.currentTagName == T.OPTION)
                    p.openElements.pop();

                if (p.openElements.currentTagName == T.OPTGROUP)
                    p.openElements.pop();

                p._insertElement(token, NS.HTML);
            }

            else if (tn == T.INPUT || tn == T.KEYGEN || tn == T.TEXTAREA || tn == T.SELECT)
            {
                if (p.openElements.hasInSelectScope(T.SELECT))
                {
                    p.openElements.popUntilTagNamePopped(T.SELECT);
                    p._resetInsertionMode();

                    if (tn != T.SELECT)
                        p._processToken(token);
                }
            }

            else if (tn == T.SCRIPT || tn == T.TEMPLATE)
                startTagInHead(p, token);
        }

        public static void endTagInSelect(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.OPTGROUP)
            {
                var prevOpenElement = p.openElements.items[p.openElements.stackTop - 1];
                var prevOpenElementTn = // prevOpenElement && p.treeAdapter.getTagName(prevOpenElement)
                                        prevOpenElement.IsTruthy() ? p.treeAdapter.getTagName(prevOpenElement) : null;

                if (p.openElements.currentTagName == T.OPTION && prevOpenElementTn == T.OPTGROUP)
                    p.openElements.pop();

                if (p.openElements.currentTagName == T.OPTGROUP)
                    p.openElements.pop();
            }

            else if (tn == T.OPTION)
            {
                if (p.openElements.currentTagName == T.OPTION)
                    p.openElements.pop();
            }

            else if (tn == T.SELECT && p.openElements.hasInSelectScope(T.SELECT))
            {
                p.openElements.popUntilTagNamePopped(T.SELECT);
                p._resetInsertionMode();
            }

            else if (tn == T.TEMPLATE)
                endTagInHead(p, token);
        }

        //12.2.5.4.17 The "in select in table" insertion mode
        //------------------------------------------------------------------
        public static void startTagInSelectInTable(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.CAPTION || tn == T.TABLE || tn == T.TBODY || tn == T.TFOOT ||
                tn == T.THEAD || tn == T.TR || tn == T.TD || tn == T.TH)
            {
                p.openElements.popUntilTagNamePopped(T.SELECT);
                p._resetInsertionMode();
                p._processToken(token);
            }

            else
                startTagInSelect(p, token);
        }

        public static void endTagInSelectInTable(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.CAPTION || tn == T.TABLE || tn == T.TBODY || tn == T.TFOOT ||
                tn == T.THEAD || tn == T.TR || tn == T.TD || tn == T.TH)
            {
                if (p.openElements.hasInTableScope(tn))
                {
                    p.openElements.popUntilTagNamePopped(T.SELECT);
                    p._resetInsertionMode();
                    p._processToken(token);
                }
            }

            else
                endTagInSelect(p, token);
        }

        //12.2.5.4.18 The "in template" insertion mode
        //------------------------------------------------------------------
        public static void startTagInTemplate(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.BASE || tn == T.BASEFONT || tn == T.BGSOUND || tn == T.LINK || tn == T.META ||
                tn == T.NOFRAMES || tn == T.SCRIPT || tn == T.STYLE || tn == T.TEMPLATE || tn == T.TITLE)
                startTagInHead(p, token);

            else
            {
                var newInsertionMode = TEMPLATE_INSERTION_MODE_SWITCH_MAP.TryGetValue(tn, out var result) ? result : IN_BODY_MODE; // || operator

                p._popTmplInsertionMode();
                p._pushTmplInsertionMode(newInsertionMode);
                p.insertionMode = newInsertionMode;
                p._processToken(token);
            }
        }

        public static void endTagInTemplate(Parser p, Token token)
        {
            if (token.tagName == T.TEMPLATE)
                endTagInHead(p, token);
        }

        public static void eofInTemplate(Parser p, Token token)
        {
            if (p.openElements.tmplCount > 0)
            {
                p.openElements.popUntilTagNamePopped(T.TEMPLATE);
                p.activeFormattingElements.ClearToLastMarker();
                p._popTmplInsertionMode();
                p._resetInsertionMode();
                p._processToken(token);
            }

            else
                p.stopped = true;
        }


        //12.2.5.4.19 The "after body" insertion mode
        //------------------------------------------------------------------
        public static void startTagAfterBody(Parser p, Token token)
        {
            if (token.tagName == T.HTML)
                startTagInBody(p, token);

            else
                tokenAfterBody(p, token);
        }

        public static void endTagAfterBody(Parser p, Token token)
        {
            if (token.tagName == T.HTML)
            {
                if (!p.fragmentContext.IsTruthy())
                    p.insertionMode = AFTER_AFTER_BODY_MODE;
            }

            else
                tokenAfterBody(p, token);
        }

        public static void tokenAfterBody(Parser p, Token token)
        {
            p.insertionMode = IN_BODY_MODE;
            p._processToken(token);
        }

        //12.2.5.4.20 The "in frameset" insertion mode
        //------------------------------------------------------------------
        public static void startTagInFrameset(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML)
                startTagInBody(p, token);

            else if (tn == T.FRAMESET)
                p._insertElement(token, NS.HTML);

            else if (tn == T.FRAME)
                p._appendElement(token, NS.HTML);

            else if (tn == T.NOFRAMES)
                startTagInHead(p, token);
        }

        public static void endTagInFrameset(Parser p, Token token)
        {
            if (token.tagName == T.FRAMESET && !p.openElements.isRootHtmlElementCurrent())
            {
                p.openElements.pop();

                if (!p.fragmentContext.IsTruthy() && p.openElements.currentTagName != T.FRAMESET)
                    p.insertionMode = AFTER_FRAMESET_MODE;
            }
        }

        //12.2.5.4.21 The "after frameset" insertion mode
        //------------------------------------------------------------------
        public static void startTagAfterFrameset(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML)
                startTagInBody(p, token);

            else if (tn == T.NOFRAMES)
                startTagInHead(p, token);
        }

        public static void endTagAfterFrameset(Parser p, Token token)
        {
            if (token.tagName == T.HTML)
                p.insertionMode = AFTER_AFTER_FRAMESET_MODE;
        }

        //12.2.5.4.22 The "after after body" insertion mode
        //------------------------------------------------------------------
        public static void startTagAfterAfterBody(Parser p, Token token)
        {
            if (token.tagName == T.HTML)
                startTagInBody(p, token);

            else
                tokenAfterAfterBody(p, token);
        }

        public static void tokenAfterAfterBody(Parser p, Token token)
        {
            p.insertionMode = IN_BODY_MODE;
            p._processToken(token);
        }

        //12.2.5.4.23 The "after after frameset" insertion mode
        //------------------------------------------------------------------
        public static void startTagAfterAfterFrameset(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == T.HTML)
                startTagInBody(p, token);

            else if (tn == T.NOFRAMES)
                startTagInHead(p, token);
        }


        //12.2.5.5 The rules for parsing tokens in foreign content
        //------------------------------------------------------------------
        public static void nullCharacterInForeignContent(Parser p, Token token)
        {
            token.chars = Unicode.ReplacementCharacter.ToString();
            p._insertCharacters(token);
        }

        public static void characterInForeignContent(Parser p, Token token)
        {
            p._insertCharacters(token);
            p.framesetOk = false;
        }

        public static void startTagInForeignContent(Parser p, Token token)
        {
            if (CausesExit(token) && !p.fragmentContext.IsTruthy())
            {
                while (p.treeAdapter.getNamespaceURI((Element) p.openElements.current) != NS.HTML && !p._isIntegrationPoint((Element) p.openElements.current))
                    p.openElements.pop();

                p._processToken(token);
            }

            else
            {
                var current = (Element) p._getAdjustedCurrentElement();
                string currentNs = p.treeAdapter.getNamespaceURI(current);

                if (currentNs == NS.MATHML)
                    AdjustTokenMathMlAttrs(token);

                else if (currentNs == NS.SVG)
                {
                    AdjustTokenSvgTagName(token);
                    AdjustTokenSvgAttrs(token);
                }

                AdjustTokenXmlAttrs(token);

                if (token.selfClosing)
                    p._appendElement(token, currentNs);
                else
                    p._insertElement(token, currentNs);
            }
        }

        public static void endTagInForeignContent(Parser p, Token token)
        {
            for (var i = p.openElements.stackTop; i > 0; i--)
            {
                var element = p.openElements.items[i];

                if (p.treeAdapter.getNamespaceURI(element) == NS.HTML)
                {
                    p._processToken(token);
                    break;
                }

                if (p.treeAdapter.getTagName(element).toLowerCase() == token.tagName)
                {
                    p.openElements.popUntilElementPopped(element);
                    break;
                }
            }
        }
    }
}
