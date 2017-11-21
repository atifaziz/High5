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
    using System.Text;
    using _ = HTML.TAG_NAMES;
    using NS = HTML.NAMESPACES;

    public static class Serializer
    {
        public static string Serialize(this HtmlNode node) =>
            Serialize(Tree.Default, node);

        public static void SerializeTo(this HtmlNode node, StringBuilder output) =>
            SerializeTo(Tree.Default, node, output);

        public static string Serialize<TNode, TElement>(
            ITree<TNode, TElement> tree, TNode node)
        {
            var html = new StringBuilder();
            SerializeTo(tree, node, html);
            return html.ToString();
        }

        public static void SerializeTo<TNode, TElement>(
            ITree<TNode, TElement> tree, TNode node,
            StringBuilder output)
        {
            if (tree == null) throw new ArgumentNullException(nameof(tree));
            if (output == null) throw new ArgumentNullException(nameof(output));
            new Serializer<TNode, TElement>(tree).Serialize(node, output);
        }
    }

    sealed class Serializer<TNode, TElement>
    {
        readonly ITree<TNode, TElement> _tree;

        public Serializer(ITree<TNode, TElement> tree) =>
            _tree = tree;

        public void Serialize(TNode node, StringBuilder html) =>
            SerializeNode(node, (false, default(TNode)), html);

        void SerializeChildNodes(TNode parentNode, IEnumerable<TNode> childNodes, StringBuilder html)
        {
            foreach (var node in childNodes)
                SerializeNode(node, (true, parentNode), html);
        }

        void SerializeNode(TNode node, (bool, TNode) parentNode, StringBuilder html)
        {
            if (_tree.TryGetElement(node, out var element))
                SerializeElement(node, element, html);
            else if (_tree.TryGetTextValue(node, out var value))
                SerializeText(value, parentNode, html);
            else if (_tree.TryGetCommentData(node, out var content))
                SerializeComment(content, html);
            else if (_tree.TryGetDocumentTypeName(node, out var doctype))
                SerializeDocumentTypeNode(doctype, html);
            else
                SerializeChildNodes(node, _tree.GetChildNodes(node), html);
        }

        static void SerializeDocumentTypeNode(string name, StringBuilder html) =>
            html.Append("<!DOCTYPE ").Append(name).Append('>');

        static void SerializeComment(string content, StringBuilder html) =>
            html.Append("<!--").Append(content).Append("-->");

        void SerializeText(string value, (bool IsDefined, TNode Value) parent, StringBuilder html)
        {
            string parentTn = null;

            if (parent.IsDefined && _tree.TryGetElement(parent.Value, out var element))
                parentTn = _tree.GetTagName(element);

            if (parentTn == _.STYLE || parentTn == _.SCRIPT || parentTn == _.XMP || parentTn == _.IFRAME ||
                parentTn == _.NOEMBED || parentTn == _.NOFRAMES || parentTn == _.PLAINTEXT || parentTn == _.NOSCRIPT)
                html.Append(value);

            else
                EscapeString(value, false, html);
        }

        void SerializeElement(TNode node, TElement element, StringBuilder html)
        {
            var tn = _tree.GetTagName(element);
            var ns = _tree.GetNamespaceUri(element);

            html.Append('<').Append(tn);
            SerializeAttributes(element, html);
            html.Append('>');

            if (tn != _.AREA && tn != _.BASE && tn != _.BASEFONT && tn != _.BGSOUND && tn != _.BR && tn != _.BR &&
                tn != _.COL && tn != _.EMBED && tn != _.FRAME && tn != _.HR && tn != _.IMG && tn != _.INPUT &&
                tn != _.KEYGEN && tn != _.LINK && tn != _.MENUITEM && tn != _.META && tn != _.PARAM && tn != _.SOURCE &&
                tn != _.TRACK && tn != _.WBR)
            {
                var childNodes = tn == _.TEMPLATE && ns == NS.HTML ?
                    _tree.GetTemplateContent(element) :
                    _tree.GetChildNodes(node);

                SerializeChildNodes(node, childNodes, html);
                html.Append("</").Append(tn).Append('>');
            }
        }

        void SerializeAttributes(TElement element, StringBuilder html)
        {
            var count = _tree.GetAttributeCount(element);
            if (count == 0)
                return;

            // TODO re-use array to reduce GC allocations
            var attrs = new (string Namespace, string Name, string Value)[count];
            _tree.ListAttributes(element, attrs, 0, attrs.Length);

            foreach (var attr in attrs)
            {
                html.Append(' ');

                if (string.IsNullOrEmpty(attr.Namespace))
                    html.Append(attr.Name);

                else if (attr.Namespace == NS.XML)
                    html.Append("xml:").Append(attr.Name);

                else if (attr.Namespace == NS.XMLNS)
                {
                    if (attr.Name != "xmlns")
                        html.Append("xmlns:");

                    html.Append(attr.Name);
                }

                else if (attr.Namespace == NS.XLINK)
                    html.Append("xlink:").Append(attr.Name);

                else
                    html.Append(attr.Namespace).Append(':').Append(attr.Name);

                html.Append("=\"");
                EscapeString(attr.Value, true, html);
                html.Append('"');
            }
        }

        static void EscapeString(string str, bool inAttributeMode, StringBuilder sb)
        {
            // TODO escape only if needed
            // Instead of looping over all characters, search for those needing
            // to be escaped and bulk-add raw chunks in-between.

            foreach (var ch in str)
            {
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '\u00a0': sb.Append("&nbsp;"); break;
                    default:
                    {
                        if (inAttributeMode)
                        {
                            if (ch == '"')
                            {
                                sb.Append("&quot;");
                                break;
                            }
                        }
                        else
                        {
                            if (ch == '<') { sb.Append("&lt;"); break; }
                            if (ch == '>') { sb.Append("&gt;"); break; }
                        }
                        sb.Append(ch);
                        break;
                    }
                }
            }
        }
    }
}
