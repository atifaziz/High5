// ReSharper disable InconsistentNaming

namespace ParseFive.Common
{
    using Extensions;

    static class Unicode
    {
        public const char REPLACEMENT_CHARACTER = '\uFFFD';

        public static class CODE_POINTS {
            public const int EOF = -1;
            public const int NULL = 0x00;
            public const int TABULATION = 0x09;
            public const int CARRIAGE_RETURN = 0x0D;
            public const int LINE_FEED = 0x0A;
            public const int FORM_FEED = 0x0C;
            public const int SPACE = 0x20;
            public const int EXCLAMATION_MARK = 0x21;
            public const int QUOTATION_MARK = 0x22;
            public const int NUMBER_SIGN = 0x23;
            public const int AMPERSAND = 0x26;
            public const int APOSTROPHE = 0x27;
            public const int HYPHEN_MINUS = 0x2D;
            public const int SOLIDUS = 0x2F;
            public const int DIGIT_0 = 0x30;
            public const int DIGIT_9 = 0x39;
            public const int SEMICOLON = 0x3B;
            public const int LESS_THAN_SIGN = 0x3C;
            public const int EQUALS_SIGN = 0x3D;
            public const int GREATER_THAN_SIGN = 0x3E;
            public const int QUESTION_MARK = 0x3F;
            public const int LATIN_CAPITAL_A = 0x41;
            public const int LATIN_CAPITAL_F = 0x46;
            public const int LATIN_CAPITAL_X = 0x58;
            public const int LATIN_CAPITAL_Z = 0x5A;
            public const int GRAVE_ACCENT = 0x60;
            public const int LATIN_SMALL_A = 0x61;
            public const int LATIN_SMALL_F = 0x66;
            public const int LATIN_SMALL_X = 0x78;
            public const int LATIN_SMALL_Z = 0x7A;
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public const int REPLACEMENT_CHARACTER = 0xFFFD;
        }

     public static class CODE_POINT_SEQUENCES {
        public static Array<int> DASH_DASH_STRING   = new Array<int>(new[] {0x2D, 0x2D}); //--
        public static Array<int> DOCTYPE_STRING     = new Array<int>(new[] {0x44, 0x4F, 0x43, 0x54, 0x59, 0x50, 0x45}); //DOCTYPE
        public static Array<int> CDATA_START_STRING = new Array<int>(new[] {0x5B, 0x43, 0x44, 0x41, 0x54, 0x41, 0x5B}); //[CDATA[
        public static Array<int> CDATA_END_STRING   = new Array<int>(new[] {0x5D, 0x5D, 0x3E}); //]]>
        public static Array<int> SCRIPT_STRING      = new Array<int>(new[] {0x73, 0x63, 0x72, 0x69, 0x70, 0x74}); //script
        public static Array<int> PUBLIC_STRING      = new Array<int>(new[] {0x50, 0x55, 0x42, 0x4C, 0x49, 0x43}); //PUBLIC
        public static Array<int> SYSTEM_STRING      = new Array<int>(new[] {0x53, 0x59, 0x53, 0x54, 0x45, 0x4D}); //SYSTEM
    }
}
}
