namespace High5.Benchmarks
{
    using System.Reflection;
    using BenchmarkDotNet.Running;

    static class Program
    {
        static void Main(string[] args) =>
            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly())
                .Run(args);
    }
}
