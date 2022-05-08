using BenchmarkDotNet.Running;

namespace Eliot.UELib.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<UnrealPackageBenchmark>();
        }
    }
}
