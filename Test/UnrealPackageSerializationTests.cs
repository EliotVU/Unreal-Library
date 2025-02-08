using Eliot.UELib.Test.upk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Branch;

namespace Eliot.UELib.Test
{
    [TestClass]
    public class UnrealPackageSerializationTests
    {
        [DataTestMethod]
        [DataRow(PackageObjectLegacyVersion.Release64 - 1)]
        [DataRow(PackageObjectLegacyVersion.Release64)]
        [DataRow(PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)]
        public void TestPackageNamesSerialization(PackageObjectLegacyVersion version)
        {
            var scriptPackage = UE3PackageContentTests.GetScriptPackageLinker();
            Assert.IsNotNull(scriptPackage);

            using var stream = UnrealPackageUtilities.CreateTempPackageStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = (uint)version
            };

            // write the script packages content to the temp stream

            linker.Names.AddRange(scriptPackage.Names);

            stream.Position = 0;
            scriptPackage.Names.ForEach(name => name.Serialize(stream));
            long endPosition = stream.Position;
            stream.Position = 0;
            scriptPackage.Names.ForEach(name => name.Deserialize(stream));
            Assert.AreEqual(endPosition, stream.Position);
        }

        [DataTestMethod]
        // No direct changes, only the serialization of UName and UIndex have changed but this is not the test for that.
        [DataRow(PackageObjectLegacyVersion.UE3)]
        public void TestPackageImportsSerialization(PackageObjectLegacyVersion version)
        {
            var scriptPackage = UE3PackageContentTests.GetScriptPackageLinker();
            Assert.IsNotNull(scriptPackage);

            using var stream = UnrealPackageUtilities.CreateTempPackageStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = (uint)version
            };

            // write the script packages content to the temp stream

            linker.Names.AddRange(scriptPackage.Names);
            linker.Imports.AddRange(scriptPackage.Imports);

            stream.Position = 0;
            scriptPackage.Imports.ForEach(imp => imp.Serialize(stream));
            long endPosition = stream.Position;
            stream.Position = 0;
            scriptPackage.Imports.ForEach(imp => imp.Deserialize(stream));
            Assert.AreEqual(endPosition, stream.Position);
        }
        
        [DataTestMethod]
        //[DataRow(PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits - 1)]
        [DataRow(PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)]
        [DataRow(PackageObjectLegacyVersion.ArchetypeAddedToExports)]
        [DataRow(PackageObjectLegacyVersion.ExportFlagsAddedToExports)]
        [DataRow(PackageObjectLegacyVersion.SerialSizeConditionRemoved)]
        [DataRow(PackageObjectLegacyVersion.NetObjectCountAdded)]
        [DataRow(PackageObjectLegacyVersion.PackageFlagsAddedToExports)]
        [DataRow(PackageObjectLegacyVersion.ComponentMapDeprecated)]
        public void TestPackageExportsSerialization(PackageObjectLegacyVersion version)
        {
            var scriptPackage = UE3PackageContentTests.GetScriptPackageLinker();
            Assert.IsNotNull(scriptPackage);

            using var stream = UnrealPackageUtilities.CreateTempPackageStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = (uint)version
            };
            
            // write the script packages content to the temp stream

            linker.Names.AddRange(scriptPackage.Names);
            linker.Exports.AddRange(scriptPackage.Exports);

            stream.Position = 0;
            scriptPackage.Exports.ForEach(exp => exp.Serialize(stream));
            long endPosition = stream.Position;
            stream.Position = 0;
            scriptPackage.Exports.ForEach(exp => exp.Deserialize(stream));
            Assert.AreEqual(endPosition, stream.Position);
        }
    }
}
