using System;
using System.IO;
using UELib;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;

namespace Eliot.UELib.Test;

public class UnrealTestArchive(
    UnrealPackage package,
    uint version,
    uint licenseeVersion = 0,
    uint ue4Version = 0,
    bool bigEndianCode = false)
    : IUnrealArchive
{
    public UnrealPackage Package { get; } = package;
    public uint Version { get; } = version;
    public uint LicenseeVersion { get; } = licenseeVersion;
    public uint UE4Version { get; } = ue4Version;
    public bool BigEndianCode { get; } = bigEndianCode;
}

/// Hackish workaround for the issue with UPackageStream requiring a file and path, so that we can perform stream tests without a package.
public class UnrealTestStream : IUnrealStream
{
    public readonly IUnrealArchive Archive;
    public readonly Stream BaseStream;

    public UnrealTestStream(IUnrealArchive archive, Stream baseStream)
    {
        Archive = archive;
        BaseStream = baseStream;

        UR = new UnrealReader(archive, baseStream);
        UW = new UnrealWriter(archive, baseStream);
    }

    public uint Version => Archive.Version;
    public uint LicenseeVersion => Archive.LicenseeVersion;
    public uint UE4Version => Archive.UE4Version;
    public bool BigEndianCode => Archive.BigEndianCode;

    public UnrealPackage Package => Archive.Package;
    public UnrealReader UR { get; }
    public UnrealWriter UW { get; }

    public IBufferDecoder? Decoder { get; set; }
    public IPackageSerializer? Serializer { get; set; }

    public int ReadObjectIndex()
    {
        throw new NotImplementedException();
    }

    public UObject ParseObject(int index)
    {
        throw new NotImplementedException();
    }

    public int ReadNameIndex()
    {
        throw new NotImplementedException();
    }

    public int ReadNameIndex(out int num)
    {
        throw new NotImplementedException();
    }

    public string ParseName(int index)
    {
        throw new NotImplementedException();
    }

    public UName ReadName()
    {
        return UR.ReadName();
    }

    public void WriteName(in UName value)
    {
        UW.WriteName(value);
    }

    public void Skip(int bytes)
    {
        Position += bytes;
    }

    public int Read(byte[] buffer, int index, int count)
    {
        return BaseStream.Read(buffer, index, count);
    }

    public long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    public long AbsolutePosition
    {
        get => Position;
        set => Position = value;
    }

    public T? ReadObject<T>() where T : UObject
    {
        return (T?)Package.IndexToObject(UR.ReadIndex());
    }

    public void WriteObject<T>(T value) where T : UObject
    {
        UW.WriteIndex(value.PackageIndex);
    }

    public long Seek(long offset, SeekOrigin origin)
    {
        return BaseStream.Seek(offset, origin);
    }

    public IUnrealStream Record(string name, object? value)
    {
        return this;
    }

    public void ConformRecordPosition()
    {
    }

    public void Dispose()
    {
        BaseStream.Dispose();
    }
}