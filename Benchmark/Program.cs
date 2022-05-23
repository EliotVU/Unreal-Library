using BenchmarkDotNet.Running;

namespace Eliot.UELib.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var packagePerf = BenchmarkRunner.Run<UnrealPackageBenchmark>();
            var streamPerf = BenchmarkRunner.Run<UnrealStreamBenchmark>();
        }
    }
}
