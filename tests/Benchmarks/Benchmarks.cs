#region Copyright (c) 2017 Atif Aziz, Adrian Guerra
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

namespace ParseFive.Benchmarks
{
    using System;
    using System.IO;
    using System.Linq;
    using BenchmarkDotNet.Attributes;

    [MemoryDiagnoser]
    public class Benchmarks
    {
        string _lhc;
        string _nodeJsOrg;
        string _npmOrg;
        string _hugePage;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _lhc       = LoadTextFile("lhc.html");
            _nodeJsOrg = LoadTextFile("nodejsorg.html");
            _npmOrg    = LoadTextFile("npmorg.html");
            _hugePage  = LoadTextFile("huge-page.html");
        }

        [Benchmark] public HtmlDocument Lhc()       => Parser.Parse(_lhc);
        [Benchmark] public HtmlDocument NodeJsOrg() => Parser.Parse(_nodeJsOrg);
        [Benchmark] public HtmlDocument NpmOrg()    => Parser.Parse(_npmOrg);
        [Benchmark] public HtmlDocument HugePage()  => Parser.Parse(_hugePage);

        static string LoadTextFile(string name)
        {
            var cd = Environment.CurrentDirectory;
            var root = Path.GetPathRoot(cd);
            var dirs = cd.Substring(root.Length)
                         .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var searches =
                from n in Enumerable.Range(1, dirs.Length).Reverse()
                select root + string.Join(Path.DirectorySeparatorChar.ToString(), dirs.Take(n))
                into path
                select Path.Combine(path, name) into path
                where File.Exists(path)
                select path;

            using (var stream = File.OpenRead(searches.First()))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
