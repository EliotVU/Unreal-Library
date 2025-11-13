using System;
using System.Collections.Generic;
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

    /// <summary>
    /// IndexName(hash) -> UnrealPackage.Names[index]
    /// </summary>
    public Dictionary<int, int> NameIndices { get; } = new(1000);

    public UnrealPackageArchive(UnrealPackage package, UnrealPackageEnvironment environment)
    {
        Package = package;
        Environment = environment;
    }

    public UnrealPackageArchive(Stream stream, string fileName)
    {
        Stream = new UnrealPackageStream(this, stream);
        Environment = new UnrealPackageEnvironment(fileName, [Path.GetDirectoryName(fileName)]);
        Package = new UnrealPackage(this, fileName);

        if (UnrealFile.GetSignature(stream) == UnrealFile.BigEndianSignature)
        {
            Flags |= UnrealArchiveFlags.BigEndian;
        }
    }

    public UnrealPackageArchive(Stream stream, string fileName, UnrealPackageEnvironment environment)
    {
        Stream = new UnrealPackageStream(this, stream);
        Environment = environment;
        Package = new UnrealPackage(this, fileName);

        if (UnrealFile.GetSignature(stream) == UnrealFile.BigEndianSignature)
        {
            Flags |= UnrealArchiveFlags.BigEndian;
        }
    }

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
    public UnrealPackageEnvironment Environment { get; }

    public uint Version => Package.Summary.Version;
    public uint LicenseeVersion => Package.Summary.LicenseeVersion;
    public uint UE4Version => Package.Summary.UE4Version;
    public UnrealPackage.GameBuild Build => Package.Build;

    [Obsolete]
    public bool BigEndianCode => Flags.HasFlag(UnrealArchiveFlags.BigEndian);
    public UnrealArchiveFlags Flags { get; set; }

    public void Dispose()
    {
        Stream?.Dispose();

        Package.Dispose();
    }
}
