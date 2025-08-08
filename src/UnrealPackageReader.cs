using System.IO;
using UELib.Branch;
using UELib.Core;
using UELib.IO;

namespace UELib;

/// <summary>
///     Wrapper for Streams with specific functions for deserializing UELib.UnrealPackage.
/// </summary>
public sealed class UnrealPackageReader(UnrealPackageArchive archive, BinaryReader baseReader)
    : UnrealBinaryReader(archive, baseReader)
{
    /// <summary>
    ///     Deserializes a <seealso cref="UName" /> from an index and number.
    /// </summary>
    /// <returns>A unique UName representing the number and index to a <seealso cref="UNameTableItem" /></returns>
    public override UName ReadName()
    {
        UName name;
        int index, number;
#if R6 || LEAD
        if (archive.Build == UnrealPackage.GameBuild.BuildName.R6Vegas ||
            archive.Build == BuildGeneration.Lead)
        {
            // Some changes were made with licensee version 71, but I couldn't make much sense of it.
            index = _BaseReader.ReadInt32();
            number = index >> 18;
            index &= 0x3FFFF; // only the 18 lower bits are used.

            name = new UName(archive.Package.Names[index], number);
            archive.NameIndices[name.Index] = index; // for re-writing purposes.

            return name;
        }
#endif
        index = ReadIndex();
        number = 0;

#if SHADOWSTRIKE
        if (archive.Build == BuildGeneration.ShadowStrike)
        {
            int externalIndex = ReadIndex();
        }
#endif
        if (archive.Version >= (uint)PackageObjectLegacyVersion.NumberAddedToName
#if BIOSHOCK
            || archive.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
           )
        {
            number = _BaseReader.ReadInt32();
        }

        name = new UName(archive.Package.Names[index], number);
        archive.NameIndices[name.Index] = index; // for re-writing purposes.

        return name;
    }
}
