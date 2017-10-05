namespace ParseFive.Benchmarks
{
    using System;
    using System.IO;
    using System.Linq;
    using BenchmarkDotNet.Running;
    using BenchmarkDotNet.Attributes;

    public class Benchmarks
    {
        readonly string _lhc;
        readonly string _nodeJsOrg;
        readonly string _npmOrg;
        readonly string _hugePage;

        public Benchmarks()
        {
            _lhc       = GetTextResource("lhc.html");
            _nodeJsOrg = GetTextResource("nodejsorg.html");
            _npmOrg    = GetTextResource("npmorg.html");
            _hugePage  = GetTextResource("huge-page.html");

            string GetTextResource(string name)
            {
                var cd = Environment.CurrentDirectory;
                var root = Path.GetPathRoot(cd);
                var dirs = cd.Substring(root.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var searches =
                    from n in Enumerable.Range(1, dirs.Length).Reverse()
                    select root + string.Join(Path.DirectorySeparatorChar.ToString(), dirs.Take(n)) into path
                    select Path.Combine(path, name) into path
                    where File.Exists(path)
                    select path;


                using (var stream = File.OpenRead(searches.First()))
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        [Benchmark]
        public Document Lhc() =>
            DefaultTreeAdapter.Instance.CreateParser().Parse(_lhc);

        [Benchmark]
        public Document NodeJsOrg() =>
            DefaultTreeAdapter.Instance.CreateParser().Parse(_nodeJsOrg);

        [Benchmark]
        public Document NpmOrg() =>
            DefaultTreeAdapter.Instance.CreateParser().Parse(_npmOrg);

        [Benchmark]
        public Document HugePage() =>
            DefaultTreeAdapter.Instance.CreateParser().Parse(_hugePage);
    }

    static class Program
    {
        static void Main()
        {
            System.Console.WriteLine(Environment.CurrentDirectory);
            BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
