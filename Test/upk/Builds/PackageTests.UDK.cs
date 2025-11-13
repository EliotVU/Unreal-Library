using System.Reflection;
using UELib;
using UELib.Core;
using static Eliot.UELib.Test.UnrealPackageTests;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace Eliot.UELib.Test.Builds
{
    /// <summary>
    /// Use the UDK build to test compatibility with UE3.
    /// </summary>
    [TestClass]
    public class PackageTestsUDK
    {
        public static UnrealPackage GetScriptPackage()
        {
            string packagePath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "UPK",
                "TestUC3", "TestUC3.u");
            var linker = UnrealLoader.LoadPackage(packagePath);
            Assert.IsNotNull(linker);
            return linker;
        }

        [TestMethod]
        public void TestScriptContent()
        {
            void AssertDefaults(UnrealPackage unrealPackage)
            {
                var defaults = UnrealPackageUtilities.AssertDefaultPropertiesClass(unrealPackage);
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
                UnrealPackageUtilities.
                                //UnrealPackageTests.AssertPropertyTagFormat(defaults, "String",
                                //    "\"String_\\\"\\\\0abf\\\\n\\\\rtv\"");
                                AssertPropertyTagFormat(defaults, "Vector",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Vector4",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000,W=4.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Vector2D",
                    "(X=1.0000000,Y=2.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Rotator",
                    "(Pitch=180,Yaw=90,Roll=45)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Quat",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000,W=4.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Plane",
                    "(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Color",
                    "(R=80,G=40,B=20,A=160)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "LinearColor",
                    "(R=0.2000000,G=0.4000000,B=0.6000000,A=0.8000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Box",
                    "(Min=(X=0.0000000,Y=1.0000000,Z=2.0000000)," +
                    "Max=(X=0.0000000,Y=2.0000000,Z=1.0000000),IsValid=1)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Matrix",
                    "(XPlane=(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)," +
                    "YPlane=(W=4.0000000,X=5.0000000,Y=6.0000000,Z=7.0000000)," +
                    "ZPlane=(W=8.0000000,X=9.0000000,Y=10.0000000,Z=11.0000000)," +
                    "WPlane=(W=12.0000000,X=13.0000000,Y=14.0000000,Z=15.0000000))");
            }

            void AssertFunctionDelegateTokens(UnrealPackage linker)
            {
                var delegateTokensFunc = linker.FindObject<UFunction>("DelegateTokens")!;
                delegateTokensFunc.Load();

                var decompiler = new UStruct.UByteCodeDecompiler(delegateTokensFunc);
                decompiler.Deserialize();
                UnrealPackageUtilities.

                                // OnDelegate();
                                AssertTokens(decompiler,
                    typeof(DelegateFunctionToken),
                    typeof(EndFunctionParmsToken));
                UnrealPackageUtilities.

                                // OnDelegate = InternalOnDelegate;
                                AssertTokens(decompiler,
                    typeof(LetDelegateToken),
                    typeof(InstanceVariableToken),
                    typeof(DelegatePropertyToken));
                UnrealPackageUtilities.

                                // OnDelegate = none;
                                AssertTokens(decompiler,
                    typeof(LetDelegateToken),
                    typeof(InstanceVariableToken),
                    typeof(DelegatePropertyToken));
                UnrealPackageUtilities.

                                // if (OnDelegate == InstanceDelegate);
                                AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceVariableToken),
                    typeof(EndFunctionParmsToken));
                UnrealPackageUtilities.

                                // if (OnDelegate != InstanceDelegate);
                                AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpNeToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceVariableToken),
                    typeof(EndFunctionParmsToken));
                UnrealPackageUtilities.

                                // if (OnDelegate == InternalOnDelegate);
                                AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateFunctionCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceDelegateToken),
                    typeof(EndFunctionParmsToken));
                UnrealPackageUtilities.

                                // if (OnDelegate != InternalOnDelegate);
                                AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateFunctionCmpNeToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceDelegateToken),
                    typeof(EndFunctionParmsToken));
                UnrealPackageUtilities.

                                // if (OnDelegate == none);
                                AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(EmptyDelegateToken),
                    typeof(EndFunctionParmsToken));
                UnrealPackageUtilities.

                                // (return)
                                AssertTokens(decompiler,
                    typeof(ReturnToken),
                    typeof(NothingToken),
                    typeof(EndOfScriptToken));

                Assert.AreEqual(decompiler.DeserializedTokens.Last(), decompiler.CurrentToken);
            }

            using var package = GetScriptPackage();
            Assert.IsNotNull(package);
            package.InitializePackage();
            AssertTestClass(package);

            var tokensClass = package.FindObject<UClass>("ExprTokens");
            Assert.IsNotNull(tokensClass);

            // Test a series of expected tokens
            AssertFunctionDelegateTokens(package);
            UnrealPackageUtilities.AssertScriptDecompile(tokensClass);
            AssertDefaults(package);
        }
    }
}
