using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Eliot.UELib.Test;
using UELib;

namespace Eliot.UELib.Benchmark
{
    public class UnrealPackageBenchmark
    {
        private readonly string _TempFilePath;
        private UnrealPackage _Linker;

        public UnrealPackageBenchmark()
        {
            byte[] testFileBytes = Packages.TestUC2;
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
            _Linker.Stream.Seek(_Linker.Summary.NamesOffset, SeekOrigin.Begin);
            for (var i = 0; i < _Linker.Summary.NamesCount; ++i)
            {
                var nameEntry = new UNameTableItem();
                nameEntry.Deserialize(_Linker.Stream);
            }
        }
    }
}