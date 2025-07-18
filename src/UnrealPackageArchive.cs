using System;
using System.IO;
using UELib.Decoding;
using UELib.IO;

namespace UELib;

internal interface IUnrealPackageArchive : IUnrealArchive
{
    UnrealPackage Package { get; }
}

public sealed class UnrealPackageArchive : IUnrealPackageArchive, IDisposable
{
    private IBufferDecoder? _Decoder;

    public UnrealPackageArchive(UnrealPackage package) => Package = package;

    public UnrealPackageArchive(FileStream stream)
    {
        Package = new UnrealPackage(this, stream.Name);
        Stream = new UnrealPackageStream(this, stream);
    }

    public UnrealPackageArchive(Stream stream, string fileName)
    {
        Package = new UnrealPackage(this, fileName);
        Stream = new UnrealPackageStream(this, stream);
    }

    [Obsolete]
    public IBufferDecoder? Decoder
    {
        get => _Decoder;
        set
        {
            _Decoder = value;

            if (value != null)
            {
                Flags |= UnrealArchiveFlags.Encoded;
            }
            else
            {
                Flags &= ~UnrealArchiveFlags.Encoded;
            }
        }
    }

    public UnrealPackageStream Stream { get; set; }

    public UnrealPackage Package { get; }

    public uint Version => Package.Summary.Version;
    public uint LicenseeVersion => Package.Summary.LicenseeVersion;
    public uint UE4Version => Package.Summary.UE4Version;
    public UnrealPackage.GameBuild Build => Package.Build;

    [Obsolete]
    public bool BigEndianCode => Flags.HasFlag(UnrealArchiveFlags.BigEndian);
    public UnrealArchiveFlags Flags { get; set; }

    public void Dispose()
    {
        Package.Dispose(); // Will also depose the stream.
    }
}
