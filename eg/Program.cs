#region Copyright (c) 2017 Atif Aziz
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

namespace Demo
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using ParseFive.Parser;

    static class Program
    {
        static void Wain(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.Error.WriteLine(Console.InputEncoding.ToString());
            Console.Error.WriteLine(Console.OutputEncoding.ToString());
            string html;

            var source = args.FirstOrDefault() ?? "-";
            if (source == "-")
            {
                html = Console.In.ReadToEnd();
            }
            else if (Uri.TryCreate(source, UriKind.Absolute, out var url) &&
                     url.Scheme != Uri.UriSchemeFile)
            {
                using (var http = new HttpClient())
                    html = http.GetStringAsync(url).GetAwaiter().GetResult();
            }
            else
            {
                html = File.ReadAllText(source);
            }

            var parser = new Parser();
            var doc = parser.Parse(html);
            char[] indent = {};
            Dump(doc, Console.Out);
            Console.WriteLine();

            void Print(TextWriter output, int level, params string[] strings)
            {
                if (level > 0)
                {
                    var width = level * 2;
                    if (indent.Length < width)
                        indent = ("|" + new string(' ', width - 1)).ToCharArray();
                    output.Write(indent, 0, width);
                }
                foreach (var s in strings)
                    output.Write(s);
                output.WriteLine();
            }

            void Dump(Node node, TextWriter output, int level = 0)
            {
                switch (node)
                {
                    case DocumentType dt:
                        Print(output, level,
                            "<!DOCTYPE ",
                            new StringBuilder()
                                .Append(dt.Name)
                                .Append(dt.PublicId != null || dt.SystemId != null ? " \"" + dt.PublicId + "\" \"" + dt.SystemId + "\"" : null)
                                .ToString(),
                            ">");
                        break;
                    case Element e:
                        var ns = e.NamespaceUri == "http://www.w3.org/2000/svg" ? "svg "
                               : e.NamespaceUri == "http://www.w3.org/1998/Math/MathML" ? "math "
                               : null;
                        Print(output, level, "<", ns, e.TagName, ">");
                        foreach (var a in e.Attributes)
                            Print(output, level + 1, a.Name, "=", Jsonify(a.Value));
                        break;
                    case Text t:
                        Print(output, level, "\"" + t.Value + "\"");
                        return;
                    case Comment c:
                        Print(output, level, $"<!-- {c.Data} -->");
                        return;
                }

                foreach (var child in node.ChildNodes)
                    Dump(child, output, level + 1);
            }
        }

        static string Jsonify(string s) => Jsonify(s, null).ToString();

        static StringBuilder Jsonify(string s, StringBuilder sb)
        {
            var length = (s ?? string.Empty).Length;

            (sb = sb ?? new StringBuilder()).Append('"');

            var ch = '\0';

            for (var index = 0; index < length; index++)
            {
                var last = ch;
                ch = s[index];

                switch (ch)
                {
                    case '\\':
                    case '"':
                    {
                        sb.Append('\\');
                        sb.Append(ch);
                        break;
                    }

                    case '/':
                    {
                        if (last == '<')
                            sb.Append('\\');
                        sb.Append(ch);
                        break;
                    }

                    case '\b': sb.Append("\\b"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\r': sb.Append("\\r"); break;

                    default:
                    {
                        if (ch < ' ')
                        {
                            sb.Append("\\u");
                            sb.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(ch);
                        }

                        break;
                    }
                }
            }

            sb.Append('"');
            return sb;
        }

        static int Main(string[] args)
        {
            try
            {
                Wain(args);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 0xbad;
            }
        }
    }
}
