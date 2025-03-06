using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Branch;
using UELib.Core;
using UELib.Flags;

namespace Eliot.UELib.Test;

/// <summary>
///     Test if the mechanics of <see cref="UnrealFlags{TEnum}" /> are working properly.
///     We do not test if the mappings are correct for UE1-3 here, mere a verification of the mapping approach.
/// </summary>
[TestClass]
public class UnrealFlagsTests
{
    [TestMethod]
    public void TestHasFlag()
    {
        const ulong serializedFlags =
            (ulong)(DefaultEngineBranch.PackageFlagsDefault.AllowDownload |
                    DefaultEngineBranch.PackageFlagsDefault.ServerSideOnly);

        var flagsMap = new ulong[(int)PackageFlag.Max];
        flagsMap[(int)PackageFlag.AllowDownload] = (ulong)DefaultEngineBranch.PackageFlagsDefault.AllowDownload;
        flagsMap[(int)PackageFlag.ServerSideOnly] = (ulong)DefaultEngineBranch.PackageFlagsDefault.ServerSideOnly;
        var flags = new UnrealFlags<PackageFlag>(serializedFlags, flagsMap);

        // Verify mapped flags
        Assert.IsTrue(flags.HasFlag(PackageFlag.AllowDownload));
        Assert.IsTrue(flags.HasFlag(PackageFlag.ServerSideOnly));
        Assert.IsFalse(flags.HasFlag(PackageFlag.ClientOptional));

        // Verify actual flags
        Assert.IsTrue(flags.HasFlags((uint)DefaultEngineBranch.PackageFlagsDefault.AllowDownload));
        Assert.IsFalse(flags.HasFlags((uint)DefaultEngineBranch.PackageFlagsDefault.ClientOptional));

        Assert.AreEqual(
            flags.GetFlag(PackageFlag.AllowDownload), 
            flagsMap[(int)PackageFlag.AllowDownload]
        );
        Assert.AreEqual(
            flags.GetFlags(PackageFlag.AllowDownload, PackageFlag.ServerSideOnly),
            flagsMap[(int)PackageFlag.AllowDownload] | flagsMap[(int)PackageFlag.ServerSideOnly]
        );

        Assert.AreEqual(serializedFlags, (ulong)flags);
    }

    [TestMethod]
    public void TestEnumerateFlags()
    {
        var flagsMap = new ulong[(int)PackageFlag.Max];
        flagsMap[(int)PackageFlag.AllowDownload] = 0x1;
        flagsMap[(int)PackageFlag.ClientOptional] = 0x2;
        flagsMap[(int)PackageFlag.ServerSideOnly] = 0x4;

        // AllowDownload and ClientOptional
        var flags = new UnrealFlags<PackageFlag>(0x3, flagsMap);
        var enumeratedFlags = flags.EnumerateFlags();
        CollectionAssert.AreEqual(new[] { 0, 1 }, enumeratedFlags.ToArray());
    }

    [TestMethod]
    public void TestBulkDataToCompressionFlags()
    {
        const BulkDataFlags dataFlags = BulkDataFlags.Unused | BulkDataFlags.CompressedLZX;

        var compressionFlags = dataFlags.ToCompressionFlags();
        Assert.IsTrue(compressionFlags.HasFlag(CompressionFlags.ZLX));
        Assert.IsTrue(compressionFlags == CompressionFlags.ZLX);
    }
}
