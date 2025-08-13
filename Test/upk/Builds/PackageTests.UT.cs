using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;
using UELib.Engine;
using static Eliot.UELib.Test.UnrealPackageTests;

namespace Eliot.UELib.Test.Builds
{
    /// <summary>
    /// Use the UT99 build to test compatibility with UE1.
    /// </summary>
    [TestClass]
    public class PackageTestsUT
    {
        public static UnrealPackage GetScriptPackageLinker()
        {
            string packagePath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "UPK",
                "TestUC1", "TestUC1.u");
            var linker = UnrealLoader.LoadPackage(packagePath);
            Assert.IsNotNull(linker);
            return linker;
        }

        public static UnrealPackage GetMusicPackageLinker(string fileName)
        {
            string packagePath = Path.Join(Packages.UT99Path, "Music", fileName);
            if (!File.Exists(packagePath))
            {
                Assert.Inconclusive($"Couldn't find package '{packagePath}'");
            }
            
            var linker = UnrealLoader.LoadPackage(packagePath);
            Assert.IsNotNull(linker);
            return linker;
        }

        public static UnrealPackage GetMapPackageLinker(string fileName)
        {
            string packagePath = Path.Join(Packages.UT99Path, "Maps", fileName);
            if (!File.Exists(packagePath))
            {
                Assert.Inconclusive($"Couldn't find package '{packagePath}'");
            }
            
            var linker = UnrealLoader.LoadPackage(packagePath);
            Assert.IsNotNull(linker);
            return linker;
        }

        public static UnrealPackage GetMaterialPackageLinker(string fileName)
        {
            string packagePath = Path.Join(Packages.UT99Path, "Textures", fileName);
            if (!File.Exists(packagePath))
            {
                Assert.Inconclusive($"Couldn't find package '{packagePath}'");
            }
            
            var linker = UnrealLoader.LoadPackage(packagePath);
            Assert.IsNotNull(linker);
            return linker;
        }

        [TestMethod]
        public void TestScriptContent()
        {
            void AssertDefaults(UnrealPackage unrealPackage)
            {
                var defaults = AssertDefaultPropertiesClass(unrealPackage);
                AssertPropertyTagFormat(defaults, "BoolTrue",
                    "true");
                AssertPropertyTagFormat(defaults, "BoolFalse",
                    "false");
                AssertPropertyTagFormat(defaults, "Byte",
                    "255");
                AssertPropertyTagFormat(defaults, "Int",
                    "1000");
                AssertPropertyTagFormat(defaults, "Float",
                    "0.0123457");
                AssertPropertyTagFormat(defaults, "NameProperty",
                    "\"Name\"");
                AssertPropertyTagFormat(defaults, "String",
                    """
                    "String_\""
                    """);
                AssertPropertyTagFormat(defaults, "Vector",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000)");
                AssertPropertyTagFormat(defaults, "Plane",
                    "(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)");
                AssertPropertyTagFormat(defaults, "Rotator",
                    "(Pitch=180,Yaw=90,Roll=45)");
                AssertPropertyTagFormat(defaults, "Coords",
                    "(Origin=(X=0.2000000,Y=0.4000000,Z=1.0000000)," +
                    "XAxis=(X=1.0000000,Y=0.0000000,Z=0.0000000)," +
                    "YAxis=(X=0.0000000,Y=1.0000000,Z=0.0000000)," +
                    "ZAxis=(X=0.0000000,Y=0.0000000,Z=1.0000000))");
                AssertPropertyTagFormat(defaults, "Scale",
                    "(Scale=(X=1.0000000,Y=2.0000000,Z=3.0000000),SheerRate=5.0000000,SheerAxis=ZY)");
                AssertPropertyTagFormat(defaults, "Color",
                    "(R=80,G=40,B=20,A=160)");
            }

            using var linker = GetScriptPackageLinker();
            Assert.IsNotNull(linker);
            linker.InitializePackage();

            var exports = linker.Objects
                .Where(obj => (int)obj > 0)
                .ToList();

            var tokensClass = linker.FindObject<UClass>("ExprTokens");
            Assert.IsNotNull(tokensClass);

            // Test a series of expected tokens
            AssertScriptDecompile(tokensClass);
            AssertDefaults(linker);
            AssertExportsOfType<UClass>(exports);
        }

        [TestMethod]
        public void TestMusicContent()
        {
            using var linker = GetMusicPackageLinker("Foregone.umx");
            linker.InitializePackage();

            var exports = linker.Objects
                .Where(obj => (int)obj > 0)
                .ToList();

            AssertExportsOfType<UMusic>(exports);
        }

        [TestMethod]
        public void TestMapContent()
        {
            using var linker = GetMapPackageLinker("DM-Phobos.unr");
            linker.InitializePackage();

            var exports = linker.Objects
                .Where(obj => (int)obj > 0)
                .ToList();

            AssertExportsOfType<UPolys>(exports);
        }

        [TestMethod]
        public void TestMaterialContent()
        {
            using var linker = GetMaterialPackageLinker("UWindowFonts.utx");
            linker.InitializePackage();

            var exports = linker.Objects
                .Where(obj => (int)obj > 0)
                .ToList();

            AssertExportsOfType<UFont>(exports);
            AssertExportsOfType<UPalette>(exports);
            AssertExportsOfType<UTexture>(exports);
        }
    }
}
