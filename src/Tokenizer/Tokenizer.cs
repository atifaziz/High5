using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Attrs = ParseFive.Extensions.List<Attr>;
using TempBuff = ParseFive.Extensions.List<int>;
using TokenQueue = ParseFive.Extensions.List<ParseFive.Tokenizer.Token>;
using ɑ = ParseFive.Common.Unicode.CODE_POINTS;
using ɑɑ = ParseFive.Common.Unicode.CODE_POINT_SEQUENCES;
using static ParseFive.Tokenizer.Index;
using ParseFive.Extensions;
using static ParseFive.Tokenizer.NamedEntityData;

namespace ParseFive.Tokenizer
{
    using System.Linq;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Method)]
    sealed class _Attribute : Attribute
    {
        public string State { get; }

        public _Attribute(string state) => State = state;
    }

    public class Token
    {
        public string type { get; set; }
        public string tagName { get; set; }
        public Attrs attrs { get; set; }
        public bool selfClosing { get; set; }
        public string data { get; set; }
        public string name { get; set; }
        public bool forceQuirks { get; set; }
        public string publicId { get; set; }
        public string systemId { get; set; }
        public string chars { get; set; }

        public Token(string type, string tagName, bool selfClosing, Attrs attrs) //START TAG
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
    

    public Token(string type, string tagName, Attrs attrs) //end tag
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


    class Tokenizer
    {
        //Token types
        public const string CHARACTER_TOKEN = "CHARACTER_TOKEN";
        public const string NULL_CHARACTER_TOKEN = "NULL_CHARACTER_TOKEN";
        public const string WHITESPACE_CHARACTER_TOKEN = "WHITESPACE_CHARACTER_TOKEN";
        public const string START_TAG_TOKEN = "START_TAG_TOKEN";
        public const string END_TAG_TOKEN = "END_TAG_TOKEN";
        public const string COMMENT_TOKEN = "COMMENT_TOKEN";
        public const string DOCTYPE_TOKEN = "DOCTYPE_TOKEN";
        public const string EOF_TOKEN = "EOF_TOKEN";
        public const string HIBERNATION_TOKEN = "HIBERNATION_TOKEN";

        //Fields
        private Preprocessor preprocessor;
        private TokenQueue tokenQueue;
        public bool allowCDATA;
        public string state { get ; set ; }
        private string returnState;
        private TempBuff tempBuff;
        private int additionalAllowedCp;
        private string lastStartTagName;
        private int consumedAfterSnapshot;
        private bool active;
        private Token currentCharacterToken;
        private Token currentToken;
        private Attr currentAttr;
        private object options;


        //Tokenizer
        public Tokenizer()
        {
            this.preprocessor = new Preprocessor();

            this.tokenQueue = new TokenQueue();

            this.allowCDATA = false;

            this.state = Index.DATA_STATE;
            this.returnState = "";

            this.tempBuff = new TempBuff();
            this.additionalAllowedCp = 0; //void 0
            this.lastStartTagName = "";

            this.consumedAfterSnapshot = -1;
            this.active = false;

            this.currentCharacterToken = null;
            this.currentToken = null;
            this.currentAttr = null;
        }

        Dictionary<string, Action<int>> _actionByState;

        Action<int> this[string state] =>
            (_actionByState ?? (_actionByState = ReflectStateMachine().ToDictionary(e => e.State, e => e.Action)))[state];

        IEnumerable<(string State, Action<int> Action)> ReflectStateMachine() =>
            from m in GetType().GetRuntimeMethods()
            where m.IsPrivate && !m.IsStatic
            select (State: m.GetCustomAttribute<_Attribute>()?.State.Trim(), Method: m) into e
            where !string.IsNullOrEmpty(e.State)
            select (e.State, (Action<int>) e.Method.CreateDelegate(typeof(Action<int>), this));

        //Tokenizer initial states for different modes
        public static class MODE
        {
            public static string DATA = Index.DATA_STATE;
            public static string RCDATA = Index.RCDATA_STATE;
            public static string RAWTEXT = Index.RAWTEXT_STATE;
            public static string SCRIPT_DATA = Index.SCRIPT_DATA_STATE;
            public static string PLAINTEXT = Index.PLAINTEXT_STATE;
        }

        //Static
        public static string getTokenAttr(Token token, string attrName)
        {
            for (var i = token.attrs.length - 1; i >= 0; i--)
            {
                if (token.attrs[i].name == attrName)
                    return token.attrs[i].value;
            }

            return null;
        }

        //API
        public Token getNextToken()
        {
            while (!this.tokenQueue.length.IsTruthy() && this.active)
            {
                this.hibernationSnapshot();

                var cp = this.consume();

                if (!this.ensureHibernation())
                    this[this.state](cp);
            }

            return tokenQueue.shift();
        }

        public void write(string chunk, bool isLastChunk)
        {
            this.active = true;
            this.preprocessor.write(chunk, isLastChunk);
        }

        public void insertHtmlAtCurrentPos(string chunk)
        {
            this.active = true;
            this.preprocessor.insertHtmlAtCurrentPos(chunk);
        }

        //Hibernation
        public void hibernationSnapshot()
        {
            this.consumedAfterSnapshot = 0;
        }

        public bool ensureHibernation()
        {
            if (this.preprocessor.endOfChunkHit)
            {
                for (; this.consumedAfterSnapshot > 0; this.consumedAfterSnapshot--)
                    this.preprocessor.retreat();

                this.active = false;
                this.tokenQueue.push(new Token(HIBERNATION_TOKEN));

                return true;
            }

            return false;
        }


        //Consumption
        public int consume()
        {
            this.consumedAfterSnapshot++;
            return this.preprocessor.advance();
        }

        public void unconsume()
        {
            this.consumedAfterSnapshot--;
            this.preprocessor.retreat();
        }

        void unconsumeSeveral(int count)
        {
            while ((count--).IsTruthy())
                this.unconsume();
        }

        void reconsumeInState(string state)
        {
            this.state = state;
            this.unconsume();
        }

        bool consumeSubsequentIfMatch(Array<int> pattern, int startCp, bool caseSensitive)
        {
            var consumedCount = 0;
            bool isMatch = true;
            var patternLength = pattern.length;
            int patternPos = 0;
            int cp = startCp;
            int? patternCp = null;//void 0;

            for (; patternPos < patternLength; patternPos++)
            {
                if (patternPos > 0)
                {
                    cp = this.consume();
                    consumedCount++;
                }

                if (cp == ɑ.EOF)
                {
                    isMatch = false;
                    break;
                }

                patternCp = pattern[patternPos];

                if (cp != patternCp && (caseSensitive || cp != Index.toAsciiLowerCodePoint(patternCp.Value)))
                {
                    isMatch = false;
                    break;
                }
            }

            if (!isMatch)
                this.unconsumeSeveral(consumedCount);

            return isMatch;
        }

        //Lookahead
        int lookahead()
        {
            var cp = this.consume();

            this.unconsume();

            return cp;
        }

        //Temp buffer
        bool isTempBufferEqualToScriptString()
        {
            if (this.tempBuff.length != ɑɑ.SCRIPT_STRING.length)
                return false;

            for (var i = 0; i < this.tempBuff.length; i++)
            {
                if (this.tempBuff[i] != ɑɑ.SCRIPT_STRING[i])
                    return false;
            }

            return true;
        }

        //Token creation
        void createStartTagToken()
        {
            this.currentToken = new Token(START_TAG_TOKEN, "", false, new Attrs());
        }

        void createEndTagToken()
        {
            this.currentToken = new Token(END_TAG_TOKEN, "", new Attrs());
        }

        void createCommentToken()
        {
            this.currentToken = new Token(COMMENT_TOKEN, "");
        }

        void createDoctypeToken(string initialName)
        {
            this.currentToken = new Token(DOCTYPE_TOKEN, name: initialName, forceQuirks: false, publicId: null, systemId: null);
        }

        void createCharacterToken(string type, char ch)
        {
            this.currentCharacterToken = new Token(type, ch);
        }

        //Tag attributes
        void createAttr(string attrNameFirstCh) //TODO Check if string or char
        {
            this.currentAttr = new Attr(attrNameFirstCh, "");
        }

        bool isDuplicateAttr()
        {
            return getTokenAttr(this.currentToken, this.currentAttr.name) != null;
        }

        void leaveAttrName(string toState)
        {
            this.state = toState;

            if (!this.isDuplicateAttr())
                this.currentToken.attrs.push(this.currentAttr);
        }

        void leaveAttrValue(string toState)
        {
            this.state = toState;
        }

        //Appropriate end tag token
        //(see: http://www.whatwg.org/specs/web-apps/current-work/multipage/tokenization.html#appropriate-end-tag-token)
        bool isAppropriateEndTagToken()
        {
            return this.lastStartTagName == this.currentToken.tagName;
        }

        //Token emission
        void emitCurrentToken()
        {
            this.emitCurrentCharacterToken();

            //NOTE: store emited start tag's tagName to determine is the following end tag token is appropriate.
            if (this.currentToken.type == START_TAG_TOKEN)
                this.lastStartTagName = this.currentToken.tagName;

            this.tokenQueue.push(this.currentToken);
            this.currentToken = null;
        }

        void emitCurrentCharacterToken()
        {
            if (this.currentCharacterToken.IsTruthy())
            {
                this.tokenQueue.push(this.currentCharacterToken);
                this.currentCharacterToken = null;
            }
        }

        void emitEOFToken()
        {
            this.emitCurrentCharacterToken();
            this.tokenQueue.push(new Token(EOF_TOKEN));
        }

        //Characters emission

        //OPTIMIZATION: specification uses only one type of character tokens (one token per character).
        //This causes a huge memory overhead and a lot of unnecessary parser loops. parse5 uses 3 groups of characters.
        //If we have a sequence of characters that belong to the same group, parser can process it
        //as a single solid character token.
        //So, there are 3 types of character tokens in parse5:
        //1)NULL_CHARACTER_TOKEN - \u0000-character sequences (e.g. '\u0000\u0000\u0000')
        //2)WHITESPACE_CHARACTER_TOKEN - any whitespace/new-line character sequences (e.g. '\n  \r\t   \f')
        //3)CHARACTER_TOKEN - any character sequence which don't belong to groups 1 and 2 (e.g. 'abcdef1234@@#ɑ%^')
        void appendCharToCurrentCharacterToken(string type, char ch)
        {
            if (this.currentCharacterToken.IsTruthy() && this.currentCharacterToken.type != type)
                this.emitCurrentCharacterToken();

            if (this.currentCharacterToken.IsTruthy())
                this.currentCharacterToken.chars += ch;

            else
                this.createCharacterToken(type, ch);
        }

        void emitCodePoint(int cp)
        {
            var type = CHARACTER_TOKEN;

            if (Index.isWhitespace(cp))
                type = WHITESPACE_CHARACTER_TOKEN;

            else if (cp == ɑ.NULL)
                type = NULL_CHARACTER_TOKEN;

            this.appendCharToCurrentCharacterToken(type, toChar(cp));
        }

        void emitSeveralCodePoints(ISomeList<int> codePoints)
        {
            for (var i = 0; i < codePoints.length; i++)
                this.emitCodePoint(codePoints[i]);
        }

        //NOTE: used then we emit character explicitly. This is always a non-whitespace and a non-null character.
        //So we can avoid additional checks here.
        void emitChar(char ch)
        {
            this.appendCharToCurrentCharacterToken(CHARACTER_TOKEN, ch);
        }

        //Character reference tokenization
        int consumeNumericEntity(bool isHex)
        {
            var digits = "";
            var nextCp = 0;

            do
            {
                digits += toChar(this.consume());
                nextCp = this.lookahead();
            } while (nextCp != ɑ.EOF && Index.isDigit(nextCp, isHex));

            if (this.lookahead() == ɑ.SEMICOLON)
                this.consume();

            int referencedCp = Extensions.Extensions.parseInt(digits, isHex ? 16 : 10);
            

            if (Index.NUMERIC_ENTITY_REPLACEMENTS.TryGetValue(referencedCp, out var replacement))
                return replacement;

            if (Index.isReservedCodePoint(referencedCp))
                return ɑ.REPLACEMENT_CHARACTER;

            return referencedCp;
        }

        // NOTE: for the details on this algorithm see
        // https://github.com/inikulin/parse5/tree/master/scripts/generate_named_entity_data/README.md
        Array<int> consumeNamedEntity(bool inAttr)
        {
            Array<int> referencedCodePoints = null;
            int referenceSize = 0;
            int cp = 0;
            var consumedCount = 0;
            var semicolonTerminated = false;

            for (var i = 0; i > -1;)
            {
                var current = neTree[i];
                var inNode = current < Index.MAX_BRANCH_MARKER_VALUE;
                var nodeWithData = inNode && (current & Index.HAS_DATA_FLAG) == Index.HAS_DATA_FLAG;

                if (nodeWithData)
                {
                    referencedCodePoints = new Array<int>((current & Index.DATA_DUPLET_FLAG) == Index.DATA_DUPLET_FLAG ? new[] { neTree[++i], neTree[++i] } : new[] { neTree[++i] });
                    referenceSize = consumedCount;

                    if (cp == ɑ.SEMICOLON)
                    {
                        semicolonTerminated = true;
                        break;
                    }


                }

                cp = this.consume();


                consumedCount++;

                if (cp == ɑ.EOF)
                    break;

                if (inNode)
                    i = (current & Index.HAS_BRANCHES_FLAG) == Index.HAS_BRANCHES_FLAG ? Index.findNamedEntityTreeBranch(i, cp) : -1; 

                else
                    i = cp == current ? ++i : -1;
            }


            if (referencedCodePoints.IsTruthy())
            {
                if (!semicolonTerminated)
                {
                    //NOTE: unconsume excess (e.g. 'it' in '&notit')
                    this.unconsumeSeveral(consumedCount - referenceSize);

                    //NOTE: If the character reference is being consumed as part of an attribute and the next character
                    //is either a U+003D EQUALS SIGN character (=) or an alphanumeric ASCII character, then, for historical
                    //reasons, all the characters that were matched after the U+0026 AMPERSAND character (&) must be
                    //unconsumed, and nothing is returned.
                    //However, if this next character is in fact a U+003D EQUALS SIGN character (=), then this is a
                    //parse error, because some legacy user agents will misinterpret the markup in those cases.
                    //(see: http://www.whatwg.org/specs/web-apps/current-work/multipage/tokenization.html#tokenizing-character-references)
                    if (inAttr)
                    {
                        var nextCp = this.lookahead();

                        if (nextCp == ɑ.EQUALS_SIGN || Index.isAsciiAlphaNumeric(nextCp))
                        {
                            this.unconsumeSeveral(referenceSize);
                            return null;
                        }
                    }
                }

                return referencedCodePoints;
            }

            this.unconsumeSeveral(consumedCount);

            return null;
        }

        Array<int> consumeCharacterReference(int startCp, bool inAttr)
        {
            if (Index.isWhitespace(startCp) || startCp == ɑ.GREATER_THAN_SIGN ||
                startCp == ɑ.AMPERSAND || startCp == this.additionalAllowedCp || startCp == ɑ.EOF)
            {
                //NOTE: not a character reference. No characters are consumed, and nothing is returned.
                this.unconsume();
                return null;
            }

            if (startCp == ɑ.NUMBER_SIGN)
            {
                //NOTE: we have a numeric entity candidate, now we should determine if it's hex or decimal
                bool isHex = false;
                int nextCp = this.lookahead();

                if (nextCp == ɑ.LATIN_SMALL_X || nextCp == ɑ.LATIN_CAPITAL_X)
                {
                    this.consume();
                    isHex = true;
                }

                nextCp = this.lookahead();

                //NOTE: if we have at least one digit this is a numeric entity for sure, so we consume it
                if (nextCp != ɑ.EOF && Index.isDigit(nextCp, isHex))
                    return new Array<int>(new[] { this.consumeNumericEntity(isHex) });

                //NOTE: otherwise this is a bogus number entity and a parse error. Unconsume the number sign
                //and the 'x'-character if appropriate.
                this.unconsumeSeveral(isHex ? 2 : 1);
                return null;
            }

            this.unconsume();

            return this.consumeNamedEntity(inAttr);
        }

        //12.2.4.1 Data state
        //------------------------------------------------------------------
        [_(Index.DATA_STATE)]
        void dataState(int cp) {
            this.preprocessor.dropParsedChunk();

            if (cp == ɑ.AMPERSAND)
                this.state = CHARACTER_REFERENCE_IN_DATA_STATE;

            else if (cp == ɑ.LESS_THAN_SIGN)
                this.state = TAG_OPEN_STATE;

            else if (cp == ɑ.NULL)
                this.emitCodePoint(cp);

            else if (cp == ɑ.EOF)
                this.emitEOFToken();

            else
                this.emitCodePoint(cp);
        }

        //12.2.4.2 Character reference in data state
        //------------------------------------------------------------------
        [_(Index.CHARACTER_REFERENCE_IN_DATA_STATE)] 
        void characterReferenceInDataState(int cp)
        {
            this.additionalAllowedCp = 0; //void 0;

            var referencedCodePoints = this.consumeCharacterReference(cp, false);

            if (!this.ensureHibernation())
            {
                if (referencedCodePoints.IsTruthy())
                    this.emitSeveralCodePoints(referencedCodePoints);

                else
                    this.emitChar('&');

                this.state = DATA_STATE;
            }
        }


        //12.2.4.3 RCDATA state
        //------------------------------------------------------------------
        [_(Index.RCDATA_STATE)]
        void rcdataState(int cp)
        {
            this.preprocessor.dropParsedChunk();

            if (cp == ɑ.AMPERSAND)
        this.state = CHARACTER_REFERENCE_IN_RCDATA_STATE;

    else if (cp == ɑ.LESS_THAN_SIGN)
        this.state = RCDATA_LESS_THAN_SIGN_STATE;

    else if (cp == ɑ.NULL)
        this.emitChar((char)ɑ.REPLACEMENT_CHARACTER);

    else if (cp == ɑ.EOF)
        this.emitEOFToken();

    else
        this.emitCodePoint(cp);
        }


        //12.2.4.4 Character reference in RCDATA state
        //------------------------------------------------------------------
        [_(Index.CHARACTER_REFERENCE_IN_RCDATA_STATE)] void characterReferenceInRcdataState(int cp)
        {
            this.additionalAllowedCp = 0; //void 0;

            var referencedCodePoints = this.consumeCharacterReference(cp, false);

            if (!this.ensureHibernation())
            {
                if (referencedCodePoints.IsTruthy())
                    this.emitSeveralCodePoints(referencedCodePoints);

                else
                    this.emitChar('&');

                this.state = RCDATA_STATE;
            }
        }


        //12.2.4.5 RAWTEXT state
        //------------------------------------------------------------------
        [_(Index.RAWTEXT_STATE)] void rawtextState(int cp)
        {
            this.preprocessor.dropParsedChunk();

            if (cp == ɑ.LESS_THAN_SIGN)
        this.state = RAWTEXT_LESS_THAN_SIGN_STATE;

    else if (cp == ɑ.NULL)
        this.emitChar((char)ɑ.REPLACEMENT_CHARACTER);

    else if (cp == ɑ.EOF)
        this.emitEOFToken();

    else
        this.emitCodePoint(cp);
        }


        //12.2.4.6 Script data state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_STATE)] void scriptDataState(int cp)
        {
            this.preprocessor.dropParsedChunk();

            if (cp == ɑ.LESS_THAN_SIGN)
        this.state = SCRIPT_DATA_LESS_THAN_SIGN_STATE;

    else if (cp == ɑ.NULL)
        this.emitChar(((char)ɑ.REPLACEMENT_CHARACTER));

    else if (cp == ɑ.EOF)
        this.emitEOFToken();

    else
        this.emitCodePoint(cp);
        }


        //12.2.4.7 PLAINTEXT state
        //------------------------------------------------------------------
        [_(Index.PLAINTEXT_STATE)] void plaintextState(int cp)
        {
            this.preprocessor.dropParsedChunk();

            if (cp == ɑ.NULL)
        this.emitChar(((char)ɑ.REPLACEMENT_CHARACTER));

    else if (cp == ɑ.EOF)
        this.emitEOFToken();

    else
        this.emitCodePoint(cp);
        }


        //12.2.4.8 Tag open state
        //------------------------------------------------------------------
        [_(Index.TAG_OPEN_STATE)] void tagOpenState(int cp)
        {
            if (cp == ɑ.EXCLAMATION_MARK)
        this.state = MARKUP_DECLARATION_OPEN_STATE;

    else if (cp == ɑ.SOLIDUS)
        this.state = END_TAG_OPEN_STATE;

    else if (isAsciiLetter(cp))
            {
                this.createStartTagToken();
                this.reconsumeInState(TAG_NAME_STATE);
            }

            else if (cp == ɑ.QUESTION_MARK)
        this.reconsumeInState(BOGUS_COMMENT_STATE);

    else {
                this.emitChar('<');
                this.reconsumeInState(DATA_STATE);
            }
        }


        //12.2.4.9 End tag open state
        //------------------------------------------------------------------
        [_(Index.END_TAG_OPEN_STATE)] void endTagOpenState(int cp)
        {
            if (isAsciiLetter(cp))
            {
                this.createEndTagToken();
                this.reconsumeInState(TAG_NAME_STATE);
            }

            else if (cp == ɑ.GREATER_THAN_SIGN)
        this.state = DATA_STATE;

    else if (cp == ɑ.EOF) {
                this.reconsumeInState(DATA_STATE);
                this.emitChar('<');
                this.emitChar('/');
            }

    else
        this.reconsumeInState(BOGUS_COMMENT_STATE);
        }


        //12.2.4.10 Tag name state
        //------------------------------------------------------------------
        [_(Index.TAG_NAME_STATE)] void tagNameState(int cp)
        {
            if (isWhitespace(cp))
                this.state = BEFORE_ATTRIBUTE_NAME_STATE;

            else if (cp == ɑ.SOLIDUS)
        this.state = SELF_CLOSING_START_TAG_STATE;

    else if (cp == ɑ.GREATER_THAN_SIGN) {
                this.state = DATA_STATE;
                this.emitCurrentToken();
            }

    else if (isAsciiUpper(cp))
                this.currentToken.tagName += toAsciiLowerChar(cp);

            else if (cp == ɑ.NULL)
        this.currentToken.tagName += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else
        this.currentToken.tagName += toChar(cp);
        }


        //12.2.4.11 RCDATA less-than sign state
        //------------------------------------------------------------------
        [_(Index.RCDATA_LESS_THAN_SIGN_STATE)] void rcdataLessThanSignState(int cp)
        {
            if (cp == ɑ.SOLIDUS) {
                this.tempBuff = new TempBuff();
                this.state = RCDATA_END_TAG_OPEN_STATE;
            }

    else {
                this.emitChar('<');
                this.reconsumeInState(RCDATA_STATE);
            }
        }


        //12.2.4.12 RCDATA end tag open state
        //------------------------------------------------------------------
        [_(Index.RCDATA_END_TAG_OPEN_STATE)] void rcdataEndTagOpenState(int cp)
        {
            if (isAsciiLetter(cp))
            {
                this.createEndTagToken();
                this.reconsumeInState(RCDATA_END_TAG_NAME_STATE);
            }

            else
            {
                this.emitChar('<');
                this.emitChar('/');
                this.reconsumeInState(RCDATA_STATE);
            }
        }


        //12.2.4.13 RCDATA end tag name state
        //------------------------------------------------------------------
        [_(Index.RCDATA_END_TAG_NAME_STATE)] void rcdataEndTagNameState(int cp)
        {
            if (isAsciiUpper(cp))
            {
                this.currentToken.tagName += toAsciiLowerChar(cp);
                this.tempBuff.push(cp);
            }

            else if (isAsciiLower(cp))
            {
                this.currentToken.tagName += toChar(cp);
                this.tempBuff.push(cp);
            }

            else
            {
                if (this.isAppropriateEndTagToken())
                {
                    if (isWhitespace(cp))
                    {
                        this.state = BEFORE_ATTRIBUTE_NAME_STATE;
                        return;
                    }

                    if (cp == ɑ.SOLIDUS) {
                        this.state = SELF_CLOSING_START_TAG_STATE;
                        return;
                    }

                    if (cp == ɑ.GREATER_THAN_SIGN) {
                        this.state = DATA_STATE;
                        this.emitCurrentToken();
                        return;
                    }
                }

                this.emitChar('<');
                this.emitChar('/');
                this.emitSeveralCodePoints(this.tempBuff);
                this.reconsumeInState(RCDATA_STATE);
            }
        }


        //12.2.4.14 RAWTEXT less-than sign state
        //------------------------------------------------------------------
        [_(Index.RAWTEXT_LESS_THAN_SIGN_STATE)] void rawtextLessThanSignState(int cp)
        {
            if (cp == ɑ.SOLIDUS) {
                this.tempBuff = new TempBuff();
                this.state = RAWTEXT_END_TAG_OPEN_STATE;
            }

    else {
                this.emitChar('<');
                this.reconsumeInState(RAWTEXT_STATE);
            }
        }


        //12.2.4.15 RAWTEXT end tag open state
        //------------------------------------------------------------------
        [_(Index.RAWTEXT_END_TAG_OPEN_STATE)] void rawtextEndTagOpenState(int cp)
        {
            if (isAsciiLetter(cp))
            {
                this.createEndTagToken();
                this.reconsumeInState(RAWTEXT_END_TAG_NAME_STATE);
            }

            else
            {
                this.emitChar('<');
                this.emitChar('/');
                this.reconsumeInState(RAWTEXT_STATE);
            }
        }


        //12.2.4.16 RAWTEXT end tag name state
        //------------------------------------------------------------------
        [_(Index.RAWTEXT_END_TAG_NAME_STATE)] void rawtextEndTagNameState(int cp)
        {
            if (isAsciiUpper(cp))
            {
                this.currentToken.tagName += toAsciiLowerChar(cp);
                this.tempBuff.push(cp);
            }

            else if (isAsciiLower(cp))
            {
                this.currentToken.tagName += toChar(cp);
                this.tempBuff.push(cp);
            }

            else
            {
                if (this.isAppropriateEndTagToken())
                {
                    if (isWhitespace(cp))
                    {
                        this.state = BEFORE_ATTRIBUTE_NAME_STATE;
                        return;
                    }

                    if (cp == ɑ.SOLIDUS) {
                        this.state = SELF_CLOSING_START_TAG_STATE;
                        return;
                    }

                    if (cp == ɑ.GREATER_THAN_SIGN) {
                        this.emitCurrentToken();
                        this.state = DATA_STATE;
                        return;
                    }
                }

                this.emitChar('<');
                this.emitChar('/');
                this.emitSeveralCodePoints(this.tempBuff);
                this.reconsumeInState(RAWTEXT_STATE);
            }
        }


        //12.2.4.17 Script data less-than sign state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_LESS_THAN_SIGN_STATE)] void scriptDataLessThanSignState(int cp)
        {
            if (cp == ɑ.SOLIDUS) {
                this.tempBuff = new TempBuff();
                this.state = SCRIPT_DATA_END_TAG_OPEN_STATE;
            }

    else if (cp == ɑ.EXCLAMATION_MARK) {
                this.state = SCRIPT_DATA_ESCAPE_START_STATE;
                this.emitChar('<');
                this.emitChar('!');
            }

    else {
                this.emitChar('<');
                this.reconsumeInState(SCRIPT_DATA_STATE);
            }
        }


        //12.2.4.18 Script data end tag open state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_END_TAG_OPEN_STATE)] void scriptDataEndTagOpenState(int cp)
        {
            if (isAsciiLetter(cp))
            {
                this.createEndTagToken();
                this.reconsumeInState(SCRIPT_DATA_END_TAG_NAME_STATE);
            }

            else
            {
                this.emitChar('<');
                this.emitChar('/');
                this.reconsumeInState(SCRIPT_DATA_STATE);
            }
        }


        //12.2.4.19 Script data end tag name state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_END_TAG_NAME_STATE)] void scriptDataEndTagNameState(int cp)
        {
            if (isAsciiUpper(cp))
            {
                this.currentToken.tagName += toAsciiLowerChar(cp);
                this.tempBuff.push(cp);
            }

            else if (isAsciiLower(cp))
            {
                this.currentToken.tagName += toChar(cp);
                this.tempBuff.push(cp);
            }

            else
            {
                if (this.isAppropriateEndTagToken())
                {
                    if (isWhitespace(cp))
                    {
                        this.state = BEFORE_ATTRIBUTE_NAME_STATE;
                        return;
                    }

                    else if (cp == ɑ.SOLIDUS) {
                        this.state = SELF_CLOSING_START_TAG_STATE;
                        return;
                    }

            else if (cp == ɑ.GREATER_THAN_SIGN) {
                        this.emitCurrentToken();
                        this.state = DATA_STATE;
                        return;
                    }
                }

                this.emitChar('<');
                this.emitChar('/');
                this.emitSeveralCodePoints(this.tempBuff);
                this.reconsumeInState(SCRIPT_DATA_STATE);
            }
        }


        //12.2.4.20 Script data escape start state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_ESCAPE_START_STATE)] void scriptDataEscapeStartState(int cp)
        {
            if (cp == ɑ.HYPHEN_MINUS) {
                this.state = SCRIPT_DATA_ESCAPE_START_DASH_STATE;
                this.emitChar('-');
            }

    else
        this.reconsumeInState(SCRIPT_DATA_STATE);
        }


        //12.2.4.21 Script data escape start dash state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_ESCAPE_START_DASH_STATE)] void scriptDataEscapeStartDashState(int cp)
        {
            if (cp == ɑ.HYPHEN_MINUS) {
                this.state = SCRIPT_DATA_ESCAPED_DASH_DASH_STATE;
                this.emitChar('-');
            }

    else
        this.reconsumeInState(SCRIPT_DATA_STATE);
        }


        //12.2.4.22 Script data escaped state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_ESCAPED_STATE)] void scriptDataEscapedState(int cp)
        {
            if (cp == ɑ.HYPHEN_MINUS) {
                this.state = SCRIPT_DATA_ESCAPED_DASH_STATE;
                this.emitChar('-');
            }

    else if (cp == ɑ.LESS_THAN_SIGN)
        this.state = SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN_STATE;

    else if (cp == ɑ.NULL)
        this.emitChar(((char)ɑ.REPLACEMENT_CHARACTER));

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else
        this.emitCodePoint(cp);
        }


        //12.2.4.23 Script data escaped dash state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_ESCAPED_DASH_STATE)] void scriptDataEscapedDashState(int cp)
        {
            if (cp == ɑ.HYPHEN_MINUS) {
                this.state = SCRIPT_DATA_ESCAPED_DASH_DASH_STATE;
                this.emitChar('-');
            }

    else if (cp == ɑ.LESS_THAN_SIGN)
        this.state = SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN_STATE;

    else if (cp == ɑ.NULL) {
                this.state = SCRIPT_DATA_ESCAPED_STATE;
                this.emitChar(((char)ɑ.REPLACEMENT_CHARACTER));
            }

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else {
                this.state = SCRIPT_DATA_ESCAPED_STATE;
                this.emitCodePoint(cp);
            }
        }


        //12.2.4.24 Script data escaped dash dash state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_ESCAPED_DASH_DASH_STATE)] void scriptDataEscapedDashDashState(int cp)
        {
            if (cp == ɑ.HYPHEN_MINUS)
        this.emitChar('-');

    else if (cp == ɑ.LESS_THAN_SIGN)
        this.state = SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN_STATE;

    else if (cp == ɑ.GREATER_THAN_SIGN) {
                this.state = SCRIPT_DATA_STATE;
                this.emitChar('>');
            }

    else if (cp == ɑ.NULL) {
                this.state = SCRIPT_DATA_ESCAPED_STATE;
                this.emitChar(((char)ɑ.REPLACEMENT_CHARACTER));
            }

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else {
                this.state = SCRIPT_DATA_ESCAPED_STATE;
                this.emitCodePoint(cp);
            }
        }


        //12.2.4.25 Script data escaped less-than sign state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN_STATE)] void scriptDataEscapedLessThanSignState(int cp)
        {
            if (cp == ɑ.SOLIDUS) {
                this.tempBuff = new TempBuff();
                this.state = SCRIPT_DATA_ESCAPED_END_TAG_OPEN_STATE;
            }

    else if (isAsciiLetter(cp))
            {
                this.tempBuff = new TempBuff();
                this.emitChar('<');
                this.reconsumeInState(SCRIPT_DATA_DOUBLE_ESCAPE_START_STATE);
            }

            else
            {
                this.emitChar('<');
                this.reconsumeInState(SCRIPT_DATA_ESCAPED_STATE);
            }
        }


        //12.2.4.26 Script data escaped end tag open state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_ESCAPED_END_TAG_OPEN_STATE)] void scriptDataEscapedEndTagOpenState(int cp)
        {
            if (isAsciiLetter(cp))
            {
                this.createEndTagToken();
                this.reconsumeInState(SCRIPT_DATA_ESCAPED_END_TAG_NAME_STATE);
            }

            else
            {
                this.emitChar('<');
                this.emitChar('/');
                this.reconsumeInState(SCRIPT_DATA_ESCAPED_STATE);
            }
        }


        //12.2.4.27 Script data escaped end tag name state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_ESCAPED_END_TAG_NAME_STATE)] void scriptDataEscapedEndTagNameState(int cp)
        {
            if (isAsciiUpper(cp))
            {
                this.currentToken.tagName += toAsciiLowerChar(cp);
                this.tempBuff.push(cp);
            }

            else if (isAsciiLower(cp))
            {
                this.currentToken.tagName += toChar(cp);
                this.tempBuff.push(cp);
            }

            else
            {
                if (this.isAppropriateEndTagToken())
                {
                    if (isWhitespace(cp))
                    {
                        this.state = BEFORE_ATTRIBUTE_NAME_STATE;
                        return;
                    }

                    if (cp == ɑ.SOLIDUS) {
                        this.state = SELF_CLOSING_START_TAG_STATE;
                        return;
                    }

                    if (cp == ɑ.GREATER_THAN_SIGN) {
                        this.emitCurrentToken();
                        this.state = DATA_STATE;
                        return;
                    }
                }

                this.emitChar('<');
                this.emitChar('/');
                this.emitSeveralCodePoints(this.tempBuff);
                this.reconsumeInState(SCRIPT_DATA_ESCAPED_STATE);
            }
        }


        //12.2.4.28 Script data double escape start state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_DOUBLE_ESCAPE_START_STATE)] void scriptDataDoubleEscapeStartState(int cp)
        {
            if (isWhitespace(cp) || cp == ɑ.SOLIDUS || cp == ɑ.GREATER_THAN_SIGN) {
                this.state = this.isTempBufferEqualToScriptString() ? SCRIPT_DATA_DOUBLE_ESCAPED_STATE : SCRIPT_DATA_ESCAPED_STATE;
                this.emitCodePoint(cp);
            }

    else if (isAsciiUpper(cp))
            {
                this.tempBuff.push(toAsciiLowerCodePoint(cp));
                this.emitCodePoint(cp);
            }

            else if (isAsciiLower(cp))
            {
                this.tempBuff.push(cp);
                this.emitCodePoint(cp);
            }

            else
                this.reconsumeInState(SCRIPT_DATA_ESCAPED_STATE);
        }


        //12.2.4.29 Script data double escaped state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_DOUBLE_ESCAPED_STATE)] void scriptDataDoubleEscapedState(int cp)
        {
            if (cp == ɑ.HYPHEN_MINUS) {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_DASH_STATE;
                this.emitChar('-');
            }

    else if (cp == ɑ.LESS_THAN_SIGN) {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN_STATE;
                this.emitChar('<');
            }

    else if (cp == ɑ.NULL)
        this.emitChar(((char)ɑ.REPLACEMENT_CHARACTER));

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else
        this.emitCodePoint(cp);
        }


        //12.2.4.30 Script data double escaped dash state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_DOUBLE_ESCAPED_DASH_STATE)] void scriptDataDoubleEscapedDashState(int cp)
        {
            if (cp == ɑ.HYPHEN_MINUS) {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH_STATE;
                this.emitChar('-');
            }

    else if (cp == ɑ.LESS_THAN_SIGN) {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN_STATE;
                this.emitChar('<');
            }

    else if (cp == ɑ.NULL) {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_STATE;
                this.emitChar(((char)ɑ.REPLACEMENT_CHARACTER));
            }

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_STATE;
                this.emitCodePoint(cp);
            }
        }


        //12.2.4.31 Script data double escaped dash dash state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH_STATE)] void scriptDataDoubleEscapedDashDashState(int cp)
        {
            if (cp == ɑ.HYPHEN_MINUS)
        this.emitChar('-');

    else if (cp == ɑ.LESS_THAN_SIGN) {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN_STATE;
                this.emitChar('<');
            }

    else if (cp == ɑ.GREATER_THAN_SIGN) {
                this.state = SCRIPT_DATA_STATE;
                this.emitChar('>');
            }

    else if (cp == ɑ.NULL) {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_STATE;
                this.emitChar(((char)ɑ.REPLACEMENT_CHARACTER));
            }

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else {
                this.state = SCRIPT_DATA_DOUBLE_ESCAPED_STATE;
                this.emitCodePoint(cp);
            }
        }


        //12.2.4.32 Script data double escaped less-than sign state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN_STATE)] void scriptDataDoubleEscapedLessThanSignState(int cp)
        {
            if (cp == ɑ.SOLIDUS) {
                this.tempBuff = new TempBuff();
                this.state = SCRIPT_DATA_DOUBLE_ESCAPE_END_STATE;
                this.emitChar('/');
            }

    else
        this.reconsumeInState(SCRIPT_DATA_DOUBLE_ESCAPED_STATE);
        }


        //12.2.4.33 Script data double escape end state
        //------------------------------------------------------------------
        [_(Index.SCRIPT_DATA_DOUBLE_ESCAPE_END_STATE)] void scriptDataDoubleEscapeEndState(int cp)
        {
            if (isWhitespace(cp) || cp == ɑ.SOLIDUS || cp == ɑ.GREATER_THAN_SIGN) {
                this.state = this.isTempBufferEqualToScriptString() ? SCRIPT_DATA_ESCAPED_STATE : SCRIPT_DATA_DOUBLE_ESCAPED_STATE;

                this.emitCodePoint(cp);
            }

    else if (isAsciiUpper(cp))
            {
                this.tempBuff.push(toAsciiLowerCodePoint(cp));
                this.emitCodePoint(cp);
            }

            else if (isAsciiLower(cp))
            {
                this.tempBuff.push(cp);
                this.emitCodePoint(cp);
            }

            else
                this.reconsumeInState(SCRIPT_DATA_DOUBLE_ESCAPED_STATE);
        }


        //12.2.4.34 Before attribute name state
        //------------------------------------------------------------------
        [_(Index.BEFORE_ATTRIBUTE_NAME_STATE)] void beforeAttributeNameState(int cp)
        {
            if (isWhitespace(cp))
                return;

            if (cp == ɑ.SOLIDUS || cp == ɑ.GREATER_THAN_SIGN || cp == ɑ.EOF)
        this.reconsumeInState(AFTER_ATTRIBUTE_NAME_STATE);

    else if (cp == ɑ.EQUALS_SIGN) {
                this.createAttr("=");
                this.state = ATTRIBUTE_NAME_STATE;
            }

    else {
                this.createAttr("");
                this.reconsumeInState(ATTRIBUTE_NAME_STATE);
            }
        }


        //12.2.4.35 Attribute name state
        //------------------------------------------------------------------
        [_(Index.ATTRIBUTE_NAME_STATE)] void attributeNameState(int cp)
        {
            if (isWhitespace(cp) || cp == ɑ.SOLIDUS || cp == ɑ.GREATER_THAN_SIGN || cp == ɑ.EOF) {
                this.leaveAttrName(AFTER_ATTRIBUTE_NAME_STATE);
                this.unconsume();
            }

    else if (cp == ɑ.EQUALS_SIGN)
        this.leaveAttrName(BEFORE_ATTRIBUTE_VALUE_STATE);

    else if (isAsciiUpper(cp))
                this.currentAttr.name += toAsciiLowerChar(cp);

            else if (cp == ɑ.QUOTATION_MARK || cp == ɑ.APOSTROPHE || cp == ɑ.LESS_THAN_SIGN)
        this.currentAttr.name += toChar(cp);

    else if (cp == ɑ.NULL)
        this.currentAttr.name += ɑ.REPLACEMENT_CHARACTER;

    else
        this.currentAttr.name += toChar(cp);
        }


        //12.2.4.36 After attribute name state
        //------------------------------------------------------------------
        [_(Index.AFTER_ATTRIBUTE_NAME_STATE)] void afterAttributeNameState(int cp)
        {
            if (isWhitespace(cp))
                return;

            if (cp == ɑ.SOLIDUS)
        this.state = SELF_CLOSING_START_TAG_STATE;

    else if (cp == ɑ.EQUALS_SIGN)
        this.state = BEFORE_ATTRIBUTE_VALUE_STATE;

    else if (cp == ɑ.GREATER_THAN_SIGN) {
                this.state = DATA_STATE;
                this.emitCurrentToken();
            }

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else {
                this.createAttr("");
                this.reconsumeInState(ATTRIBUTE_NAME_STATE);
            }
        }


        //12.2.4.37 Before attribute value state
        //------------------------------------------------------------------
        [_(Index.BEFORE_ATTRIBUTE_VALUE_STATE)] void beforeAttributeValueState(int cp)
        {
            if (isWhitespace(cp))
                return;

            if (cp == ɑ.QUOTATION_MARK)
        this.state = ATTRIBUTE_VALUE_DOUBLE_QUOTED_STATE;

    else if (cp == ɑ.APOSTROPHE)
        this.state = ATTRIBUTE_VALUE_SINGLE_QUOTED_STATE;

    else
        this.reconsumeInState(ATTRIBUTE_VALUE_UNQUOTED_STATE);
        }


        //12.2.4.38 Attribute value (double-quoted) state
        //------------------------------------------------------------------
        [_(Index.ATTRIBUTE_VALUE_DOUBLE_QUOTED_STATE)] void attributeValueDoubleQuotedState(int cp)
        {
            if (cp == ɑ.QUOTATION_MARK)
        this.state = AFTER_ATTRIBUTE_VALUE_QUOTED_STATE;

    else if (cp == ɑ.AMPERSAND) {
                this.additionalAllowedCp = ɑ.QUOTATION_MARK;
                this.returnState = this.state;
                this.state = CHARACTER_REFERENCE_IN_ATTRIBUTE_VALUE_STATE;
            }

    else if (cp == ɑ.NULL)
        this.currentAttr.value += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else
        this.currentAttr.value += toChar(cp);
        }


        //12.2.4.39 Attribute value (single-quoted) state
        //------------------------------------------------------------------
        [_(Index.ATTRIBUTE_VALUE_SINGLE_QUOTED_STATE)] void attributeValueSingleQuotedState(int cp)
        {
            if (cp == ɑ.APOSTROPHE)
        this.state = AFTER_ATTRIBUTE_VALUE_QUOTED_STATE;

    else if (cp == ɑ.AMPERSAND) {
                this.additionalAllowedCp = ɑ.APOSTROPHE;
                this.returnState = this.state;
                this.state = CHARACTER_REFERENCE_IN_ATTRIBUTE_VALUE_STATE;
            }

    else if (cp == ɑ.NULL)
        this.currentAttr.value += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else
        this.currentAttr.value += toChar(cp);
        }


        //12.2.4.40 Attribute value (unquoted) state
        //------------------------------------------------------------------
        [_(Index.ATTRIBUTE_VALUE_UNQUOTED_STATE)] void attributeValueUnquotedState(int cp)
        {
            if (isWhitespace(cp))
                this.leaveAttrValue(BEFORE_ATTRIBUTE_NAME_STATE);

            else if (cp == ɑ.AMPERSAND) {
                this.additionalAllowedCp = ɑ.GREATER_THAN_SIGN;
                this.returnState = this.state;
                this.state = CHARACTER_REFERENCE_IN_ATTRIBUTE_VALUE_STATE;
            }

    else if (cp == ɑ.GREATER_THAN_SIGN) {
                this.leaveAttrValue(DATA_STATE);
                this.emitCurrentToken();
            }

    else if (cp == ɑ.NULL)
        this.currentAttr.value += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.QUOTATION_MARK || cp == ɑ.APOSTROPHE || cp == ɑ.LESS_THAN_SIGN ||
             cp == ɑ.EQUALS_SIGN || cp == ɑ.GRAVE_ACCENT)
        this.currentAttr.value += toChar(cp);

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else
        this.currentAttr.value += toChar(cp);
}


//12.2.4.41 Character reference in attribute value state
//------------------------------------------------------------------
[_(Index.CHARACTER_REFERENCE_IN_ATTRIBUTE_VALUE_STATE)] void characterReferenceInAttributeValueState(int cp) {
    var referencedCodePoints = this.consumeCharacterReference(cp, true);

    if (!this.ensureHibernation()) {
        if (referencedCodePoints.IsTruthy()) {
            for (var i = 0; i < referencedCodePoints.length; i++)
                this.currentAttr.value += toChar(referencedCodePoints[i]);
        }
        else
            this.currentAttr.value += '&';

        this.state = this.returnState;
    }
}


//12.2.4.42 After attribute value (quoted) state
//------------------------------------------------------------------
[_(Index.AFTER_ATTRIBUTE_VALUE_QUOTED_STATE)] void afterAttributeValueQuotedState(int cp) {
    if (isWhitespace(cp))
        this.leaveAttrValue(BEFORE_ATTRIBUTE_NAME_STATE);

    else if (cp == ɑ.SOLIDUS)
        this.leaveAttrValue(SELF_CLOSING_START_TAG_STATE);

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.leaveAttrValue(DATA_STATE);
        this.emitCurrentToken();
    }

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else
        this.reconsumeInState(BEFORE_ATTRIBUTE_NAME_STATE);
}


//12.2.4.43 Self-closing start tag state
//------------------------------------------------------------------
[_(Index.SELF_CLOSING_START_TAG_STATE)] void selfClosingStartTagState(int cp) {
    if (cp == ɑ.GREATER_THAN_SIGN) {
        this.currentToken.selfClosing = true;
        this.state = DATA_STATE;
        this.emitCurrentToken();
    }

    else if (cp == ɑ.EOF)
        this.reconsumeInState(DATA_STATE);

    else
        this.reconsumeInState(BEFORE_ATTRIBUTE_NAME_STATE);
}


//12.2.4.44 Bogus comment state
//------------------------------------------------------------------
[_(Index.BOGUS_COMMENT_STATE)] void bogusCommentState(int cp) {
    this.createCommentToken();
    this.reconsumeInState(BOGUS_COMMENT_STATE_CONTINUATION);
}

//HACK: to support streaming and make BOGUS_COMMENT_STATE reentrant we've
//introduced BOGUS_COMMENT_STATE_CONTINUATION state which will not produce
//comment token on each call.
[_(Index.BOGUS_COMMENT_STATE_CONTINUATION)] void bogusCommentStateContinuation(int cp) {
    while (true) {
        if (cp == ɑ.GREATER_THAN_SIGN) {
            this.state = DATA_STATE;
            break;
        }

        else if (cp == ɑ.EOF) {
            this.reconsumeInState(DATA_STATE);
            break;
        }

        else {
            this.currentToken.data += (char) (cp == ɑ.NULL ? ɑ.REPLACEMENT_CHARACTER : toChar(cp));

            this.hibernationSnapshot();
            cp = this.consume();

            if (this.ensureHibernation())
                return;
        }
    }

    this.emitCurrentToken();
}

//12.2.4.45 Markup declaration open state
//------------------------------------------------------------------
[_(Index.MARKUP_DECLARATION_OPEN_STATE)] void markupDeclarationOpenState(int cp) {
            var dashDashMatch = this.consumeSubsequentIfMatch(ɑɑ.DASH_DASH_STRING, cp, true);
            var doctypeMatch = !dashDashMatch && this.consumeSubsequentIfMatch(ɑɑ.DOCTYPE_STRING, cp, false);
        var cdataMatch = !dashDashMatch && !doctypeMatch &&
                     this.allowCDATA &&
                     this.consumeSubsequentIfMatch(ɑɑ.CDATA_START_STRING, cp, true);

    if (!this.ensureHibernation()) {
        if (dashDashMatch) {
            this.createCommentToken();
            this.state = COMMENT_START_STATE;
        }

        else if (doctypeMatch)
            this.state = DOCTYPE_STATE;

        else if (cdataMatch)
            this.state = CDATA_SECTION_STATE;

        else
            this.reconsumeInState(BOGUS_COMMENT_STATE);
    }
}


//12.2.4.46 Comment start state
//------------------------------------------------------------------
[_(Index.COMMENT_START_STATE)] void commentStartState(int cp) {
    if (cp == ɑ.HYPHEN_MINUS)
        this.state = COMMENT_START_DASH_STATE;

    else if (cp == ɑ.NULL) {
        this.currentToken.data += ɑ.REPLACEMENT_CHARACTER;
        this.state = COMMENT_STATE;
    }

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.state = DATA_STATE;
        this.emitCurrentToken();
    }

    else if (cp == ɑ.EOF) {
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else {
        this.currentToken.data += toChar(cp);
        this.state = COMMENT_STATE;
    }
}


//12.2.4.47 Comment start dash state
//------------------------------------------------------------------
[_(Index.COMMENT_START_DASH_STATE)] void commentStartDashState(int cp) {
    if (cp == ɑ.HYPHEN_MINUS)
        this.state = COMMENT_END_STATE;

    else if (cp == ɑ.NULL) {
        this.currentToken.data += '-';
        this.currentToken.data += ɑ.REPLACEMENT_CHARACTER;
        this.state = COMMENT_STATE;
    }

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.state = DATA_STATE;
        this.emitCurrentToken();
    }

    else if (cp == ɑ.EOF) {
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else {
        this.currentToken.data += '-';
        this.currentToken.data += toChar(cp);
        this.state = COMMENT_STATE;
    }
}


//12.2.4.48 Comment state
//------------------------------------------------------------------
[_(Index.COMMENT_STATE)] void commentState(int cp) {
    if (cp == ɑ.HYPHEN_MINUS)
        this.state = COMMENT_END_DASH_STATE;

    else if (cp == ɑ.NULL)
        this.currentToken.data += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.EOF) {
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else
        this.currentToken.data += toChar(cp);
}


//12.2.4.49 Comment end dash state
//------------------------------------------------------------------
[_(Index.COMMENT_END_DASH_STATE)] void commentEndDashState(int cp) {
    if (cp == ɑ.HYPHEN_MINUS)
        this.state = COMMENT_END_STATE;

    else if (cp == ɑ.NULL) {
        this.currentToken.data += '-';
        this.currentToken.data += ɑ.REPLACEMENT_CHARACTER;
        this.state = COMMENT_STATE;
    }

    else if (cp == ɑ.EOF) {
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else {
        this.currentToken.data += '-';
        this.currentToken.data += toChar(cp);
        this.state = COMMENT_STATE;
    }
}


//12.2.4.50 Comment end state
//------------------------------------------------------------------
[_(Index.COMMENT_END_STATE)] void commentEndState(int cp) {
    if (cp == ɑ.GREATER_THAN_SIGN) {
        this.state = DATA_STATE;
        this.emitCurrentToken();
    }

    else if (cp == ɑ.EXCLAMATION_MARK)
        this.state = COMMENT_END_BANG_STATE;

    else if (cp == ɑ.HYPHEN_MINUS)
        this.currentToken.data += '-';

    else if (cp == ɑ.NULL) {
        this.currentToken.data += "--";
        this.currentToken.data += ɑ.REPLACEMENT_CHARACTER;
        this.state = COMMENT_STATE;
    }

    else if (cp == ɑ.EOF) {
        this.reconsumeInState(DATA_STATE);
        this.emitCurrentToken();
    }

    else {
        this.currentToken.data += "--";
        this.currentToken.data += toChar(cp);
        this.state = COMMENT_STATE;
    }
}


//12.2.4.51 Comment end bang state
//------------------------------------------------------------------
[_(Index.COMMENT_END_BANG_STATE)] void commentEndBangState(int cp) {
    if (cp == ɑ.HYPHEN_MINUS) {
        this.currentToken.data += "--!";
        this.state = COMMENT_END_DASH_STATE;
    }

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.state = DATA_STATE;
        this.emitCurrentToken();
    }

    else if (cp == ɑ.NULL) {
        this.currentToken.data += "--!";
        this.currentToken.data += ɑ.REPLACEMENT_CHARACTER;
        this.state = COMMENT_STATE;
    }

    else if (cp == ɑ.EOF) {
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else {
        this.currentToken.data += "--!";
        this.currentToken.data += toChar(cp);
        this.state = COMMENT_STATE;
    }
}


//12.2.4.52 DOCTYPE state
//------------------------------------------------------------------
[_(Index.DOCTYPE_STATE)] void doctypeState(int cp) {
    if (isWhitespace(cp))
        return;

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.createDoctypeToken(null);
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.state = DATA_STATE;
    }

    else if (cp == ɑ.EOF) {
        this.createDoctypeToken(null);
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }
    else {
        this.createDoctypeToken("");
        this.reconsumeInState(DOCTYPE_NAME_STATE);
    }
}


//12.2.4.54 DOCTYPE name state
//------------------------------------------------------------------
[_(Index.DOCTYPE_NAME_STATE)] void doctypeNameState(int cp) {
    if (isWhitespace(cp) || cp == ɑ.GREATER_THAN_SIGN || cp == ɑ.EOF)
        this.reconsumeInState(AFTER_DOCTYPE_NAME_STATE);

    else if (isAsciiUpper(cp))
        this.currentToken.name += toAsciiLowerChar(cp);

    else if (cp == ɑ.NULL)
        this.currentToken.name += ɑ.REPLACEMENT_CHARACTER;

    else
        this.currentToken.name += toChar(cp);
}


//12.2.4.55 After DOCTYPE name state
//------------------------------------------------------------------
[_(Index.AFTER_DOCTYPE_NAME_STATE)] void afterDoctypeNameState(int cp) {
    if (isWhitespace(cp))
        return;

    if (cp == ɑ.GREATER_THAN_SIGN) {
        this.state = DATA_STATE;
        this.emitCurrentToken();
    }

    else {
            var publicMatch = this.consumeSubsequentIfMatch(ɑɑ.PUBLIC_STRING, cp, false);
            var systemMatch = !publicMatch && this.consumeSubsequentIfMatch(ɑɑ.SYSTEM_STRING, cp, false);

        if (!this.ensureHibernation()) {
            if (publicMatch)
                this.state = BEFORE_DOCTYPE_PUBLIC_IDENTIFIER_STATE;

            else if (systemMatch)
                this.state = BEFORE_DOCTYPE_SYSTEM_IDENTIFIER_STATE;

            else {
                this.currentToken.forceQuirks = true;
                this.state = BOGUS_DOCTYPE_STATE;
            }
        }
    }
}


//12.2.4.57 Before DOCTYPE public identifier state
//------------------------------------------------------------------
[_(Index.BEFORE_DOCTYPE_PUBLIC_IDENTIFIER_STATE)] void beforeDoctypePublicIdentifierState(int cp) {
    if (isWhitespace(cp))
        return;

    if (cp == ɑ.QUOTATION_MARK) {
        this.currentToken.publicId = "";
        this.state = DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED_STATE;
    }

    else if (cp == ɑ.APOSTROPHE) {
        this.currentToken.publicId = "";
        this.state = DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED_STATE;
    }

    else {
        this.currentToken.forceQuirks = true;
        this.reconsumeInState(BOGUS_DOCTYPE_STATE);
    }
}


//12.2.4.58 DOCTYPE public identifier (double-quoted) state
//------------------------------------------------------------------
[_(Index.DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED_STATE)] void doctypePublicIdentifierDoubleQuotedState(int cp) {
    if (cp == ɑ.QUOTATION_MARK)
        this.state = BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS_STATE;

    else if (cp == ɑ.NULL)
        this.currentToken.publicId += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.state = DATA_STATE;
    }

    else if (cp == ɑ.EOF) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else
        this.currentToken.publicId += toChar(cp);
}


//12.2.4.59 DOCTYPE public identifier (single-quoted) state
//------------------------------------------------------------------
[_(Index.DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED_STATE)] void doctypePublicIdentifierSingleQuotedState(int cp) {
    if (cp == ɑ.APOSTROPHE)
        this.state = BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS_STATE;

    else if (cp == ɑ.NULL)
        this.currentToken.publicId += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.state = DATA_STATE;
    }

    else if (cp == ɑ.EOF) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else
        this.currentToken.publicId += toChar(cp);
}


//12.2.4.61 Between DOCTYPE public and system identifiers state
//------------------------------------------------------------------
[_(Index.BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS_STATE)] void betweenDoctypePublicAndSystemIdentifiersState(int cp) {
    if (isWhitespace(cp))
        return;

    if (cp == ɑ.GREATER_THAN_SIGN) {
        this.emitCurrentToken();
        this.state = DATA_STATE;
    }

    else if (cp == ɑ.QUOTATION_MARK) {
        this.currentToken.systemId = "";
        this.state = DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED_STATE;
    }


    else if (cp == ɑ.APOSTROPHE) {
        this.currentToken.systemId = "";
        this.state = DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED_STATE;
    }

    else {
        this.currentToken.forceQuirks = true;
        this.reconsumeInState(BOGUS_DOCTYPE_STATE);
    }
}


//12.2.4.63 Before DOCTYPE system identifier state
//------------------------------------------------------------------
[_(Index.BEFORE_DOCTYPE_SYSTEM_IDENTIFIER_STATE)] void beforeDoctypeSystemIdentifierState(int cp) {
    if (isWhitespace(cp))
        return;

    if (cp == ɑ.QUOTATION_MARK) {
        this.currentToken.systemId = "";
        this.state = DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED_STATE;
    }

    else if (cp == ɑ.APOSTROPHE) {
        this.currentToken.systemId = "";
        this.state = DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED_STATE;
    }

    else {
        this.currentToken.forceQuirks = true;
        this.reconsumeInState(BOGUS_DOCTYPE_STATE);
    }
}


//12.2.4.64 DOCTYPE system identifier (double-quoted) state
//------------------------------------------------------------------
[_(Index.DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED_STATE)] void doctypeSystemIdentifierDoubleQuotedState(int cp) {
    if (cp == ɑ.QUOTATION_MARK)
        this.state = AFTER_DOCTYPE_SYSTEM_IDENTIFIER_STATE;

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.state = DATA_STATE;
    }

    else if (cp == ɑ.NULL)
        this.currentToken.systemId += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.EOF) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else
        this.currentToken.systemId += toChar(cp);
}


//12.2.4.65 DOCTYPE system identifier (single-quoted) state
//------------------------------------------------------------------
[_(Index.DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED_STATE)] void doctypeSystemIdentifierSingleQuotedState(int cp) {
    if (cp == ɑ.APOSTROPHE)
        this.state = AFTER_DOCTYPE_SYSTEM_IDENTIFIER_STATE;

    else if (cp == ɑ.GREATER_THAN_SIGN) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.state = DATA_STATE;
    }

    else if (cp == ɑ.NULL)
        this.currentToken.systemId += ɑ.REPLACEMENT_CHARACTER;

    else if (cp == ɑ.EOF) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else
        this.currentToken.systemId += toChar(cp);
}


//12.2.4.66 After DOCTYPE system identifier state
//------------------------------------------------------------------
[_(Index.AFTER_DOCTYPE_SYSTEM_IDENTIFIER_STATE)] void afterDoctypeSystemIdentifierState(int cp) {
    if (isWhitespace(cp))
        return;

    if (cp == ɑ.GREATER_THAN_SIGN) {
        this.emitCurrentToken();
        this.state = DATA_STATE;
    }

    else if (cp == ɑ.EOF) {
        this.currentToken.forceQuirks = true;
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }

    else
        this.state = BOGUS_DOCTYPE_STATE;
}


//12.2.4.67 Bogus DOCTYPE state
//------------------------------------------------------------------
[_(Index.BOGUS_DOCTYPE_STATE)] void bogusDoctypeState(int cp) {
    if (cp == ɑ.GREATER_THAN_SIGN) {
        this.emitCurrentToken();
        this.state = DATA_STATE;
    }

    else if (cp == ɑ.EOF) {
        this.emitCurrentToken();
        this.reconsumeInState(DATA_STATE);
    }
}


//12.2.4.68 CDATA section state
//------------------------------------------------------------------
[_(Index.CDATA_SECTION_STATE)] void cdataSectionState(int cp) {
    while (true) {
        if (cp == ɑ.EOF) {
            this.reconsumeInState(DATA_STATE);
            break;
        }

        else {
            var cdataEndMatch = this.consumeSubsequentIfMatch(ɑɑ.CDATA_END_STRING, cp, true);

            if (this.ensureHibernation())
                break;

            if (cdataEndMatch) {
                this.state = DATA_STATE;
                break;
            }

            this.emitCodePoint(cp);

            this.hibernationSnapshot();
            cp = this.consume();

            if (this.ensureHibernation())
                break;
        }
    }
}

    }
}
