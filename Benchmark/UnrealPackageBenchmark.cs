using System.IO;
using BenchmarkDotNet.Attributes;
using UELib;

namespace Eliot.UELib.Benchmark
{
    [BenchmarkCategory("UnrealPackage")]
    public class UnrealPackageBenchmark
    {
        private readonly UnrealPackage _Package;

        public UnrealPackageBenchmark()
        {
            var fileStream = new FileStream(
                Path.Combine("Samples", "TestUC3.u"),
                FileMode.Open,
                FileAccess.Read
            );

            _Package = new UnrealPackage(fileStream, fileStream.Name, null);
            _Package.Deserialize();

            _Package.BinaryMetaData?.Fields.Clear();
        }

        [Benchmark]
        public void PackageDeserialization()
        {
            _Package.Stream.Position = 0;
            _Package.Deserialize(_Package.Stream);

            _Package.BinaryMetaData?.Fields.Clear();
        }

        [Benchmark]
        public void PackageInitialization()
        {
            _Package.InitializePackage(null);
            _Package.Linker.PackageEnvironment.ObjectContainer.Dispose(_Package);
        }
    }
}
