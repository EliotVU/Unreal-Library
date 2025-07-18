using System;
using System.Runtime.CompilerServices;

namespace UELib.IO;

/// <summary>
///     An archive of the bare minimum data that is required to read and write various Unreal package formats using an
///     <see cref="IUnrealStream" />
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

    [Obsolete("Use IsBigEndianness instead")]
    bool BigEndianCode { get; }

    UnrealArchiveFlags Flags { get; }
}

public static class UnrealArchiveExtensions
{
    /// <summary>
    ///     Checks if the archive is serialized using big-endian byte order.
    /// </summary>
    /// <param name="archive">The archive.</param>
    /// <returns>True if the archive package is serialized using big-endian byte-order./returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBigEndianness(this IUnrealArchive archive)
    {
        return archive.Flags.HasFlag(UnrealArchiveFlags.BigEndian);
    }

    /// <summary>
    ///     Checks if the archive has encoded (obfuscated or "encrypted") stream data.
    /// </summary>
    /// <param name="archive">The archive.</param>
    /// <returns>True if the archive package has encoded stream data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEncoded(this IUnrealArchive archive)
    {
        return archive.Flags.HasFlag(UnrealArchiveFlags.Encoded);
    }

    /// <summary>
    ///     Checks if the archive is a UE4 or UE5 package.
    /// </summary>
    /// <param name="archive">The archive.</param>
    /// <returns>True if the archive is a UE4 or UE5 package.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUE4(this IUnrealArchive archive)
    {
        return archive.UE4Version > 0;
    }

    /// <summary>
    ///     Checks if the archive is a legacy package, which is defined as having a UE4Version of 0.
    /// </summary>
    /// <param name="archive">The archive.</param>
    /// <returns>True if the archive is a UE1, UE2 or UE3 package.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLegacy(this IUnrealArchive archive)
    {
        return archive.UE4Version == 0;
    }
}