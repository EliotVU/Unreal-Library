using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using UELib;

namespace Eliot.UELib.Benchmark
{
    public class UnrealPackageBenchmark
    {
        private readonly string _TempFilePath;
        private readonly UnrealPackage _Linker;

        public UnrealPackageBenchmark()
        {
            byte[] testFileBytes = { };
            _TempFilePath = Path.Join(Assembly.GetExecutingAssembly().Location, "../test.u");
            // Workaround due the enforced use of UnrealLoader's UPackageStream
            File.WriteAllBytes(_TempFilePath, testFileBytes);

            var stream = new UPackageStream(_TempFilePath, FileMode.Open, FileAccess.Read);
            _Linker = new UnrealPackage(stream);
            _Linker.Deserialize(stream);
        }

        [Benchmark]
        public void PackageDeserialization()
        {
            _Linker.Stream.Position = 4;
            _Linker.Deserialize(_Linker.Stream);
        }

        [Benchmark]
        public void NamesDeserialization()
        {
            _Linker.Stream.Position = 4;
            _Linker.Stream.Seek(_Linker.Summary.NameOffset, SeekOrigin.Begin);
            for (var i = 0; i < _Linker.Summary.NameCount; ++i)
            {
                var nameEntry = new UNameTableItem();
                nameEntry.Deserialize(_Linker.Stream);
            }
        }
    }
}