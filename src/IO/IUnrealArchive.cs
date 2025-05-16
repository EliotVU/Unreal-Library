using System;

namespace UELib.IO;

/// <summary>
///     An archive of the bare minimum data that is required to read and write various Unreal package formats using an <see cref="IUnrealStream"/>
///     Not to be confused with the actual definition of an archive of files.
/// </summary>
public interface IUnrealArchive
{
    [Obsolete("Deprecated, use the UnrealPackageArchive class instead", true)]
    UnrealPackage Package { get; }

    uint Version { get; }
    uint LicenseeVersion { get; }
    uint UE4Version { get; }
    UnrealPackage.GameBuild Build { get; }

    bool BigEndianCode { get; }
    UnrealArchiveFlags Flags { get; }
}
