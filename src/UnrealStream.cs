using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;
using UELib.Flags;
using UELib.IO;

namespace UELib;

public interface IUnrealStream : IUnrealArchive, IDisposable
{
    new UnrealPackage Package { get; }

    UnrealBinaryReader UR { get; }
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

    [Obsolete]
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
    public static T? ReadObject<T>(this IUnrealStream stream)
        where T : UObject => stream.ReadObject<T>();

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadStruct<T>(this IUnrealStream stream)
        where T : struct, IUnrealDeserializableClass
    {
        var item = new T();
        item.Deserialize(stream);

        return item;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadClass<T>(this IUnrealStream stream)
        where T : class, IUnrealDeserializableClass, new()
    {
        var item = new T();
        item.Deserialize(stream);

        return item;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<byte> ReadByteArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<byte>(c);
        // Yeah, could read the whole array at once, but this is required for the decoding (decryption and the like) to work properly.
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadByte();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<short> ReadShortArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<short>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadInt16();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<ushort> ReadUShortArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<ushort>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadUInt16();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<int> ReadIntArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<int>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadInt32();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<uint> ReadUIntArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<uint>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadUInt32();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<long> ReadLongArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<long>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadInt64();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<ulong> ReadULongArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<ulong>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadUInt64();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<float> ReadFloatArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<float>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadFloat();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<string> ReadStringArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<string>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadString();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<UName> ReadNameArray(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var array = new UArray<UName>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadName();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<T> ReadObjectArray<T>(this IUnrealStream stream)
        where T : UObject
    {
        int c = stream.ReadLength();
        var array = new UArray<T>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = stream.ReadObject<T>();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<T> ReadArray<T>(this IUnrealStream stream)
        where T : IUnrealDeserializableClass, new()
    {
        int c = stream.ReadLength();
        var array = new UArray<T>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = new T();
            element.Deserialize(stream);
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<TValue> ReadArray<TValue>(this IUnrealStream stream, Func<TValue> elementSelector)
    {
        int c = stream.ReadLength();
        var array = new UArray<TValue>(c);
        for (int i = 0; i < c; ++i)
        {
            var element = elementSelector();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<T> ReadArray<T>(this IUnrealStream stream, int count)
        where T : IUnrealDeserializableClass, new()
    {
        var array = new UArray<T>(count);
        for (int i = 0; i < count; ++i)
        {
            var element = new T();
            element.Deserialize(stream);
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UArray<TValue> ReadArray<TValue>(this IUnrealStream stream, int count, Func<TValue> elementSelector)
    {
        var array = new UArray<TValue>(count);
        for (int i = 0; i < count; ++i)
        {
            var element = elementSelector();
            array.Add(element);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray<T>(this IUnrealStream stream, out UArray<T> array, int count)
        where T : IUnrealDeserializableClass, new()
    {
        array = stream.ReadArray<T>(count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<byte> array) => array = stream.ReadByteArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<int> array) => array = stream.ReadIntArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<uint> array) => array = stream.ReadUIntArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<ushort> array) => array = stream.ReadUShortArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<short> array) => array = stream.ReadShortArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<float> array) => array = stream.ReadFloatArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<string> array) => array = stream.ReadStringArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<UName> array) => array = stream.ReadNameArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<UObject> array) => array = stream.ReadObjectArray<UObject>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray<T>(this IUnrealStream stream, out UArray<T> array)
        where T : IUnrealDeserializableClass, new() =>
        array = stream.ReadArray<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadArray(this IUnrealStream stream, out UArray<UObject> array, int count)
    {
        array = new UArray<UObject>(count);
        for (int i = 0; i < count; ++i)
        {
            var element = stream.ReadObject<UObject>();
            array.Add(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadMap<TValue>(this IUnrealStream stream, out UMap<UName, TValue> map)
        where TValue : UObject
    {
        int c = stream.ReadLength();
        map = new UMap<UName, TValue>(c);
        for (int i = 0; i < c; ++i)
        {
            Read(stream, out UName key);
            Read(stream, out TValue value);
            map.Add(key, value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /// <summary>
    /// Reads a map using dynamic typing (that should hopefully be optimized out by the compiler).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadMap<TKey, TValue>(this IUnrealStream stream, out UMap<TKey, TValue> map) => map = ReadMap<TKey, TValue>(stream);

    /// <summary>
    /// Reads a map using dynamic typing (that should hopefully be optimized out by the compiler).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UMap<TKey, TValue> ReadMap<TKey, TValue>(this IUnrealStream stream)
    {
        int c = stream.ReadLength();
        var map = new UMap<TKey, TValue>(c);
        for (int i = 0; i < c; ++i)
        {
            ReadTyped(stream, out TKey key);
            ReadTyped(stream, out TValue value);
            map.Add(key, value);
        }

        return map;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UMap<TKey, TValue> ReadMap<TKey, TValue>(this IUnrealStream stream,
                                                           Func<TKey> keySelector,
                                                           Func<TValue> valueSelector)
    {
        int c = stream.ReadLength();
        var map = new UMap<TKey, TValue>(c);
        for (int i = 0; i < c; ++i)
        {
            var key = keySelector();
            var value = valueSelector();
            map.Add(key, value);
        }

        return map;
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
    public static void Read(this IUnrealStream stream, out UPackageIndex value) => value = stream.UR.ReadIndex();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read<T>(this IUnrealStream stream, out T value)
        where T : UObject =>
        value = ReadObject<T>(stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    private static void ReadTyped<TValue>(this IUnrealStream stream, out TValue value)
    {
        value = default;
        switch (value)
        {
            case byte v:
                Read(stream, out v);
                break;

            case short v:
                Read(stream, out v);
                break;

            case ushort v:
                Read(stream, out v);
                break;

            case int v:
                Read(stream, out v);
                break;

            case uint v:
                Read(stream, out v);
                break;

            case long v:
                Read(stream, out v);
                break;

            case ulong v:
                Read(stream, out v);
                break;

            case float v:
                Read(stream, out v);
                break;

            case bool v:
                Read(stream, out v);
                break;

            case string v:
                Read(stream, out v);
                break;

            case UName v:
                Read(stream, out v);
                break;

            case UPackageIndex v:
                Read(stream, out v);
                break;

            case UObject v:
                Read(stream, out v);
                break;

            case IUnrealSerializableClass v:
                v.Deserialize(stream);
                break;
        }
    }

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
    public static void WriteArray(this IUnrealStream stream, in UArray<float>? array)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (float element in array)
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
    public static void WriteArray<TValue>(this IUnrealStream stream, in UArray<TValue>? array, Action<TValue> valueWriter)
    {
        if (array == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, array.Count);
        foreach (var element in array)
        {
            valueWriter(element);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteMap(this IUnrealStream stream, in UMap<ushort, ushort>? map)
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
            stream.Write((UObject)pair.Value);
        }
    }

    /// <summary>
    /// WriteMap with dynamic type checking, hopefully optimized out when the type is known at compile time.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteMap<TKey, TValue>(this IUnrealStream stream, in UMap<TKey, TValue>? map)
    {
        if (map == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, map.Count);
        foreach (var pair in map)
        {
            stream.WriteTyped(pair.Key);
            stream.WriteTyped(pair.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteMap<TKey, TValue>(this IUnrealStream stream, in UMap<TKey, TValue>? map,
                                              Action<TKey> keyWriter,
                                              Action<TValue> valueWriter)
    {
        if (map == null)
        {
            WriteIndex(stream, 0);

            return;
        }

        WriteIndex(stream, map.Count);
        foreach (var pair in map)
        {
            keyWriter(pair.Key);
            valueWriter(pair.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTyped<TValue>(this IUnrealStream stream, in TValue value)
    {
        switch (value)
        {
            case byte v:
                Write(stream, v);
                break;

            case short v:
                Write(stream, v);
                break;

            case ushort v:
                Write(stream, v);
                break;

            case int v:
                Write(stream, v);
                break;

            case uint v:
                Write(stream, v);
                break;

            case long v:
                Write(stream, v);
                break;

            case ulong v:
                Write(stream, v);
                break;

            case float v:
                Write(stream, v);
                break;

            case bool v:
                Write(stream, v);
                break;

            case string v:
                Write(stream, v);
                break;

            case UName v:
                Write(stream, v);
                break;

            case UPackageIndex v:
                Write(stream, v);
                break;

            case UObject v:
                Write(stream, v);
                break;

            case IUnrealSerializableClass v:
                v.Serialize(stream);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this IUnrealStream stream, in UPackageIndex value) => stream.UW.WriteIndex(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this IUnrealStream stream, ref T item)
        where T : struct, IUnrealSerializableClass => item.Serialize(stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStruct<T>(this IUnrealStream stream, ref T item)
        where T : struct, IUnrealSerializableClass => item.Serialize(stream);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStruct<T>(this IUnrealStream stream, T item)
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
