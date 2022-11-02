using System.IO;
using BenchmarkDotNet.Attributes;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Benchmark
{
    public class UnrealStreamBenchmark
    {
        private readonly IUnrealStream _Stream;
        private UColor _Color;

        public UnrealStreamBenchmark()
        {
            _Color = new UColor(128, 64, 32, 0);

            var baseStream = new MemoryStream(new byte[4]);
            _Stream = new UnrealTestStream(null, baseStream);
        }

        [Benchmark]
        public void WriteColor()
        {
            _Stream.Position = 0;
            _Stream.WriteStruct(ref _Color);
        }

        [Benchmark]
        public void ReadColor()
        {
            _Stream.Position = 0;
            _Stream.ReadStruct(out _Color);
        }

        [Benchmark]
        public void WriteColorMarshal()
        {
            _Stream.Position = 0;
            _Stream.WriteAtomicStruct(ref _Color);
        }

        [Benchmark]
        public void ReadColorMarshal()
        {
            _Stream.Position = 0;
            _Stream.ReadAtomicStruct(out _Color);
        }
    }
}