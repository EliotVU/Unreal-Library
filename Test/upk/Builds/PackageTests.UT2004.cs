using System.Reflection;
using UELib;
using UELib.Core;
using static Eliot.UELib.Test.UnrealPackageTests;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace Eliot.UELib.Test.Builds
{
    /// <summary>
    /// Use the UT2004 build to test compatibility with UE2 (Might actually be testing for UE2.5)
    /// </summary>
    [TestClass]
    public class PackageTestsUT2004
    {
        public static UnrealPackage GetScriptPackage()
        {
            string packagePath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "UPK",
                "TestUC2", "TestUC2.u");
            var package = UnrealLoader.LoadPackage(packagePath);
            Assert.IsNotNull(package);
            return package;
        }

        public static UnrealPackage GetMapPackage(string fileName)
        {
            string? gamesPath = Environment.GetEnvironmentVariable("UEGamesTestDirectory");
            string packagePath = Path.Join(gamesPath, "UT2004", "Maps", fileName);
            if (!File.Exists(packagePath))
            {
                Assert.Inconclusive($"Couldn't find package '{packagePath}'");
            }

            var package = UnrealLoader.LoadPackage(packagePath);
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
                    "\"String_\\\"\\\\0abfnrtv\"");
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
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Quat",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000,W=4.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Range",
                    "(Min=80.0000000,Max=40.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Scale",
                    "(Scale=(X=1.0000000,Y=2.0000000,Z=3.0000000),SheerRate=5.0000000,SheerAxis=6)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Color",
                    "(R=80,G=40,B=20,A=160)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Box",
                    "(Min=(X=0.0000000,Y=1.0000000,Z=2.0000000)," +
                    "Max=(X=0.0000000,Y=2.0000000,Z=1.0000000),IsValid=1)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Matrix",
                    "(XPlane=(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)," +
                    "YPlane=(W=4.0000000,X=5.0000000,Y=6.0000000,Z=7.0000000)," +
                    "ZPlane=(W=8.0000000,X=9.0000000,Y=10.0000000,Z=11.0000000)," +
                    "WPlane=(W=12.0000000,X=13.0000000,Y=14.0000000,Z=15.0000000))");
            }

            void AssertFunctionDelegateTokens(UnrealPackageLinker packageLinker)
            {
                var delegateTokensFunc = packageLinker.FindObject<UFunction>("DelegateTokens")!;
                packageLinker.LoadObject(delegateTokensFunc);

                var decompiler = new UStruct.UByteCodeDecompiler(delegateTokensFunc);
                decompiler.Deserialize();

                // OnDelegate();
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(DelegateFunctionToken),
                    typeof(EndFunctionParmsToken));
                // OnDelegate = InternalOnDelegate;
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(LetDelegateToken),
                    typeof(InstanceVariableToken),
                    typeof(DelegatePropertyToken));
                // OnDelegate = none;
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(LetDelegateToken),
                    typeof(InstanceVariableToken),
                    typeof(DelegatePropertyToken));
                // (return)
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(ReturnToken),
                    typeof(NothingToken),
                    typeof(EndOfScriptToken));

                Assert.AreEqual(decompiler.DeserializedTokens.Last(), decompiler.CurrentToken);
            }

            using var package = GetScriptPackage();
            package.InitializePackage(null);

            AssertTestClass(package.Linker);

            var tokensClass = package.Linker.FindObject<UClass>("ExprTokens");
            Assert.IsNotNull(tokensClass);

            // Test a series of expected tokens
            AssertFunctionDelegateTokens(package.Linker);
            UnrealPackageUtilities.AssertScriptDecompile(tokensClass);
            AssertDefaults(package.Linker);
        }
    }
}
