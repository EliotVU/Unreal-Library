using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace UELib.IO;

/// <inheritdoc />
public sealed class BigEndianBinaryReader(Stream input) : BinaryReader(input)
{
    /// <inheritdoc />
    public override short ReadInt16() => BinaryPrimitives.ReadInt16BigEndian(ReadBytes(2));

    /// <inheritdoc />
    public override ushort ReadUInt16() => BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(2));

    /// <inheritdoc />
    public override int ReadInt32() => BinaryPrimitives.ReadInt32BigEndian(ReadBytes(4));

    /// <inheritdoc />
    public override uint ReadUInt32() => BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4));

    /// <inheritdoc />
    public override long ReadInt64() => BinaryPrimitives.ReadInt64BigEndian(ReadBytes(8));

    /// <inheritdoc />
    public override ulong ReadUInt64() => BinaryPrimitives.ReadUInt64BigEndian(ReadBytes(8));

    /// <inheritdoc />
    public override float ReadSingle()
    {
#if NET5_0_OR_GREATER
        return BinaryPrimitives.ReadSingleBigEndian(ReadBytes(4));
#else
        uint value = ReadUInt32();
        return Unsafe.As<uint, float>(ref value);
#endif
    }

    /// <inheritdoc />
    public override double ReadDouble()
    {
#if NET5_0_OR_GREATER
        return BinaryPrimitives.ReadDoubleBigEndian(ReadBytes(8));
#else
        return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(ReadBytes(8)));
#endif
    }
}
