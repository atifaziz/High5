#region Copyright (c) 2017 Atif Aziz
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

namespace High5.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Xunit;
    using Xunit.Sdk;
    using static MoreLinq.Extensions.ZipLongestExtension;
    using static ThisAssembly.Resources.data;

    public class TreeConstructionTests
    {
        [Theory, ResourceData(typeof(tree_construction.adoption01))]
        public void Adoption01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.adoption02))]
        public void Adoption02(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.comments01))]
        public void Comments01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.doctype01))]
        public void Doctype01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.domjs_unsafe))]
        public void DomjsUnsafe(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.entities01))]
        public void Entities01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.entities02))]
        public void Entities02(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.foreign_fragment))]
        public void ForeignFragment(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.html5test_com))]
        public void Html5TestCom(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.inbody01))]
        public void Inbody01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.isindex))]
        public void Isindex(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.main_element))]
        public void MainElement(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.math))]
        public void Math(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.menuitem_element))]
        public void MenuitemElement(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.namespace_sensitivity))]
        public void NamespaceSensitivity(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory(Skip = "Skip tests with the scripting disabled since we always act as the interactive user agent."), ResourceData(typeof(tree_construction.noscript01))]
        public void Noscript01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.pending_spec_changes_plain_text_unsafe))]
        public void PendingSpecChangesPlainTextUnsafe(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.pending_spec_changes))]
        public void PendingSpecChanges(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.plain_text_unsafe))]
        public void PlainTextUnsafe(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.ruby))]
        public void Ruby(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.scriptdata01))]
        public void Scriptdata01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tables01))]
        public void Tables01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.template))]
        public void Template(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests1))]
        public void Tests1(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests10))]
        public void Tests10(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests11))]
        public void Tests11(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests12))]
        public void Tests12(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests14))]
        public void Tests14(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests15))]
        public void Tests15(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests16))]
        public void Tests16(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests17))]
        public void Tests17(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests18))]
        public void Tests18(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests19))]
        public void Tests19(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests2))]
        public void Tests2(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests20))]
        public void Tests20(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests21))]
        public void Tests21(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests22))]
        public void Tests22(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests23))]
        public void Tests23(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests24))]
        public void Tests24(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests25))]
        public void Tests25(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests26))]
        public void Tests26(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests3))]
        public void Tests3(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests4))]
        public void Tests4(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests5))]
        public void Tests5(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests6))]
        public void Tests6(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests7))]
        public void Tests7(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests8))]
        public void Tests8(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests9))]
        public void Tests9(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tests_innerHTML_1))]
        public void TestsInnerHtml1(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.tricky01))]
        public void Tricky01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.webkit01))]
        public void Webkit01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction.webkit02))]
        public void Webkit02(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, ResourceData(typeof(tree_construction_regression.gh40_form_in_template))]
        public void Gh40FormInTemplate(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory(Skip = "Scripting not available"), ResourceData(typeof(tree_construction_scripting.document_write))]
        public void DocumentWrite(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        void Dat(int      line,
                 string   html,
                 string   documentFragment,
                 string[] document)
        {
            const string nsSvg = "http://www.w3.org/2000/svg";
            const string nsMath = "http://www.w3.org/1998/Math/MathML";
            const string nsHtml = "http://www.w3.org/1999/xhtml";

            var parse = new Func<string, HtmlNode>(Parser.Parse);

            if (documentFragment != null)
            {
                var tokens = documentFragment.Split(' ');
                var (ns, tagName) =
                    tokens.Length > 1
                    ? (tokens[0], tokens[1])
                    : (null, documentFragment);

                var context =
                    TreeBuilder.Default.CreateElement(
                        tagName, ns == "svg"  ? nsSvg
                               : ns == "math" ? nsMath
                               : nsHtml,
                        new ArraySegment<HtmlAttribute>());

                parse = s => Parser.ParseFragment(s, context);
            }

            var doc = parse(html);
            char[] indent = {};
            var actuals = Dump(doc).Append(string.Empty);
            foreach (var t in document.ZipLongest(actuals, (exp, act) => new { Expected = exp, Actual = act }))
                Assert.Equal(t.Expected, t.Actual);

            string Print(int level, params string[] strings)
            {
                var sb = new StringBuilder();
                if (level > 0)
                {
                    var width = level * 2;
                    if (indent.Length < width)
                        indent = ("|" + new string(' ', width - 1)).ToCharArray();
                    sb.Append(indent, 0, width);
                }
                foreach (var s in strings)
                    sb.Append(s);
                return sb.ToString();
            }

            IEnumerable<string> Dump(HtmlNode node, int level = 0)
            {
                switch (node)
                {
                    case HtmlDocumentType dt:
                        yield return Print(
                            level,
                            "<!DOCTYPE ",
                            new StringBuilder()
                                .Append(dt.Name)
                                .Append(dt.PublicId != null || dt.SystemId != null ? " \"" + dt.PublicId + "\" \"" + dt.SystemId + "\"" : null)
                                .ToString(),
                            ">");
                        break;
                    case HtmlElement e:
                        var ns = e.NamespaceUri == nsSvg ? "svg "
                               : e.NamespaceUri == nsMath ? "math "
                               : null;
                        yield return Print(level, "<", ns, e.TagName, ">");
                        foreach (var a in e.Attributes
                                           .Select(a => (Name : (!string.IsNullOrEmpty(a.Prefix) ? a.Prefix + " " : null) + a.Name,
                                                         Value: a.Value))
                                           .OrderBy(a => a.Name, StringComparer.Ordinal))
                        {
                            yield return Print(level + 1, a.Name, "=", "\"", a.Value, "\"");
                        }
                        if (e is HtmlTemplateElement te && te.Content != null)
                        {
                            yield return Print(level + 1, "content");
                            foreach (var dump in from child in te.Content.ChildNodes
                                                 from dump in Dump(child, level + 2)
                                                 select dump)
                            {
                                yield return dump;
                            }
                        }
                        break;
                    case HtmlText t:
                        var lines = t.Value.Split('\n');
                        if (lines.Length == 1)
                        {
                            yield return Print(level, "\"", t.Value, "\"");
                        }
                        else
                        {
                            for (var i = 0; i < lines.Length; i++)
                                yield return i == 0 ? Print(level, "\"", lines[i])
                                           : i < lines.Length - 1 ? lines[i]
                                           : lines[i] + "\"";
                        }
                        break;
                    case HtmlComment c:
                        yield return Print(level, $"<!-- {c.Data} -->");
                        break;
                }

                foreach (var dump in from child in node.ChildNodes
                                     from dump in Dump(child, level + 1)
                                     select dump)
                {
                    yield return dump;
                }
            }
        }

        sealed class TestData
        {
            public int LineNumber               { get; }
            public string Data                  { get; }
            public IEnumerable<string> Errors   { get; }
            public string DocumentFragment      { get; }
            public IEnumerable<string> Document { get; }
            public bool IsScriptOff             { get; }

            public TestData(int lineNumber, string data,
                            IEnumerable<string> errors = null,
                            string documentFragment = null,
                            IEnumerable<string> document = null,
                            bool isScriptOff = false)
            {
                LineNumber       = lineNumber;
                Data             = data;
                Errors           = errors;
                DocumentFragment = documentFragment;
                Document         = document;
                IsScriptOff      = isScriptOff;
            }

            TestData With(IEnumerable<string> errors, string documentFragment, IEnumerable<string> document, bool isScriptOff) =>
                new TestData(LineNumber, Data, errors, documentFragment, document, isScriptOff);

            public TestData WithErrors(IEnumerable<string> value) =>
                ReferenceEquals(Errors, value) ? this : With(value, DocumentFragment, Document, IsScriptOff);

            public TestData WithDocumentFragment(string value) =>
                DocumentFragment == value ? this : With(Errors, value, Document, IsScriptOff);

            public TestData WithDocument(IEnumerable<string> value) =>
                ReferenceEquals(Document, value) ? this : With(Errors, DocumentFragment, value, IsScriptOff);

            public TestData WithIsScriptOff(bool value) =>
                IsScriptOff == value ? this : With(Errors, DocumentFragment, Document, value);
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class ResourceDataAttribute : DataAttribute
        {
            public ResourceDataAttribute(Type sourceType) => SourceType = sourceType;

            public Type SourceType { get; set; }

            public override IEnumerable<object[]> GetData(MethodInfo testMethod)
            {
                var opener = (Func<Stream>)
                    Delegate.CreateDelegate(typeof(Func<Stream>),
                                            SourceType, nameof(tree_construction.adoption01.GetStream),
                                            throwOnBindFailure: true,
                                            ignoreCase: false);

                return
                    from test in ParseTestData(ReadTextLines(opener))
                    // NOTE! Skip tests with the scripting disabled
                    // since we always act as the interactive user agent.
                    where !test.IsScriptOff
                    select new object[]
                    {
                        test.LineNumber, test.Data, test.DocumentFragment, test.Document.ToArray()
                    };

                static IEnumerable<string> ReadTextLines(Func<Stream> opener)
                {
                    using (var stream = opener())
                    using (var reader = new StreamReader(stream))
                    {
                        foreach (var line in Regex.Split(reader.ReadToEnd(), @"\r?\n"))
                            yield return line;
                    }
                }
            }

            static IEnumerable<TestData> ParseTestData(IEnumerable<string> lines)
            {
                var numberedLines = lines.Select((s, i) => (Nr: i + 1, Line: s));

                TestData td = null;

                string ReadLine(IEnumerator<(int, string Line)> e) =>
                    e.MoveNext() ? e.Current.Line : throw new FormatException();

                IEnumerable<string> ReadLines(IEnumerator<(int, string)> e, ref int nr, ref string line)
                {
                    var list = new List<string>();
                    while (e.MoveNext())
                    {
                        (nr, line) = e.Current;
                        if (line.Length > 0 && line[0] == '#')
                            break;
                        list.Add(line);
                        nr = 0; line = null;
                    }
                    return list;
                }

                using (var e = numberedLines.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        var (lnr, line) = e.Current;

                        do
                        {
                            if (line == null)
                                break;

                            if (line == "#data")
                            {
                                if (td != null)
                                    yield return td;
                                td = new TestData(lnr, string.Join("\r\n", ReadLines(e, ref lnr, ref line)));
                            }
                            else
                            {
                                Debug.Assert(td != null);
                                switch (line)
                                {
                                    case "#errors": td = td.WithErrors(ReadLines(e, ref lnr, ref line)); continue;
                                    case "#document-fragment": td = td.WithDocumentFragment(ReadLine(e)); break;
                                    case "#document": td = td.WithDocument(ReadLines(e, ref lnr, ref line)); continue;
                                    case "#script-on": td = td.WithIsScriptOff(false); break;
                                    case "#script-off": td = td.WithIsScriptOff(true); break;
                                    default: throw new FormatException($"Error parsing line #{lnr}: {line}");
                                }

                                if (!e.MoveNext())
                                    yield break;

                                (lnr, line) = e.Current;
                            }
                        }
                        while (true);
                    }
                }

                if (td != null)
                    yield return td;
            }
        }
    }
}
