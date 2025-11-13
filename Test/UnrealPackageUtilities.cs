using UELib;
using UELib.Branch;
using UELib.Core;

namespace Eliot.UELib.Test
{
    public static class UnrealPackageUtilities
    {
        internal static UnrealPackageEnvironment s_environment = new("Transient", []);
        
        public static FileStream CreateTempPackageStream()
        {
            string tempFilePath = Path.Join(Path.GetTempFileName());
            var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite);

            return fileStream;
        }

        public static UnrealPackageArchive CreateTempArchive(PackageObjectLegacyVersion version,
            ushort licenseeVersion = 0) => CreateTempArchive((uint)version, licenseeVersion);
        public static UnrealPackageArchive CreateTempArchive(uint version, ushort licenseeVersion = 0)
        {
            var fileStream = CreateTempPackageStream();

            var archive = new UnrealPackageArchive(fileStream, fileStream.Name);
            var package = archive.Package;
            package.Build = new UnrealPackage.GameBuild(package);
            package.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = version,
                LicenseeVersion = licenseeVersion
            };

            return archive;
        }
        
        public static UnrealPackageArchive CreateMemoryArchive(PackageObjectLegacyVersion version,
            ushort licenseeVersion = 0) => CreateMemoryArchive((uint)version, licenseeVersion);
        public static UnrealPackageArchive CreateMemoryArchive(uint version, ushort licenseeVersion = 0)
        {
            var memoryStream = new MemoryStream();
            var archive = new UnrealPackageArchive(memoryStream, "Transient");
            var package = archive.Package;
            package.Build = new UnrealPackage.GameBuild(package);
            package.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = version,
                LicenseeVersion = licenseeVersion
            };

            return archive;
        }

        internal static void AssertScriptDecompile(UStruct scriptInstance)
        {
            try
            {
                var decompiler = new UStruct.UByteCodeDecompiler(scriptInstance);
                decompiler.Decompile();
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    $"Token decompilation exception in script instance {scriptInstance.GetReferencePath()}: {ex.Message}");
            }

            foreach (var subScriptInstance in scriptInstance
                         .EnumerateFields()
                         .OfType<UStruct>())
            {
                try
                {

                    // ... for states
                    AssertScriptDecompile(subScriptInstance);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Token decompilation exception in script instance {subScriptInstance.GetReferencePath()}: {ex.Message}");
                }
            }
        }

        internal static UObject AssertDefaultPropertiesClass(UnrealPackage linker)
        {
            var testClass = linker.FindObject<UClass>("DefaultProperties");
            Assert.IsNotNull(testClass);

            var defaults = testClass.Default ?? testClass;
            defaults.Load();

            return defaults;
        }

        internal static void AssertPropertyTagFormat(UObject obj, string tagName, string expectedFormat)
        {
            Assert.IsNotNull(obj.Properties);
            var tag = obj.Properties.Find(tagName);
            Assert.IsNotNull(tag, $"Couldn't find property tag of '{tagName}'");
            string colorValue = tag.DeserializeValue();
            Assert.AreEqual(expectedFormat, colorValue, $"tag '{tagName}'");
        }

        internal static void AssertExportsOfType<T>(IEnumerable<UObject> objects)
            where T : UObject
        {
            var textures = objects.OfType<T>()
                .ToList();
            Assert.IsTrue(textures.Any());
            textures.ForEach(AssertObjectDeserialization);
        }

        internal static void AssertExports(IEnumerable<UObject> objects)
        {
            var compatibleExports = objects.Where(exp => exp is not UnknownObject)
                .ToList();
            Assert.IsTrue(compatibleExports.Any());
            compatibleExports.ForEach(AssertObjectDeserialization);
        }

        internal static void AssertObjectDeserialization(UObject obj)
        {
            if (obj.DeserializationState == 0)
            {
                obj.Load();
            }

            Assert.IsTrue(obj.DeserializationState == UObject.ObjectState.Deserialized, obj.GetReferencePath());
        }

        internal static void AssertTokenType<T>(UStruct.UByteCodeDecompiler.Token token)
            where T : UStruct.UByteCodeDecompiler.Token
        {
            Assert.AreEqual(typeof(T), token.GetType());
        }

        internal static void AssertTokenType(UStruct.UByteCodeDecompiler.Token token, Type tokenType)
        {
            Assert.AreEqual(tokenType, token.GetType());
        }

        internal static void AssertTokens(UStruct.UByteCodeDecompiler decompiler, params Type[] tokenTypesSequence)
        {
            foreach (var tokenType in tokenTypesSequence)
            {
                AssertTokenType(decompiler.NextToken, tokenType);
            }
        }
    }
}
