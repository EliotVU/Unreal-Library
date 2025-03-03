using System.IO;
using BenchmarkDotNet.Attributes;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Benchmark
{
    public class UnrealStreamBenchmark
    {
        private readonly byte[] _ArchiveData = new byte[20];
        private readonly IUnrealStream _Stream;

        private UColor _Color = new UColor(128, 64, 32, 0);
        private readonly long _ColorPosition;

        private int _CompactIndex1 = 0x40 - 1;
        private int _CompactIndex2 = 0x40 + (0x80 - 1);
        private int _CompactIndex3 = 0x40 + 0x80 + (0x80 - 1);
        private readonly long _CompactIndexPosition1, _CompactIndexPosition2, _CompactIndexPosition3;

        private string _String = "String";
        private readonly long _StringPosition;

        public UnrealStreamBenchmark()
        {
            var baseStream = new MemoryStream(_ArchiveData);
            var testArchive = new UnrealTestArchive(null, 100);
            _Stream = new UnrealTestStream(testArchive, baseStream);

            _CompactIndexPosition1 = _Stream.Position;
            _Stream.WriteIndex(_CompactIndex1);

            _CompactIndexPosition2 = _Stream.Position;
            _Stream.WriteIndex(_CompactIndex2);

            _CompactIndexPosition3 = _Stream.Position;
            _Stream.WriteIndex(_CompactIndex3);

            _StringPosition = _Stream.Position;
            _Stream.WriteString(_String);

            _ColorPosition = _Stream.Position;
            _Stream.WriteStruct(ref _Color);
        }

        [Benchmark]
        public void ReadCompactIndex1()
        {
            _Stream.Position = _CompactIndexPosition1;
            _CompactIndex1 = _Stream.ReadIndex();
        }

        [Benchmark]
        public void WriteCompactIndex1()
        {
            _Stream.Position = _CompactIndexPosition1;
            _Stream.WriteIndex(_CompactIndex1);
        }

        [Benchmark]
        public void ReadCompactIndex2()
        {
            _Stream.Position = _CompactIndexPosition2;
            _CompactIndex2 = _Stream.ReadIndex();
        }

        [Benchmark]
        public void WriteCompactIndex2()
        {
            _Stream.Position = _CompactIndexPosition2;
            _Stream.WriteIndex(_CompactIndex2);
        }

        [Benchmark]
        public void ReadCompactIndex3()
        {
            _Stream.Position = _CompactIndexPosition3;
            _CompactIndex3 = _Stream.ReadIndex();
        }

        [Benchmark]
        public void WriteCompactIndex3()
        {
            _Stream.Position = _CompactIndexPosition3;
            _Stream.WriteIndex(_CompactIndex3);
        }

        [Benchmark]
        public void ReadString()
        {
            _Stream.Position = _StringPosition;
            _String = _Stream.ReadString();
        }

        [Benchmark]
        public void WriteString()
        {
            _Stream.Position = _StringPosition;
            _Stream.WriteString(_String);
        }

        [Benchmark]
        public void ReadColor()
        {
            _Stream.Position = _ColorPosition;
            _Stream.ReadStruct(_Color);
        }

        [Benchmark]
        public void WriteColor()
        {
            _Stream.Position = _ColorPosition;
            _Stream.WriteStruct(ref _Color);
        }

        // So far marshal is faster! But writing is still slower
        // |             Method |     Mean |    Error |   StdDev |
        // |------------------- |---------:|---------:|---------:|
        // |   ReadColor        | 38.17 ns | 0.781 ns | 0.767 ns |
        // |   ReadColorMarshal | 16.06 ns | 0.344 ns | 0.545 ns |
        [Benchmark]
        public void ReadColorMarshal()
        {
            _Stream.Position = _ColorPosition;
            _Stream.ReadStructMarshal(out _Color);
        }

        [Benchmark]
        public void WriteColorMarshal()
        {
            _Stream.Position = _ColorPosition;
            _Stream.WriteStructMarshal(ref _Color);
        }
    }
}
