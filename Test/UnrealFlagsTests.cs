using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Branch;
using UELib.Core;
using UELib.Flags;

namespace Eliot.UELib.Test
{
    /// <summary>
    /// Test if the mechanics of <see cref="UnrealFlags{TEnum}"/> are working properly.
    ///
    /// We do not test if the mappings are correct for UE1-3 here, mere a verification of the mapping approach.
    /// </summary>
    [TestClass]
    public class UnrealFlagsTests
    {
        [TestMethod]
        public void TestUnrealPackageFlags()
        {
            const ulong serializedFlags =
                (ulong)(DefaultEngineBranch.PackageFlagsDefault.AllowDownload |
                        DefaultEngineBranch.PackageFlagsDefault.ServerSideOnly);
            
            var flagsMap = new ulong[(int)PackageFlags.Max];
            flagsMap[(int)PackageFlags.AllowDownload] = (ulong)DefaultEngineBranch.PackageFlagsDefault.AllowDownload;
            flagsMap[(int)PackageFlags.ServerSideOnly] = (ulong)DefaultEngineBranch.PackageFlagsDefault.ServerSideOnly;
            var flags = new UnrealFlags<PackageFlags>(serializedFlags, ref flagsMap);

            // Verify mapped flags
            Assert.IsTrue(flags.HasFlag(PackageFlags.AllowDownload));
            Assert.IsTrue(flags.HasFlag(PackageFlags.ServerSideOnly));
            Assert.IsFalse(flags.HasFlag(PackageFlags.ClientOptional));
            
            // Verify actual flags
            Assert.IsTrue(flags.HasFlags((uint)DefaultEngineBranch.PackageFlagsDefault.AllowDownload));
            Assert.IsFalse(flags.HasFlags((uint)DefaultEngineBranch.PackageFlagsDefault.ClientOptional));
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
}
