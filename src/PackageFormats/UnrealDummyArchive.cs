using System;
using UELib.Branch;
using UELib.IO;

namespace UELib.PackageFormats;

/// <summary>
///     A dummy archive that does not contain any data.
///     This class serves as a placeholder for when no <see cref="UnrealPackage"/> is available, but an <see cref="IUnrealStream" /> is required for serialization.
/// </summary>
internal sealed class UnrealDummyArchive : IUnrealArchive
{
    [Obsolete("Not of use", true)]

    public UnrealPackage Package => null;
    public uint Version { get; }
    public uint LicenseeVersion { get; }
    public uint UE4Version { get; }

    public UnrealPackage.GameBuild Build { get; } =
        new(0, 0, BuildGeneration.Undefined, typeof(DefaultEngineBranch), 0);

    public bool BigEndianCode => Flags.HasFlag(UnrealArchiveFlags.BigEndian);
    public UnrealArchiveFlags Flags { get; }
}
