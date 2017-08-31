using System;
using System.Collections.Generic;
using System.Text;
using ParseFive.Common;
using ɑ = ParseFive.Common.Unicode.CODE_POINTS;
using ɑɑ = ParseFive.Common.Unicode.CODE_POINT_SEQUENCES;
using String = ParseFive.Compatibility.String;
using static ParseFive.Tokenizer.NamedEntityData;

namespace ParseFive.Tokenizer
{
    static class Index
    {
        //var Preprocessor = require('./preprocessor'),
        //    UNICODE = require('../common/unicode'),
        //    neTree = require('./named_entity_data');

        //Aliases
        //var ɑ = UNICODE.CODE_POINTS,
        //ɑɑ = UNICODE.CODE_POINT_SEQUENCES;

        //Replacement code points for numeric entities
        public static IDictionary<int, int> NUMERIC_ENTITY_REPLACEMENTS = new Dictionary<int, int> {
            {0x00, 0xFFFD}, {0x0D, 0x000D}, {0x80, 0x20AC}, {0x81, 0x0081}, {0x82, 0x201A}, {0x83, 0x0192}, {0x84, 0x201E},
            {0x85, 0x2026}, {0x86, 0x2020}, {0x87, 0x2021}, {0x88, 0x02C6}, {0x89, 0x2030}, {0x8A, 0x0160}, {0x8B, 0x2039},
            {0x8C, 0x0152}, {0x8D, 0x008D}, {0x8E, 0x017D}, {0x8F, 0x008F}, {0x90, 0x0090}, {0x91, 0x2018}, {0x92, 0x2019},
            {0x93, 0x201C}, {0x94, 0x201D}, {0x95, 0x2022}, {0x96, 0x2013}, {0x97, 0x2014}, {0x98, 0x02DC}, {0x99, 0x2122},
            {0x9A, 0x0161}, {0x9B, 0x203A}, {0x9C, 0x0153}, {0x9D, 0x009D}, {0x9E, 0x017E}, {0x9F, 0x0178}
        };

        public static int HAS_DATA_FLAG; 
        public static int DATA_DUPLET_FLAG;
        public static int HAS_BRANCHES_FLAG;
        public static int MAX_BRANCH_MARKER_VALUE;

        static Index()
        {
            // Named entity tree flags
            HAS_DATA_FLAG = 1 << 0;
            DATA_DUPLET_FLAG = 1 << 1;
            HAS_BRANCHES_FLAG = 1 << 2;
            MAX_BRANCH_MARKER_VALUE = HAS_DATA_FLAG | DATA_DUPLET_FLAG | HAS_BRANCHES_FLAG;
        }


        //States
        public const string DATA_STATE = "DATA_STATE";
        public const string CHARACTER_REFERENCE_IN_DATA_STATE = "CHARACTER_REFERENCE_IN_DATA_STATE";
        public const string RCDATA_STATE = "RCDATA_STATE";
        public const string CHARACTER_REFERENCE_IN_RCDATA_STATE = "CHARACTER_REFERENCE_IN_RCDATA_STATE";
        public const string RAWTEXT_STATE = "RAWTEXT_STATE";
        public const string SCRIPT_DATA_STATE = "SCRIPT_DATA_STATE";
        public const string PLAINTEXT_STATE = "PLAINTEXT_STATE";
        public const string TAG_OPEN_STATE = "TAG_OPEN_STATE";
        public const string END_TAG_OPEN_STATE = "END_TAG_OPEN_STATE";
        public const string TAG_NAME_STATE = "TAG_NAME_STATE";
        public const string RCDATA_LESS_THAN_SIGN_STATE = "RCDATA_LESS_THAN_SIGN_STATE";
        public const string RCDATA_END_TAG_OPEN_STATE = "RCDATA_END_TAG_OPEN_STATE";
        public const string RCDATA_END_TAG_NAME_STATE = "RCDATA_END_TAG_NAME_STATE";
        public const string RAWTEXT_LESS_THAN_SIGN_STATE = "RAWTEXT_LESS_THAN_SIGN_STATE";
        public const string RAWTEXT_END_TAG_OPEN_STATE = "RAWTEXT_END_TAG_OPEN_STATE";
        public const string RAWTEXT_END_TAG_NAME_STATE = "RAWTEXT_END_TAG_NAME_STATE";
        public const string SCRIPT_DATA_LESS_THAN_SIGN_STATE = "SCRIPT_DATA_LESS_THAN_SIGN_STATE";
        public const string SCRIPT_DATA_END_TAG_OPEN_STATE = "SCRIPT_DATA_END_TAG_OPEN_STATE";
        public const string SCRIPT_DATA_END_TAG_NAME_STATE = "SCRIPT_DATA_END_TAG_NAME_STATE";
        public const string SCRIPT_DATA_ESCAPE_START_STATE = "SCRIPT_DATA_ESCAPE_START_STATE";
        public const string SCRIPT_DATA_ESCAPE_START_DASH_STATE = "SCRIPT_DATA_ESCAPE_START_DASH_STATE";
        public const string SCRIPT_DATA_ESCAPED_STATE = "SCRIPT_DATA_ESCAPED_STATE";
        public const string SCRIPT_DATA_ESCAPED_DASH_STATE = "SCRIPT_DATA_ESCAPED_DASH_STATE";
        public const string SCRIPT_DATA_ESCAPED_DASH_DASH_STATE = "SCRIPT_DATA_ESCAPED_DASH_DASH_STATE";
        public const string SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN_STATE = "SCRIPT_DATA_ESCAPED_LESS_THAN_SIGN_STATE";
        public const string SCRIPT_DATA_ESCAPED_END_TAG_OPEN_STATE = "SCRIPT_DATA_ESCAPED_END_TAG_OPEN_STATE";
        public const string SCRIPT_DATA_ESCAPED_END_TAG_NAME_STATE = "SCRIPT_DATA_ESCAPED_END_TAG_NAME_STATE";
        public const string SCRIPT_DATA_DOUBLE_ESCAPE_START_STATE = "SCRIPT_DATA_DOUBLE_ESCAPE_START_STATE";
        public const string SCRIPT_DATA_DOUBLE_ESCAPED_STATE = "SCRIPT_DATA_DOUBLE_ESCAPED_STATE";
        public const string SCRIPT_DATA_DOUBLE_ESCAPED_DASH_STATE = "SCRIPT_DATA_DOUBLE_ESCAPED_DASH_STATE";
        public const string SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH_STATE = "SCRIPT_DATA_DOUBLE_ESCAPED_DASH_DASH_STATE";
        public const string SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN_STATE = "SCRIPT_DATA_DOUBLE_ESCAPED_LESS_THAN_SIGN_STATE";
        public const string SCRIPT_DATA_DOUBLE_ESCAPE_END_STATE = "SCRIPT_DATA_DOUBLE_ESCAPE_END_STATE";
        public const string BEFORE_ATTRIBUTE_NAME_STATE = "BEFORE_ATTRIBUTE_NAME_STATE";
        public const string ATTRIBUTE_NAME_STATE = "ATTRIBUTE_NAME_STATE";
        public const string AFTER_ATTRIBUTE_NAME_STATE = "AFTER_ATTRIBUTE_NAME_STATE";
        public const string BEFORE_ATTRIBUTE_VALUE_STATE = "BEFORE_ATTRIBUTE_VALUE_STATE";
        public const string ATTRIBUTE_VALUE_DOUBLE_QUOTED_STATE = "ATTRIBUTE_VALUE_DOUBLE_QUOTED_STATE";
        public const string ATTRIBUTE_VALUE_SINGLE_QUOTED_STATE = "ATTRIBUTE_VALUE_SINGLE_QUOTED_STATE";
        public const string ATTRIBUTE_VALUE_UNQUOTED_STATE = "ATTRIBUTE_VALUE_UNQUOTED_STATE";
        public const string CHARACTER_REFERENCE_IN_ATTRIBUTE_VALUE_STATE = "CHARACTER_REFERENCE_IN_ATTRIBUTE_VALUE_STATE";
        public const string AFTER_ATTRIBUTE_VALUE_QUOTED_STATE = "AFTER_ATTRIBUTE_VALUE_QUOTED_STATE";
        public const string SELF_CLOSING_START_TAG_STATE = "SELF_CLOSING_START_TAG_STATE";
        public const string BOGUS_COMMENT_STATE = "BOGUS_COMMENT_STATE";
        public const string BOGUS_COMMENT_STATE_CONTINUATION = "BOGUS_COMMENT_STATE_CONTINUATION";
        public const string MARKUP_DECLARATION_OPEN_STATE = "MARKUP_DECLARATION_OPEN_STATE";
        public const string COMMENT_START_STATE = "COMMENT_START_STATE";
        public const string COMMENT_START_DASH_STATE = "COMMENT_START_DASH_STATE";
        public const string COMMENT_STATE = "COMMENT_STATE";
        public const string COMMENT_END_DASH_STATE = "COMMENT_END_DASH_STATE";
        public const string COMMENT_END_STATE = "COMMENT_END_STATE";
        public const string COMMENT_END_BANG_STATE = "COMMENT_END_BANG_STATE";
        public const string DOCTYPE_STATE = "DOCTYPE_STATE";
        public const string DOCTYPE_NAME_STATE = "DOCTYPE_NAME_STATE";
        public const string AFTER_DOCTYPE_NAME_STATE = "AFTER_DOCTYPE_NAME_STATE";
        public const string BEFORE_DOCTYPE_PUBLIC_IDENTIFIER_STATE = "BEFORE_DOCTYPE_PUBLIC_IDENTIFIER_STATE";
        public const string DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED_STATE = "DOCTYPE_PUBLIC_IDENTIFIER_DOUBLE_QUOTED_STATE";
        public const string DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED_STATE = "DOCTYPE_PUBLIC_IDENTIFIER_SINGLE_QUOTED_STATE";
        public const string BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS_STATE = "BETWEEN_DOCTYPE_PUBLIC_AND_SYSTEM_IDENTIFIERS_STATE";
        public const string BEFORE_DOCTYPE_SYSTEM_IDENTIFIER_STATE = "BEFORE_DOCTYPE_SYSTEM_IDENTIFIER_STATE";
        public const string DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED_STATE = "DOCTYPE_SYSTEM_IDENTIFIER_DOUBLE_QUOTED_STATE";
        public const string DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED_STATE = "DOCTYPE_SYSTEM_IDENTIFIER_SINGLE_QUOTED_STATE";
        public const string AFTER_DOCTYPE_SYSTEM_IDENTIFIER_STATE = "AFTER_DOCTYPE_SYSTEM_IDENTIFIER_STATE";
        public const string BOGUS_DOCTYPE_STATE = "BOGUS_DOCTYPE_STATE";
        public const string CDATA_SECTION_STATE = "CDATA_SECTION_STATE";

        //Utils

        //OPTIMIZATION: these utility functions should not be moved out of this module. V8 Crankshaft will not inline
        //this functions if they will be situated in another module due to context switch.
        //Always perform inlining check before modifying this functions ("node --trace-inlining").
        public static bool isWhitespace(int cp)
        {
            return cp == ɑ.SPACE || cp == ɑ.LINE_FEED || cp == ɑ.TABULATION || cp == ɑ.FORM_FEED;
        }

        public static bool isAsciiDigit(int cp)
        {
            return cp >= ɑ.DIGIT_0 && cp <= ɑ.DIGIT_9;
        }

        public static bool isAsciiUpper(int cp)
        {
            return cp >= ɑ.LATIN_CAPITAL_A && cp <= ɑ.LATIN_CAPITAL_Z;
        }

        public static bool isAsciiLower(int cp)
        {
            return cp >= ɑ.LATIN_SMALL_A && cp <= ɑ.LATIN_SMALL_Z;
        }

        public static bool isAsciiLetter(int cp)
        {
            return isAsciiLower(cp) || isAsciiUpper(cp);
        }

        public static bool isAsciiAlphaNumeric(int cp)
        {
            return isAsciiLetter(cp) || isAsciiDigit(cp);
        }

        public static bool isDigit(int cp, bool isHex)
        {
            return isAsciiDigit(cp) || isHex && (cp >= ɑ.LATIN_CAPITAL_A && cp <= ɑ.LATIN_CAPITAL_F ||
                                                 cp >= ɑ.LATIN_SMALL_A && cp <= ɑ.LATIN_SMALL_F);
        }

        public static bool isReservedCodePoint(int cp)
        {
            return cp >= 0xD800 && cp <= 0xDFFF || cp > 0x10FFFF;
        }

        public static int toAsciiLowerCodePoint(int cp)
        {
            return cp + 0x0020;
        }

        //NOTE: String.fromCharCode() function can handle only characters from BMP subset.
        //So, we need to workaround this manually.
        //(see: https://developer.mozilla.org/en-US/docs/JavaScript/Reference/Global_Objects/String/fromCharCode#Getting_it_to_work_with_higher_values)
        public static string toChar(int cp) //TODO consider if cp can be typed as uint
        {
            if (cp <= 0xFFFF)
                return ((char) cp).ToString();

            cp -= 0x10000;
            return new string(new[] { String.fromCharCode((int)(((uint)cp) >> 10) & 0x3FF | 0xD800), String.fromCharCode(0xDC00 | cp & 0x3FF) });
        }

        public static char toAsciiLowerChar(int cp)
        {
            return String.fromCharCode(toAsciiLowerCodePoint(cp));
        }

        public static int findNamedEntityTreeBranch(int nodeIx, int cp)
        {
            var branchCount = neTree[++nodeIx];
            var lo = ++nodeIx;
            var hi = lo + branchCount - 1;

            while (lo <= hi)
            {
                var mid = unchecked((int) ((uint) lo + hi) >> 1);
                var midCp = neTree[mid];

                if (midCp < cp)
                    lo = mid + 1;

                else if (midCp > cp)
                    hi = mid - 1;

                else
                    return neTree[mid + branchCount];
            }

            return -1;
        }
    }
}
