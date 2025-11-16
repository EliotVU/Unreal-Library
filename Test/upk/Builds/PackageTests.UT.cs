using System.Reflection;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Test.Builds
{
    /// <summary>
    /// Use the UT99 build to test compatibility with UE1.
    /// </summary>
    [TestClass]
    public class PackageTestsUT
    {
        public static UnrealPackage GetScriptPackage()
        {
            string packagePath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "UPK",
                "TestUC1", "TestUC1.u");
            var package = UnrealLoader.LoadPackage(packagePath);
            Assert.IsNotNull(package);
            return package;
        }

        [TestMethod]
        public void TestScriptContent()
        {
            void AssertDefaults(UnrealPackageLinker packageLinker)
            {
                var defaults = UnrealPackageUtilities.AssertDefaultPropertiesClass(packageLinker);
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "BoolTrue",
                    "true");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "BoolFalse",
                    "false");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Byte",
                    "255");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Int",
                    "1000");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Float",
                    "0.0123457");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "NameProperty",
                    "\"Name\"");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "String",
                    """
                    "String_\""
                    """);
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Vector",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Plane",
                    "(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Rotator",
                    "(Pitch=180,Yaw=90,Roll=45)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Coords",
                    "(Origin=(X=0.2000000,Y=0.4000000,Z=1.0000000)," +
                    "XAxis=(X=1.0000000,Y=0.0000000,Z=0.0000000)," +
                    "YAxis=(X=0.0000000,Y=1.0000000,Z=0.0000000)," +
                    "ZAxis=(X=0.0000000,Y=0.0000000,Z=1.0000000))");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Scale",
                    "(Scale=(X=1.0000000,Y=2.0000000,Z=3.0000000),SheerRate=5.0000000,SheerAxis=ZY)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Color",
                    "(R=80,G=40,B=20,A=160)");
            }

            using var package = GetScriptPackage();
            package.InitializePackage(null);

            var tokensClass = package.Linker.FindObject<UClass>("ExprTokens");
            Assert.IsNotNull(tokensClass);
            UnrealPackageUtilities.

            // Test a series of expected tokens
            AssertScriptDecompile(tokensClass);
            AssertDefaults(package.Linker);
        }
    }
}
