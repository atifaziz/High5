namespace ParseFive.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using MoreLinq;
    using Parser;
    using TreeAdapters;
    using Xunit;

    public class TreeConstructionTests
    {

        [Theory, MemberData(nameof(GetTestData), "adoption01.dat")]
        public void Adoption01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "adoption02.dat")]
        public void Adoption02(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "comments01.dat")]
        public void Comments01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "doctype01.dat")]
        public void Doctype01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "domjs-unsafe.dat")]
        public void DomjsUnsafe(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "entities01.dat")]
        public void Entities01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "entities02.dat")]
        public void Entities02(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "foreign-fragment.dat")]
        public void ForeignFragment(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "html5test-com.dat")]
        public void Html5TestCom(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "inbody01.dat")]
        public void Inbody01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "isindex.dat")]
        public void Isindex(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "main-element.dat")]
        public void MainElement(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "math.dat")]
        public void Math(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "menuitem-element.dat")]
        public void MenuitemElement(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "namespace-sensitivity.dat")]
        public void NamespaceSensitivity(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory(Skip = "Skip tests with the scripting disabled since we always act as the interactive user agent."), MemberData(nameof(GetTestData), "noscript01.dat")]
        public void Noscript01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "pending-spec-changes-plain-text-unsafe.dat")]
        public void PendingSpecChangesPlainTextUnsafe(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "pending-spec-changes.dat")]
        public void PendingSpecChanges(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "plain-text-unsafe.dat")]
        public void PlainTextUnsafe(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "ruby.dat")]
        public void Ruby(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "scriptdata01.dat")]
        public void Scriptdata01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tables01.dat")]
        public void Tables01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "template.dat")]
        public void Template(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests1.dat")]
        public void Tests1(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests10.dat")]
        public void Tests10(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests11.dat")]
        public void Tests11(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests12.dat")]
        public void Tests12(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests14.dat")]
        public void Tests14(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests15.dat")]
        public void Tests15(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests16.dat")]
        public void Tests16(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests17.dat")]
        public void Tests17(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests18.dat")]
        public void Tests18(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests19.dat")]
        public void Tests19(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests2.dat")]
        public void Tests2(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests20.dat")]
        public void Tests20(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests21.dat")]
        public void Tests21(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests22.dat")]
        public void Tests22(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests23.dat")]
        public void Tests23(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests24.dat")]
        public void Tests24(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests25.dat")]
        public void Tests25(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests26.dat")]
        public void Tests26(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests3.dat")]
        public void Tests3(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests4.dat")]
        public void Tests4(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests5.dat")]
        public void Tests5(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests6.dat")]
        public void Tests6(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests7.dat")]
        public void Tests7(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests8.dat")]
        public void Tests8(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests9.dat")]
        public void Tests9(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tests_innerHTML_1.dat")]
        public void TestsInnerHtml1(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "tricky01.dat")]
        public void Tricky01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "webkit01.dat")]
        public void Webkit01(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "webkit02.dat")]
        public void Webkit02(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory, MemberData(nameof(GetTestData), "gh40_form_in_template.dat")]
        public void Gh40FormInTemplate(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        [Theory(Skip = "Scripting not available"), MemberData(nameof(GetTestData), "document_write.dat")]
        public void DocumentWrite(int line, string html, string documentFragment, string[] document) =>
            Dat(line, html, documentFragment, document);

        public void Dat(int      line,
                        string   html,
                        string   documentFragment,
                        string[] document)
        {
            const string nsSvg = "http://www.w3.org/2000/svg";
            const string nsMath = "http://www.w3.org/1998/Math/MathML";
            const string nsHtml = "http://www.w3.org/1999/xhtml";

            var parse = new Func<Parser, string, Node>((p, s) => p.Parse(s));

            if (documentFragment != null)
            {
                var tokens = documentFragment.Split(' ');
                var (ns, tagName) =
                    tokens.Length > 1
                    ? (tokens[0], tokens[1])
                    : (null, documentFragment);

                var context =
                    DefaultTreeAdapter.Instance.CreateElement(
                        tagName, ns == "svg" ? nsSvg
                               : ns == "math" ? nsMath
                               : nsHtml,
                        new List<Attr>());

                parse = (p, s) => p.ParseFragment(s, context);
            }

            var parser = new Parser();
            var doc = parse(parser, html);
            char[] indent = {};
            var actuals = Dump(doc).Concat(string.Empty);
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

            IEnumerable<string> Dump(Node node, int level = 0)
            {
                switch (node)
                {
                    case DocumentType dt:
                        yield return Print(
                            level,
                            "<!DOCTYPE ",
                            new StringBuilder()
                                .Append(dt.Name)
                                .Append(dt.PublicId != null || dt.SystemId != null ? " \"" + dt.PublicId + "\" \"" + dt.SystemId + "\"" : null)
                                .ToString(),
                            ">");
                        break;
                    case Element e:
                        var ns = e.NamespaceUri == nsSvg ? "svg "
                               : e.NamespaceUri == nsMath ? "math "
                               : null;
                        yield return Print(level, "<", ns, e.TagName, ">");
                        foreach (var a in e.Attributes
                                           .Select(a => (Name : (!string.IsNullOrEmpty(a.prefix) ? a.prefix + " " : null) + a.name,
                                                         Value: a.value))
                                           .OrderBy(a => a.Name, StringComparer.Ordinal))
                        {
                            yield return Print(level + 1, a.Name, "=", "\"", a.Value, "\"");
                        }
                        if (e is TemplateElement te && te.Content != null)
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
                    case Text t:
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
                    case Comment c:
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

        public static IEnumerable<object[]> GetTestData(string dat)
        {
            var assembly = MethodBase.GetCurrentMethod().DeclaringType.Assembly;

            return
                from name in assembly.GetManifestResourceNames()
                let tokens = name.Split('.').SkipWhile(e => e != "data").Skip(1).ToArray()
                where tokens.Length > 1
                   && tokens.First().StartsWith("tree_construction", StringComparison.OrdinalIgnoreCase)
                   && name.EndsWith("." + dat, StringComparison.OrdinalIgnoreCase)
                from test in ParseTestData(ReadTextResourceLines(name))
                // NOTE! Skip tests with the scripting disabled
                // since we always act as the interactive user agent.
                where !test.IsScriptOff
                select new object[]
                {
                    test.LineNumber, test.Data, test.DocumentFragment, test.Document.ToArray()
                };

            IEnumerable<string> ReadTextResourceLines(string rn)
            {
                using (var stream = assembly.GetManifestResourceStream(rn))
                using (var reader = new StreamReader(stream))
                {
                    foreach (var line in Regex.Split(reader.ReadToEnd(), @"\r?\n"))
                        yield return line;
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
