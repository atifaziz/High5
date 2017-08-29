using System;
using System.Text;
using static ParseFive.Tokenizer.Tokenizer;
using ɑ = HTML.TAG_NAMES;
using NS = HTML.NAMESPACES;
using ATTRS = HTML.ATTRS;
using static ParseFive.Tokenizer.Tokenizer.MODE;
using ParseFive.Tokenizer;
using ParseFive.Extensions;
using Attrs = ParseFive.Common.ForeignContent;
using static ParseFive.Parser.Index;
using static ParseFive.Common.ForeignContent;
using static ParseFive.Common.Doctype;
using UNICODE = ParseFive.Common.Unicode;
// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeThisQualifier

namespace ParseFive.Parser
{
    using TreeAdapters;

    public class Parser
    {
        readonly TreeAdapter treeAdapter;
        Element pendingScript;
        private string originalInsertionMode;
        private Element headElement;
        private Element formElement;
        private OpenElementStack openElements;
        private FormattingElementList activeFormattingElements;
        private List<string> tmplInsertionModeStack;
        private int tmplInsertionModeStackTop;
        private string currentTmplInsertionMode;
        private List<Token> pendingCharacterTokens;
        private bool hasNonWhitespacePendingCharacterToken;
        private bool framesetOk;
        private bool skipNextNewLine;
        private bool fosterParentingEnabled;

        Tokenizer.Tokenizer tokenizer;
        public bool stopped { get; private set; }
        public string insertionMode { get; private set; }
        private Node document { get; set; }
        public Node fragmentContext { get; private set; }

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
            this.tokenizer.write(html, true);
            this._runParsingLoop(null);

            return document;
        }

        public DocumentFragment parseFragment(string html, Node fragmentContext)
        {
            //NOTE: use <template> element as a fragment context if context element was not provided,
            //so we will parse in "forgiving" manner
            if (!fragmentContext.IsTruthy())
                fragmentContext = this.treeAdapter.createElement(ɑ.TEMPLATE, NS.HTML, new List<Attr>());

            //NOTE: create fake element which will be used as 'document' for fragment parsing.
            //This is important for jsdom there 'document' can't be recreated, therefore
            //fragment parsing causes messing of the main `document`.
            var documentMock = this.treeAdapter.createElement("documentmock", NS.HTML, new List<Attr>());

            this._bootstrap(documentMock, fragmentContext);

            if (this.treeAdapter.getTagName((Element) fragmentContext) == ɑ.TEMPLATE)
                this._pushTmplInsertionMode(IN_TEMPLATE_MODE);

            this._initTokenizerForFragmentParsing();
            this._insertFakeRootElement();
            this._resetInsertionMode();
            this._findFormInFragmentContext();
            this.tokenizer.write(html, true);
            this._runParsingLoop(null);

            var rootElement = (Element) this.treeAdapter.getFirstChild(documentMock);
            var fragment = this.treeAdapter.createDocumentFragment();

            this._adoptNodes(rootElement, fragment);

            return fragment;
        }

        //Bootstrap parser
        private void _bootstrap(Node document, Node fragmentContext)
        {
            this.tokenizer = new Tokenizer.Tokenizer();

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
        void _runParsingLoop(Action<Element> scriptHandler)
        {
            while (!this.stopped)
            {
                this._setupTokenizerCDATAMode();

                var token = this.tokenizer.getNextToken();

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

            this.tokenizer.allowCDATA = current.IsTruthy() && current != this.document &&
                                        this.treeAdapter.getNamespaceURI((Element)current) != NS.HTML && !this._isIntegrationPoint((Element)current);
        }

        void _switchToTextParsing(Token currentToken, string nextTokenizerState)
        {
            this._insertElement(currentToken, NS.HTML);
            this.tokenizer.state = nextTokenizerState;
            this.originalInsertionMode = this.insertionMode;
            this.insertionMode = TEXT_MODE;
        }

        void switchToPlaintextParsing()
        {
            this.insertionMode = TEXT_MODE;
            this.originalInsertionMode = IN_BODY_MODE;
            this.tokenizer.state = PLAINTEXT;
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
                if (this.treeAdapter.getTagName(node) == ɑ.FORM)
                {
                    this.formElement = node;
                    break;
                }

                node = (Element) this.treeAdapter.getParentNode(node);
            } while (node.IsTruthy()); //TODO
        }

        void _initTokenizerForFragmentParsing()
        {
            if (this.treeAdapter.getNamespaceURI((Element) this.fragmentContext) == NS.HTML)
            {
                var tn = this.treeAdapter.getTagName((Element) this.fragmentContext);

                if (tn == ɑ.TITLE || tn == ɑ.TEXTAREA)
                    this.tokenizer.state = RCDATA;

                else if (tn == ɑ.STYLE || tn == ɑ.XMP || tn == ɑ.IFRAME ||
                         tn == ɑ.NOEMBED || tn == ɑ.NOFRAMES || tn == ɑ.NOSCRIPT)
                    this.tokenizer.state = RAWTEXT;

                else if (tn == ɑ.SCRIPT)
                    this.tokenizer.state = SCRIPT_DATA;

                else if (tn == ɑ.PLAINTEXT)
                    this.tokenizer.state = PLAINTEXT;
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
                var parent = this.openElements.currentTmplContent ?? this.openElements.current; //TODO || operator

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
            var element = this.treeAdapter.createElement(tagName, NS.HTML, new List<Attr>());

            this._attachElementToTree(element);
            this.openElements.push(element);
        }

        void _insertTemplate(Token token)
        {
            var tmpl = this.treeAdapter.createElement(token.tagName, NS.HTML, token.attrs);
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
            var element = this.treeAdapter.createElement(ɑ.HTML, NS.HTML, new List<Attr>());

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
                var parent = this.openElements.currentTmplContent ?? this.openElements.current; //TODO || operator

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

            var current = (Element) current_; // TODO

            var ns = this.treeAdapter.getNamespaceURI(current);

            if (ns == NS.HTML)
                return false;

            if (this.treeAdapter.getTagName(current) == ɑ.ANNOTATION_XML && ns == NS.MATHML &&
                token.type == START_TAG_TOKEN && token.tagName == ɑ.SVG)
                return false;

            var isCharacterToken = token.type == CHARACTER_TOKEN ||
                                   token.type == NULL_CHARACTER_TOKEN ||
                                   token.type == WHITESPACE_CHARACTER_TOKEN;
            var isMathMLTextStartTag = token.type == START_TAG_TOKEN &&
                                       token.tagName != ɑ.MGLYPH &&
                                       token.tagName != ɑ.MALIGNMARK;

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

            return isIntegrationPoint(tn, ns, attrs, foreignNS);
        }

        bool _isIntegrationPoint(Element element)
        {
            return _isIntegrationPoint(element, "");
        }

        //Active formatting elements reconstruction
        void _reconstructActiveFormattingElements()
        {
            int listLength = this.activeFormattingElements.length;

            if (listLength.IsTruthy())
            {
                var unopenIdx = listLength;
                IEntry entry;

                do
                {
                    unopenIdx--;
                    entry = this.activeFormattingElements.entries[unopenIdx];

                    if (entry.type == FormattingElementList.MARKER_ENTRY || this.openElements.contains(entry.element))
                    {
                        unopenIdx++;
                        break;
                    }
                } while (unopenIdx > 0);

                for (var i = unopenIdx; i < listLength; i++)
                {
                    entry = this.activeFormattingElements.entries[i];
                    this._insertElement(entry.token, this.treeAdapter.getNamespaceURI(entry.element));
                    entry.element = (Element) this.openElements.current;
                }
            }
        }

        //Close elements
        void _closeTableCell()
        {
            this.openElements.generateImpliedEndTags();
            this.openElements.popUntilTableCellPopped();
            this.activeFormattingElements.clearToLastMarker();
            this.insertionMode = IN_ROW_MODE;
        }

        void _closePElement()
        {
            this.openElements.generateImpliedEndTagsWithExclusion(ɑ.P);
            this.openElements.popUntilTagNamePopped(ɑ.P);
        }

        //Insertion modes
        void _resetInsertionMode()
        {
            bool last = false;
            for (int i = this.openElements.items.length; i >= 0; i--) //this.openElements.stackTop
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

                else if (!last && (tn == ɑ.TD || tn == ɑ.TH))
                {
                    this.insertionMode = IN_CELL_MODE;
                    break;
                }

                else if (!last && tn == ɑ.HEAD)
                {
                    this.insertionMode = IN_HEAD_MODE;
                    break;
                }

                else if (tn == ɑ.SELECT)
                {
                    this._resetInsertionModeForSelect(i);
                    break;
                }

                else if (tn == ɑ.TEMPLATE)
                {
                    this.insertionMode = this.currentTmplInsertionMode;
                    break;
                }

                else if (tn == ɑ.HTML)
                {
                    this.insertionMode = this.headElement.IsTruthy() ? AFTER_HEAD_MODE : BEFORE_HEAD_MODE; //TODO
                    break;
                }

                else if (last.IsTruthy())
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

                    if (tn == ɑ.TEMPLATE)
                        break;

                    else if (tn == ɑ.TABLE)
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
            this.currentTmplInsertionMode = this.tmplInsertionModeStack[this.tmplInsertionModeStackTop];
        }

        //Foster parenting
        bool _isElementCausesFosterParenting(Element element)
        {
            var tn = this.treeAdapter.getTagName(element);

            return tn == ɑ.TABLE || tn == ɑ.TBODY || tn == ɑ.TFOOT || tn == ɑ.THEAD || tn == ɑ.TR;
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

                if (tn == ɑ.TEMPLATE && ns == NS.HTML)
                {
                    location.parent = this.treeAdapter.getTemplateContent(openElement);
                    break;
                }

                else if (tn == ɑ.TABLE)
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

            return HTML.SPECIAL_ELEMENTS[ns][tn];
        }

        //Adoption agency algorithm
        //(see: http://www.whatwg.org/specs/web-apps/current-work/multipage/tree-construction.html#adoptionAgency)
        //------------------------------------------------------------------

        //Steps 5-8 of the algorithm
        static IEntry aaObtainFormattingElementEntry(Parser p, Token token)
        {
            var formattingElementEntry = p.activeFormattingElements.getElementEntryInScopeWithTagName(token.tagName);

            if (formattingElementEntry.IsTruthy())
            {
                if (!p.openElements.contains(formattingElementEntry.element))
                {
                    p.activeFormattingElements.removeEntry(formattingElementEntry);
                    formattingElementEntry = null;
                }

                else if (!p.openElements.hasInScope(token.tagName))
                    formattingElementEntry = null;
            }

            else
                genericEndTagInBody(p, token);

            return formattingElementEntry;
        }

        static IEntry aaObtainFormattingElementEntry(Parser p, Token token, IEntry formattingElementEntry)
        {
            return aaObtainFormattingElementEntry(p, token);
        }

        //Steps 9 and 10 of the algorithm
        static Element aaObtainFurthestBlock(Parser p, IEntry formattingElementEntry)
        {
            Element furthestBlock = null;

            for (var i = p.openElements.stackTop; i >= 0; i--)
            {
                var element = p.openElements.items[i];

                if (element == formattingElementEntry.element)
                    break;

                if (p._isSpecialElement(element))
                    furthestBlock = element;
            }

            if (!furthestBlock.IsTruthy())
            {
                p.openElements.popUntilElementPopped(formattingElementEntry.element);
                p.activeFormattingElements.removeEntry(formattingElementEntry);
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
                element = nextElement;
                //NOTE: store next element for the next loop iteration (it may be deleted from the stack by step 9.5)
                nextElement = p.openElements.getCommonAncestor(element);

                var elementEntry = p.activeFormattingElements.getElementEntry(element);
                var counterOverflow = elementEntry.IsTruthy() && i >= AA_INNER_LOOP_ITER;
                var shouldRemoveFromOpenElements = !elementEntry.IsTruthy() || counterOverflow;

                if (shouldRemoveFromOpenElements)
                {
                    if (counterOverflow.IsTruthy())
                        p.activeFormattingElements.removeEntry(elementEntry);

                    p.openElements.remove(element);
                }

                else
                {
                    element = aaRecreateElementFromEntry(p, elementEntry);

                    if (lastElement == furthestBlock)
                        p.activeFormattingElements.bookmark = elementEntry;

                    p.treeAdapter.detachNode(lastElement);
                    p.treeAdapter.appendChild(element, lastElement);
                    lastElement = element;
                }
            }

            return lastElement;
        }

        //Step 13.7 of the algorithm
        static Element aaRecreateElementFromEntry(Parser p, IEntry elementEntry)
        {
            var ns = p.treeAdapter.getNamespaceURI(elementEntry.element);
            var newElement = p.treeAdapter.createElement(elementEntry.token.tagName, ns, elementEntry.token.attrs);

            p.openElements.replace(elementEntry.element, newElement);
            elementEntry.element = newElement;

            return newElement;
        }

        //Step 14 of the algorithm
        static void aaInsertLastNodeInCommonAncestor(Parser p, Element commonAncestor, Element lastElement)
        {
            if (p._isElementCausesFosterParenting(commonAncestor))
                p._fosterParentElement(lastElement);

            else
            {
                var tn = p.treeAdapter.getTagName(commonAncestor);
                string ns = p.treeAdapter.getNamespaceURI(commonAncestor);

                if (tn == ɑ.TEMPLATE && ns == NS.HTML)
                    commonAncestor = p.treeAdapter.getTemplateContent(commonAncestor);

                p.treeAdapter.appendChild(commonAncestor, lastElement);
            }
        }

        //Steps 15-19 of the algorithm
        static void aaReplaceFormattingElement(Parser p, Element furthestBlock, IEntry formattingElementEntry)
        {
            string ns = p.treeAdapter.getNamespaceURI(formattingElementEntry.element);
            Token token = formattingElementEntry.token;
            Element newElement = p.treeAdapter.createElement(token.tagName, ns, token.attrs);

            p._adoptNodes(furthestBlock, newElement);
            p.treeAdapter.appendChild(furthestBlock, newElement);

            p.activeFormattingElements.insertElementAfterBookmark(newElement, formattingElementEntry.token);
            p.activeFormattingElements.removeEntry(formattingElementEntry);

            p.openElements.remove(formattingElementEntry.element);
            p.openElements.insertAfter(furthestBlock, newElement);
        }

        //Algorithm entry point
        static void callAdoptionAgency(Parser p, Token token)
        {
            IEntry formattingElementEntry = null;

            for (var i = 0; i < AA_OUTER_LOOP_ITER; i++)
            {
                formattingElementEntry = aaObtainFormattingElementEntry(p, token, formattingElementEntry);

                if (!formattingElementEntry.IsTruthy())
                    break;

                var furthestBlock = aaObtainFurthestBlock(p, formattingElementEntry);

                if (!furthestBlock.IsTruthy())
                    break;

                p.activeFormattingElements.bookmark = formattingElementEntry;

                var lastElement = aaInnerLoop(p, furthestBlock, formattingElementEntry.element);
                var commonAncestor = p.openElements.getCommonAncestor(formattingElementEntry.element);

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
            p._appendCommentNode(token, p.openElements.currentTmplContent ?? p.openElements.current); //TODO || operator
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
                getDocumentMode(token.name, token.publicId, token.systemId);

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
            if (token.tagName == ɑ.HTML)
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

            if (tn == ɑ.HTML || tn == ɑ.HEAD || tn == ɑ.BODY || tn == ɑ.BR)
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

            if (tn == ɑ.HTML)
                startTagInBody(p, token);

            else if (tn == ɑ.HEAD)
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

            if (tn == ɑ.HEAD || tn == ɑ.BODY || tn == ɑ.HTML || tn == ɑ.BR)
                tokenBeforeHead(p, token);
        }

        public static void tokenBeforeHead(Parser p, Token token)
        {
            p._insertFakeElement(ɑ.HEAD);
            p.headElement = (Element) p.openElements.current;
            p.insertionMode = IN_HEAD_MODE;
            p._processToken(token);
        }


        //12.2.5.4.4 The "in head" insertion mode
        //------------------------------------------------------------------
        public static void startTagInHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.HTML)
                startTagInBody(p, token);

            else if (tn == ɑ.BASE || tn == ɑ.BASEFONT || tn == ɑ.BGSOUND || tn == ɑ.LINK || tn == ɑ.META)
                p._appendElement(token, NS.HTML);

            else if (tn == ɑ.TITLE)
                p._switchToTextParsing(token, MODE.RCDATA);

            //NOTE: here we assume that we always act as an interactive user agent with enabled scripting, so we parse
            //<noscript> as a rawtext.
            else if (tn == ɑ.NOSCRIPT || tn == ɑ.NOFRAMES || tn == ɑ.STYLE)
                p._switchToTextParsing(token, MODE.RAWTEXT);

            else if (tn == ɑ.SCRIPT)
                p._switchToTextParsing(token, MODE.SCRIPT_DATA);

            else if (tn == ɑ.TEMPLATE)
            {
                p._insertTemplate(token, NS.HTML);
                p.activeFormattingElements.insertMarker();
                p.framesetOk = false;
                p.insertionMode = IN_TEMPLATE_MODE;
                p._pushTmplInsertionMode(IN_TEMPLATE_MODE);
            }

            else if (tn != ɑ.HEAD)
                tokenInHead(p, token);
        }

        public static void endTagInHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.HEAD)
            {
                p.openElements.pop();
                p.insertionMode = AFTER_HEAD_MODE;
            }

            else if (tn == ɑ.BODY || tn == ɑ.BR || tn == ɑ.HTML)
                tokenInHead(p, token);

            else if (tn == ɑ.TEMPLATE && p.openElements.tmplCount > 0)
            {
                p.openElements.generateImpliedEndTags();
                p.openElements.popUntilTagNamePopped(ɑ.TEMPLATE);
                p.activeFormattingElements.clearToLastMarker();
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

            if (tn == ɑ.HTML)
                startTagInBody(p, token);

            else if (tn == ɑ.BODY)
            {
                p._insertElement(token, NS.HTML);
                p.framesetOk = false;
                p.insertionMode = IN_BODY_MODE;
            }

            else if (tn == ɑ.FRAMESET)
            {
                p._insertElement(token, NS.HTML);
                p.insertionMode = IN_FRAMESET_MODE;
            }

            else if (tn == ɑ.BASE || tn == ɑ.BASEFONT || tn == ɑ.BGSOUND || tn == ɑ.LINK || tn == ɑ.META ||
                     tn == ɑ.NOFRAMES || tn == ɑ.SCRIPT || tn == ɑ.STYLE || tn == ɑ.TEMPLATE || tn == ɑ.TITLE)
            {
                p.openElements.push(p.headElement);
                startTagInHead(p, token);
                p.openElements.remove(p.headElement);
            }

            else if (tn != ɑ.HEAD)
                tokenAfterHead(p, token);
        }

        public static void endTagAfterHead(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.BODY || tn == ɑ.HTML || tn == ɑ.BR)
                tokenAfterHead(p, token);

            else if (tn == ɑ.TEMPLATE)
                endTagInHead(p, token);
        }

        public static void tokenAfterHead(Parser p, Token token)
        {
            p._insertFakeElement(ɑ.BODY);
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
            if (p.openElements.hasInButtonScope(ɑ.P))
                p._closePElement();

            p._insertElement(token, NS.HTML);
        }

        public static void numberedHeaderStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(ɑ.P))
                p._closePElement();

            var tn = p.openElements.currentTagName;

            if (tn == ɑ.H1 || tn == ɑ.H2 || tn == ɑ.H3 || tn == ɑ.H4 || tn == ɑ.H5 || tn == ɑ.H6)
                p.openElements.pop();

            p._insertElement(token, NS.HTML);
        }

        public static void preStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(ɑ.P))
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
                if (p.openElements.hasInButtonScope(ɑ.P))
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

                if (tn == ɑ.LI && elementTn == ɑ.LI)
                    closeTn = ɑ.LI;

                else if ((tn == ɑ.DD || tn == ɑ.DT) && (elementTn == ɑ.DD || elementTn == ɑ.DT))
                    closeTn = elementTn;

                if (closeTn.IsTruthy())
                {
                    p.openElements.generateImpliedEndTagsWithExclusion(closeTn);
                    p.openElements.popUntilTagNamePopped(closeTn);
                    break;
                }

                if (elementTn != ɑ.ADDRESS && elementTn != ɑ.DIV && elementTn != ɑ.P && p._isSpecialElement(element))
                    break;
            }

            if (p.openElements.hasInButtonScope(ɑ.P))
                p._closePElement();

            p._insertElement(token, NS.HTML);
        }

        public static void plaintextStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(ɑ.P))
                p._closePElement();

            p._insertElement(token, NS.HTML);
            p.tokenizer.state = MODE.PLAINTEXT;
        }

        public static void buttonStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInScope(ɑ.BUTTON))
            {
                p.openElements.generateImpliedEndTags();
                p.openElements.popUntilTagNamePopped(ɑ.BUTTON);
            }

            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
            p.framesetOk = false;
        }

        public static void aStartTagInBody(Parser p, Token token)
        {
            var activeElementEntry = p.activeFormattingElements.getElementEntryInScopeWithTagName(ɑ.A);

            if (activeElementEntry.IsTruthy())
            {
                callAdoptionAgency(p, token);
                p.openElements.remove(activeElementEntry.element);
                p.activeFormattingElements.removeEntry(activeElementEntry);
            }

            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
            p.activeFormattingElements.pushElement((Element) p.openElements.current, token);
        }

        public static void bStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
            p.activeFormattingElements.pushElement((Element) p.openElements.current, token);
        }

        public static void nobrStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();

            if (p.openElements.hasInScope(ɑ.NOBR))
            {
                callAdoptionAgency(p, token);
                p._reconstructActiveFormattingElements();
            }

            p._insertElement(token, NS.HTML);
            p.activeFormattingElements.pushElement((Element) p.openElements.current, token);
        }

        public static void appletStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
            p.activeFormattingElements.insertMarker();
            p.framesetOk = false;
        }

        public static void tableStartTagInBody(Parser p, Token token)
        {
            if (p.treeAdapter.getDocumentMode((Document) p.document) != HTML.DOCUMENT_MODE.QUIRKS && p.openElements.hasInButtonScope(ɑ.P))
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

            var inputType = getTokenAttr(token, ATTRS.TYPE);

            if (!inputType.IsTruthy() || inputType.toLowerCase() != HIDDEN_INPUT_TYPE)
                p.framesetOk = false;

        }

        public static void paramStartTagInBody(Parser p, Token token)
        {
            p._appendElement(token, NS.HTML);
        }

        public static void hrStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(ɑ.P))
                p._closePElement();

            if (p.openElements.currentTagName == ɑ.MENUITEM)
                p.openElements.pop();

            p._appendElement(token, NS.HTML);
            p.framesetOk = false;
        }

        public static void imageStartTagInBody(Parser p, Token token)
        {
            token.tagName = ɑ.IMG;
            areaStartTagInBody(p, token);
        }

        public static void textareaStartTagInBody(Parser p, Token token)
        {
            p._insertElement(token, NS.HTML);
            //NOTE: If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move
            //on to the next one. (Newlines at the start of textarea elements are ignored as an authoring convenience.)
            p.skipNextNewLine = true;
            p.tokenizer.state = MODE.RCDATA;
            p.originalInsertionMode = p.insertionMode;
            p.framesetOk = false;
            p.insertionMode = TEXT_MODE;
        }

        public static void xmpStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(ɑ.P))
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
            if (p.openElements.currentTagName == ɑ.OPTION)
                p.openElements.pop();

            p._reconstructActiveFormattingElements();
            p._insertElement(token, NS.HTML);
        }

        public static void rbStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInScope(ɑ.RUBY))
                p.openElements.generateImpliedEndTags();

            p._insertElement(token, NS.HTML);
        }

        public static void rtStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInScope(ɑ.RUBY))
                p.openElements.generateImpliedEndTagsWithExclusion(ɑ.RTC);

            p._insertElement(token, NS.HTML);
        }

        public static void menuitemStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.currentTagName == ɑ.MENUITEM)
                p.openElements.pop();

            // TODO needs clarification, see https://github.com/whatwg/html/pull/907/files#r73505877
            p._reconstructActiveFormattingElements();

            p._insertElement(token, NS.HTML);
        }

        public static void menuStartTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInButtonScope(ɑ.P))
                p._closePElement();

            if (p.openElements.currentTagName == ɑ.MENUITEM)
                p.openElements.pop();

            p._insertElement(token, NS.HTML);
        }

        public static void mathStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();

            adjustTokenMathMLAttrs(token);
            adjustTokenXMLAttrs(token);

            if (token.selfClosing)
                p._appendElement(token, NS.MATHML);
            else
                p._insertElement(token, NS.MATHML);
        }

        public static void svgStartTagInBody(Parser p, Token token)
        {
            p._reconstructActiveFormattingElements();

            adjustTokenSVGAttrs(token);
            adjustTokenXMLAttrs(token);

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
                    if (tn == ɑ.I || tn == ɑ.S || tn == ɑ.B || tn == ɑ.U)
                        bStartTagInBody(p, token);

                    else if (tn == ɑ.P)
                        addressStartTagInBody(p, token);

                    else if (tn == ɑ.A)
                        aStartTagInBody(p, token);

                    else
                        genericStartTagInBody(p, token);

                    break;

                case 2:
                    if (tn == ɑ.DL || tn == ɑ.OL || tn == ɑ.UL)
                        addressStartTagInBody(p, token);

                    else if (tn == ɑ.H1 || tn == ɑ.H2 || tn == ɑ.H3 || tn == ɑ.H4 || tn == ɑ.H5 || tn == ɑ.H6)
                        numberedHeaderStartTagInBody(p, token);

                    else if (tn == ɑ.LI || tn == ɑ.DD || tn == ɑ.DT)
                        listItemStartTagInBody(p, token);

                    else if (tn == ɑ.EM || tn == ɑ.TT)
                        bStartTagInBody(p, token);

                    else if (tn == ɑ.BR)
                        areaStartTagInBody(p, token);

                    else if (tn == ɑ.HR)
                        hrStartTagInBody(p, token);

                    else if (tn == ɑ.RB)
                        rbStartTagInBody(p, token);

                    else if (tn == ɑ.RT || tn == ɑ.RP)
                        rtStartTagInBody(p, token);

                    else if (tn != ɑ.TH && tn != ɑ.TD && tn != ɑ.TR)
                        genericStartTagInBody(p, token);

                    break;

                case 3:
                    if (tn == ɑ.DIV || tn == ɑ.DIR || tn == ɑ.NAV)
                        addressStartTagInBody(p, token);

                    else if (tn == ɑ.PRE)
                        preStartTagInBody(p, token);

                    else if (tn == ɑ.BIG)
                        bStartTagInBody(p, token);

                    else if (tn == ɑ.IMG || tn == ɑ.WBR)
                        areaStartTagInBody(p, token);

                    else if (tn == ɑ.XMP)
                        xmpStartTagInBody(p, token);

                    else if (tn == ɑ.SVG)
                        svgStartTagInBody(p, token);

                    else if (tn == ɑ.RTC)
                        rbStartTagInBody(p, token);

                    else if (tn != ɑ.COL)
                        genericStartTagInBody(p, token);

                    break;

                case 4:
                    if (tn == ɑ.HTML)
                        htmlStartTagInBody(p, token);

                    else if (tn == ɑ.BASE || tn == ɑ.LINK || tn == ɑ.META)
                        startTagInHead(p, token);

                    else if (tn == ɑ.BODY)
                        bodyStartTagInBody(p, token);

                    else if (tn == ɑ.MAIN)
                        addressStartTagInBody(p, token);

                    else if (tn == ɑ.FORM)
                        formStartTagInBody(p, token);

                    else if (tn == ɑ.CODE || tn == ɑ.FONT)
                        bStartTagInBody(p, token);

                    else if (tn == ɑ.NOBR)
                        nobrStartTagInBody(p, token);

                    else if (tn == ɑ.AREA)
                        areaStartTagInBody(p, token);

                    else if (tn == ɑ.MATH)
                        mathStartTagInBody(p, token);

                    else if (tn == ɑ.MENU)
                        menuStartTagInBody(p, token);

                    else if (tn != ɑ.HEAD)
                        genericStartTagInBody(p, token);

                    break;

                case 5:
                    if (tn == ɑ.STYLE || tn == ɑ.TITLE)
                        startTagInHead(p, token);

                    else if (tn == ɑ.ASIDE)
                        addressStartTagInBody(p, token);

                    else if (tn == ɑ.SMALL)
                        bStartTagInBody(p, token);

                    else if (tn == ɑ.TABLE)
                        tableStartTagInBody(p, token);

                    else if (tn == ɑ.EMBED)
                        areaStartTagInBody(p, token);

                    else if (tn == ɑ.INPUT)
                        inputStartTagInBody(p, token);

                    else if (tn == ɑ.PARAM || tn == ɑ.TRACK)
                        paramStartTagInBody(p, token);

                    else if (tn == ɑ.IMAGE)
                        imageStartTagInBody(p, token);

                    else if (tn != ɑ.FRAME && tn != ɑ.TBODY && tn != ɑ.TFOOT && tn != ɑ.THEAD)
                        genericStartTagInBody(p, token);

                    break;

                case 6:
                    if (tn == ɑ.SCRIPT)
                        startTagInHead(p, token);

                    else if (tn == ɑ.CENTER || tn == ɑ.FIGURE || tn == ɑ.FOOTER || tn == ɑ.HEADER || tn == ɑ.HGROUP)
                        addressStartTagInBody(p, token);

                    else if (tn == ɑ.BUTTON)
                        buttonStartTagInBody(p, token);

                    else if (tn == ɑ.STRIKE || tn == ɑ.STRONG)
                        bStartTagInBody(p, token);

                    else if (tn == ɑ.APPLET || tn == ɑ.OBJECT)
                        appletStartTagInBody(p, token);

                    else if (tn == ɑ.KEYGEN)
                        areaStartTagInBody(p, token);

                    else if (tn == ɑ.SOURCE)
                        paramStartTagInBody(p, token);

                    else if (tn == ɑ.IFRAME)
                        iframeStartTagInBody(p, token);

                    else if (tn == ɑ.SELECT)
                        selectStartTagInBody(p, token);

                    else if (tn == ɑ.OPTION)
                        optgroupStartTagInBody(p, token);

                    else
                        genericStartTagInBody(p, token);

                    break;

                case 7:
                    if (tn == ɑ.BGSOUND)
                        startTagInHead(p, token);

                    else if (tn == ɑ.DETAILS || tn == ɑ.ADDRESS || tn == ɑ.ARTICLE || tn == ɑ.SECTION || tn == ɑ.SUMMARY)
                        addressStartTagInBody(p, token);

                    else if (tn == ɑ.LISTING)
                        preStartTagInBody(p, token);

                    else if (tn == ɑ.MARQUEE)
                        appletStartTagInBody(p, token);

                    else if (tn == ɑ.NOEMBED)
                        noembedStartTagInBody(p, token);

                    else if (tn != ɑ.CAPTION)
                        genericStartTagInBody(p, token);

                    break;

                case 8:
                    if (tn == ɑ.BASEFONT)
                        startTagInHead(p, token);

                    else if (tn == ɑ.MENUITEM)
                        menuitemStartTagInBody(p, token);

                    else if (tn == ɑ.FRAMESET)
                        framesetStartTagInBody(p, token);

                    else if (tn == ɑ.FIELDSET)
                        addressStartTagInBody(p, token);

                    else if (tn == ɑ.TEXTAREA)
                        textareaStartTagInBody(p, token);

                    else if (tn == ɑ.TEMPLATE)
                        startTagInHead(p, token);

                    else if (tn == ɑ.NOSCRIPT)
                        noembedStartTagInBody(p, token);

                    else if (tn == ɑ.OPTGROUP)
                        optgroupStartTagInBody(p, token);

                    else if (tn != ɑ.COLGROUP)
                        genericStartTagInBody(p, token);

                    break;

                case 9:
                    if (tn == ɑ.PLAINTEXT)
                        plaintextStartTagInBody(p, token);

                    else
                        genericStartTagInBody(p, token);

                    break;

                case 10:
                    if (tn == ɑ.BLOCKQUOTE || tn == ɑ.FIGCAPTION)
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
            if (p.openElements.hasInScope(ɑ.BODY))
                p.insertionMode = AFTER_BODY_MODE;
        }

        public static void bodyEndTagInBody(Parser p, Token token)
        {
            bodyEndTagInBody(p);
        }

        public static void htmlEndTagInBody(Parser p, Token token)
        {
            if (p.openElements.hasInScope(ɑ.BODY))
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

            if ((formElement.IsTruthy() || inTemplate) && p.openElements.hasInScope(ɑ.FORM))
            {
                p.openElements.generateImpliedEndTags();

                if (inTemplate)
                    p.openElements.popUntilTagNamePopped(ɑ.FORM);

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
            if (!p.openElements.hasInButtonScope(ɑ.P))
                p._insertFakeElement(ɑ.P);

            p._closePElement();
        }

        private static void pEndTagInBody(Parser p, Token token)
        {
            pEndTagInBody(p);
        }

        static void liEndTagInBody(Parser p)
        {
            if (p.openElements.hasInListItemScope(ɑ.LI))
            {
                p.openElements.generateImpliedEndTagsWithExclusion(ɑ.LI);
                p.openElements.popUntilTagNamePopped(ɑ.LI);
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
                p.activeFormattingElements.clearToLastMarker();
            }
        }

        static void brEndTagInBody(Parser p)
        {
            p._reconstructActiveFormattingElements();
            p._insertFakeElement(ɑ.BR);
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
                    if (tn == ɑ.A || tn == ɑ.B || tn == ɑ.I || tn == ɑ.S || tn == ɑ.U)
                        callAdoptionAgency(p, token);

                    else if (tn == ɑ.P)
                        pEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 2:
                    if (tn == ɑ.DL || tn == ɑ.UL || tn == ɑ.OL)
                        addressEndTagInBody(p, token);

                    else if (tn == ɑ.LI)
                        liEndTagInBody(p, token);

                    else if (tn == ɑ.DD || tn == ɑ.DT)
                        ddEndTagInBody(p, token);

                    else if (tn == ɑ.H1 || tn == ɑ.H2 || tn == ɑ.H3 || tn == ɑ.H4 || tn == ɑ.H5 || tn == ɑ.H6)
                        numberedHeaderEndTagInBody(p, token);

                    else if (tn == ɑ.BR)
                        brEndTagInBody(p, token);

                    else if (tn == ɑ.EM || tn == ɑ.TT)
                        callAdoptionAgency(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 3:
                    if (tn == ɑ.BIG)
                        callAdoptionAgency(p, token);

                    else if (tn == ɑ.DIR || tn == ɑ.DIV || tn == ɑ.NAV)
                        addressEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 4:
                    if (tn == ɑ.BODY)
                        bodyEndTagInBody(p, token);

                    else if (tn == ɑ.HTML)
                        htmlEndTagInBody(p, token);

                    else if (tn == ɑ.FORM)
                        formEndTagInBody(p, token);

                    else if (tn == ɑ.CODE || tn == ɑ.FONT || tn == ɑ.NOBR)
                        callAdoptionAgency(p, token);

                    else if (tn == ɑ.MAIN || tn == ɑ.MENU)
                        addressEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 5:
                    if (tn == ɑ.ASIDE)
                        addressEndTagInBody(p, token);

                    else if (tn == ɑ.SMALL)
                        callAdoptionAgency(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 6:
                    if (tn == ɑ.CENTER || tn == ɑ.FIGURE || tn == ɑ.FOOTER || tn == ɑ.HEADER || tn == ɑ.HGROUP)
                        addressEndTagInBody(p, token);

                    else if (tn == ɑ.APPLET || tn == ɑ.OBJECT)
                        appletEndTagInBody(p, token);

                    else if (tn == ɑ.STRIKE || tn == ɑ.STRONG)
                        callAdoptionAgency(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 7:
                    if (tn == ɑ.ADDRESS || tn == ɑ.ARTICLE || tn == ɑ.DETAILS || tn == ɑ.SECTION || tn == ɑ.SUMMARY)
                        addressEndTagInBody(p, token);

                    else if (tn == ɑ.MARQUEE)
                        appletEndTagInBody(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 8:
                    if (tn == ɑ.FIELDSET)
                        addressEndTagInBody(p, token);

                    else if (tn == ɑ.TEMPLATE)
                        endTagInHead(p, token);

                    else
                        genericEndTagInBody(p, token);

                    break;

                case 10:
                    if (tn == ɑ.BLOCKQUOTE || tn == ɑ.FIGCAPTION)
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
            if (token.tagName == ɑ.SCRIPT)
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

            if (curTn == ɑ.TABLE || curTn == ɑ.TBODY || curTn == ɑ.TFOOT || curTn == ɑ.THEAD || curTn == ɑ.TR)
            {
                p.pendingCharacterTokens = new List<Token>();
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
            p.activeFormattingElements.insertMarker();
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
            p._insertFakeElement(ɑ.COLGROUP);
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
            p._insertFakeElement(ɑ.TBODY);
            p.insertionMode = IN_TABLE_BODY_MODE;
            p._processToken(token);
        }

        public static void tableStartTagInTable(Parser p, Token token)
        {
            if (p.openElements.hasInTableScope(ɑ.TABLE))
            {
                p.openElements.popUntilTagNamePopped(ɑ.TABLE);
                p._resetInsertionMode();
                p._processToken(token);
            }
        }

        public static void inputStartTagInTable(Parser p, Token token)
        {
            var inputType = getTokenAttr(token, ATTRS.TYPE);

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
                    if (tn == ɑ.TD || tn == ɑ.TH || tn == ɑ.TR)
                        tdStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 3:
                    if (tn == ɑ.COL)
                        colStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 4:
                    if (tn == ɑ.FORM)
                        formStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 5:
                    if (tn == ɑ.TABLE)
                        tableStartTagInTable(p, token);

                    else if (tn == ɑ.STYLE)
                        startTagInHead(p, token);

                    else if (tn == ɑ.TBODY || tn == ɑ.TFOOT || tn == ɑ.THEAD)
                        tbodyStartTagInTable(p, token);

                    else if (tn == ɑ.INPUT)
                        inputStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 6:
                    if (tn == ɑ.SCRIPT)
                        startTagInHead(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 7:
                    if (tn == ɑ.CAPTION)
                        captionStartTagInTable(p, token);

                    else
                        tokenInTable(p, token);

                    break;

                case 8:
                    if (tn == ɑ.COLGROUP)
                        colgroupStartTagInTable(p, token);

                    else if (tn == ɑ.TEMPLATE)
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

            if (tn == ɑ.TABLE)
            {
                if (p.openElements.hasInTableScope(ɑ.TABLE))
                {
                    p.openElements.popUntilTagNamePopped(ɑ.TABLE);
                    p._resetInsertionMode();
                }
            }

            else if (tn == ɑ.TEMPLATE)
                endTagInHead(p, token);

            else if (tn != ɑ.BODY && tn != ɑ.CAPTION && tn != ɑ.COL && tn != ɑ.COLGROUP && tn != ɑ.HTML &&
                     tn != ɑ.TBODY && tn != ɑ.TD && tn != ɑ.TFOOT && tn != ɑ.TH && tn != ɑ.THEAD && tn != ɑ.TR)
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

            if (tn == ɑ.CAPTION || tn == ɑ.COL || tn == ɑ.COLGROUP || tn == ɑ.TBODY ||
                tn == ɑ.TD || tn == ɑ.TFOOT || tn == ɑ.TH || tn == ɑ.THEAD || tn == ɑ.TR)
            {
                if (p.openElements.hasInTableScope(ɑ.CAPTION))
                {
                    p.openElements.generateImpliedEndTags();
                    p.openElements.popUntilTagNamePopped(ɑ.CAPTION);
                    p.activeFormattingElements.clearToLastMarker();
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

            if (tn == ɑ.CAPTION || tn == ɑ.TABLE)
            {
                if (p.openElements.hasInTableScope(ɑ.CAPTION))
                {
                    p.openElements.generateImpliedEndTags();
                    p.openElements.popUntilTagNamePopped(ɑ.CAPTION);
                    p.activeFormattingElements.clearToLastMarker();
                    p.insertionMode = IN_TABLE_MODE;

                    if (tn == ɑ.TABLE)
                        p._processToken(token);
                }
            }

            else if (tn != ɑ.BODY && tn != ɑ.COL && tn != ɑ.COLGROUP && tn != ɑ.HTML && tn != ɑ.TBODY &&
                     tn != ɑ.TD && tn != ɑ.TFOOT && tn != ɑ.TH && tn != ɑ.THEAD && tn != ɑ.TR)
                endTagInBody(p, token);
        }


        //12.2.5.4.12 The "in column group" insertion mode
        //------------------------------------------------------------------
        public static void startTagInColumnGroup(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.HTML)
                startTagInBody(p, token);

            else if (tn == ɑ.COL)
                p._appendElement(token, NS.HTML);

            else if (tn == ɑ.TEMPLATE)
                startTagInHead(p, token);

            else
                tokenInColumnGroup(p, token);
        }

        public static void endTagInColumnGroup(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.COLGROUP)
            {
                if (p.openElements.currentTagName == ɑ.COLGROUP)
                {
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_MODE;
                }
            }

            else if (tn == ɑ.TEMPLATE)
                endTagInHead(p, token);

            else if (tn != ɑ.COL)
                tokenInColumnGroup(p, token);
        }

        public static void tokenInColumnGroup(Parser p, Token token)
        {
            if (p.openElements.currentTagName == ɑ.COLGROUP)
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

            if (tn == ɑ.TR)
            {
                p.openElements.clearBackToTableBodyContext();
                p._insertElement(token, NS.HTML);
                p.insertionMode = IN_ROW_MODE;
            }

            else if (tn == ɑ.TH || tn == ɑ.TD)
            {
                p.openElements.clearBackToTableBodyContext();
                p._insertFakeElement(ɑ.TR);
                p.insertionMode = IN_ROW_MODE;
                p._processToken(token);
            }

            else if (tn == ɑ.CAPTION || tn == ɑ.COL || tn == ɑ.COLGROUP ||
                     tn == ɑ.TBODY || tn == ɑ.TFOOT || tn == ɑ.THEAD)
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

            if (tn == ɑ.TBODY || tn == ɑ.TFOOT || tn == ɑ.THEAD)
            {
                if (p.openElements.hasInTableScope(tn))
                {
                    p.openElements.clearBackToTableBodyContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_MODE;
                }
            }

            else if (tn == ɑ.TABLE)
            {
                if (p.openElements.hasTableBodyContextInTableScope())
                {
                    p.openElements.clearBackToTableBodyContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_MODE;
                    p._processToken(token);
                }
            }

            else if (tn != ɑ.BODY && tn != ɑ.CAPTION && tn != ɑ.COL && tn != ɑ.COLGROUP ||
                     tn != ɑ.HTML && tn != ɑ.TD && tn != ɑ.TH && tn != ɑ.TR)
                endTagInTable(p, token);
        }

        //12.2.5.4.14 The "in row" insertion mode
        //------------------------------------------------------------------
        public static void startTagInRow(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.TH || tn == ɑ.TD)
            {
                p.openElements.clearBackToTableRowContext();
                p._insertElement(token, NS.HTML);
                p.insertionMode = IN_CELL_MODE;
                p.activeFormattingElements.insertMarker();
            }

            else if (tn == ɑ.CAPTION || tn == ɑ.COL || tn == ɑ.COLGROUP || tn == ɑ.TBODY ||
                     tn == ɑ.TFOOT || tn == ɑ.THEAD || tn == ɑ.TR)
            {
                if (p.openElements.hasInTableScope(ɑ.TR))
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

            if (tn == ɑ.TR)
            {
                if (p.openElements.hasInTableScope(ɑ.TR))
                {
                    p.openElements.clearBackToTableRowContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                }
            }

            else if (tn == ɑ.TABLE)
            {
                if (p.openElements.hasInTableScope(ɑ.TR))
                {
                    p.openElements.clearBackToTableRowContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                    p._processToken(token);
                }
            }

            else if (tn == ɑ.TBODY || tn == ɑ.TFOOT || tn == ɑ.THEAD)
            {
                if (p.openElements.hasInTableScope(tn) || p.openElements.hasInTableScope(ɑ.TR))
                {
                    p.openElements.clearBackToTableRowContext();
                    p.openElements.pop();
                    p.insertionMode = IN_TABLE_BODY_MODE;
                    p._processToken(token);
                }
            }

            else if (tn != ɑ.BODY && tn != ɑ.CAPTION && tn != ɑ.COL && tn != ɑ.COLGROUP ||
                     tn != ɑ.HTML && tn != ɑ.TD && tn != ɑ.TH)
                endTagInTable(p, token);
        }


        //12.2.5.4.15 The "in cell" insertion mode
        //------------------------------------------------------------------
        public static void startTagInCell(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.CAPTION || tn == ɑ.COL || tn == ɑ.COLGROUP || tn == ɑ.TBODY ||
                tn == ɑ.TD || tn == ɑ.TFOOT || tn == ɑ.TH || tn == ɑ.THEAD || tn == ɑ.TR)
            {

                if (p.openElements.hasInTableScope(ɑ.TD) || p.openElements.hasInTableScope(ɑ.TH))
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

            if (tn == ɑ.TD || tn == ɑ.TH)
            {
                if (p.openElements.hasInTableScope(tn))
                {
                    p.openElements.generateImpliedEndTags();
                    p.openElements.popUntilTagNamePopped(tn);
                    p.activeFormattingElements.clearToLastMarker();
                    p.insertionMode = IN_ROW_MODE;
                }
            }

            else if (tn == ɑ.TABLE || tn == ɑ.TBODY || tn == ɑ.TFOOT || tn == ɑ.THEAD || tn == ɑ.TR)
            {
                if (p.openElements.hasInTableScope(tn))
                {
                    p._closeTableCell();
                    p._processToken(token);
                }
            }

            else if (tn != ɑ.BODY && tn != ɑ.CAPTION && tn != ɑ.COL && tn != ɑ.COLGROUP && tn != ɑ.HTML)
                endTagInBody(p, token);
        }

        //12.2.5.4.16 The "in select" insertion mode
        //------------------------------------------------------------------
        public static void startTagInSelect(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.HTML)
                startTagInBody(p, token);

            else if (tn == ɑ.OPTION)
            {
                if (p.openElements.currentTagName == ɑ.OPTION)
                    p.openElements.pop();

                p._insertElement(token, NS.HTML);
            }

            else if (tn == ɑ.OPTGROUP)
            {
                if (p.openElements.currentTagName == ɑ.OPTION)
                    p.openElements.pop();

                if (p.openElements.currentTagName == ɑ.OPTGROUP)
                    p.openElements.pop();

                p._insertElement(token, NS.HTML);
            }

            else if (tn == ɑ.INPUT || tn == ɑ.KEYGEN || tn == ɑ.TEXTAREA || tn == ɑ.SELECT)
            {
                if (p.openElements.hasInSelectScope(ɑ.SELECT))
                {
                    p.openElements.popUntilTagNamePopped(ɑ.SELECT);
                    p._resetInsertionMode();

                    if (tn != ɑ.SELECT)
                        p._processToken(token);
                }
            }

            else if (tn == ɑ.SCRIPT || tn == ɑ.TEMPLATE)
                startTagInHead(p, token);
        }

        public static void endTagInSelect(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.OPTGROUP)
            {
                var prevOpenElement = p.openElements.items[p.openElements.stackTop - 1];
                var prevOpenElementTn = // prevOpenElement && p.treeAdapter.getTagName(prevOpenElement)
                                        prevOpenElement.IsTruthy() ? p.treeAdapter.getTagName(prevOpenElement) : null;

                if (p.openElements.currentTagName == ɑ.OPTION && prevOpenElementTn == ɑ.OPTGROUP)
                    p.openElements.pop();

                if (p.openElements.currentTagName == ɑ.OPTGROUP)
                    p.openElements.pop();
            }

            else if (tn == ɑ.OPTION)
            {
                if (p.openElements.currentTagName == ɑ.OPTION)
                    p.openElements.pop();
            }

            else if (tn == ɑ.SELECT && p.openElements.hasInSelectScope(ɑ.SELECT))
            {
                p.openElements.popUntilTagNamePopped(ɑ.SELECT);
                p._resetInsertionMode();
            }

            else if (tn == ɑ.TEMPLATE)
                endTagInHead(p, token);
        }

        //12.2.5.4.17 The "in select in table" insertion mode
        //------------------------------------------------------------------
        public static void startTagInSelectInTable(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.CAPTION || tn == ɑ.TABLE || tn == ɑ.TBODY || tn == ɑ.TFOOT ||
                tn == ɑ.THEAD || tn == ɑ.TR || tn == ɑ.TD || tn == ɑ.TH)
            {
                p.openElements.popUntilTagNamePopped(ɑ.SELECT);
                p._resetInsertionMode();
                p._processToken(token);
            }

            else
                startTagInSelect(p, token);
        }

        public static void endTagInSelectInTable(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.CAPTION || tn == ɑ.TABLE || tn == ɑ.TBODY || tn == ɑ.TFOOT ||
                tn == ɑ.THEAD || tn == ɑ.TR || tn == ɑ.TD || tn == ɑ.TH)
            {
                if (p.openElements.hasInTableScope(tn))
                {
                    p.openElements.popUntilTagNamePopped(ɑ.SELECT);
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

            if (tn == ɑ.BASE || tn == ɑ.BASEFONT || tn == ɑ.BGSOUND || tn == ɑ.LINK || tn == ɑ.META ||
                tn == ɑ.NOFRAMES || tn == ɑ.SCRIPT || tn == ɑ.STYLE || tn == ɑ.TEMPLATE || tn == ɑ.TITLE)
                startTagInHead(p, token);

            else
            {
                var newInsertionMode = TEMPLATE_INSERTION_MODE_SWITCH_MAP[tn] /*||*/ ?? IN_BODY_MODE; //TODO

                p._popTmplInsertionMode();
                p._pushTmplInsertionMode(newInsertionMode);
                p.insertionMode = newInsertionMode;
                p._processToken(token);
            }
        }

        public static void endTagInTemplate(Parser p, Token token)
        {
            if (token.tagName == ɑ.TEMPLATE)
                endTagInHead(p, token);
        }

        public static void eofInTemplate(Parser p, Token token)
        {
            if (p.openElements.tmplCount > 0)
            {
                p.openElements.popUntilTagNamePopped(ɑ.TEMPLATE);
                p.activeFormattingElements.clearToLastMarker();
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
            if (token.tagName == ɑ.HTML)
                startTagInBody(p, token);

            else
                tokenAfterBody(p, token);
        }

        public static void endTagAfterBody(Parser p, Token token)
        {
            if (token.tagName == ɑ.HTML)
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

            if (tn == ɑ.HTML)
                startTagInBody(p, token);

            else if (tn == ɑ.FRAMESET)
                p._insertElement(token, NS.HTML);

            else if (tn == ɑ.FRAME)
                p._appendElement(token, NS.HTML);

            else if (tn == ɑ.NOFRAMES)
                startTagInHead(p, token);
        }

        public static void endTagInFrameset(Parser p, Token token)
        {
            if (token.tagName == ɑ.FRAMESET && !p.openElements.isRootHtmlElementCurrent())
            {
                p.openElements.pop();

                if (!p.fragmentContext.IsTruthy() && p.openElements.currentTagName != ɑ.FRAMESET)
                    p.insertionMode = AFTER_FRAMESET_MODE;
            }
        }

        //12.2.5.4.21 The "after frameset" insertion mode
        //------------------------------------------------------------------
        public static void startTagAfterFrameset(Parser p, Token token)
        {
            var tn = token.tagName;

            if (tn == ɑ.HTML)
                startTagInBody(p, token);

            else if (tn == ɑ.NOFRAMES)
                startTagInHead(p, token);
        }

        public static void endTagAfterFrameset(Parser p, Token token)
        {
            if (token.tagName == ɑ.HTML)
                p.insertionMode = AFTER_AFTER_FRAMESET_MODE;
        }

        //12.2.5.4.22 The "after after body" insertion mode
        //------------------------------------------------------------------
        public static void startTagAfterAfterBody(Parser p, Token token)
        {
            if (token.tagName == ɑ.HTML)
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

            if (tn == ɑ.HTML)
                startTagInBody(p, token);

            else if (tn == ɑ.NOFRAMES)
                startTagInHead(p, token);
        }


        //12.2.5.5 The rules for parsing tokens in foreign content
        //------------------------------------------------------------------
        public static void nullCharacterInForeignContent(Parser p, Token token)
        {
            token.chars = UNICODE.REPLACEMENT_CHARACTER.ToString();
            p._insertCharacters(token);
        }

        public static void characterInForeignContent(Parser p, Token token)
        {
            p._insertCharacters(token);
            p.framesetOk = false;
        }

        public static void startTagInForeignContent(Parser p, Token token)
        {
            if (causesExit(token) && !p.fragmentContext.IsTruthy())
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
                    adjustTokenMathMLAttrs(token);

                else if (currentNs == NS.SVG)
                {
                    adjustTokenSVGTagName(token);
                    adjustTokenSVGAttrs(token);
                }

                adjustTokenXMLAttrs(token);

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
