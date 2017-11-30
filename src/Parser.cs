#region Copyright (c) 2017 Atif Aziz, Adrian Guerra
//
// Portions Copyright (c) 2013 Ivan Nikulin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace High5
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Extensions;
    using Microsoft.CodeAnalysis.PooledObjects;
    using static TokenType;
    using static Tokenizer.MODE;
    using static ForeignContent;
    using static Truthiness;
    using static InsertionMode;
    using ATTRS = HTML.ATTRS;
    using NS = HTML.NAMESPACES;
    using MODE = Tokenizer.MODE;
    using T = HTML.TAG_NAMES;

    public static class Parser
    {
        public static HtmlDocument Parse(string html) =>
            Parse(html, (doc, _) => doc);

        public static TResult Parse<TResult>(string html,
            Func<HtmlDocument, string, TResult> resultSelector) =>
            Parse(TreeBuilder.Default, html, resultSelector);

        public static TResult Parse<TNode, TContainer,
                                    TDocument, TDocumentFragment,
                                    TElement, TAttribute, TTemplateElement,
                                    TComment,
                                    TResult>(
            IDocumentTreeBuilder<TNode, TContainer,
                                 TDocument, TDocumentFragment,
                                 TElement, TAttribute, TTemplateElement,
                                 TComment> builder,
            string html,
            Func<TDocument, string, TResult> resultSelector)
            where TNode             : class
            where TContainer        : class, TNode
            where TDocument         : TContainer
            where TDocumentFragment : class, TContainer
            where TElement          : class, TContainer
            where TTemplateElement  : class, TElement
            where TComment          : class, TNode
        {
            var parser = new Parser<TNode, TContainer,
                                    TDocumentFragment,
                                    TElement, TAttribute, TTemplateElement,
                                    TComment>(builder);

            var document =
                new Document<TDocument, TContainer, TNode>(
                    builder.CreateDocument(),
                    builder.SetDocumentType);

            parser.ParseTo(html, document);
            return resultSelector(document.Node, document.Mode);
        }

        public static HtmlDocumentFragment ParseFragment(string html, HtmlNode context) =>
            ParseFragment(TreeBuilder.Default, html, context);

        public static TDocumentFragment ParseFragment<TNode, TContainer,
                                                      TDocumentFragment,
                                                      TElement, TAttribute, TTemplateElement,
                                                      TComment>(
            ITreeBuilder<TNode, TContainer,
                         TDocumentFragment,
                         TElement, TAttribute, TTemplateElement,
                         TComment> builder, string html, TNode context)
            where TNode             : class
            where TContainer        : class, TNode
            where TDocumentFragment : class, TContainer
            where TElement          : class, TContainer
            where TTemplateElement  : class, TElement
            where TComment          : class, TNode
        {
            var parser = new Parser<TNode, TContainer,
                                    TDocumentFragment,
                                    TElement, TAttribute, TTemplateElement,
                                    TComment>(builder);
            return parser.ParseFragment(html, context);
        }

        sealed class Document<TDocument, TContainer, TNode> : IDocument<TContainer, TNode>
            where TDocument : TContainer
        {
            readonly Func<TDocument, string, string, string, TNode> _documentTypeSetter;

            public Document(TDocument node,
                            Func<TDocument, string, string, string, TNode> documentTypeSetter)
            {
                Node = node;
                _documentTypeSetter = documentTypeSetter;
            }

            public TDocument Node { get; }
            TContainer IDocument<TContainer, TNode>.Node => Node;

            public TNode SetDocumentType(string name, string publicId, string systemId) =>
                _documentTypeSetter(Node, name, publicId, systemId);

            // TODO Fix Mode to be enum
            public string Mode { get; set; }
        }
    }

    interface IDocument<out TDocument, out TDoctype>
    {
        TDocument Node { get; }
        TDoctype SetDocumentType(string name, string publicId, string systemId);
        string Mode { get; set; }
    }

    enum InsertionMode
    {
        UNDEFINED_MODE,
        INITIAL_MODE,
        BEFORE_HTML_MODE,
        BEFORE_HEAD_MODE,
        IN_HEAD_MODE,
        AFTER_HEAD_MODE,
        IN_BODY_MODE,
        TEXT_MODE,
        IN_TABLE_MODE,
        IN_TABLE_TEXT_MODE,
        IN_CAPTION_MODE,
        IN_COLUMN_GROUP_MODE,
        IN_TABLE_BODY_MODE,
        IN_ROW_MODE,
        IN_CELL_MODE,
        IN_SELECT_MODE,
        IN_SELECT_IN_TABLE_MODE,
        IN_TEMPLATE_MODE,
        AFTER_BODY_MODE,
        IN_FRAMESET_MODE,
        AFTER_FRAMESET_MODE,
        AFTER_AFTER_BODY_MODE,
        AFTER_AFTER_FRAMESET_MODE,
    }

    // ReSharper disable ArrangeThisQualifier

    sealed class Parser<Node, Container,
                        DocumentFragment,
                        Element, Attr, TemplateElement,
                        Comment>
        where Node             : class
        where Container        : class, Node
        where DocumentFragment : class, Container
        where Element          : class, Container
        where TemplateElement  : class, Element
        where Comment          : class, Node
    {
        //Misc constants
        const string HIDDEN_INPUT_TYPE = "hidden";

        //Adoption agency loops iteration count
        const int AA_OUTER_LOOP_ITER = 8;
        const int AA_INNER_LOOP_ITER = 3;

        //Insertion mode reset map
        static readonly IDictionary<string, InsertionMode> INSERTION_MODE_RESET_MAP = new Dictionary<string, InsertionMode>
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
        static readonly IDictionary<string, InsertionMode> TEMPLATE_INSERTION_MODE_SWITCH_MAP = new Dictionary<string, InsertionMode>
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

        static TValue[] ArrayMap<TKey, TValue>(Func<TKey, int> indexSelector, params (TKey Key, TValue Value)[] args)
        {
            if (args.Length == 0)
                return EmptyArray<TValue>.Value;
            var length = args.Max(e => indexSelector(e.Key)) + 1;
            var array = new TValue[length];
            foreach (var e in args)
                array[indexSelector(e.Key)] = e.Value;
            return array;
        }

        //Token handlers map for insertion modes
        static readonly Action<Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment>, Token>[][] _ =
            ArrayMap(
                m => (int) m,
                (INITIAL_MODE, ArrayMap<TokenType, Action<Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment>, Token>>(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , TokenInInitialMode  ),
                    (NULL_CHARACTER_TOKEN      , TokenInInitialMode  ),
                    (WHITESPACE_CHARACTER_TOKEN, IgnoreToken         ),
                    (COMMENT_TOKEN             , AppendComment       ),
                    (DOCTYPE_TOKEN             , DoctypeInInitialMode),
                    (START_TAG_TOKEN           , TokenInInitialMode  ),
                    (END_TAG_TOKEN             , TokenInInitialMode  ),
                    (EOF_TOKEN                 , TokenInInitialMode  ))),

                (BEFORE_HTML_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , TokenBeforeHtml                     ),
                    (NULL_CHARACTER_TOKEN      , TokenBeforeHtml                     ),
                    (WHITESPACE_CHARACTER_TOKEN, IgnoreToken                         ),
                    (COMMENT_TOKEN             , AppendComment                       ),
                    (DOCTYPE_TOKEN             , IgnoreToken                         ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagBeforeHtml)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagBeforeHtml)    ),
                    (EOF_TOKEN                 , TokenBeforeHtml                     ))),

                (BEFORE_HEAD_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , TokenBeforeHead                     ),
                    (NULL_CHARACTER_TOKEN      , TokenBeforeHead                     ),
                    (WHITESPACE_CHARACTER_TOKEN, IgnoreToken                         ),
                    (COMMENT_TOKEN             , AppendComment                       ),
                    (DOCTYPE_TOKEN             , IgnoreToken                         ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagBeforeHead)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagBeforeHead)    ),
                    (EOF_TOKEN                 , TokenBeforeHead                     ))),

                (IN_HEAD_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , TokenInHead                     ),
                    (NULL_CHARACTER_TOKEN      , TokenInHead                     ),
                    (WHITESPACE_CHARACTER_TOKEN, InsertCharacters                ),
                    (COMMENT_TOKEN             , AppendComment                   ),
                    (DOCTYPE_TOKEN             , IgnoreToken                     ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInHead)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInHead)    ),
                    (EOF_TOKEN                 , TokenInHead                     ))),

                (AFTER_HEAD_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , TokenAfterHead                     ),
                    (NULL_CHARACTER_TOKEN      , TokenAfterHead                     ),
                    (WHITESPACE_CHARACTER_TOKEN, InsertCharacters                   ),
                    (COMMENT_TOKEN             , AppendComment                      ),
                    (DOCTYPE_TOKEN             , IgnoreToken                        ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagAfterHead)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagAfterHead)    ),
                    (EOF_TOKEN                 , TokenAfterHead                     ))),

                (IN_BODY_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , CharacterInBody                 ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                     ),
                    (WHITESPACE_CHARACTER_TOKEN, WhitespaceCharacterInBody       ),
                    (COMMENT_TOKEN             , AppendComment                   ),
                    (DOCTYPE_TOKEN             , IgnoreToken                     ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInBody)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInBody)    ),
                    (EOF_TOKEN                 , EofInBody                       ))),

                (TEXT_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , InsertCharacters            ),
                    (NULL_CHARACTER_TOKEN      , InsertCharacters            ),
                    (WHITESPACE_CHARACTER_TOKEN, InsertCharacters            ),
                    (COMMENT_TOKEN             , IgnoreToken                 ),
                    (DOCTYPE_TOKEN             , IgnoreToken                 ),
                    (START_TAG_TOKEN           , IgnoreToken                 ),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInText)),
                    (EOF_TOKEN                 , EofInText                   ))),

                (IN_TABLE_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , CharacterInTable                 ),
                    (NULL_CHARACTER_TOKEN      , CharacterInTable                 ),
                    (WHITESPACE_CHARACTER_TOKEN, CharacterInTable                 ),
                    (COMMENT_TOKEN             , AppendComment                    ),
                    (DOCTYPE_TOKEN             , IgnoreToken                      ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInTable)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInTable)    ),
                    (EOF_TOKEN                 , EofInBody                        ))),

                (IN_TABLE_TEXT_MODE, ArrayMap<TokenType, Action<Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment>, Token>>(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , CharacterInTableText          ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                   ),
                    (WHITESPACE_CHARACTER_TOKEN, WhitespaceCharacterInTableText),
                    (COMMENT_TOKEN             , TokenInTableText              ),
                    (DOCTYPE_TOKEN             , TokenInTableText              ),
                    (START_TAG_TOKEN           , TokenInTableText              ),
                    (END_TAG_TOKEN             , TokenInTableText              ),
                    (EOF_TOKEN                 , TokenInTableText              ))),

                (IN_CAPTION_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , CharacterInBody                    ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                        ),
                    (WHITESPACE_CHARACTER_TOKEN, WhitespaceCharacterInBody          ),
                    (COMMENT_TOKEN             , AppendComment                      ),
                    (DOCTYPE_TOKEN             , IgnoreToken                        ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInCaption)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInCaption)    ),
                    (EOF_TOKEN                 , EofInBody                          ))),

                (IN_COLUMN_GROUP_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , TokenInColumnGroup                     ),
                    (NULL_CHARACTER_TOKEN      , TokenInColumnGroup                     ),
                    (WHITESPACE_CHARACTER_TOKEN, InsertCharacters                       ),
                    (COMMENT_TOKEN             , AppendComment                          ),
                    (DOCTYPE_TOKEN             , IgnoreToken                            ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInColumnGroup)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInColumnGroup)    ),
                    (EOF_TOKEN                 , EofInBody                              ))),

                (IN_TABLE_BODY_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , CharacterInTable                     ),
                    (NULL_CHARACTER_TOKEN      , CharacterInTable                     ),
                    (WHITESPACE_CHARACTER_TOKEN, CharacterInTable                     ),
                    (COMMENT_TOKEN             , AppendComment                        ),
                    (DOCTYPE_TOKEN             , IgnoreToken                          ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInTableBody)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInTableBody)    ),
                    (EOF_TOKEN                 , EofInBody                            ))),

                (IN_ROW_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , CharacterInTable               ),
                    (NULL_CHARACTER_TOKEN      , CharacterInTable               ),
                    (WHITESPACE_CHARACTER_TOKEN, CharacterInTable               ),
                    (COMMENT_TOKEN             , AppendComment                  ),
                    (DOCTYPE_TOKEN             , IgnoreToken                    ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInRow)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInRow)    ),
                    (EOF_TOKEN                 , EofInBody                      ))),

                (IN_CELL_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , CharacterInBody                 ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                     ),
                    (WHITESPACE_CHARACTER_TOKEN, WhitespaceCharacterInBody       ),
                    (COMMENT_TOKEN             , AppendComment                   ),
                    (DOCTYPE_TOKEN             , IgnoreToken                     ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInCell)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInCell)    ),
                    (EOF_TOKEN                 , EofInBody                       ))),

                (IN_SELECT_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , InsertCharacters                  ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                       ),
                    (WHITESPACE_CHARACTER_TOKEN, InsertCharacters                  ),
                    (COMMENT_TOKEN             , AppendComment                     ),
                    (DOCTYPE_TOKEN             , IgnoreToken                       ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInSelect)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInSelect)    ),
                    (EOF_TOKEN                 , EofInBody                         ))),

                (IN_SELECT_IN_TABLE_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , InsertCharacters                         ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                              ),
                    (WHITESPACE_CHARACTER_TOKEN, InsertCharacters                         ),
                    (COMMENT_TOKEN             , AppendComment                            ),
                    (DOCTYPE_TOKEN             , IgnoreToken                              ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInSelectInTable)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInSelectInTable)    ),
                    (EOF_TOKEN                 , EofInBody                                ))),

                (IN_TEMPLATE_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , CharacterInBody                     ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                         ),
                    (WHITESPACE_CHARACTER_TOKEN, WhitespaceCharacterInBody           ),
                    (COMMENT_TOKEN             , AppendComment                       ),
                    (DOCTYPE_TOKEN             , IgnoreToken                         ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInTemplate)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInTemplate)    ),
                    (EOF_TOKEN                 , EofInTemplate                       ))),

                (AFTER_BODY_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , TokenAfterBody                     ),
                    (NULL_CHARACTER_TOKEN      , TokenAfterBody                     ),
                    (WHITESPACE_CHARACTER_TOKEN, WhitespaceCharacterInBody          ),
                    (COMMENT_TOKEN             , AppendCommentToRootHtmlElement     ),
                    (DOCTYPE_TOKEN             , IgnoreToken                        ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagAfterBody)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagAfterBody)    ),
                    (EOF_TOKEN                 , StopParsing                        ))),

                (IN_FRAMESET_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , IgnoreToken                         ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                         ),
                    (WHITESPACE_CHARACTER_TOKEN, InsertCharacters                    ),
                    (COMMENT_TOKEN             , AppendComment                       ),
                    (DOCTYPE_TOKEN             , IgnoreToken                         ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagInFrameset)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagInFrameset)    ),
                    (EOF_TOKEN                 , StopParsing                         ))),

                (AFTER_FRAMESET_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , IgnoreToken                            ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                            ),
                    (WHITESPACE_CHARACTER_TOKEN, InsertCharacters                       ),
                    (COMMENT_TOKEN             , AppendComment                          ),
                    (DOCTYPE_TOKEN             , IgnoreToken                            ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagAfterFrameset)),
                    (END_TAG_TOKEN             , F<EndTagToken>(EndTagAfterFrameset)    ),
                    (EOF_TOKEN                 , StopParsing                            ))),

                (AFTER_AFTER_BODY_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , TokenAfterAfterBody                     ),
                    (NULL_CHARACTER_TOKEN      , TokenAfterAfterBody                     ),
                    (WHITESPACE_CHARACTER_TOKEN, WhitespaceCharacterInBody               ),
                    (COMMENT_TOKEN             , AppendCommentToDocument                 ),
                    (DOCTYPE_TOKEN             , IgnoreToken                             ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagAfterAfterBody)),
                    (END_TAG_TOKEN             , TokenAfterAfterBody                     ),
                    (EOF_TOKEN                 , StopParsing                             ))),

                (AFTER_AFTER_FRAMESET_MODE, ArrayMap(
                    tt => (int) tt,
                    (CHARACTER_TOKEN           , IgnoreToken                                 ),
                    (NULL_CHARACTER_TOKEN      , IgnoreToken                                 ),
                    (WHITESPACE_CHARACTER_TOKEN, WhitespaceCharacterInBody                   ),
                    (COMMENT_TOKEN             , AppendCommentToDocument                     ),
                    (DOCTYPE_TOKEN             , IgnoreToken                                 ),
                    (START_TAG_TOKEN           , F<StartTagToken>(StartTagAfterAfterFrameset)),
                    (END_TAG_TOKEN             , IgnoreToken                                 ),
                    (EOF_TOKEN                 , StopParsing                                 ))));

        static Action<Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment>, Token> F<T>(Action<Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment>, T> action)
            where T : Token => (p, token) => action(p, (T) token);

        readonly Dictionary<Node, Container> parentByNode = new Dictionary<Node, Container>();

        readonly ITreeBuilder<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> treeBuilder;
        Element pendingScript;
        InsertionMode originalInsertionMode;
        Element headElement;
        Element formElement;
        OpenElementStack<Container, DocumentFragment, Element, TemplateElement> openElements;
        FormattingElementList<Element, Attr> activeFormattingElements;
        List<InsertionMode> tmplInsertionModeStack;
        int tmplInsertionModeStackTop;
        InsertionMode currentTmplInsertionMode;
        List<Token> pendingCharacterTokens;
        bool hasNonWhitespacePendingCharacterToken;
        bool framesetOk;
        bool skipNextNewLine;
        bool fosterParentingEnabled;

        Tokenizer tokenizer;
        bool stopped;
        InsertionMode insertionMode;
        IDocument<Container, Node> doc;
        Container document;
        Node fragmentContext;

        internal class Location
        {
            public Container parent;
            public Element beforeElement;

            public Location(Container parent, Element beforeElement)
            {
                this.parent = parent;
                this.beforeElement = beforeElement;
            }
        }

        //Parser
        public Parser(ITreeBuilder<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> treeBuilder)
        {
            //this.options = mergeOptions(DEFAULT_OPTIONS, options);

            this.treeBuilder = treeBuilder;
            this.pendingScript = null;

            //TODO check Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment>mixin
            //if (this.options.locationInfo)
            //    new LocationInfoParserMixin(this);
        }

        Func<string, string, string, string, Attr> createAttribute;
        Func<string, string, string, string, Attr> CreateAttribute =>
            createAttribute ?? (createAttribute = this.treeBuilder.CreateAttribute);

        // API
        public void ParseTo(string html, IDocument<Container, Node> document)
        {
            doc = document;
            this.Bootstrap(document.Node, null);
            this.tokenizer.Write(html, true);
            this.RunParsingLoop(null);
        }

        public DocumentFragment ParseFragment(string html, Node fragmentContext)
        {
            //NOTE: use <template> element as a fragment context if context element was not provided,
            //so we will parse in "forgiving" manner
            if (fragmentContext == null)
                fragmentContext = this.treeBuilder.CreateElement(T.TEMPLATE, NS.HTML, default(ArraySegment<Attr>));

            //NOTE: create fake element which will be used as 'document' for fragment parsing.
            //This is important for jsdom there 'document' can't be recreated, therefore
            //fragment parsing causes messing of the main `document`.
            var documentMock = this.treeBuilder.CreateElement("documentmock", NS.HTML, default(ArraySegment<Attr>));

            this.Bootstrap(documentMock, fragmentContext);

            if (this.treeBuilder.GetTagName((Element) fragmentContext) == T.TEMPLATE)
                this.PushTmplInsertionMode(IN_TEMPLATE_MODE);

            this.InitTokenizerForFragmentParsing();
            this.InsertFakeRootElement();
            this.ResetInsertionMode();
            this.FindFormInFragmentContext();
            this.tokenizer.Write(html, true);
            this.RunParsingLoop(null);

            var rootElement = (Element) this.treeBuilder.GetFirstChild(documentMock);
            var fragment = this.treeBuilder.CreateDocumentFragment();

            this.AdoptNodes(rootElement, fragment);

            return fragment;
        }

        //Bootstrap parser
        void Bootstrap(Container document, Node fragmentContext)
        {
            this.tokenizer = new Tokenizer();

            this.stopped = false;

            this.insertionMode = INITIAL_MODE;
            this.originalInsertionMode = UNDEFINED_MODE;

            this.document = document;
            this.fragmentContext = fragmentContext;

            this.headElement = null;
            this.formElement = null;

            this.openElements = new OpenElementStack<Container, DocumentFragment, Element, TemplateElement>(this.document, this.treeBuilder.GetNamespaceUri, this.treeBuilder.GetTagName, this.treeBuilder.GetTemplateContent);
            this.activeFormattingElements = new FormattingElementList<Element, Attr>(this.treeBuilder.GetNamespaceUri, this.treeBuilder.GetTagName, this.treeBuilder.GetAttributeCount, this.treeBuilder.ListAttributes, this.treeBuilder.GetAttributeName, this.treeBuilder.GetAttributeValue);

            this.tmplInsertionModeStack = new List<InsertionMode>();
            this.tmplInsertionModeStackTop = -1;
            this.currentTmplInsertionMode = UNDEFINED_MODE;

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

                    if (token is CharacterToken ctoken && token.Type == WHITESPACE_CHARACTER_TOKEN && ctoken[0] == '\n')
                    {
                        if (ctoken.Length == 1)
                            continue;

                        ctoken.Remove(0, 1);
                    }
                }

                this.ProcessInputToken(token);

                if (scriptHandler != null && this.pendingScript != null)
                    break;
            }
        }

        void RunParsingLoopForCurrentChunk(Action writeCallback, Action<Element> scriptHandler)
        {
            this.RunParsingLoop(scriptHandler);

            if (scriptHandler != null && this.pendingScript != null)
            {
                var script = this.pendingScript;

                this.pendingScript = null;

                scriptHandler(script);

                return;
            }

            if (writeCallback != null)
                writeCallback();
        }

        //Text parsing
        void SetupTokenizerCDataMode()
        {
            var current = this.GetAdjustedCurrentElement();

            this.tokenizer.AllowCData = current != null && current != this.document &&
                                        this.treeBuilder.GetNamespaceUri((Element)current) != NS.HTML && !this.IsIntegrationPoint((Element)current);
        }

        void SwitchToTextParsing(StartTagToken currentToken, TokenizerState nextTokenizerState)
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
            return (this.openElements.StackTop == 0 && this.fragmentContext != null ?
                this.fragmentContext :
                this.openElements.Current);
        }

        void FindFormInFragmentContext()
        {
            var node = (Element) this.fragmentContext;

            do
            {
                if (this.treeBuilder.GetTagName(node) == T.FORM)
                {
                    this.formElement = node;
                    break;
                }

                node = (Element) this.GetParentNode(node);
            } while (node != null);
        }

        void InitTokenizerForFragmentParsing()
        {
            if (this.treeBuilder.GetNamespaceUri((Element) this.fragmentContext) == NS.HTML)
            {
                var tn = this.treeBuilder.GetTagName((Element) this.fragmentContext);

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
            var node = this.doc.SetDocumentType(token.Name, token.PublicId, token.SystemId);
            if (node != null)
                this.parentByNode.Add(node, this.doc.Node);
        }

        void AttachElementToTree(Element element)
        {
            if (this.ShouldFosterParentOnInsertion())
                this.FosterParentElement(element);

            else
            {
                var parent = this.openElements.CurrentTmplContent ?? this.openElements.Current; //|| operator

                this.AppendChild(parent, element);
            }
        }

        void AppendElement(StartTagToken token, string namespaceURI)
        {
            Element element;
            using (var attrs = attrsPool.Allocate())
                element = this.treeBuilder.CreateElement(token.TagName, namespaceURI, token.CopyAttrsTo(attrs, this.CreateAttribute));

            this.AttachElementToTree(element);
        }

        void InsertElement(StartTagToken token, string namespaceURI)
        {
            Element element;
            using (var attrs = attrsPool.Allocate())
                element = this.treeBuilder.CreateElement(token.TagName, namespaceURI, token.CopyAttrsTo(attrs, this.CreateAttribute));

            this.AttachElementToTree(element);
            this.openElements.Push(element);
        }

        void InsertFakeElement(string tagName)
        {
            var element = this.treeBuilder.CreateElement(tagName, NS.HTML, default(ArraySegment<Attr>));

            this.AttachElementToTree(element);
            this.openElements.Push(element);
        }

        void InsertTemplate(StartTagToken token)
        {
            TemplateElement tmpl;
            using (var attrs = attrsPool.Allocate())
                tmpl = (TemplateElement) this.treeBuilder.CreateElement(token.TagName, NS.HTML, token.CopyAttrsTo(attrs, this.CreateAttribute));
            var content = this.treeBuilder.CreateDocumentFragment();

            this.treeBuilder.SetTemplateContent(tmpl, content);
            this.AttachElementToTree(tmpl);
            this.openElements.Push(tmpl);
        }

        void InsertTemplate(StartTagToken token, string s)
        {
            InsertTemplate(token);
        }

        void InsertFakeRootElement()
        {
            var element = this.treeBuilder.CreateElement(T.HTML, NS.HTML, default(ArraySegment<Attr>));

            this.AppendChild(this.openElements.Current, element);
            this.openElements.Push(element);
        }

        void AppendCommentNode(CommentToken token, Container parent)
        {
            var commentNode = this.treeBuilder.CreateCommentNode(token.Data);

            this.AppendChild(parent, commentNode);
        }

        void InsertCharacters(CharacterToken token)
        {
            if (this.ShouldFosterParentOnInsertion())
                this.FosterParentText(token.Chars);

            else
            {
                var parent = this.openElements.CurrentTmplContent ?? this.openElements.Current; // || operator

                this.InsertText(parent, token.Chars);
            }
        }

        void AdoptNodes(Element donor, Container recipient)
        {
            while (true)
            {
                var child = this.treeBuilder.GetFirstChild(donor);

                if (child == null)
                    break;

                this.DetachNode(child);
                this.AppendChild(recipient, child);
            }
        }

        //Token processing
        bool ShouldProcessTokenInForeignContent(Token token)
        {
            var current_ = this.GetAdjustedCurrentElement();

            if (current_ == null || current_ == this.document)
                return false;

            var current = (Element) current_;

            var ns = this.treeBuilder.GetNamespaceUri(current);

            if (ns == NS.HTML)
                return false;

            if (this.treeBuilder.GetTagName(current) == T.ANNOTATION_XML && ns == NS.MATHML &&
                token is StartTagToken svg /* token.Type == START_TAG_TOKEN */ && svg.TagName == T.SVG)
                return false;

            var isCharacterToken = token.Type == CHARACTER_TOKEN ||
                                   token.Type == NULL_CHARACTER_TOKEN ||
                                   token.Type == WHITESPACE_CHARACTER_TOKEN;
            var isMathMLTextStartTag = token is StartTagToken startTagToken /* token.Type == START_TAG_TOKEN */ &&
                                       startTagToken.TagName != T.MGLYPH &&
                                       startTagToken.TagName != T.MALIGNMARK;

            if ((isMathMLTextStartTag || isCharacterToken) && this.IsIntegrationPoint(current, NS.MATHML))
                return false;

            if ((token.Type == START_TAG_TOKEN || isCharacterToken) && this.IsIntegrationPoint(current, NS.HTML))
                return false;

            return token.Type != EOF_TOKEN;
        }

        void ProcessToken(Token token)
        {
            _[(int) this.insertionMode][(int) token.Type](this, token);
        }

        void ProcessTokenInBodyMode(Token token)
        {
            _[(int) IN_BODY_MODE][(int) token.Type](this, token);
        }

        void ProcessTokenInForeignContent(Token token)
        {
            if (token.Type == CHARACTER_TOKEN)
                CharacterInForeignContent(this, (CharacterToken) token);

            else if (token.Type == NULL_CHARACTER_TOKEN)
                NullCharacterInForeignContent(this, (CharacterToken) token);

            else if (token.Type == WHITESPACE_CHARACTER_TOKEN)
                InsertCharacters(this, (CharacterToken) token);

            else if (token.Type == COMMENT_TOKEN)
                AppendComment(this, token);

            else if (token.Type == START_TAG_TOKEN)
                StartTagInForeignContent(this, (StartTagToken) token);

            else if (token.Type == END_TAG_TOKEN)
                EndTagInForeignContent(this, (EndTagToken) token);
        }

        void ProcessInputToken(Token token)
        {
            if (this.ShouldProcessTokenInForeignContent(token))
                this.ProcessTokenInForeignContent(token);

            else
                this.ProcessToken(token);
        }

        readonly ObjectPool<PooledArray<Attr>> attrsPool = PooledArray<Attr>.CreatePool();
        readonly ObjectPool<PooledArray<(string Name, string Value)>> nvattrsPool = PooledArray<(string Name, string Value)>.CreatePool();

        //Integration points
        bool IsIntegrationPoint(Element element, string foreignNS)
        {
            var tn = this.treeBuilder.GetTagName(element);
            var ns = this.treeBuilder.GetNamespaceUri(element);
            var attrsLength = this.treeBuilder.GetAttributeCount(element);
            using (var attrs = attrsPool.Allocate())
            {
                attrs.Array.Init(attrsLength);
                this.treeBuilder.ListAttributes(element, attrs.Array.ToArraySegment());
                using (var nvattrs = nvattrsPool.Allocate())
                {
                    nvattrs.Array.Capacity = attrsLength;
                    for (var i = 0; i < attrs.Length; i++)
                        nvattrs.Array[i] = (this.treeBuilder.GetAttributeName(attrs.Array[i]), this.treeBuilder.GetAttributeValue(attrs.Array[i]));
                    return ForeignContent.IsIntegrationPoint(tn, ns, nvattrs.Array.ToArraySegment(), foreignNS);
                }
            }
        }

        bool IsIntegrationPoint(Element element)
        {
            return IsIntegrationPoint(element, "");
        }

        //Active formatting elements reconstruction
        void ReconstructActiveFormattingElements()
        {
            var listLength = this.activeFormattingElements.Length;

            if (listLength > 0)
            {
                var unopenIdx = listLength;

                do
                {
                    unopenIdx--;
                    var entry = this.activeFormattingElements[unopenIdx];

                    if (entry.Type == Entry.MARKER_ENTRY || this.openElements.Contains(((ElementEntry<Element>) entry).Element))
                    {
                        unopenIdx++;
                        break;
                    }
                } while (unopenIdx > 0);

                for (var i = unopenIdx; i < listLength; i++)
                {
                    var entry = (ElementEntry<Element>) this.activeFormattingElements[i];
                    this.InsertElement((StartTagToken) entry.Token, this.treeBuilder.GetNamespaceUri(entry.Element));
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

                    if (this.fragmentContext != null)
                        element = (Element) this.fragmentContext;
                }

                var tn = this.treeBuilder.GetTagName(element);

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
                    this.insertionMode = this.headElement != null ? AFTER_HEAD_MODE : BEFORE_HEAD_MODE;
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
                    var tn = this.treeBuilder.GetTagName(ancestor);

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

        void PushTmplInsertionMode(InsertionMode mode)
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
                                          : UNDEFINED_MODE;
        }

        //Foster parenting
        bool IsElementCausesFosterParenting(Element element)
        {
            var tn = this.treeBuilder.GetTagName(element);

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
                var tn = this.treeBuilder.GetTagName(openElement);
                var ns = this.treeBuilder.GetNamespaceUri(openElement);

                if (tn == T.TEMPLATE && ns == NS.HTML)
                {
                    location.parent = this.treeBuilder.GetTemplateContent((TemplateElement) openElement);
                    break;
                }

                else if (tn == T.TABLE)
                {
                    location.parent = this.GetParentNode(openElement);

                    if (location.parent != null)
                        location.beforeElement = openElement;
                    else
                        location.parent = this.openElements[i - 1];

                    break;
                }
            }

            if (location.parent == null)
                location.parent = this.openElements[0];

            return location;
        }

        void FosterParentElement(Element element)
        {
            var location = this.FindFosterParentingLocation();

            if (location.beforeElement != null)
                this.InsertBefore(location.parent, element, location.beforeElement);
            else
                this.AppendChild(location.parent, element);
        }

        void FosterParentText(string chars)
        {
            var location = this.FindFosterParentingLocation();

            if (location.beforeElement != null)
                this.InsertTextBefore(location.parent, chars, location.beforeElement);
            else
                this.InsertText(location.parent, chars);
        }

        void InsertTextBefore(Container parentNode, string text, Node referenceNode)
        {
            var newTextNode = this.treeBuilder.InsertTextBefore(parentNode, text, referenceNode);
            if (newTextNode != null)
                parentByNode.Add(newTextNode, parentNode);
        }

        void InsertText(Container parentNode, string text)
        {
            var newTextNode = this.treeBuilder.InsertText(parentNode, text);
            if (newTextNode != null)
                parentByNode.Add(newTextNode, parentNode);
        }

        void AppendChild(Container parentNode, Node newNode)
        {
            this.treeBuilder.AppendChild(parentNode, newNode);
            this.parentByNode.Add(newNode, parentNode);
        }

        void DetachNode(Node node)
        {
            if (!parentByNode.TryGetValue(node, out var parentNode))
                return;
            this.treeBuilder.DetachNode(parentNode, node);
            this.parentByNode.Remove(node);
        }

        Container GetParentNode(Node node) =>
            this.parentByNode.TryGetValue(node, out var parent) ? parent : null;

        void InsertBefore(Container parentNode, Element newNode, Element referenceNode)
        {
            this.treeBuilder.InsertBefore(parentNode, newNode, referenceNode);
            this.parentByNode.Add(newNode, parentNode);
        }

        //Special elements
        bool IsSpecialElement(Element element)
        {
            var tn = this.treeBuilder.GetTagName(element);
            var ns = this.treeBuilder.GetNamespaceUri(element);

            return HTML.SPECIAL_ELEMENTS[ns].TryGetValue(tn, out var result) ? result : false;
        }

        //Adoption agency algorithm
        //(see: http://www.whatwg.org/specs/web-apps/current-work/multipage/tree-construction.html#adoptionAgency)
        //------------------------------------------------------------------

        //Steps 5-8 of the algorithm
        static ElementEntry<Element> AaObtainFormattingElementEntry(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, TagToken token)
        {
            var formattingElementEntry = p.activeFormattingElements.GetElementEntryInScopeWithTagName(token.TagName);

            if (formattingElementEntry != null)
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
                GenericEndTagInBody(p, (EndTagToken) token);

            return formattingElementEntry;
        }

        static ElementEntry<Element> AaObtainFormattingElementEntry(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, TagToken token, ElementEntry<Element> formattingElementEntry)
        {
            return AaObtainFormattingElementEntry(p, token);
        }

        //Steps 9 and 10 of the algorithm
        static Element AaObtainFurthestBlock(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, ElementEntry<Element> formattingElementEntry)
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

            if (furthestBlock == null)
            {
                p.openElements.PopUntilElementPopped(formattingElementEntry.Element);
                p.activeFormattingElements.RemoveEntry(formattingElementEntry);
            }

            return furthestBlock;
        }

        //Step 13 of the algorithm
        static Element AaInnerLoop(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Element furthestBlock, Element formattingElement)
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
                var counterOverflow = elementEntry != null && i >= AA_INNER_LOOP_ITER;
                var shouldRemoveFromOpenElements = elementEntry == null || counterOverflow;

                if (shouldRemoveFromOpenElements)
                {
                    if (counterOverflow)
                        p.activeFormattingElements.RemoveEntry(elementEntry);

                    p.openElements.Remove(element);
                }

                else
                {
                    element = AaRecreateElementFromEntry(p, elementEntry);

                    if (lastElement == furthestBlock)
                        p.activeFormattingElements.Bookmark = elementEntry;

                    p.DetachNode(lastElement);
                    p.AppendChild(element, lastElement);
                    lastElement = element;
                }
                element = nextElement;
            }

            return lastElement;
        }

        //Step 13.7 of the algorithm
        static Element AaRecreateElementFromEntry(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, ElementEntry<Element> elementEntry)
        {
            var ns = p.treeBuilder.GetNamespaceUri(elementEntry.Element);
            var token = (StartTagToken) elementEntry.Token;
            Element newElement;
            using (var attrs = PooledArray<Attr>.GetInstance())
                newElement = p.treeBuilder.CreateElement(token.TagName, ns, token.CopyAttrsTo(attrs, p.CreateAttribute));

            p.openElements.Replace(elementEntry.Element, newElement);
            elementEntry.Element = newElement;

            return newElement;
        }

        //Step 14 of the algorithm
        static void AaInsertLastNodeInCommonAncestor(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Element commonAncestor, Element lastElement)
        {
            if (p.IsElementCausesFosterParenting(commonAncestor))
                p.FosterParentElement(lastElement);

            else
            {
                Container commonAncestorNode = commonAncestor;
                var tn = p.treeBuilder.GetTagName(commonAncestor);
                var ns = p.treeBuilder.GetNamespaceUri(commonAncestor);

                if (tn == T.TEMPLATE && ns == NS.HTML)
                    commonAncestorNode = p.treeBuilder.GetTemplateContent((TemplateElement) commonAncestor);

                p.AppendChild(commonAncestorNode, lastElement);
            }
        }

        //Steps 15-19 of the algorithm
        static void AaReplaceFormattingElement(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Element furthestBlock, ElementEntry<Element> formattingElementEntry)
        {
            var ns = p.treeBuilder.GetNamespaceUri(formattingElementEntry.Element);
            var token = (StartTagToken) formattingElementEntry.Token;
            Element newElement;
            using (var attrs = PooledArray<Attr>.GetInstance())
                newElement = p.treeBuilder.CreateElement(token.TagName, ns, token.CopyAttrsTo(attrs.Array, p.CreateAttribute));

            p.AdoptNodes(furthestBlock, newElement);
            p.AppendChild(furthestBlock, newElement);

            p.activeFormattingElements.InsertElementAfterBookmark(newElement, formattingElementEntry.Token);
            p.activeFormattingElements.RemoveEntry(formattingElementEntry);

            p.openElements.Remove(formattingElementEntry.Element);
            p.openElements.InsertAfter(furthestBlock, newElement);
        }

        //Algorithm entry point
        static void CallAdoptionAgency(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, TagToken token)
        {
            ElementEntry<Element> formattingElementEntry = null;

            for (var i = 0; i < AA_OUTER_LOOP_ITER; i++)
            {
                formattingElementEntry = AaObtainFormattingElementEntry(p, token, formattingElementEntry);

                if (formattingElementEntry == null)
                    break;

                var furthestBlock = AaObtainFurthestBlock(p, formattingElementEntry);

                if (furthestBlock == null)
                    break;

                p.activeFormattingElements.Bookmark = formattingElementEntry;

                var lastElement = AaInnerLoop(p, furthestBlock, formattingElementEntry.Element);
                var commonAncestor = p.openElements.GetCommonAncestor(formattingElementEntry.Element);

                p.DetachNode(lastElement);
                AaInsertLastNodeInCommonAncestor(p, commonAncestor, lastElement);
                AaReplaceFormattingElement(p, furthestBlock, formattingElementEntry);
            }
        }


        //Generic token handlers
        //------------------------------------------------------------------
        static void IgnoreToken(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            //NOTE: do nothing =)
        }

        static void AppendComment(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.AppendCommentNode((CommentToken) token, p.openElements.CurrentTmplContent ?? p.openElements.Current); //|| operator
        }

        static void AppendCommentToRootHtmlElement(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.AppendCommentNode((CommentToken) token, p.openElements[0]);
        }

        static void AppendCommentToDocument(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.AppendCommentNode((CommentToken) token, p.document);
        }

        static void InsertCharacters(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.InsertCharacters((CharacterToken) token);
        }

        static void InsertCharacters(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, CharacterToken token)
        {
            p.InsertCharacters(token);
        }

        static void StopParsing(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.stopped = true;
        }

        //12.2.5.4.1 The "initial" insertion mode
        //------------------------------------------------------------------
        static void DoctypeInInitialMode(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token tokenObject)
        {
            var token = (DoctypeToken) tokenObject;
            p.SetDocumentType(token);

            var mode = token.ForceQuirks ?
                HTML.DOCUMENT_MODE.QUIRKS :
                Doctype.GetDocumentMode(token.Name, token.PublicId, token.SystemId);

            p.doc.Mode = mode;

            p.insertionMode = BEFORE_HTML_MODE;
        }

        static void TokenInInitialMode(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.doc.Mode = HTML.DOCUMENT_MODE.QUIRKS;
            p.insertionMode = BEFORE_HTML_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.2 The "before html" insertion mode
        //------------------------------------------------------------------
        static void StartTagBeforeHtml(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (token.TagName == T.HTML)
            {
                p.InsertElement(token, NS.HTML);
                p.insertionMode = BEFORE_HEAD_MODE;
            }

            else
                TokenBeforeHtml(p, token);
        }

        static void EndTagBeforeHtml(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            var tn = token.TagName;

            if (tn == T.HTML || tn == T.HEAD || tn == T.BODY || tn == T.BR)
                TokenBeforeHtml(p, token);
        }

        static void TokenBeforeHtml(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.InsertFakeRootElement();
            p.insertionMode = BEFORE_HEAD_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.3 The "before head" insertion mode
        //------------------------------------------------------------------
        static void StartTagBeforeHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagBeforeHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            var tn = token.TagName;

            if (tn == T.HEAD || tn == T.BODY || tn == T.HTML || tn == T.BR)
                TokenBeforeHead(p, token);
        }

        static void TokenBeforeHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.InsertFakeElement(T.HEAD);
            p.headElement = (Element) p.openElements.Current;
            p.insertionMode = IN_HEAD_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.4 The "in head" insertion mode
        //------------------------------------------------------------------
        static void StartTagInHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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

        static void TokenInHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.openElements.Pop();
            p.insertionMode = AFTER_HEAD_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.6 The "after head" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagAfterHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            var tn = token.TagName;

            if (tn == T.BODY || tn == T.HTML || tn == T.BR)
                TokenAfterHead(p, token);

            else if (tn == T.TEMPLATE)
                EndTagInHead(p, token);
        }

        static void TokenAfterHead(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.InsertFakeElement(T.BODY);
            p.insertionMode = IN_BODY_MODE;
            p.ProcessToken(token);
        }


        //12.2.5.4.7 The "in body" insertion mode
        //------------------------------------------------------------------
        static void WhitespaceCharacterInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertCharacters((CharacterToken) token);
        }

        static void CharacterInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertCharacters((CharacterToken) token);
            p.framesetOk = false;
        }

        static void HtmlStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.TmplCount == 0)
                using (var attrs = PooledArray<Attr>.GetInstance())
                    p.treeBuilder.AdoptAttributes(p.openElements[0], token.CopyAttrsTo(attrs.Array, p.CreateAttribute));
        }

        static void BodyStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            var bodyElement = p.openElements.TryPeekProperlyNestedBodyElement();

            if (bodyElement != null && p.openElements.TmplCount == 0)
            {
                p.framesetOk = false;
                using (var attrs = PooledArray<Attr>.GetInstance())
                    p.treeBuilder.AdoptAttributes(bodyElement, token.CopyAttrsTo(attrs.Array, p.CreateAttribute));
            }
        }

        static void FramesetStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            var bodyElement = p.openElements.TryPeekProperlyNestedBodyElement();

            if (p.framesetOk && bodyElement != null)
            {
                p.DetachNode(bodyElement);
                p.openElements.PopAllUpToHtmlElement();
                p.InsertElement(token, NS.HTML);
                p.insertionMode = IN_FRAMESET_MODE;
            }
        }

        static void AddressStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
        }

        static void NumberedHeaderStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            var tn = p.openElements.CurrentTagName;

            if (tn == T.H1 || tn == T.H2 || tn == T.H3 || tn == T.H4 || tn == T.H5 || tn == T.H6)
                p.openElements.Pop();

            p.InsertElement(token, NS.HTML);
        }

        static void PreStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
            //NOTE: If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move
            //on to the next one. (Newlines at the start of pre blocks are ignored as an authoring convenience.)
            p.skipNextNewLine = true;
            p.framesetOk = false;
        }

        static void FormStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            var inTemplate = p.openElements.TmplCount > 0;

            if (p.formElement == null || inTemplate)
            {
                if (p.openElements.HasInButtonScope(T.P))
                    p.ClosePElement();

                p.InsertElement(token, NS.HTML);

                if (!inTemplate)
                    p.formElement = (Element) p.openElements.Current;
            }
        }

        static void ListItemStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.framesetOk = false;

            var tn = token.TagName;

            for (var i = p.openElements.StackTop; i >= 0; i--)
            {
                var element = p.openElements[i];
                var elementTn = p.treeBuilder.GetTagName(element);
                string closeTn = null;

                if (tn == T.LI && elementTn == T.LI)
                    closeTn = T.LI;

                else if ((tn == T.DD || tn == T.DT) && (elementTn == T.DD || elementTn == T.DT))
                    closeTn = elementTn;

                if (IsTruthy(closeTn))
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

        static void PlaintextStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
            p.tokenizer.State = MODE.PLAINTEXT;
        }

        static void ButtonStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void AStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            var activeElementEntry = p.activeFormattingElements.GetElementEntryInScopeWithTagName(T.A);

            if (activeElementEntry != null)
            {
                CallAdoptionAgency(p, token);
                p.openElements.Remove(activeElementEntry.Element);
                p.activeFormattingElements.RemoveEntry(activeElementEntry);
            }

            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
            p.activeFormattingElements.PushElement((Element) p.openElements.Current, token);
        }

        static void BStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
            p.activeFormattingElements.PushElement((Element) p.openElements.Current, token);
        }

        static void NobrStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void AppletStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
            p.activeFormattingElements.InsertMarker();
            p.framesetOk = false;
        }

        static void TableStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            var mode = p.doc?.Mode;
            if (mode != HTML.DOCUMENT_MODE.QUIRKS && p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.InsertElement(token, NS.HTML);
            p.framesetOk = false;
            p.insertionMode = IN_TABLE_MODE;
        }

        static void AreaStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.ReconstructActiveFormattingElements();
            p.AppendElement(token, NS.HTML);
            p.framesetOk = false;
        }

        static void InputStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.ReconstructActiveFormattingElements();
            p.AppendElement(token, NS.HTML);

            var inputType = Tokenizer.GetTokenAttr(token, ATTRS.TYPE);

            if (!IsTruthy(inputType) || inputType.ToLowerCase() != HIDDEN_INPUT_TYPE)
                p.framesetOk = false;

        }

        static void ParamStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.AppendElement(token, NS.HTML);
        }

        static void HrStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            if (p.openElements.CurrentTagName == T.MENUITEM)
                p.openElements.Pop();

            p.AppendElement(token, NS.HTML);
            p.framesetOk = false;
        }

        static void ImageStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            token.TagName = T.IMG;
            AreaStartTagInBody(p, token);
        }

        static void TextareaStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void XmpStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            p.ReconstructActiveFormattingElements();
            p.framesetOk = false;
            p.SwitchToTextParsing(token, MODE.RAWTEXT);
        }

        static void IframeStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.framesetOk = false;
            p.SwitchToTextParsing(token, MODE.RAWTEXT);
        }

        //NOTE: here we assume that we always act as an user agent with enabled plugins, so we parse
        //<noembed> as a rawtext.
        static void NoembedStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.SwitchToTextParsing(token, MODE.RAWTEXT);
        }

        static void SelectStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void OptgroupStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.CurrentTagName == T.OPTION)
                p.openElements.Pop();

            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
        }

        static void RbStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInScope(T.RUBY))
                p.openElements.GenerateImpliedEndTags();

            p.InsertElement(token, NS.HTML);
        }

        static void RtStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInScope(T.RUBY))
                p.openElements.GenerateImpliedEndTagsWithExclusion(T.RTC);

            p.InsertElement(token, NS.HTML);
        }

        static void MenuitemStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.CurrentTagName == T.MENUITEM)
                p.openElements.Pop();

            // TODO needs clarification, see https://github.com/whatwg/html/pull/907/files#r73505877
            p.ReconstructActiveFormattingElements();

            p.InsertElement(token, NS.HTML);
        }

        static void MenuStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInButtonScope(T.P))
                p.ClosePElement();

            if (p.openElements.CurrentTagName == T.MENUITEM)
                p.openElements.Pop();

            p.InsertElement(token, NS.HTML);
        }

        static void MathStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.ReconstructActiveFormattingElements();

            AdjustTokenMathMlAttrs(token);
            AdjustTokenXmlAttrs(token);

            if (token.SelfClosing)
                p.AppendElement(token, NS.MATHML);
            else
                p.InsertElement(token, NS.MATHML);
        }

        static void SvgStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.ReconstructActiveFormattingElements();

            AdjustTokenSvgAttrs(token);
            AdjustTokenXmlAttrs(token);

            if (token.SelfClosing)
                p.AppendElement(token, NS.SVG);
            else
                p.InsertElement(token, NS.SVG);
        }

        static void GenericStartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertElement(token, NS.HTML);
        }

        //OPTIMIZATION: Integer comparisons are low-cost, so we can use very fast tag name.Length filters here.
        //It's faster than using dictionary.
        static void StartTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void BodyEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p)
        {
            if (p.openElements.HasInScope(T.BODY))
                p.insertionMode = AFTER_BODY_MODE;
        }

        static void BodyEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            BodyEndTagInBody(p);
        }

        static void HtmlEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            if (p.openElements.HasInScope(T.BODY))
            {
                p.insertionMode = AFTER_BODY_MODE;
                p.ProcessToken(token);
            }
        }

        static void AddressEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            var tn = token.TagName;

            if (p.openElements.HasInScope(tn))
            {
                p.openElements.GenerateImpliedEndTags();
                p.openElements.PopUntilTagNamePopped(tn);
            }
        }

        static void FormEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p)
        {
            var inTemplate = p.openElements.TmplCount > 0;
            var formElement = p.formElement;

            if (!inTemplate)
                p.formElement = null;

            if ((formElement != null || inTemplate) && p.openElements.HasInScope(T.FORM))
            {
                p.openElements.GenerateImpliedEndTags();

                if (inTemplate)
                    p.openElements.PopUntilTagNamePopped(T.FORM);

                else
                    p.openElements.Remove(formElement);
            }
        }

        static void FormEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            FormEndTagInBody(p);
        }

        static void PEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p)
        {
            if (!p.openElements.HasInButtonScope(T.P))
                p.InsertFakeElement(T.P);

            p.ClosePElement();
        }

        static void PEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            PEndTagInBody(p);
        }

        static void LiEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p)
        {
            if (p.openElements.HasInListItemScope(T.LI))
            {
                p.openElements.GenerateImpliedEndTagsWithExclusion(T.LI);
                p.openElements.PopUntilTagNamePopped(T.LI);
            }
        }

        static void LiEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            LiEndTagInBody(p);
        }

        static void DdEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            var tn = token.TagName;

            if (p.openElements.HasInScope(tn))
            {
                p.openElements.GenerateImpliedEndTagsWithExclusion(tn);
                p.openElements.PopUntilTagNamePopped(tn);
            }
        }

        static void NumberedHeaderEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p)
        {
            if (p.openElements.HasNumberedHeaderInScope())
            {
                p.openElements.GenerateImpliedEndTags();
                p.openElements.PopUntilNumberedHeaderPopped();
            }
        }

        static void NumberedHeaderEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            NumberedHeaderEndTagInBody(p);
        }

        static void AppletEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            var tn = token.TagName;

            if (p.openElements.HasInScope(tn))
            {
                p.openElements.GenerateImpliedEndTags();
                p.openElements.PopUntilTagNamePopped(tn);
                p.activeFormattingElements.ClearToLastMarker();
            }
        }

        static void BrEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p)
        {
            p.ReconstructActiveFormattingElements();
            p.InsertFakeElement(T.BR);
            p.openElements.Pop();
            p.framesetOk = false;
        }

        static void BrEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            BrEndTagInBody(p);
        }

        static void GenericEndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            var tn = token.TagName;

            for (var i = p.openElements.StackTop; i > 0; i--)
            {
                var element = p.openElements[i];

                if (p.treeBuilder.GetTagName(element) == tn)
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
        static void EndTagInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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


        static void EofInBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            if (p.tmplInsertionModeStackTop > -1)
                EofInTemplate(p, token);

            else
                p.stopped = true;
        }

        //12.2.5.4.8 The "text" insertion mode
        //------------------------------------------------------------------
        static void EndTagInText(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            if (token.TagName == T.SCRIPT)
                p.pendingScript = (Element) p.openElements.Current;

            p.openElements.Pop();
            p.insertionMode = p.originalInsertionMode;
        }


        static void EofInText(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.openElements.Pop();
            p.insertionMode = p.originalInsertionMode;
            p.ProcessToken(token);
        }


        //12.2.5.4.9 The "in table" insertion mode
        //------------------------------------------------------------------
        static void CharacterInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
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

        static void CaptionStartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.openElements.ClearBackToTableContext();
            p.activeFormattingElements.InsertMarker();
            p.InsertElement(token, NS.HTML);
            p.insertionMode = IN_CAPTION_MODE;
        }

        static void ColgroupStartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.openElements.ClearBackToTableContext();
            p.InsertElement(token, NS.HTML);
            p.insertionMode = IN_COLUMN_GROUP_MODE;
        }

        static void ColStartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.openElements.ClearBackToTableContext();
            p.InsertFakeElement(T.COLGROUP);
            p.insertionMode = IN_COLUMN_GROUP_MODE;
            p.ProcessToken(token);
        }

        static void TbodyStartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.openElements.ClearBackToTableContext();
            p.InsertElement(token, NS.HTML);
            p.insertionMode = IN_TABLE_BODY_MODE;
        }

        static void TdStartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            p.openElements.ClearBackToTableContext();
            p.InsertFakeElement(T.TBODY);
            p.insertionMode = IN_TABLE_BODY_MODE;
            p.ProcessToken(token);
        }

        static void TableStartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.openElements.HasInTableScope(T.TABLE))
            {
                p.openElements.PopUntilTagNamePopped(T.TABLE);
                p.ResetInsertionMode();
                p.ProcessToken(token);
            }
        }

        static void InputStartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            var inputType = Tokenizer.GetTokenAttr(token, ATTRS.TYPE);

            if (IsTruthy(inputType) && inputType.ToLowerCase() == HIDDEN_INPUT_TYPE)
                p.AppendElement(token, NS.HTML);

            else
                TokenInTable(p, token);
        }

        static void FormStartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (p.formElement == null && p.openElements.TmplCount == 0)
            {
                p.InsertElement(token, NS.HTML);
                p.formElement = (Element) p.openElements.Current;
                p.openElements.Pop();
            }
        }

        static void StartTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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

        static void TokenInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            var savedFosterParentingState = p.fosterParentingEnabled;

            p.fosterParentingEnabled = true;
            p.ProcessTokenInBodyMode(token);
            p.fosterParentingEnabled = savedFosterParentingState;
        }


        //12.2.5.4.10 The "in table text" insertion mode
        //------------------------------------------------------------------
        static void WhitespaceCharacterInTableText(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.pendingCharacterTokens.Push(token);
        }

        static void CharacterInTableText(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.pendingCharacterTokens.Push(token);
            p.hasNonWhitespacePendingCharacterToken = true;
        }

        static void TokenInTableText(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            if (p.hasNonWhitespacePendingCharacterToken)
            {
                foreach (CharacterToken pendingCharacterToken in p.pendingCharacterTokens)
                    TokenInTable(p, pendingCharacterToken);
            }

            else
            {
                foreach (CharacterToken pendingCharacterToken in p.pendingCharacterTokens)
                    p.InsertCharacters(pendingCharacterToken);
            }

            p.insertionMode = p.originalInsertionMode;
            p.ProcessToken(token);
        }


        //12.2.5.4.11 The "in caption" insertion mode
        //------------------------------------------------------------------
        static void StartTagInCaption(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInCaption(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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
        static void StartTagInColumnGroup(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInColumnGroup(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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

        static void TokenInColumnGroup(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
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
        static void StartTagInTableBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInTableBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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
        static void StartTagInRow(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInRow(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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
        static void StartTagInCell(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInCell(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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
        static void StartTagInSelect(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInSelect(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            var tn = token.TagName;

            if (tn == T.OPTGROUP)
            {
                var prevOpenElement = p.openElements[p.openElements.StackTop - 1];
                var prevOpenElementTn = // prevOpenElement && p.treeAdapter.getTagName(prevOpenElement)
                                        prevOpenElement != null ? p.treeBuilder.GetTagName(prevOpenElement) : null;

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
        static void StartTagInSelectInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInSelectInTable(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
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
        static void StartTagInTemplate(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInTemplate(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            if (token.TagName == T.TEMPLATE)
                EndTagInHead(p, token);
        }

        static void EofInTemplate(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
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
        static void StartTagAfterBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (token.TagName == T.HTML)
                StartTagInBody(p, token);

            else
                TokenAfterBody(p, token);
        }

        static void EndTagAfterBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            if (token.TagName == T.HTML)
            {
                if (p.fragmentContext == null)
                    p.insertionMode = AFTER_AFTER_BODY_MODE;
            }

            else
                TokenAfterBody(p, token);
        }

        static void TokenAfterBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.insertionMode = IN_BODY_MODE;
            p.ProcessToken(token);
        }

        //12.2.5.4.20 The "in frameset" insertion mode
        //------------------------------------------------------------------
        static void StartTagInFrameset(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
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

        static void EndTagInFrameset(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            if (token.TagName == T.FRAMESET && !p.openElements.IsRootHtmlElementCurrent())
            {
                p.openElements.Pop();

                if (p.fragmentContext == null && p.openElements.CurrentTagName != T.FRAMESET)
                    p.insertionMode = AFTER_FRAMESET_MODE;
            }
        }

        //12.2.5.4.21 The "after frameset" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterFrameset(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.NOFRAMES)
                StartTagInHead(p, token);
        }

        static void EndTagAfterFrameset(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            if (token.TagName == T.HTML)
                p.insertionMode = AFTER_AFTER_FRAMESET_MODE;
        }

        //12.2.5.4.22 The "after after body" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterAfterBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (token.TagName == T.HTML)
                StartTagInBody(p, token);

            else
                TokenAfterAfterBody(p, token);
        }

        static void TokenAfterAfterBody(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, Token token)
        {
            p.insertionMode = IN_BODY_MODE;
            p.ProcessToken(token);
        }

        //12.2.5.4.23 The "after after frameset" insertion mode
        //------------------------------------------------------------------
        static void StartTagAfterAfterFrameset(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            var tn = token.TagName;

            if (tn == T.HTML)
                StartTagInBody(p, token);

            else if (tn == T.NOFRAMES)
                StartTagInHead(p, token);
        }


        //12.2.5.5 The rules for parsing tokens in foreign content
        //------------------------------------------------------------------
        static void NullCharacterInForeignContent(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, CharacterToken token)
        {
            token.Clear(); token.Append(Unicode.ReplacementCharacter);
            p.InsertCharacters(token);
        }

        static void CharacterInForeignContent(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, CharacterToken token)
        {
            p.InsertCharacters(token);
            p.framesetOk = false;
        }

        static void StartTagInForeignContent(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, StartTagToken token)
        {
            if (CausesExit(token) && p.fragmentContext == null)
            {
                while (p.treeBuilder.GetNamespaceUri((Element) p.openElements.Current) != NS.HTML && !p.IsIntegrationPoint((Element) p.openElements.Current))
                    p.openElements.Pop();

                p.ProcessToken(token);
            }

            else
            {
                var current = (Element) p.GetAdjustedCurrentElement();
                var currentNs = p.treeBuilder.GetNamespaceUri(current);

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

        static void EndTagInForeignContent(Parser<Node, Container, DocumentFragment, Element, Attr, TemplateElement, Comment> p, EndTagToken token)
        {
            for (var i = p.openElements.StackTop; i > 0; i--)
            {
                var element = p.openElements[i];

                if (p.treeBuilder.GetNamespaceUri(element) == NS.HTML)
                {
                    p.ProcessToken(token);
                    break;
                }

                if (p.treeBuilder.GetTagName(element).ToLowerCase() == token.TagName)
                {
                    p.openElements.PopUntilElementPopped(element);
                    break;
                }
            }
        }
    }
}
