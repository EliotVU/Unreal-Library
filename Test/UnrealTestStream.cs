using UELib;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;
using UELib.IO;

namespace Eliot.UELib.Test;

public sealed class UnrealTestArchive(
    UnrealPackage package,
    uint version,
    UnrealArchiveFlags flags = 0,
    uint licenseeVersion = 0,
    uint ue4Version = 0)
    : IUnrealArchive
{
    public UnrealPackage Package { get; } = package;
    public uint Version { get; } = version;
    public uint LicenseeVersion { get; } = licenseeVersion;
    public uint UE4Version { get; } = ue4Version;
    public UnrealPackage.GameBuild Build { get; } = package.Build ?? new UnrealPackage.GameBuild(package);
    public bool BigEndianCode => Flags.HasFlag(UnrealArchiveFlags.BigEndian);
    public UnrealArchiveFlags Flags { get; } = flags;
}

/// Hackish workaround for the issue with UPackageStream requiring a file and path, so that we can perform stream tests without a package.
public sealed class UnrealTestStream(UnrealTestArchive baseArchive, Stream baseStream)
    : PipedStream(baseStream), IUnrealStream
{
    public uint Version => baseArchive.Version;
    public uint LicenseeVersion => baseArchive.LicenseeVersion;
    public uint UE4Version => baseArchive.UE4Version;
    public UnrealPackage.GameBuild Build => baseArchive.Build;
    public bool BigEndianCode => Flags.HasFlag(UnrealArchiveFlags.BigEndian);
    public UnrealArchiveFlags Flags => baseArchive.Flags;

    public UnrealPackage Package => baseArchive.Package;
    public UnrealBinaryReader UR { get; } = new(baseArchive, new BinaryReader(baseStream));
    public UnrealBinaryWriter UW { get; } = new(baseArchive, new BinaryWriter(baseStream));

    [Obsolete] public IBufferDecoder? Decoder { get; set; }

    public IPackageSerializer Serializer { get; set; }

    public UName ReadName() => UR.ReadName();
    public void WriteName(in UName value) => UW.WriteName(in value);

    public void Skip(int bytes) => Position += bytes;

    public long AbsolutePosition
    {
        get => Position;
        set => Position = value;
    }

    public T ReadObject<T>() where T : UObject? => baseArchive.Package.Linker.IndexToObject<T>(UR.ReadIndex());
    public void WriteObject<T>(T value) where T : UObject? => UW.WriteIndex(value?.PackageIndex ?? 0);

    public IUnrealStream Record(string name, object? value) => this;

    public void ConformRecordPosition()
    {
    }
}
