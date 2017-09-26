using System.Collections.Generic;
//Aliases
using ɑ = HTML.TAG_NAMES;
using NS = HTML.NAMESPACES;
using ATTRS = HTML.ATTRS;
using ParseFive.Tokenizer;

namespace ParseFive.Common
{
    using Compatibility;
    using Extensions;
    using Tokenizer = Tokenizer.Tokenizer;

    static class ForeignContent
    {
        //var Tokenizer = require("../tokenizer"),
        //var HTML = require("./html");

        //MIME types
        static class MimeTypes
        {
            public const string TextHtml = "text/html";
            public const string ApplicationXml = "application/xhtml+xml";
        }

        //Attributes
        const string DefinitionUrlAttr = "definitionurl";
        const string AdjustedDefinitionUrlAttr = "definitionURL";
        static readonly IDictionary<string, string> SvgAttrsAdjustmentMap = new Dictionary<string, string>
        {
            {"attributename", "attributeName"},
            {"attributetype", "attributeType"},
            {"basefrequency", "baseFrequency"},
            {"baseprofile", "baseProfile"},
            {"calcmode", "calcMode"},
            {"clippathunits", "clipPathUnits"},
            {"diffuseconstant", "diffuseConstant"},
            {"edgemode", "edgeMode"},
            {"filterunits", "filterUnits"},
            {"glyphref", "glyphRef"},
            {"gradienttransform", "gradientTransform"},
            {"gradientunits", "gradientUnits"},
            {"kernelmatrix", "kernelMatrix"},
            {"kernelunitlength", "kernelUnitLength"},
            {"keypoints", "keyPoints"},
            {"keysplines", "keySplines"},
            {"keytimes", "keyTimes"},
            {"lengthadjust", "lengthAdjust"},
            {"limitingconeangle", "limitingConeAngle"},
            {"markerheight", "markerHeight"},
            {"markerunits", "markerUnits"},
            {"markerwidth", "markerWidth"},
            {"maskcontentunits", "maskContentUnits"},
            {"maskunits", "maskUnits"},
            {"numoctaves", "numOctaves"},
            {"pathlength", "pathLength"},
            {"patterncontentunits", "patternContentUnits"},
            {"patterntransform", "patternTransform"},
            {"patternunits", "patternUnits"},
            {"pointsatx", "pointsAtX"},
            {"pointsaty", "pointsAtY"},
            {"pointsatz", "pointsAtZ"},
            {"preservealpha", "preserveAlpha"},
            {"preserveaspectratio", "preserveAspectRatio"},
            {"primitiveunits", "primitiveUnits"},
            {"refx", "refX"},
            {"refy", "refY"},
            {"repeatcount", "repeatCount"},
            {"repeatdur", "repeatDur"},
            {"requiredextensions", "requiredExtensions"},
            {"requiredfeatures", "requiredFeatures"},
            {"specularconstant", "specularConstant"},
            {"specularexponent", "specularExponent"},
            {"spreadmethod", "spreadMethod"},
            {"startoffset", "startOffset"},
            {"stddeviation", "stdDeviation"},
            {"stitchtiles", "stitchTiles"},
            {"surfacescale", "surfaceScale"},
            {"systemlanguage", "systemLanguage"},
            {"tablevalues", "tableValues"},
            {"targetx", "targetX"},
            {"targety", "targetY"},
            {"textlength", "textLength"},
            {"viewbox", "viewBox"},
            {"viewtarget", "viewTarget"},
            {"xchannelselector", "xChannelSelector"},
            {"ychannelselector", "yChannelSelector"},
            {"zoomandpan", "zoomAndPan" },
        };

        sealed class XmlAdjustment
        {
            public readonly string Prefix;
            public readonly string Name;
            public readonly string Namespace;

            public XmlAdjustment(string prefix, string name, string @namespace)
            {
                Prefix = prefix;
                Name = name;
                Namespace = @namespace;
            }
        }

        static readonly IDictionary<string, XmlAdjustment> XmlAttrsAdjustmentMap = new Dictionary<string, XmlAdjustment>
        {
            ["xlink:actuate"] = new XmlAdjustment("xlink", "actuate", NS.XLINK),
            ["xlink:arcrole"] = new XmlAdjustment("xlink", "arcrole", NS.XLINK),
            ["xlink:href"] = new XmlAdjustment("xlink", "href", NS.XLINK),
            ["xlink:role"] = new XmlAdjustment("xlink", "role", NS.XLINK),
            ["xlink:show"] = new XmlAdjustment("xlink", "show", NS.XLINK),
            ["xlink:title"] = new XmlAdjustment("xlink", "title", NS.XLINK),
            ["xlink:type"] = new XmlAdjustment("xlink", "type", NS.XLINK),
            ["xml:base"] = new XmlAdjustment("xml", "base", NS.XML),
            ["xml:lang"] = new XmlAdjustment("xml", "lang", NS.XML),
            ["xml:space"] = new XmlAdjustment("xml", "space", NS.XML),
            ["xmlns"] = new XmlAdjustment("", "xmlns", NS.XMLNS),
            ["xmlns:xlink"] = new XmlAdjustment("xmlns", "xlink", NS.XMLNS)
        };



        //SVG tag names adjustment map
        static readonly IDictionary<string, string> SvgTagNamesAdjustmentMap = new Dictionary<string, string>
        {
            ["altglyph"] = "altGlyph",
            ["altglyphdef"] = "altGlyphDef",
            ["altglyphitem"] = "altGlyphItem",
            ["animatecolor"] = "animateColor",
            ["animatemotion"] = "animateMotion",
            ["animatetransform"] = "animateTransform",
            ["clippath"] = "clipPath",
            ["feblend"] = "feBlend",
            ["fecolormatrix"] = "feColorMatrix",
            ["fecomponenttransfer"] = "feComponentTransfer",
            ["fecomposite"] = "feComposite",
            ["feconvolvematrix"] = "feConvolveMatrix",
            ["fediffuselighting"] = "feDiffuseLighting",
            ["fedisplacementmap"] = "feDisplacementMap",
            ["fedistantlight"] = "feDistantLight",
            ["feflood"] = "feFlood",
            ["fefunca"] = "feFuncA",
            ["fefuncb"] = "feFuncB",
            ["fefuncg"] = "feFuncG",
            ["fefuncr"] = "feFuncR",
            ["fegaussianblur"] = "feGaussianBlur",
            ["feimage"] = "feImage",
            ["femerge"] = "feMerge",
            ["femergenode"] = "feMergeNode",
            ["femorphology"] = "feMorphology",
            ["feoffset"] = "feOffset",
            ["fepointlight"] = "fePointLight",
            ["fespecularlighting"] = "feSpecularLighting",
            ["fespotlight"] = "feSpotLight",
            ["fetile"] = "feTile",
            ["feturbulence"] = "feTurbulence",
            ["foreignobject"] = "foreignObject",
            ["glyphref"] = "glyphRef",
            ["lineargradient"] = "linearGradient",
            ["radialgradient"] = "radialGradient",
            ["textpath"] = "textPath",
        };

        //Tags that causes exit from foreign content
        static readonly IDictionary<string, bool> ExitsForeignContent = new Dictionary<string, bool>
        {
            [ɑ.B] = true,
            [ɑ.BIG] = true,
            [ɑ.BLOCKQUOTE] = true,
            [ɑ.BODY] = true,
            [ɑ.BR] = true,
            [ɑ.CENTER] = true,
            [ɑ.CODE] = true,
            [ɑ.DD] = true,
            [ɑ.DIV] = true,
            [ɑ.DL] = true,
            [ɑ.DT] = true,
            [ɑ.EM] = true,
            [ɑ.EMBED] = true,
            [ɑ.H1] = true,
            [ɑ.H2] = true,
            [ɑ.H3] = true,
            [ɑ.H4] = true,
            [ɑ.H5] = true,
            [ɑ.H6] = true,
            [ɑ.HEAD] = true,
            [ɑ.HR] = true,
            [ɑ.I] = true,
            [ɑ.IMG] = true,
            [ɑ.LI] = true,
            [ɑ.LISTING] = true,
            [ɑ.MENU] = true,
            [ɑ.META] = true,
            [ɑ.NOBR] = true,
            [ɑ.OL] = true,
            [ɑ.P] = true,
            [ɑ.PRE] = true,
            [ɑ.RUBY] = true,
            [ɑ.S] = true,
            [ɑ.SMALL] = true,
            [ɑ.SPAN] = true,
            [ɑ.STRONG] = true,
            [ɑ.STRIKE] = true,
            [ɑ.SUB] = true,
            [ɑ.SUP] = true,
            [ɑ.TABLE] = true,
            [ɑ.TT] = true,
            [ɑ.U] = true,
            [ɑ.UL] = true,
            [ɑ.VAR] = true,
        };



        //Check exit from foreign content
        public static bool causesExit(Token startTagToken)
        {
            var tn = startTagToken.tagName;
            var isFontWithAttrs = tn == ɑ.FONT && (Tokenizer.getTokenAttr(startTagToken, ATTRS.COLOR) != null ||
                                                    Tokenizer.getTokenAttr(startTagToken, ATTRS.SIZE) != null ||
                                                    Tokenizer.getTokenAttr(startTagToken, ATTRS.FACE) != null);

            return isFontWithAttrs ? true : ExitsForeignContent.TryGetValue(tn, out var value) ? value : false;
        }

        //Token adjustments
        public static void adjustTokenMathMLAttrs(Token token)
        {
            for (var i = 0; i < token.attrs.length; i++)
            {
                if (token.attrs[i].name == DefinitionUrlAttr)
                {
                    token.attrs[i].name = AdjustedDefinitionUrlAttr;
                    break;
                }
            }
        }

        public static void adjustTokenSVGAttrs(Token token)
        {
            for (var i = 0; i < token.attrs.length; i++)
            {
                if (SvgAttrsAdjustmentMap.TryGetValue(token.attrs[i].name, out var adjustedAttrName))
                    token.attrs[i].name = adjustedAttrName;
            }
        }

        public static void adjustTokenXMLAttrs(Token token)
        {
            for (var i = 0; i < token.attrs.length; i++)
            {
                if (XmlAttrsAdjustmentMap.TryGetValue(token.attrs[i].name, out var adjustedAttrEntry))
                {
                    token.attrs[i].prefix = adjustedAttrEntry.Prefix;
                    token.attrs[i].name = adjustedAttrEntry.Name;
                    token.attrs[i].@namespace = adjustedAttrEntry.Namespace;
                }
            }
        }


        public static void adjustTokenSVGTagName(Token token)
        {
            if (SvgTagNamesAdjustmentMap.TryGetValue(token.tagName, out var adjustedTagName))
                token.tagName = adjustedTagName;
        }

        //Integration points
        static bool isMathMLTextIntegrationPoint(string tn, string ns)
        {
            return ns == NS.MATHML && (tn == ɑ.MI || tn == ɑ.MO || tn == ɑ.MN || tn == ɑ.MS || tn == ɑ.MTEXT);
        }

        static bool isHtmlIntegrationPoint(string tn, string ns, List<Attr> attrs)
        {
            if (ns == NS.MATHML && tn == ɑ.ANNOTATION_XML)
            {
                for (var i = 0; i < attrs.length; i++)
                {
                    if (attrs[i].name == ATTRS.ENCODING)
                    {
                        var value = attrs[i].value.toLowerCase();

                        return value == MimeTypes.TextHtml || value == MimeTypes.ApplicationXml;
                    }
                }
            }

            return ns == NS.SVG && (tn == ɑ.FOREIGN_OBJECT || tn == ɑ.DESC || tn == ɑ.TITLE);
        }

        public static bool isIntegrationPoint(string tn, string ns, List<Attr> attrs, string foreignNS)
        {
            if ((!foreignNS.isTruthy() || foreignNS == NS.HTML) && isHtmlIntegrationPoint(tn, ns, attrs))
                return true;

            if ((!foreignNS.isTruthy() || foreignNS == NS.MATHML) && isMathMLTextIntegrationPoint(tn, ns))
                return true;

            return false;
        }
    }
}
