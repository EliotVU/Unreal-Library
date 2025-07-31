using System.Numerics;
using BenchmarkDotNet.Attributes;
using Eliot.UELib.Test;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Benchmark;

[BenchmarkCategory("UnrealStream")]
public class UnrealStreamBenchmark
{
    //| Method                 | Mean       | Error     | StdDev     | Median     |
    //|----------------------- |-----------:|----------:|-----------:|-----------:|
    //| ReadInt32              |   5.294 ns | 0.2509 ns |  0.6869 ns |   4.980 ns |
    //| WriteInt32             |   9.282 ns | 0.4072 ns |  1.1619 ns |   9.020 ns |
    //| ReadCompactIndex1      |   6.499 ns | 0.1571 ns |  0.1614 ns |   6.518 ns |
    //| WriteCompactIndex1     |   8.888 ns | 0.3097 ns |  0.9035 ns |   8.658 ns |
    //| ReadCompactIndex2      |   8.677 ns | 0.4152 ns |  1.2177 ns |   8.068 ns |
    //| WriteCompactIndex2     |  12.246 ns | 0.3799 ns |  1.1141 ns |  12.063 ns |
    //| ReadCompactIndex3      |   9.146 ns | 0.2274 ns |  0.6670 ns |   8.961 ns |
    //| WriteCompactIndex3     |  12.351 ns | 0.4338 ns |  1.2790 ns |  12.007 ns |
    //| ReadString             |  33.230 ns | 0.7688 ns |  2.2182 ns |  32.793 ns |
    //| WriteString            |  47.027 ns | 0.6626 ns |  0.5533 ns |  46.892 ns |
    //| ReadUnicodeString      |  56.253 ns | 1.9718 ns |  5.8139 ns |  55.199 ns |
    //| WriteUnicodeString     |  47.195 ns | 0.7643 ns |  0.6776 ns |  46.796 ns |
    //| ReadAnsiNullString     | 135.265 ns | 4.5381 ns | 12.9475 ns | 131.953 ns |
    //| WriteAnsiNullString    |  29.486 ns | 0.7090 ns |  2.0794 ns |  29.138 ns |
    //| ReadUnicodeNullString  | 218.980 ns | 4.3951 ns |  6.9711 ns | 215.966 ns |
    //| WriteUnicodeNullString |  41.222 ns | 0.7677 ns |  0.7884 ns |  40.930 ns |
    //| ReadName               |  15.753 ns | 0.2120 ns |  0.1983 ns |  15.653 ns |
    //| WriteName              |  10.935 ns | 0.5582 ns |  1.6106 ns |  11.028 ns |
    //| ReadColor              |  15.036 ns | 0.3183 ns |  0.7985 ns |  14.835 ns |
    //| WriteColor             |  20.262 ns | 0.4038 ns |  0.4147 ns |  20.274 ns |
    //| ReadColorMarshal       |  13.243 ns | 0.2127 ns |  0.1776 ns |  13.167 ns |
    //| WriteColorMarshal      |  16.553 ns | 0.8599 ns |  2.5354 ns |  15.883 ns |
    //| ReadMatrix             |  64.193 ns | 1.9424 ns |  5.6968 ns |  63.202 ns |
    //| WriteMatrix            | 119.797 ns | 3.9709 ns | 11.5834 ns | 117.552 ns |
    //| ReadMatrixMarshal      |  18.508 ns | 0.3762 ns |  0.9507 ns |  18.262 ns |
    //| WriteMatrixMarshal     |  20.775 ns | 1.1954 ns |  3.5245 ns |  19.036 ns |
    private readonly UnrealPackageArchive _Archive;
    private readonly IUnrealStream _Stream;

    private int _CompactIndex1 = 0x40 - 1;
    private int _CompactIndex2 = 0x40 + (0x80 - 1);
    private int _CompactIndex3 = 0x40 + 0x80 + (0x80 - 1);

    private int _Int32 = 0x7FFFFFFF;
    private readonly long _Int32Position, _CompactIndexPosition1, _CompactIndexPosition2, _CompactIndexPosition3;

    private UName _Name = new("Name");
    private readonly long _NamePosition;

    private string _String = "String";
    private string _UnicodeString = "语言处理";
    private string _AnsiNullString = "NullTerminatedString";
    private string _UnicodeNullString = "NullTerminated语言处理";
    private readonly long _StringPosition, _UnicodeStringPosition, _AnsiNullStringPosition, _UnicodeNullStringPosition;

    private UColor _Color = new(128, 64, 32, 0);
    private readonly long _ColorPosition;

    private UMatrix _Matrix = (UMatrix)Matrix4x4.Identity;
    private readonly long _MatrixPosition;

    public UnrealStreamBenchmark()
    {
        _Archive = UnrealPackageUtilities.CreateMemoryArchive(100);
        _Archive.Package.Names.Add(new UNameTableItem(_Name)); // Ensure that index 0 exists for ReadName
        _Archive.NameIndices.Add(_Name.Index, 0);

        _Stream = _Archive.Stream;

        _Int32Position = _Stream.Position;
        _Stream.WriteIndex(_Int32);

        _CompactIndexPosition1 = _Stream.Position;
        _Stream.WriteIndex(_CompactIndex1);

        _CompactIndexPosition2 = _Stream.Position;
        _Stream.WriteIndex(_CompactIndex2);

        _CompactIndexPosition3 = _Stream.Position;
        _Stream.WriteIndex(_CompactIndex3);

        _StringPosition = _Stream.Position;
        _Stream.WriteString(_String);

        _UnicodeStringPosition = _Stream.Position;
        _Stream.WriteString(_UnicodeString);

        _AnsiNullStringPosition = _Stream.Position;
        _Stream.WriteString(_AnsiNullString);

        _UnicodeNullStringPosition = _Stream.Position;
        _Stream.WriteString(_UnicodeNullString);

        _NamePosition = _Stream.Position;
        _Stream.WriteName(_Name);

        _ColorPosition = _Stream.Position;
        _Stream.WriteStruct(ref _Color);

        _MatrixPosition = _Stream.Position;
        _Stream.WriteStruct(ref _Matrix);
    }

    ~UnrealStreamBenchmark() => _Archive.Dispose();

    [Benchmark]
    public void ReadInt32()
    {
        _Stream.Position = _Int32Position;
        _Int32 = _Stream.ReadInt32();
    }

    [Benchmark]
    public void WriteInt32()
    {
        _Stream.Position = _Int32Position;
        _Stream.Write(_Int32);
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
    public void ReadUnicodeString()
    {
        _Stream.Position = _UnicodeStringPosition;
        _UnicodeString = _Stream.ReadString();
    }

    [Benchmark]
    public void WriteUnicodeString()
    {
        _Stream.Position = _UnicodeStringPosition;
        _Stream.WriteString(_UnicodeString);
    }

    [Benchmark]
    public void ReadAnsiNullString()
    {
        _Stream.Position = _AnsiNullStringPosition;
        _AnsiNullString = _Stream.ReadAnsiNullString();
    }

    [Benchmark]
    public void WriteAnsiNullString()
    {
        _Stream.Position = _AnsiNullStringPosition;
        _Stream.WriteAnsiNullString(_AnsiNullString);
    }

    [Benchmark]
    public void ReadUnicodeNullString()
    {
        _Stream.Position = _UnicodeNullStringPosition;
        _UnicodeNullString = _Stream.ReadUnicodeNullString();
    }

    [Benchmark]
    public void WriteUnicodeNullString()
    {
        _Stream.Position = _UnicodeNullStringPosition;
        _Stream.WriteUnicodeNullString(_UnicodeNullString);
    }

    [Benchmark]
    public void ReadName()
    {
        _Stream.Position = _NamePosition;
        _Name = _Stream.ReadName();
    }

    [Benchmark]
    public void WriteName()
    {
        _Stream.Position = _NamePosition;
        _Stream.WriteName(_Name);
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

    [Benchmark]
    public void ReadMatrix()
    {
        _Stream.Position = _MatrixPosition;
        _Stream.ReadStruct(_Matrix);
    }

    [Benchmark]
    public void WriteMatrix()
    {
        _Stream.Position = _MatrixPosition;
        _Stream.WriteStruct(ref _Matrix);
    }

    [Benchmark]
    public void ReadMatrixMarshal()
    {
        _Stream.Position = _MatrixPosition;
        _Stream.ReadStructMarshal(out _Matrix);
    }

    [Benchmark]
    public void WriteMatrixMarshal()
    {
        _Stream.Position = _MatrixPosition;
        _Stream.WriteStructMarshal(ref _Matrix);
    }
}
