using System.IO;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;

namespace UELib.IO;

public abstract class UnrealProxyStream(IUnrealStream baseStream) : IUnrealStream
{
    public UnrealPackage Package { get; } = baseStream.Package;
    public UnrealBinaryReader UR { get; } = baseStream.UR;
    public UnrealBinaryWriter UW { get; } = baseStream.UW;

    public IBufferDecoder? Decoder { get; set; }
    public IPackageSerializer Serializer { get; set; }

    uint IUnrealArchive.Version => baseStream.Version;

    public uint LicenseeVersion { get; } = baseStream.LicenseeVersion;
    public uint UE4Version { get; } = baseStream.UE4Version;

    UnrealPackage.GameBuild IUnrealArchive.Build => baseStream.Build;

    public bool BigEndianCode { get; } = baseStream.BigEndianCode;
    public UnrealArchiveFlags Flags { get; } = baseStream.Flags;

    public long Position
    {
        get => baseStream.Position;
        set => baseStream.Position = value;
    }

    public long Length { get; } = baseStream.Length;
    public long AbsolutePosition
    {
        get => baseStream.AbsolutePosition;
        set => baseStream.AbsolutePosition = value;
    }

    public T ReadObject<T>() where T : UObject?
    {
        return baseStream.ReadObject<T>();
    }

    public void WriteObject<T>(T value) where T : UObject?
    {
        baseStream.WriteObject(value);
    }

    public UName ReadName()
    {
        return baseStream.ReadName();
    }

    public void WriteName(in UName value)
    {
        baseStream.WriteName(value);
    }

    public void Skip(int bytes)
    {
        baseStream.Skip(bytes);
    }

    public long Seek(long offset, SeekOrigin origin)
    {
        return baseStream.Seek(offset, origin);
    }

    public int Read(byte[] buffer, int index, int count)
    {
        return baseStream.Read(buffer, index, count);
    }

    public void Write(byte[] buffer, int index, int count)
    {
        baseStream.Write(buffer, index, count);
    }

    public IUnrealStream Record(string name, object? value)
    {
        return baseStream.Record(name, value);
    }

    public void ConformRecordPosition()
    {
        baseStream.ConformRecordPosition();
    }

    public void Dispose()
    {
        baseStream.Dispose();
    }
}
