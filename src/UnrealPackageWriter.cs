using System;
using System.IO;
using UELib.Branch;
using UELib.Core;
using UELib.IO;
using UELib.Services;

namespace UELib;

public sealed class UnrealPackageWriter(UnrealPackageArchive archive, BinaryWriter baseWriter)
    : UnrealBinaryWriter(archive, baseWriter)
{
    /// <summary>
    ///     Serializes a <see cref="UName" /> with an index and number.
    /// </summary>
    public override void WriteName(in UName value)
    {
        int hash = value.Index;
        int index = archive.NameIndices[hash];
        int number = value.Number + 1;
#if R6
        if (archive.Build == UnrealPackage.GameBuild.BuildName.R6Vegas)
        {
            _BaseWriter.Write(index | (number << 18));

            return;
        }
#endif
        WriteIndex(index);

#if SHADOWSTRIKE
        if (archive.Build == BuildGeneration.ShadowStrike)
        {
            LibServices.LogService.SilentException(
                new NotSupportedException("Writing external index 0 for ShadowStrike."));
            WriteIndex(0);
        }
#endif
        if (archive.Version >= (uint)PackageObjectLegacyVersion.NumberAddedToName
#if BIOSHOCK
            || archive.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
           )
        {
            _BaseWriter.Write(number);
        }
    }
}