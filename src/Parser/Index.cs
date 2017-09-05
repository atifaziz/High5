using System;
using System.Collections.Generic;
using System.Text;
using ParseFive.Common;
using static ParseFive.Tokenizer.Tokenizer;
using static ParseFive.Parser.Parser;
using ParseFive.Tokenizer;

//using Tokenizer = require('../tokenizer');
//using OpenElementStack = require('./open_element_stack');
//using FormattingElementList = require('./formatting_element_list');
//using LocationInfoParserMixin = require('../extensions/location_info/parser_mixin');
//using defaultTreeAdapter = require('../tree_adapters/default');
//using mergeOptions = require('../utils/merge_options');
//using doctype = require('../common/doctype');
//using foreignContent = require('../common/foreign_content');
//using UNICODE = require('../common/unicode');
//using HTML = require('../common/html');
//Aliases
using ɑ = HTML.TAG_NAMES;
using NS = HTML.NAMESPACES;
using ATTRS = HTML.ATTRS;

namespace ParseFive.Parser
{
    using static TreeAdapters.StockTreeAdapters;

    class Index
    {
        public class Options {
            public readonly bool locationInfo = false;
            readonly TreeAdapter treeAdapter = defaultTreeAdapter; //TODO
        }

        //Misc constants
        public const string HIDDEN_INPUT_TYPE = "hidden";

        //Adoption agency loops iteration count
        public const int AA_OUTER_LOOP_ITER = 8;
        public const int AA_INNER_LOOP_ITER = 3;

        //Insertion modes
        public const string INITIAL_MODE = "INITIAL_MODE";
        public const string BEFORE_HTML_MODE = "BEFORE_HTML_MODE";
        public const string BEFORE_HEAD_MODE = "BEFORE_HEAD_MODE";
        public const string IN_HEAD_MODE = "IN_HEAD_MODE";
        public const string AFTER_HEAD_MODE = "AFTER_HEAD_MODE";
        public const string IN_BODY_MODE = "IN_BODY_MODE";
        public const string TEXT_MODE = "TEXT_MODE";
        public const string IN_TABLE_MODE = "IN_TABLE_MODE";
        public const string IN_TABLE_TEXT_MODE = "IN_TABLE_TEXT_MODE";
        public const string IN_CAPTION_MODE = "IN_CAPTION_MODE";
        public const string IN_COLUMN_GROUP_MODE = "IN_COLUMN_GROUP_MODE";
        public const string IN_TABLE_BODY_MODE = "IN_TABLE_BODY_MODE";
        public const string IN_ROW_MODE = "IN_ROW_MODE";
        public const string IN_CELL_MODE = "IN_CELL_MODE";
        public const string IN_SELECT_MODE = "IN_SELECT_MODE";
        public const string IN_SELECT_IN_TABLE_MODE = "IN_SELECT_IN_TABLE_MODE";
        public const string IN_TEMPLATE_MODE = "IN_TEMPLATE_MODE";
        public const string AFTER_BODY_MODE = "AFTER_BODY_MODE";
        public const string IN_FRAMESET_MODE = "IN_FRAMESET_MODE";
        public const string AFTER_FRAMESET_MODE = "AFTER_FRAMESET_MODE";
        public const string AFTER_AFTER_BODY_MODE = "AFTER_AFTER_BODY_MODE";
        public const string AFTER_AFTER_FRAMESET_MODE = "AFTER_AFTER_FRAMESET_MODE";

        //Insertion mode reset map
        public static IDictionary<string, string> INSERTION_MODE_RESET_MAP = new Dictionary<string, string>
        {
            [ɑ.TR] =  IN_ROW_MODE,
            [ɑ.TBODY] = IN_TABLE_BODY_MODE,
            [ɑ.THEAD] = IN_TABLE_BODY_MODE,
            [ɑ.TFOOT] =  IN_TABLE_BODY_MODE,
            [ɑ.CAPTION] =  IN_CAPTION_MODE,
            [ɑ.COLGROUP] =  IN_COLUMN_GROUP_MODE,
            [ɑ.TABLE] =  IN_TABLE_MODE,
            [ɑ.BODY] =  IN_BODY_MODE,
            [ɑ.FRAMESET] =  IN_FRAMESET_MODE,
        };



        //Template insertion mode switch map
        public static IDictionary<string, string> TEMPLATE_INSERTION_MODE_SWITCH_MAP = new Dictionary<string, string>
        {
            [ɑ.CAPTION] = IN_TABLE_MODE,
            [ɑ.COLGROUP] = IN_TABLE_MODE,
            [ɑ.TBODY] = IN_TABLE_MODE,
            [ɑ.TFOOT] = IN_TABLE_MODE,
            [ɑ.THEAD] = IN_TABLE_MODE,
            [ɑ.COL] = IN_COLUMN_GROUP_MODE,
            [ɑ.TR] = IN_TABLE_BODY_MODE,
            [ɑ.TD] = IN_ROW_MODE,
            [ɑ.TH] = IN_ROW_MODE,
        };

        //Token handlers map for insertion modes
        public static IDictionary<string, IDictionary<string, Action<Parser, Tokenizer.Token>>> _ =
            new Dictionary<string, IDictionary<string, Action<Parser, Tokenizer.Token>>>
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
    }
}
