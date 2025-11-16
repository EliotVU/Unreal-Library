using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using UELib.Decoding;
using UELib.IO;

namespace UELib;

internal interface IUnrealPackageArchive : IUnrealArchive
{
    UnrealPackage Package { get; }
}

/// <summary>
/// An archive for <see cref="UnrealPackage"/> to manage and dispose of a <see cref="UnrealPackageStream"/>
///
/// It is also used to expose package versioning to its stream, as well as track the encoding and serialization state.
/// </summary>
public sealed class UnrealPackageArchive : IUnrealPackageArchive, IDisposable
{
    private IBufferDecoder? _Decoder;

    /// <summary>
    /// IndexName(hash) -> UnrealPackage.Names[index]
    /// </summary>
    public Dictionary<int, int> NameIndices { get; } = new(1000);

    public UnrealPackageArchive(UnrealPackage package, Stream? baseStream = null)
    {
        Package = package;

        if (baseStream == null)
        {
            return;
        }

        if (UnrealFile.GetSignature(baseStream) == UnrealFile.BigEndianSignature)
        {
            // Move this to the setter of 'Stream'?
            //Contract.Assert((Flags & UnrealArchiveFlags.BigEndian) == 0, "Archive is already marked with 'BigEndian'");
            Flags |= UnrealArchiveFlags.BigEndian;
        }

        Stream = new UnrealPackageStream(this, baseStream);
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

    /// <summary>
    /// May be null briefly, or if the <see cref="Package"/> is a transient package.
    /// </summary>
    public UnrealPackageStream Stream
    {
        get;
        set;
    }

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
        Stream?.Dispose();
    }
}
