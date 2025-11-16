using UELib;
using UELib.Branch;
using UELib.Core;

namespace Eliot.UELib.Test
{
    public static class UnrealPackageUtilities
    {
        public static FileStream CreateTempPackageStream()
        {
            string tempFilePath = Path.Join(Path.GetTempFileName());
            var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite);

            return fileStream;
        }

        public static UnrealPackage CreateTempPackage(
            PackageObjectLegacyVersion version,
            ushort licenseeVersion = 0,
            UnrealPackageEnvironment? packageEnvironment = null)
            => CreateTempPackage((uint)version, licenseeVersion, packageEnvironment);

        public static UnrealPackage CreateTempPackage(
            uint version,
            ushort licenseeVersion = 0,
            UnrealPackageEnvironment? packageEnvironment = null)
        {
            packageEnvironment ??= new UnrealPackageEnvironment("Temp", RegisterUnrealClassesStrategy.None);

            var fileStream = CreateTempPackageStream();
            var package = new UnrealPackage(fileStream, fileStream.Name, packageEnvironment);
            package.Build = new UnrealPackage.GameBuild(package);
            package.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = version,
                LicenseeVersion = licenseeVersion
            };

            return package;
        }

        public static UnrealPackage CreateMemoryPackage(
            PackageObjectLegacyVersion version,
            ushort licenseeVersion = 0,
            UnrealPackageEnvironment? packageEnvironment = null)
            => CreateMemoryPackage((uint)version, licenseeVersion, packageEnvironment);

        public static UnrealPackage CreateMemoryPackage(
            uint version,
            ushort licenseeVersion = 0,
            UnrealPackageEnvironment? packageEnvironment = null)
        {
            packageEnvironment ??= new UnrealPackageEnvironment("Temp", RegisterUnrealClassesStrategy.None);

            var memoryStream = new MemoryStream();
            var package = new UnrealPackage(memoryStream, "Transient", packageEnvironment);
            package.Build = new UnrealPackage.GameBuild(package);
            package.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = version,
                LicenseeVersion = licenseeVersion
            };

            return package;
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

        internal static UObject AssertDefaultPropertiesClass(UnrealPackageLinker packageLinker)
        {
            var testClass = packageLinker.FindObject<UClass>("DefaultProperties");
            Assert.IsNotNull(testClass);

            var defaults = testClass.Default ?? testClass;
            packageLinker.LoadObject(defaults);

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
            var textures = objects
                .OfType<T>()
                .ToList();
            Assert.AreNotEqual(0, textures.Count);
            textures.ForEach(AssertObjectDeserialization);
        }

        internal static void AssertExports(IEnumerable<UObject> objects)
        {
            var compatibleExports = objects
                .Where(exp => exp is not UnknownObject)
                .ToList();
            Assert.AreNotEqual(0, compatibleExports.Count);
            compatibleExports.ForEach(AssertObjectDeserialization);
        }

        internal static void AssertObjectDeserialization(UObject obj)
        {
            if (obj.DeserializationState == 0)
            {
                obj.Load();
            }

            Assert.AreEqual(UObject.ObjectState.Deserialized, obj.DeserializationState, obj.GetReferencePath());
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
