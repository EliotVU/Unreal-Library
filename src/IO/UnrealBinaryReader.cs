using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UELib.Branch;
using UELib.Core;

namespace UELib.IO;

public class UnrealBinaryReader(IUnrealArchive archive, BinaryReader baseReader) : IDisposable
{
    internal readonly BinaryReader _BaseReader = baseReader;
    private readonly Stream _BaseStream = baseReader.BaseStream;

    // Allow bulk reading of strings if the package is not big endian encoded.
    // Generally this is the case for most packages, but some games have big endian encoded packages, where a string is serialized per byte.
    private readonly bool _CanBulkRead =
        archive.Flags.HasFlag(UnrealArchiveFlags.BigEndian);

    /// <inheritdoc />
    public void Dispose() => _BaseReader.Dispose();

    // Replace the BaseReader.ReadByte to dispose of the virtualized call.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadByte()
    {
        int b = _BaseStream.ReadByte();
        if (b == -1)
        {
            throw new EndOfStreamException();
        }

        return (byte)b;
    }

    /// <summary>Reads a string from the base stream.</summary>
    /// <param name="length">The length in characters; a negative length indicates an unicode string.</param>
    /// <returns>The string being read with the null termination cut off.</returns>
    private string ReadString(int length)
    {
        int size = length < 0
            ? -length
            : length;
        if (length > 0) // ANSI
        {
            byte[] chars = new byte[size];
#if SHADOWSTRIKE
            // Ugly, re-factor when we have a versioned UnrealReader stream.
            // We could bulk read this and then step back to decrypt, but that's a bit of a hassle.
            if (archive.Build == BuildGeneration.ShadowStrike && archive.LicenseeVersion >= 175)
            {
                for (int i = 0; i < chars.Length; ++i)
                {
                    _BaseStream.Read(chars, i, 1);
                    chars[i] ^= (byte)(((IUnrealStream)archive).AbsolutePosition - 1);
                }
            }
            else
#endif
                // Always bulk read, because the BaseStream is no longer effected by the big-endian byte-order.
            {
                _BaseStream.Read(chars, 0, chars.Length);
            }

            return chars[size - 1] == '\0'
                ? Encoding.ASCII.GetString(chars, 0, chars.Length - 1)
                : Encoding.ASCII.GetString(chars, 0, chars.Length);
        }

        if (length < 0) // UNICODE
        {
#if SHADOWSTRIKE
            // Ugly, re-factor when we have a versioned UnrealReader stream.
            // We could bulk read this and then step back to decrypt, but that's a bit of a hassle.
            if (archive.Build == BuildGeneration.ShadowStrike && archive.LicenseeVersion >= 175)
            {
                char[] chars = new char[size];
                for (int i = 0; i < chars.Length; ++i)
                {
                    char w = (char)_BaseReader.ReadInt16();
                    chars[i] = (char)(w ^ (short)(((IUnrealStream)archive).AbsolutePosition - 1));
                }

                return chars[size - 1] == '\0'
                    ? new string(chars, 0, chars.Length - 1)
                    : new string(chars);
            }
#endif
            if (_CanBulkRead)
            {
                byte[] chars = new byte[size * 2];
                _BaseStream.Read(chars, 0, chars.Length);

                return chars[size * 2 - 2] == '\0' && chars[size * 2 - 1] == '\0'
                    ? Encoding.Unicode.GetString(chars, 0, chars.Length - 2)
                    : Encoding.Unicode.GetString(chars, 0, chars.Length);
            }
            else
            {
                char[] chars = new char[size];
                for (int i = 0; i < chars.Length; ++i)
                {
                    char w = (char)_BaseReader.ReadInt16();
                    chars[i] = w;
                }

                return chars[size - 1] == '\0'
                    ? new string(chars, 0, chars.Length - 1)
                    : new string(chars);
            }
        }

        return string.Empty;
    }

    /// <summary>Reads a length prefixed string from the base stream.</summary>
    /// <returns>The string being read with the null termination cut off.</returns>
    public string ReadString()
    {
        int unfixedSize = ReadIndex();
#if BIOSHOCK
        // TODO: Make this a build option instead.
        if (archive.Build == BuildGeneration.Vengeance &&
            archive.Version >= 135)
        {
            unfixedSize = -unfixedSize;
        }
#endif
        return ReadString(unfixedSize);
    }

    /// <summary>
    ///     Reads a null-terminated Unreal ansi (1 byte) string from the base stream.
    /// </summary>
    /// <returns>the read string without the null-terminal.</returns>
    public string ReadAnsi()
    {
        var strBytes = new List<byte>();
    nextChar:
        byte c = ReadByte();
        if (c != '\0')
        {
            strBytes.Add(c);
            goto nextChar;
        }

        string s = Encoding.ASCII.GetString(strBytes.ToArray());
        return s;
    }

    /// <summary>
    ///     Reads a null-terminated Unreal unicode (2 bytes) string from the base stream.
    /// </summary>
    /// <returns>the read string without the null-terminal.</returns>
    public string ReadUnicode()
    {
        var strBytes = new List<byte>();
    nextWord:
        short w = _BaseReader.ReadInt16();
        if (w != 0)
        {
            strBytes.Add((byte)w);
            strBytes.Add((byte)(w >> 8));
            goto nextWord;
        }

        string s = Encoding.Unicode.GetString(strBytes.ToArray());
        return s;
    }

    /// <summary>Reads a compact 1-5-byte signed integer from the base stream.</summary>
    /// <returns>A 4-byte signed integer read from the base stream.</returns>
    public int ReadCompactIndex()
    {
        int index = 0;

        byte b0 = ReadByte();
        if ((b0 & 0x40) != 0)
        {
            byte b1 = ReadByte();
            if ((b1 & 0x80) != 0)
            {
                byte b2 = ReadByte();
                if ((b2 & 0x80) != 0)
                {
                    byte b3 = ReadByte();
                    if ((b3 & 0x80) != 0)
                    {
                        byte b4 = ReadByte();
                        index = b4;
                    }

                    index = (index << 7) | (b3 & 0x7F);
                }

                index = (index << 7) | (b2 & 0x7F);
            }

            index = (index << 7) | (b1 & 0x7F);
        }

        index = (index << 6) | (b0 & 0x3F);

        if ((b0 & 0x80) != 0)
        {
            index *= -1;
        }

        return index;
    }

    /// <summary>Reads an index from the base stream.</summary>
    /// <returns>A 4-byte signed integer read from the base stream.</returns>
    public int ReadIndex() =>
        archive.Version >= (uint)PackageObjectLegacyVersion.CompactIndexDeprecated
            ? _BaseReader.ReadInt32()
            : ReadCompactIndex();

    /// <summary>
    ///     Reads an Unreal string as a <seealso cref="UName" /> from the base stream.
    /// </summary>
    /// <returns>A unique UName representing the number and index to a <seealso cref="UNameTableItem" /></returns>
    public virtual UName ReadName() => new(ReadString());
}
