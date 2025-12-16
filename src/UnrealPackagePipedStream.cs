using System;
using System.IO;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;
using UELib.IO;

namespace UELib;

public abstract class UnrealPackagePipedStream : PipedStream, IUnrealStream
{
    protected readonly UnrealPackageArchive BaseArchive;

    protected UnrealPackagePipedStream(UnrealPackageArchive baseArchive, Stream baseStream) : base(baseStream)
    {
        BaseArchive = baseArchive;

        if (baseStream.CanRead)
        {
            var binaryReader = CreateBinaryReader(baseStream);
            Reader = new UnrealPackageReader(baseArchive, binaryReader);
        }

        if (baseStream.CanWrite)
        {
            var binaryWriter = CreateBinaryWriter(baseStream);
            Writer = new UnrealPackageWriter(baseArchive, binaryWriter);
        }
    }

    protected UnrealPackageReader? Reader { get; set; }
    protected UnrealPackageWriter? Writer { get; set; }

    [Obsolete] public IBufferDecoder? Decoder { get; set; }

    public IPackageSerializer Serializer { get; set; }
    UnrealBinaryReader IUnrealStream.UR => Reader;
    UnrealBinaryWriter IUnrealStream.UW => Writer;

    public UnrealPackage Package => BaseArchive.Package;
    public uint Version => BaseArchive.Version;
    public uint LicenseeVersion => BaseArchive.LicenseeVersion;
    public uint UE4Version => BaseArchive.UE4Version;
    public UnrealPackage.GameBuild Build => BaseArchive.Build;

    [Obsolete]
    public bool BigEndianCode => Flags.HasFlag(UnrealArchiveFlags.BigEndian);
    public UnrealArchiveFlags Flags => BaseArchive.Flags;

    public virtual long AbsolutePosition
    {
        get => Position;
        set => Position = value;
    }

    // We need this implementation to keep the interface extensions working.
    public void Skip(int bytes) => Position += bytes;

    public virtual IUnrealStream Record(string name, object value) => this;

    public virtual void ConformRecordPosition()
    {
    }

    public T ReadObject<T>() where T : UObject?
    {
        int index = Reader.ReadIndex();
        return Package.Linker.IndexToObject<T>(index);
    }

    public void WriteObject<T>(T? value) where T : UObject => Writer.WriteIndex((int)value);

    public UName ReadName() => Reader.ReadName();

    public void WriteName(in UName value) => Writer.WriteName(value);

    protected BinaryReader CreateBinaryReader(Stream stream) =>
        (Flags & UnrealArchiveFlags.BigEndian) == 0
            ? new BinaryReader(stream)
            : new BigEndianBinaryReader(stream);

    protected BinaryWriter CreateBinaryWriter(Stream stream) =>
        (Flags & UnrealArchiveFlags.BigEndian) == 0
            ? new BinaryWriter(stream)
            : new BigEndianBinaryWriter(stream);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        Reader?.Dispose();
        Writer?.Dispose();
    }
}
