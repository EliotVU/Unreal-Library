using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using UELib;

namespace Eliot.UELib.Benchmark
{
    [BenchmarkCategory("UnrealPackage")]
    public class UnrealPackageBenchmark
    {
        private readonly UnrealPackage _Linker;

        public UnrealPackageBenchmark()
        {
            var stream =
                new FileStream(
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Samples", "TestUC3.u"),
                    FileMode.Open, FileAccess.Read);
            _Linker = new UnrealPackage(stream);
            _Linker.Deserialize();

            _Linker.BinaryMetaData?.Fields.Clear();
        }

        [Benchmark]
        public void PackageDeserialization()
        {
            _Linker.Stream.Position = 0;
            _Linker.Deserialize(_Linker.Stream);
            _Linker.BinaryMetaData?.Fields.Clear();
        }

        [Benchmark]
        public void PackageInitialization()
        {
            _Linker.InitializePackage();
        }
    }
}
