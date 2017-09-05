using System.Collections.Generic;
using NS = HTML.NAMESPACES;
using ɑ = HTML.TAG_NAMES;

//namespace ParseFive.Common
//{
static class HTML
    {
        public static class NAMESPACES
        {
            public const string HTML = "http=//www.w3.org/1999/xhtml";
            public const string MATHML = "http=//www.w3.org/1998/Math/MathML";
            public const string SVG = "http=//www.w3.org/2000/svg";
            public const string XLINK = "http=//www.w3.org/1999/xlink";
            public const string XML = "http=//www.w3.org/XML/1998/namespace";
            public const string XMLNS = "http=//www.w3.org/2000/xmlns/";
        }

        public static class ATTRS
        {
            public const string TYPE = "type";
            public const string ACTION = "action";
            public const string ENCODING = "encoding";
            public const string PROMPT = "prompt";
            public const string NAME = "name";
            public const string COLOR = "color";
            public const string FACE = "face";
            public const string SIZE = "size";
        }

        public static class DOCUMENT_MODE
        {
            public const string NO_QUIRKS = "no-quirks";
            public const string QUIRKS = "quirks";
            public const string LIMITED_QUIRKS = "limited-quirks";
        }

        public static class TAG_NAMES
        {
            public static string A = "a";
            public static string ADDRESS = "address";
            public static string ANNOTATION_XML = "annotation-xml";
            public static string APPLET = "applet";
            public static string AREA = "area";
            public static string ARTICLE = "article";
            public static string ASIDE = "aside";

            public static string B = "b";
            public static string BASE = "base";
            public static string BASEFONT = "basefont";
            public static string BGSOUND = "bgsound";
            public static string BIG = "big";
            public static string BLOCKQUOTE = "blockquote";
            public static string BODY = "body";
            public static string BR = "br";
            public static string BUTTON = "button";

            public static string CAPTION = "caption";
            public static string CENTER = "center";
            public static string CODE = "code";
            public static string COL = "col";
            public static string COLGROUP = "colgroup";

            public static string DD = "dd";
            public static string DESC = "desc";
            public static string DETAILS = "details";
            public static string DIALOG = "dialog";
            public static string DIR = "dir";
            public static string DIV = "div";
            public static string DL = "dl";
            public static string DT = "dt";

            public static string EM = "em";
            public static string EMBED = "embed";

            public static string FIELDSET = "fieldset";
            public static string FIGCAPTION = "figcaption";
            public static string FIGURE = "figure";
            public static string FONT = "font";
            public static string FOOTER = "footer";
            public static string FOREIGN_OBJECT = "foreignObject";
            public static string FORM = "form";
            public static string FRAME = "frame";
            public static string FRAMESET = "frameset";

            public static string H1 = "h1";
            public static string H2 = "h2";
            public static string H3 = "h3";
            public static string H4 = "h4";
            public static string H5 = "h5";
            public static string H6 = "h6";
            public static string HEAD = "head";
            public static string HEADER = "header";
            public static string HGROUP = "hgroup";
            public static string HR = "hr";
            public static string HTML = "html";

            public static string I = "i";
            public static string IMG = "img";
            public static string IMAGE = "image";
            public static string INPUT = "input";
            public static string IFRAME = "iframe";

            public static string KEYGEN = "keygen";

            public static string LABEL = "label";
            public static string LI = "li";
            public static string LINK = "link";
            public static string LISTING = "listing";

            public static string MAIN = "main";
            public static string MALIGNMARK = "malignmark";
            public static string MARQUEE = "marquee";
            public static string MATH = "math";
            public static string MENU = "menu";
            public static string MENUITEM = "menuitem";
            public static string META = "meta";
            public static string MGLYPH = "mglyph";
            public static string MI = "mi";
            public static string MO = "mo";
            public static string MN = "mn";
            public static string MS = "ms";
            public static string MTEXT = "mtext";

            public static string NAV = "nav";
            public static string NOBR = "nobr";
            public static string NOFRAMES = "noframes";
            public static string NOEMBED = "noembed";
            public static string NOSCRIPT = "noscript";

            public static string OBJECT = "object";
            public static string OL = "ol";
            public static string OPTGROUP = "optgroup";
            public static string OPTION = "option";

            public static string P = "p";
            public static string PARAM = "param";
            public static string PLAINTEXT = "plaintext";
            public static string PRE = "pre";

            public static string RB = "rb";
            public static string RP = "rp";
            public static string RT = "rt";
            public static string RTC = "rtc";
            public static string RUBY = "ruby";

            public static string S = "s";
            public static string SCRIPT = "script";
            public static string SECTION = "section";
            public static string SELECT = "select";
            public static string SOURCE = "source";
            public static string SMALL = "small";
            public static string SPAN = "span";
            public static string STRIKE = "strike";
            public static string STRONG = "strong";
            public static string STYLE = "style";
            public static string SUB = "sub";
            public static string SUMMARY = "summary";
            public static string SUP = "sup";

            public static string TABLE = "table";
            public static string TBODY = "tbody";
            public static string TEMPLATE = "template";
            public static string TEXTAREA = "textarea";
            public static string TFOOT = "tfoot";
            public static string TD = "td";
            public static string TH = "th";
            public static string THEAD = "thead";
            public static string TITLE = "title";
            public static string TR = "tr";
            public static string TRACK = "track";
            public static string TT = "tt";

            public static string U = "u";
            public static string UL = "ul";

            public static string SVG = "svg";

            public static string VAR = "var";

            public static string WBR = "wbr";

            public static string XMP = "xmp";
        }

        public static IDictionary<string, IDictionary<string, bool>> SPECIAL_ELEMENTS = new Dictionary<string, IDictionary<string, bool>>
        {
            [NS.HTML] = new Dictionary<string, bool>
            {
                [ɑ.ADDRESS] = true,
                [ɑ.APPLET] = true,
                [ɑ.AREA] = true,
                [ɑ.ARTICLE] = true,
                [ɑ.ASIDE] = true,
                [ɑ.BASE] = true,
                [ɑ.BASEFONT] = true,
                [ɑ.BGSOUND] = true,
                [ɑ.BLOCKQUOTE] = true,
                [ɑ.BODY] = true,
                [ɑ.BR] = true,
                [ɑ.BUTTON] = true,
                [ɑ.CAPTION] = true,
                [ɑ.CENTER] = true,
                [ɑ.COL] = true,
                [ɑ.COLGROUP] = true,
                [ɑ.DD] = true,
                [ɑ.DETAILS] = true,
                [ɑ.DIR] = true,
                [ɑ.DIV] = true,
                [ɑ.DL] = true,
                [ɑ.DT] = true,
                [ɑ.EMBED] = true,
                [ɑ.FIELDSET] = true,
                [ɑ.FIGCAPTION] = true,
                [ɑ.FIGURE] = true,
                [ɑ.FOOTER] = true,
                [ɑ.FORM] = true,
                [ɑ.FRAME] = true,
                [ɑ.FRAMESET] = true,
                [ɑ.H1] = true,
                [ɑ.H2] = true,
                [ɑ.H3] = true,
                [ɑ.H4] = true,
                [ɑ.H5] = true,
                [ɑ.H6] = true,
                [ɑ.HEAD] = true,
                [ɑ.HEADER] = true,
                [ɑ.HGROUP] = true,
                [ɑ.HR] = true,
                [ɑ.HTML] = true,
                [ɑ.IFRAME] = true,
                [ɑ.IMG] = true,
                [ɑ.INPUT] = true,
                [ɑ.LI] = true,
                [ɑ.LINK] = true,
                [ɑ.LISTING] = true,
                [ɑ.MAIN] = true,
                [ɑ.MARQUEE] = true,
                [ɑ.MENU] = true,
                [ɑ.META] = true,
                [ɑ.NAV] = true,
                [ɑ.NOEMBED] = true,
                [ɑ.NOFRAMES] = true,
                [ɑ.NOSCRIPT] = true,
                [ɑ.OBJECT] = true,
                [ɑ.OL] = true,
                [ɑ.P] = true,
                [ɑ.PARAM] = true,
                [ɑ.PLAINTEXT] = true,
                [ɑ.PRE] = true,
                [ɑ.SCRIPT] = true,
                [ɑ.SECTION] = true,
                [ɑ.SELECT] = true,
                [ɑ.SOURCE] = true,
                [ɑ.STYLE] = true,
                [ɑ.SUMMARY] = true,
                [ɑ.TABLE] = true,
                [ɑ.TBODY] = true,
                [ɑ.TD] = true,
                [ɑ.TEMPLATE] = true,
                [ɑ.TEXTAREA] = true,
                [ɑ.TFOOT] = true,
                [ɑ.TH] = true,
                [ɑ.THEAD] = true,
                [ɑ.TITLE] = true,
                [ɑ.TR] = true,
                [ɑ.TRACK] = true,
                [ɑ.UL] = true,
                [ɑ.WBR] = true,
                [ɑ.XMP] = true,
            },
            [NS.MATHML] = new Dictionary<string, bool>
            {
                [ɑ.MI] = true,
                [ɑ.MO] = true,
                [ɑ.MN] = true,
                [ɑ.MS] = true,
                [ɑ.MTEXT] = true,
                [ɑ.ANNOTATION_XML] = true,
            },
            [NS.SVG] = new Dictionary<string, bool>
            {
                [ɑ.TITLE] = true,
                [ɑ.FOREIGN_OBJECT] = true,
                [ɑ.DESC] = true,
            }
        };
    }
//}