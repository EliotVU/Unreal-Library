using System;
using System.Buffers.Binary;
using System.IO;

namespace UELib.IO;

/// <inheritdoc />
public sealed class BigEndianBinaryWriter(Stream input) : BinaryWriter(input)
{
    /// <inheritdoc />
    public override void Write(short value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        base.Write(value);
    }

    /// <inheritdoc />
    public override void Write(ushort value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        base.Write(value);
    }

    /// <inheritdoc />
    public override void Write(int value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        base.Write(value);
    }

    /// <inheritdoc />
    public override void Write(uint value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        base.Write(value);
    }

    /// <inheritdoc />
    public override void Write(long value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        base.Write(value);
    }

    /// <inheritdoc />
    public override void Write(ulong value)
    {
        if (BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        base.Write(value);
    }

    /// <inheritdoc />
    public override void Write(float value)
    {
        unsafe
        {
            Write(*(uint*)&value);
        }
    }

    /// <inheritdoc />
    public override void Write(double value)
    {
        unsafe
        {
            Write(*(ulong*)&value);
        }
    }
}
