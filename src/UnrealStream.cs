using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;
using UELib.Flags;
using UELib.IO;
using UELib.Services;

namespace UELib;

public interface IUnrealStream : IUnrealArchive, IDisposable
{
    new UnrealPackage Package { get; }

    [Obsolete("To be deprecated; ideally we want to pass the UnrealReader directly to the Deserialize methods.")]
    UnrealBinaryReader UR { get; }

    [Obsolete("To be deprecated; ideally we want to pass the UnrealWriter directly to the Serialize methods.")]
    UnrealBinaryWriter UW { get; }

    [Obsolete("Use the EncodedStream instead", true)]
    IBufferDecoder? Decoder { get; set; }

    IPackageSerializer Serializer { get; set; }

    long Position { get; set; }
    long Length { get; }

    // HACK: To be deprecated, but for now we need this to help us retrieve the absolute position when serializing within an UObject's buffer.
    long AbsolutePosition { get; set; }

    // We need to virtualize this so we can override the logic to track where objects are read.
    T? ReadObject<T>() where T : UObject;

    // We need to virtualize this so we can override the logic to track where objects are written.
    void WriteObject<T>(T? value) where T : UObject;

    // We need to virtualize this so we can override the logic to track where names are read.
    UName ReadName();

    // We need to virtualize this so we can override the logic to track where names are written.
    void WriteName(in UName value);

    void Skip(int bytes);
    long Seek(long offset, SeekOrigin origin);

    int Read(byte[] buffer, int index, int count);
    void Write(byte[] buffer, int index, int count);

    public IUnrealStream Record(string name, object? value);
    public void ConformRecordPosition();
}

public static class UnrealStreamExtensions
{
    /// <summary>
    ///     Checks if the stream package is cooked, meaning it has been processed for a specific platform.
    /// </summary>
    /// <param name="streeam">The stream.</param>
    /// <returns>True if the stream package is cooked.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCooked(this IUnrealStream stream)
    {
        return stream.Package.Summary.PackageFlags.HasFlag(PackageFlag.Cooked);
    }

    /// <summary>
    ///     Checks if the stream package contains editor-only data; this is generally false only for packages that are cooked
    ///     for a console platform.
    ///     Except for PC packages that are also cooked for console, this is indicated by the directory name "CookedPCConsole"
    ///     in the package path.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>True if the stream package has serialized editor-only data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsEditorOnlyData(this IUnrealStream stream)
    {
        return !IsCooked(stream) || stream.Package.CookerPlatform != BuildPlatform.Console;
    }
}

public sealed class UnrealWriter(UnrealPackageArchive archive, BinaryWriter baseWriter)
    : UnrealBinaryWriter(archive, baseWriter)
{
    [Obsolete("See WriteString")]
    public void WriteText(string s) => WriteString(s);

    /// <summary>
    ///     Serializes a <see cref="UName" /> with an index and number.
    /// </summary>
    public override void WriteName(in UName value)
    {
        int hash = value.Index;
        int index = archive.NameIndices[hash];
        int number = value.Number + 1;
#if R6
        if (archive.Build == UnrealPackage.GameBuild.BuildName.R6Vegas)
        {
            _BaseWriter.Write(index | (number << 18));

            return;
        }
#endif
        WriteIndex(index);

#if SHADOWSTRIKE
        if (archive.Build == BuildGeneration.ShadowStrike)
        {
            LibServices.LogService.SilentException(new NotSupportedException("Writing external index 0 for ShadowStrike."));
            WriteIndex(0);
        }
#endif
        if (archive.Version >= (uint)PackageObjectLegacyVersion.NumberAddedToName
#if BIOSHOCK
            || archive.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
           )
        {
            _BaseWriter.Write(number);
        }
    }
}

/// <summary>
///     Wrapper for Streams with specific functions for deserializing UELib.UnrealPackage.
/// </summary>
public sealed class UnrealReader(UnrealPackageArchive archive, BinaryReader baseReader)
    : UnrealBinaryReader(archive, baseReader)
{
    [Obsolete("See ReadString")]
    public string ReadText() => ReadString();

    /// <summary>
    ///     Deserializes a <seealso cref="UName" /> from an index and number.
    /// </summary>
    /// <returns>A unique UName representing the number and index to a <seealso cref="UNameTableItem" /></returns>
    public override UName ReadName()
    {
        int index, number;
#if R6 || LEAD
        if (archive.Build == UnrealPackage.GameBuild.BuildName.R6Vegas ||
            archive.Build == BuildGeneration.Lead)
        {
            // Some changes were made with licensee version 71, but I couldn't make much sense of it.
            index = _BaseReader.ReadInt32();
            number = (index >> 18) - 1;
            index &= 0x3FFFF;

            // only the 18 lower bits are used.
            return new UName(archive.Package.Names[index], number);
        }
#endif
        index = ReadIndex();
        number = -1;

#if SHADOWSTRIKE
        if (archive.Build == BuildGeneration.ShadowStrike)
        {
            int externalIndex = ReadIndex();
        }
#endif
        if (archive.Version >= (uint)PackageObjectLegacyVersion.NumberAddedToName
#if BIOSHOCK
            || archive.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
           )
        {
            number = _BaseReader.ReadInt32() - 1;
        }

        return new UName(archive.Package.Names[index], number);
    }

    [Obsolete("Deprecated", true)]
    public int ReadNameIndex(out int num)
    {
        throw new NotImplementedException("Deprecated");
    }
}

[Obsolete]
public class UPackageFileStream : FileStream
{
    internal UPackageFileStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode,
        access, share)
    {
    }
}

[Obsolete("Use UnrealPackageStream")]
public sealed class UPackageStream(string path, FileMode mode, FileAccess access) : UPackageFileStream(path, mode, access,
    FileShare.Read)
{
    [Obsolete("Deprecated", true)]
    public void PostInit(UnrealPackage package) => throw new NotImplementedException("Deprecated");
}

public class UObjectStream(UnrealPackageArchive baseArchive, Stream baseStream, long virtualPosition = 0)
    : UnrealPackagePipedStream(baseArchive, baseStream)
{
    private long _PeekStartPosition;

    /// <summary>
    ///     The position inclusive of the object offset in the package.
    /// </summary>
    public override long AbsolutePosition
    {
        get => Position + virtualPosition;
        set => Position = value - virtualPosition;
    }

    public new byte ReadByte() => (byte)base.ReadByte();

    [Obsolete]
    public void StartPeek() => _PeekStartPosition = Position;

    [Obsolete]
    public void StartPeek(long position)
    {
        _PeekStartPosition = Position;
        Position = position;
    }

    [Obsolete]
    public void EndPeek() => Position = _PeekStartPosition;
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class UObjectRecordStream(UnrealPackageArchive baseArchive, Stream baseStream, long virtualPosition = 0)
    : UObjectStream(baseArchive, baseStream, virtualPosition)
{
#if BINARYMETADATA
    private long _LastRecordPosition;
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void ConformRecordPosition() => _LastRecordPosition = Position;

#if BINARYMETADATA
    public BinaryMetaData BinaryMetaData { get; } = new();

    /// <summary>
    ///     TODO: Move this feature into a stream.
    ///     Outputs the present position and the value of the parsed object.
    ///     Only called in the DEBUGBUILD!
    /// </summary>
    /// <param name="varName">The struct that was read from the previous buffer position.</param>
    /// <param name="varObject">The struct value that was read.</param>
    public override IUnrealStream Record(string varName, object? varObject)
    {
        long size = Position - _LastRecordPosition;
        BinaryMetaData.AddField(varName, varObject, _LastRecordPosition, size);
        _LastRecordPosition = Position;

        return this;
    }
#endif
}

/// <summary>
///     Helper methods to redirect all interface based calls to the UnrealReader/Writer.
/// </summary>
public static class UnrealStreamImplementations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(this IUnrealStream stream) => stream.UR._BaseReader.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUShort(this IUnrealStream stream) => stream.UR._BaseReader.ReadUInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32(this IUnrealStream stream) => stream.UR._BaseReader.ReadUInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadUInt64(this IUnrealStream stream) => stream.UR._BaseReader.ReadUInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadInt16(this IUnrealStream stream) => stream.UR._BaseReader.ReadInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16(this IUnrealStream stream) => stream.UR._BaseReader.ReadUInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32(this IUnrealStream stream) => stream.UR._BaseReader.ReadInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadInt64(this IUnrealStream stream) => stream.UR._BaseReader.ReadInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReadFloat(this IUnrealStream stream) => stream.UR._BaseReader.ReadSingle();

    [Obsolete("Use ReadString")]
    public static string ReadText(this IUnrealStream stream) => stream.UR.ReadString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadString(this IUnrealStream stream) => stream.UR.ReadString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadAnsiNullString(this IUnrealStream stream) => stream.UR.ReadAnsi();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReadUnicodeNullString(this IUnrealStream stream) => stream.UR.ReadUnicode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadBool(this IUnrealStream stream)
    {
        int value = stream.UR._BaseReader.ReadInt32();
        Debug.Assert(value <= 1, $"Unexpected value '{value}' for a boolean");
        return Convert.ToBoolean(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadIndex(this IUnrealStream stream) => stream.UR.ReadIndex();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UObject? ReadObject(this IUnrealStream stream) => stream.ReadObject<UObject>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? ReadObject<T>(this IUnrealStream stream) where T : UObject => stream.ReadObject<T>();

    [Obsolete("Use ReadName")]
    public static UName ReadNameReference(this IUnrealStream stream) => stream.ReadName();

    /// <summary>
    ///     Use this to read a compact integer for Arrays/Maps.
    ///     TODO: Use a custom PackageStream and override ReadLength.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadLength(this IUnrealStream stream)
    {
#if VANGUARD
        if (stream.Build == UnrealPackage.GameBuild.BuildName.Vanguard_SOH)
        {
            return stream.ReadInt32();
        }
#endif
        return stream.ReadIndex();
    }

    public static void ReadArray<T>(this IUnrealStream stream, out UArray<T> array)
        where T : IUnrealDeserializableClass, new()
    {
        int c = stream.ReadLength();
        array = new UArray<T>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = new T();
            element.Deserialize(stream);
            array.Add(element);
        }
    }

    public static void ReadArray<T>(this IUnrealStream stream, out UArray<T> array, int count)
        where T : IUnrealDeserializableClass, new()
    {
        array = new UArray<T>(count);
        for (int i = 0; i < count; ++i)
        {
            var element = new T();
            element.Deserialize(stream);
            array.Add(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStruct<T>(this IUnrealStream stream, out T item)
        where T : struct, IUnrealDeserializableClass
    {
        item = new T();
        item.Deserialize(stream);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStruct<T>(this IUnrealStream stream, T item)
        where T : struct, IUnrealDeserializableClass =>
        item.Deserialize(stream);

    public static unsafe void ReadStructMarshal<T>(this IUnrealStream stream, out T item)
        where T : unmanaged, IUnrealAtomicStruct
    {
        int structSize = sizeof(T);
        byte[] data = new byte[structSize];
        stream.Read(data, 0, structSize);
        fixed (byte* ptr = &data[0])
        {
            item = *(T*)ptr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadClass<T>(this IUnrealStream stream, out T item)
        where T : class, IUnrealDeserializableClass, new()
    {
        item = new T();
        item.Deserialize(stream);
    }

    // Can't seem to overload this :(
    public static unsafe void ReadArrayMarshal<T>(this IUnrealStream stream, out UArray<T> array, int count)
        where T : unmanaged, IUnrealAtomicStruct
    {
        int structSize = sizeof(T);
        array = new UArray<T>(count);
        byte[] data = new byte[structSize * count];
        stream.Read(data, 0, structSize * count);

        for (int i = 0; i < count; ++i)
        {
            fixed (byte* ptr = &data[i * structSize])
            {
                var element = *(T*)ptr;
                array.Add(element);
            }
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<byte> array)
    {
        int c = stream.ReadLength();
        array = new UArray<byte>(c);
        for (int i = 0; i < c; ++i)
        {
            stream.Read(out byte element);
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<int> array)
    {
        int c = stream.ReadLength();
        array = new UArray<int>(c);
        for (int i = 0; i < c; ++i)
        {
            stream.Read(out int element);
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<uint> array)
    {
        int c = stream.ReadLength();
        array = new UArray<uint>(c);
        for (int i = 0; i < c; ++i)
        {
            stream.Read(out uint element);
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<ushort> array)
    {
        int c = stream.ReadLength();
        array = new UArray<ushort>(c);
        for (int i = 0; i < c; ++i)
        {
            stream.Read(out ushort element);
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<short> array)
    {
        int c = stream.ReadLength();
        array = new UArray<short>(c);
        for (int i = 0; i < c; ++i)
        {
            stream.Read(out short element);
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<float> array)
    {
        int c = stream.ReadLength();
        array = new UArray<float>(c);
        for (int i = 0; i < c; ++i)
        {
            Read(stream, out float element);
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<string> array)
    {
        int c = stream.ReadLength();
        array = new UArray<string>(c);
        for (int i = 0; i < c; ++i)
        {
            string element = stream.ReadString();
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<UName> array)
    {
        int c = stream.ReadLength();
        array = new UArray<UName>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadName();
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<UObject> array)
    {
        int c = stream.ReadLength();
        array = new UArray<UObject>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadObject<UObject>();
            array.Add(element);
        }
    }

    public static void ReadArray(this IUnrealStream stream, out UArray<UObject> array, int count)
    {
        array = new UArray<UObject>(count);
        for (int i = 0; i < count; ++i)
        {
            var element = stream.ReadObject<UObject>();
            array.Add(element);
        }
    }

    public static void ReadMap(this IUnrealStream stream, out UMap<ushort, ushort> map)
    {
        int c = stream.ReadLength();
        map = new UMap<ushort, ushort>(c);
        for (int i = 0; i < c; ++i)
        {
            Read(stream, out ushort key);
            Read(stream, out ushort value);
            map.Add(key, value);
        }
    }

    public static void ReadMap<TValue>(this IUnrealStream stream, out UMap<UName, TValue> map)
        where TValue : UObject
    {
        int c = stream.ReadLength();
        map = new UMap<UName, TValue>(c);
        for (int i = 0; i < c; ++i)
        {
            Read(stream, out UName key);
            Read(stream, out TValue value);
            map.Add((UName)key, value);
        }
    }

    public static void ReadMap(this IUnrealStream stream, out UMap<string, UArray<string>> map)
    {
        int c = stream.ReadLength();
        map = new UMap<string, UArray<string>>(c);
        for (int i = 0; i < c; ++i)
        {
            Read(stream, out string key);
            ReadArray(stream, out UArray<string> value);
            map.Add(key, value);
        }
    }

    public static void ReadMap<TValue>(this IUnrealStream stream, out UMap<string, TValue> map)
        where TValue : struct, IUnrealDeserializableClass
    {
        int c = stream.ReadLength();
        map = new UMap<string, TValue>(c);
        for (int i = 0; i < c; ++i)
        {
            Read(stream, out string key);
            ReadStruct<TValue>(stream, out var value);
            map.Add(key, value);
        }
    }

    public static void ReadMap(this IUnrealStream stream, out UMap<UObject, UName> map)
    {
        int c = stream.ReadLength();
        map = new UMap<UObject, UName>(c);
        for (int i = 0; i < c; ++i)
        {
            Read(stream, out UObject key);
            Read(stream, out UName value);
            map.Add(key, value);
        }
    }

    [Obsolete("See UGuid")]
    public static Guid ReadGuid(this IUnrealStream stream)
    {
        // A, B, C, D
        byte[] guidBuffer = new byte[16];
        stream.Read(guidBuffer, 0, 16);
        var guid = new Guid(guidBuffer);
        return guid;
    }

    public static UnrealFlags<TEnum> ReadFlags32<TEnum>(this IUnrealStream stream)
        where TEnum : Enum
    {
        stream.Package.Branch.EnumFlagsMap.TryGetValue(typeof(TEnum), out ulong[] enumMap);
        Debug.Assert(enumMap != null, nameof(enumMap) + " != null");

        ulong flags = stream.UR._BaseReader.ReadUInt32();
        return new UnrealFlags<TEnum>(flags, enumMap);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(this IUnrealStream stream, out UnrealFlags<T> value) where T : Enum =>
        value = ReadFlags32<T>(stream);

    public static UnrealFlags<TEnum> ReadFlags64<TEnum>(this IUnrealStream stream)
        where TEnum : Enum
    {
        stream.Package.Branch.EnumFlagsMap.TryGetValue(typeof(TEnum), out ulong[] enumMap);
        Debug.Assert(enumMap != null, nameof(enumMap) + " != null");

        ulong flags = stream.UR._BaseReader.ReadUInt64();
        return new UnrealFlags<TEnum>(flags, enumMap);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VersionedDeserialize(this IUnrealStream stream, IUnrealDeserializableClass obj)
    {
        Debug.Assert(stream.Serializer != null, "stream.Serializer != null");
        stream.Serializer.Deserialize(stream, obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UPackageIndex value) => value = stream.UR.ReadIndex();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(this IUnrealStream stream, out T value)
        where T : UObject =>
        value = ReadObject<T>(stream);

    public static void Read<T>(this IUnrealStream stream, out IUnrealDeserializableClass item)
        where T : struct, IUnrealDeserializableClass
    {
        item = new T();
        item.Deserialize(stream);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(this IUnrealStream stream, out UBulkData<T> value)
        where T : unmanaged =>
        ReadStruct(stream, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UObject? value) => value = ReadObject<UObject>(stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out string value) => value = stream.UR.ReadString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UName value) => value = stream.ReadName();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(this IUnrealStream stream, out UArray<T> array)
        where T : IUnrealSerializableClass, new() =>
        ReadArray(stream, out array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UArray<UObject> array) => ReadArray(stream, out array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UArray<UName> array) => ReadArray(stream, out array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UArray<string> array) => ReadArray(stream, out array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UArray<uint> array) => ReadArray(stream, out array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UArray<int> array) => ReadArray(stream, out array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UArray<short> array) => ReadArray(stream, out array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UArray<ushort> array) => ReadArray(stream, out array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<TValue>(this IUnrealStream stream, out UMap<UName, TValue> map)
        where TValue : UObject =>
        ReadMap(stream, out map);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out UMap<ushort, ushort> map) => ReadMap(stream, out map);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out byte value) => value = stream.UR._BaseReader.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out short value) => value = stream.UR._BaseReader.ReadInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out ushort value) => value = stream.UR._BaseReader.ReadUInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out int value) => value = stream.UR._BaseReader.ReadInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out uint value) => value = stream.UR._BaseReader.ReadUInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out long value) => value = stream.UR._BaseReader.ReadInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out ulong value) => value = stream.UR._BaseReader.ReadUInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out float value) => value = stream.UR._BaseReader.ReadSingle();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read(this IUnrealStream stream, out bool value) => value = ReadBool(stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteString(this IUnrealStream stream, string value) => stream.UW.WriteString(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAnsiNullString(this IUnrealStream stream, string value) => stream.UW.WriteAnsi(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnicodeNullString(this IUnrealStream stream, string value) =>
        stream.UW.WriteUnicode(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray<T>(this IUnrealStream stream, in UArray<T>? array)
        where T : IUnrealSerializableClass
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (var element in array)
        {
            element.Serialize(stream);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<byte>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        Write(stream, array.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<short>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (short element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<ushort>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (ushort element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<int>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (int element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<uint>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (uint element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<long>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (long element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<ulong>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (ulong element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<bool>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (bool element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<string>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (string element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<UName>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (var element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray(this IUnrealStream stream, in UArray<UObject>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (var element in array)
        {
            stream.Write(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteMap<TValue>(this IUnrealStream stream, in UMap<string, TValue>? map)
        where TValue : struct, IUnrealSerializableClass
    {
        if (map == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, map.Count);
        foreach (var pair in map)
        {
            stream.Write(pair.Key);
            var value = pair.Value;
            stream.WriteStruct(ref value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteMap<TValue>(this IUnrealStream stream, in UMap<UName, TValue>? map)
        where TValue : UObject
    {
        if (map == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, map.Count);
        foreach (var pair in map)
        {
            stream.Write(pair.Key);
            stream.Write(pair.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write<TValue>(this IUnrealStream stream, TValue value) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UPackageIndex value) => stream.UW.WriteIndex(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IUnrealStream stream, ref T item)
        where T : struct, IUnrealSerializableClass => item.Serialize(stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStruct<T>(this IUnrealStream stream, ref T item)
        where T : struct, IUnrealSerializableClass => item.Serialize(stream);

    public static unsafe void WriteStructMarshal<T>(this IUnrealStream stream, ref T item)
        where T : unmanaged, IUnrealAtomicStruct
    {
        int structSize = sizeof(T);
        byte[] data = new byte[structSize];

        fixed (void* ptr = &item)
        {
            //Marshal.Copy((IntPtr)ptr, data, 0, structSize);
            fixed (byte* dataPtr = data)
            {
                Unsafe.CopyBlock(dataPtr, ptr, (uint)structSize);
            }
        }

        stream.Write(data, 0, data.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteClass<T>(this IUnrealStream stream, T item)
        where T : class, IUnrealSerializableClass, new() =>
        item.Serialize(stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, byte[] data) => stream.Write(data, 0, data.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, string value) => stream.UW.WriteString(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, UName name) => stream.WriteName(name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IUnrealStream stream, ref UBulkData<T> value)
        where T : unmanaged =>
        WriteStruct(stream, ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, UObject? obj) => stream.WriteObject(obj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IUnrealStream stream, UArray<T>? array)
        where T : IUnrealSerializableClass =>
        WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IUnrealStream stream, ref UArray<T>? array)
        where T : IUnrealSerializableClass =>
        WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<byte>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<short>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<ushort>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<int>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<uint>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<long>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<ulong>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<bool>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<UName>? array) => WriteArray(stream, in array);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UArray<UObject>? array) => WriteArray(stream, in array);
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static void Write<T>(this IUnrealStream stream, ref T item)
    //    where T : struct, IUnrealSerializableClass =>
    //    stream.WriteStruct(ref item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, byte value) => stream.UW._BaseWriter.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, short value) => stream.UW._BaseWriter.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, ushort value) => stream.UW._BaseWriter.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, int value) => stream.UW._BaseWriter.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, uint value) => stream.UW._BaseWriter.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, long value) => stream.UW._BaseWriter.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, ulong value) => stream.UW._BaseWriter.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, float value) => stream.UW._BaseWriter.Write(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, bool value) =>
        stream.UW._BaseWriter.Write(Convert.ToInt32(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteIndex(this IUnrealStream stream, int index) => stream.UW.WriteIndex(index);

    private readonly struct Peeker(IUnrealStream stream, long position) : IDisposable
    {
        public void Dispose()
        {
            stream.Position = position;
        }
    }

    public static IDisposable Peek(this IUnrealStream stream, long position)
    {
        var peeker = new Peeker(stream, stream.Position);
        stream.Position = position;
        return peeker;
    }
}
