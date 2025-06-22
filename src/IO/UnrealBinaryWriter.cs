using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UELib.Branch;
using UELib.Core;

namespace UELib.IO;

public class UnrealBinaryWriter(IUnrealArchive archive, BinaryWriter baseWriter) : IDisposable
{
    private readonly Stream _BaseStream = baseWriter.BaseStream;
    internal readonly BinaryWriter _BaseWriter = baseWriter;

    private readonly bool _CanBulkWrite =
        archive.Flags.HasFlag(UnrealArchiveFlags.BigEndian);

    /// <inheritdoc />
    public void Dispose() => _BaseWriter.Dispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsUnicode(string s) => s.Any(c => c >= 127);

    /// <summary>Writes a length prefixed string to the base stream.</summary>
    /// <param name="value">the string to be written.</param>
    public void WriteString(string value)
    {
        if (!IsUnicode(value))
        {
            int length = value.Length + 1;
            WriteIndex(length);

            byte[] buffer = Encoding.ASCII.GetBytes(value);
            _BaseStream.Write(buffer, 0, buffer.Length);
            _BaseStream.WriteByte(0); // FIXME: Not all games use null-terminated strings.
        }
        else
        {
            int length = value.Length + 1;
            WriteIndex(-length);

            if (_CanBulkWrite)
            {
                byte[] buffer = Encoding.Unicode.GetBytes(value);
                _BaseStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                // Write 2 bytes at a time due Big-Endian byte-order.
                foreach (char c in value)
                {
                    _BaseWriter.Write((short)c);
                }
            }

            _BaseWriter.Write((short)0); // FIXME: Not all games use null-terminated strings.
        }
    }

    /// <summary>
    ///     Writes a null-terminated Unreal ansi (1 byte) string to the base stream.
    /// </summary>
    /// <param name="value">the string to be written.</param>
    public void WriteAnsi(string value)
    {
        byte[] data = Encoding.ASCII.GetBytes(value);
        _BaseWriter.Write(data, 0, data.Length);
        _BaseStream.WriteByte(0);
    }

    /// <summary>
    ///     Writes a null-terminated Unreal unicode (2 byte) string to the base stream.
    /// </summary>
    /// <param name="value">the string to be written.</param>
    public void WriteUnicode(string value)
    {
        if (_CanBulkWrite)
        {
            byte[] data = Encoding.Unicode.GetBytes(value);
            _BaseWriter.Write(data, 0, data.Length);
        }
        else
        {
            // Write 2 bytes at a time due Big-Endian byte-order.
            foreach (char c in value)
            {
                _BaseWriter.Write((short)c);
            }
        }

        _BaseWriter.Write((short)0);
    }

    /// <summary>Writes a compact 1-5-byte signed integer to the base stream.</summary>
    /// <param name="index">the index to be written.</param>
    public void WriteCompactIndex(int index)
    {
        bool isNegated = index < 0;
        index = Math.Abs(index);
        byte b0 = (byte)(index < 0x40 ? index : (index & 0x3F) | 0x40);

        if (isNegated)
        {
            b0 |= 0x80;
        }

        _BaseStream.WriteByte(b0);
        if ((b0 & 0x40) == 0)
        {
            return;
        }

        index >>= 6;
        byte b1 = (byte)(index < 0x80 ? index : (index & 0x7F) | 0x80);
        _BaseStream.WriteByte(b1);

        if ((b1 & 0x80) == 0)
        {
            return;
        }

        index >>= 7;
        byte b2 = (byte)(index < 0x80 ? index : (index & 0x7F) | 0x80);
        _BaseStream.WriteByte(b2);

        if ((b2 & 0x80) == 0)
        {
            return;
        }

        index >>= 7;
        byte b3 = (byte)(index < 0x80 ? index : (index & 0x7F) | 0x80);
        _BaseStream.WriteByte(b3);

        if ((b3 & 0x80) == 0)
        {
            return;
        }

        byte b4 = (byte)(index >> 7);
        _BaseStream.WriteByte(b4);
    }

    /// <summary>
    ///     Writes an index to the base stream.
    /// </summary>
    /// <param name="index">the index to be written.</param>
    public void WriteIndex(int index)
    {
        if (archive.Version >= (uint)PackageObjectLegacyVersion.CompactIndexDeprecated)
        {
            _BaseWriter.Write(index);

            return;
        }
#if ADVENT
        // FIXME: Implement a ReadIndex serializer class so we can override this logic.
        if (archive.Build == UnrealPackage.GameBuild.BuildName.Advent &&
            archive.Version >= 144)
        {
            _BaseWriter.Write(index);

            return;
        }
#endif
        WriteCompactIndex(index);
    }

    /// <summary>
    ///     Writes a <see cref="UName" /> as an Unreal string to the base stream.
    /// </summary>
    /// <param name="value">the name to be written.</param>
    public virtual void WriteName(in UName value) => WriteString(value.ToString());
}
