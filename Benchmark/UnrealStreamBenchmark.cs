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
        private int _CompactIndex;

        private readonly byte[] _ArchiveData =
        {
            // _Color
            128, 64, 32, 0,
            // _CompactIndex
            0x4F, 0xEF, 0x01
        };
        private long _ColorPosition;
        private long _CompactIndexPosition;

        public UnrealStreamBenchmark()
        {
            var baseStream = new MemoryStream(_ArchiveData);
            var testArchive = new UnrealTestArchive(null, 100);
            _Stream = new UnrealTestStream(testArchive, baseStream);

            _ColorPosition = _Stream.Position;
            _Stream.ReadStruct(out _Color);

            _CompactIndexPosition = _Stream.Position;
            _CompactIndex = _Stream.ReadIndex();
        }

        [Benchmark]
        public void ReadCompactIndex()
        {
            _Stream.Position = _CompactIndexPosition;
            _CompactIndex = _Stream.ReadIndex();
        }

        [Benchmark]
        public void WriteColor()
        {
            _Stream.Position = _ColorPosition;
            _Stream.WriteStruct(ref _Color);
        }

        [Benchmark]
        public void ReadColor()
        {
            _Stream.Position = _ColorPosition;
            _Stream.ReadStruct(out _Color);
        }

        [Benchmark]
        public void WriteColorMarshal()
        {
            _Stream.Position = _ColorPosition;
            _Stream.WriteAtomicStruct(ref _Color);
        }

        [Benchmark]
        public void ReadColorMarshal()
        {
            _Stream.Position = _ColorPosition;
            _Stream.ReadAtomicStruct(out _Color);
        }
    }
}
