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
    using System.Collections.Generic;
    using NS = HTML.NAMESPACES;
    using T = HTML.TAG_NAMES;

    static class HTML
    {
        public static class NAMESPACES
        {
            public const string HTML = "http://www.w3.org/1999/xhtml";
            public const string MATHML = "http://www.w3.org/1998/Math/MathML";
            public const string SVG = "http://www.w3.org/2000/svg";
            public const string XLINK = "http://www.w3.org/1999/xlink";
            public const string XML = "http://www.w3.org/XML/1998/namespace";
            public const string XMLNS = "http://www.w3.org/2000/xmlns/";
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

        public static class TAG_NAMES
        {
            public const string A = "a";
            public const string ADDRESS = "address";
            public const string ANNOTATION_XML = "annotation-xml";
            public const string APPLET = "applet";
            public const string AREA = "area";
            public const string ARTICLE = "article";
            public const string ASIDE = "aside";

            public const string B = "b";
            public const string BASE = "base";
            public const string BASEFONT = "basefont";
            public const string BGSOUND = "bgsound";
            public const string BIG = "big";
            public const string BLOCKQUOTE = "blockquote";
            public const string BODY = "body";
            public const string BR = "br";
            public const string BUTTON = "button";

            public const string CAPTION = "caption";
            public const string CENTER = "center";
            public const string CODE = "code";
            public const string COL = "col";
            public const string COLGROUP = "colgroup";

            public const string DD = "dd";
            public const string DESC = "desc";
            public const string DETAILS = "details";
            public const string DIALOG = "dialog";
            public const string DIR = "dir";
            public const string DIV = "div";
            public const string DL = "dl";
            public const string DT = "dt";

            public const string EM = "em";
            public const string EMBED = "embed";

            public const string FIELDSET = "fieldset";
            public const string FIGCAPTION = "figcaption";
            public const string FIGURE = "figure";
            public const string FONT = "font";
            public const string FOOTER = "footer";
            public const string FOREIGN_OBJECT = "foreignObject";
            public const string FORM = "form";
            public const string FRAME = "frame";
            public const string FRAMESET = "frameset";

            public const string H1 = "h1";
            public const string H2 = "h2";
            public const string H3 = "h3";
            public const string H4 = "h4";
            public const string H5 = "h5";
            public const string H6 = "h6";
            public const string HEAD = "head";
            public const string HEADER = "header";
            public const string HGROUP = "hgroup";
            public const string HR = "hr";
            public const string HTML = "html";

            public const string I = "i";
            public const string IMG = "img";
            public const string IMAGE = "image";
            public const string INPUT = "input";
            public const string IFRAME = "iframe";

            public const string KEYGEN = "keygen";

            public const string LABEL = "label";
            public const string LI = "li";
            public const string LINK = "link";
            public const string LISTING = "listing";

            public const string MAIN = "main";
            public const string MALIGNMARK = "malignmark";
            public const string MARQUEE = "marquee";
            public const string MATH = "math";
            public const string MENU = "menu";
            public const string MENUITEM = "menuitem";
            public const string META = "meta";
            public const string MGLYPH = "mglyph";
            public const string MI = "mi";
            public const string MO = "mo";
            public const string MN = "mn";
            public const string MS = "ms";
            public const string MTEXT = "mtext";

            public const string NAV = "nav";
            public const string NOBR = "nobr";
            public const string NOFRAMES = "noframes";
            public const string NOEMBED = "noembed";
            public const string NOSCRIPT = "noscript";

            public const string OBJECT = "object";
            public const string OL = "ol";
            public const string OPTGROUP = "optgroup";
            public const string OPTION = "option";

            public const string P = "p";
            public const string PARAM = "param";
            public const string PLAINTEXT = "plaintext";
            public const string PRE = "pre";

            public const string RB = "rb";
            public const string RP = "rp";
            public const string RT = "rt";
            public const string RTC = "rtc";
            public const string RUBY = "ruby";

            public const string S = "s";
            public const string SCRIPT = "script";
            public const string SECTION = "section";
            public const string SELECT = "select";
            public const string SOURCE = "source";
            public const string SMALL = "small";
            public const string SPAN = "span";
            public const string STRIKE = "strike";
            public const string STRONG = "strong";
            public const string STYLE = "style";
            public const string SUB = "sub";
            public const string SUMMARY = "summary";
            public const string SUP = "sup";

            public const string TABLE = "table";
            public const string TBODY = "tbody";
            public const string TEMPLATE = "template";
            public const string TEXTAREA = "textarea";
            public const string TFOOT = "tfoot";
            public const string TD = "td";
            public const string TH = "th";
            public const string THEAD = "thead";
            public const string TITLE = "title";
            public const string TR = "tr";
            public const string TRACK = "track";
            public const string TT = "tt";

            public const string U = "u";
            public const string UL = "ul";

            public const string SVG = "svg";

            public const string VAR = "var";

            public const string WBR = "wbr";

            public const string XMP = "xmp";
        }

        public static readonly IDictionary<string, HashSet<string>> SPECIAL_ELEMENTS = new Dictionary<string, HashSet<string>>
        {
            [NS.HTML] = new HashSet<string>
            {
                T.ADDRESS,
                T.APPLET,
                T.AREA,
                T.ARTICLE,
                T.ASIDE,
                T.BASE,
                T.BASEFONT,
                T.BGSOUND,
                T.BLOCKQUOTE,
                T.BODY,
                T.BR,
                T.BUTTON,
                T.CAPTION,
                T.CENTER,
                T.COL,
                T.COLGROUP,
                T.DD,
                T.DETAILS,
                T.DIR,
                T.DIV,
                T.DL,
                T.DT,
                T.EMBED,
                T.FIELDSET,
                T.FIGCAPTION,
                T.FIGURE,
                T.FOOTER,
                T.FORM,
                T.FRAME,
                T.FRAMESET,
                T.H1,
                T.H2,
                T.H3,
                T.H4,
                T.H5,
                T.H6,
                T.HEAD,
                T.HEADER,
                T.HGROUP,
                T.HR,
                T.HTML,
                T.IFRAME,
                T.IMG,
                T.INPUT,
                T.LI,
                T.LINK,
                T.LISTING,
                T.MAIN,
                T.MARQUEE,
                T.MENU,
                T.META,
                T.NAV,
                T.NOEMBED,
                T.NOFRAMES,
                T.NOSCRIPT,
                T.OBJECT,
                T.OL,
                T.P,
                T.PARAM,
                T.PLAINTEXT,
                T.PRE,
                T.SCRIPT,
                T.SECTION,
                T.SELECT,
                T.SOURCE,
                T.STYLE,
                T.SUMMARY,
                T.TABLE,
                T.TBODY,
                T.TD,
                T.TEMPLATE,
                T.TEXTAREA,
                T.TFOOT,
                T.TH,
                T.THEAD,
                T.TITLE,
                T.TR,
                T.TRACK,
                T.UL,
                T.WBR,
                T.XMP,
            },
            [NS.MATHML] = new HashSet<string>
            {
                T.MI,
                T.MO,
                T.MN,
                T.MS,
                T.MTEXT,
                T.ANNOTATION_XML,
            },
            [NS.SVG] = new HashSet<string>
            {
                T.TITLE,
                T.FOREIGN_OBJECT,
                T.DESC,
            }
        };
    }
}
