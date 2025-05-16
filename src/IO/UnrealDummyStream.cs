using System;
using System.IO;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;

namespace UELib.IO;

public sealed class UnrealDummyStream : PipedStream, IUnrealStream
{
    private readonly IUnrealArchive _Archive;

    public UnrealDummyStream(IUnrealArchive archive, Stream baseStream) : base(baseStream)
    {
        _Archive = archive;

        if (baseStream.CanRead)
        {
            UR = new UnrealBinaryReader(archive, new BinaryReader(baseStream));
        }

        if (baseStream.CanWrite)
        {
            UW = new UnrealBinaryWriter(archive, new BinaryWriter(baseStream));
        }
    }

    public UnrealBinaryReader UR { get; }
    public UnrealBinaryWriter UW { get; }

    [Obsolete("", true)]
    public UnrealPackage Package => null;
    public IBufferDecoder Decoder { get; set; }
    public IPackageSerializer Serializer { get; set; }
    public long AbsolutePosition { get; set; }

    // Could read from JSON or from a path string and map it back by path?
    public T? ReadObject<T>() where T : UObject => throw new NotImplementedException();
    public void WriteObject<T>(T? value) where T : UObject => throw new NotImplementedException();

    public UName ReadName() => new(UR.ReadString());
    public void WriteName(in UName value) => UW.WriteString(value.ToString());

    public void Skip(int bytes) => throw new NotImplementedException();

    public IUnrealStream Record(string name, object value) => throw new NotImplementedException();
    public void ConformRecordPosition() => throw new NotImplementedException();

    public uint Version => _Archive.Version;
    public uint LicenseeVersion => _Archive.LicenseeVersion;
    public uint UE4Version => _Archive.UE4Version;

    public UnrealPackage.GameBuild Build => _Archive.Build;
    public bool BigEndianCode => _Archive.Flags.HasFlag(UnrealArchiveFlags.BigEndian);
    public UnrealArchiveFlags Flags => _Archive.Flags;
}
