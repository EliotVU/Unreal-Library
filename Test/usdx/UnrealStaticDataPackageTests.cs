using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Branch.UE3.R6.PackageFormat;

namespace Eliot.UELib.Test.usdx
{
    [TestClass]
    public class UnrealStaticDataPackageTests
    {
        [DataTestMethod]
        [DataRow(@"KellerGame\CookedPC\weapons.usdx")]
        [DataRow(@"KellerGame\CookedPC\pec.usdx")]
        public void TestPackageFile(string fileName)
        {
            using var fileStream = File.Open(
                Path.Combine(Packages.R6Vegas2Path, fileName),
                FileMode.Open, FileAccess.Read
            );

            var package = new UnrealStaticDataPackage(Path.GetFileNameWithoutExtension(fileStream.Name));
            var loader = new UnrealStaticDataPackageLoader(package);

            using var stream = loader.Load(fileStream);
        }
    }
}
