using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;
using static Eliot.UELib.Test.UnrealPackageTests;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace Eliot.UELib.Test.upk
{
    [TestClass]
    public class UE3PackageContentTests
    {
        public static UnrealPackage GetScriptPackageLinker()
        {
            string packagePath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "upk",
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
                //UnrealPackageTests.AssertPropertyTagFormat(defaults, "String",
                //    "\"String_\\\"\\\\0abf\\\\n\\\\rtv\"");
                AssertPropertyTagFormat(defaults, "Vector",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000)");
                AssertPropertyTagFormat(defaults, "Vector4",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000,W=4.0000000)");
                AssertPropertyTagFormat(defaults, "Vector2D",
                    "(X=1.0000000,Y=2.0000000)");
                AssertPropertyTagFormat(defaults, "Rotator",
                    "(Pitch=180,Yaw=90,Roll=45)");
                AssertPropertyTagFormat(defaults, "Quat",
                    "(X=1.0000000,Y=2.0000000,Z=3.0000000,W=4.0000000)");
                AssertPropertyTagFormat(defaults, "Plane",
                    "(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)");
                AssertPropertyTagFormat(defaults, "Color",
                    "(R=80,G=40,B=20,A=160)");
                AssertPropertyTagFormat(defaults, "LinearColor",
                    "(R=0.2000000,G=0.4000000,B=0.6000000,A=0.8000000)");
                AssertPropertyTagFormat(defaults, "Box",
                    "(Min=(X=0.0000000,Y=1.0000000,Z=2.0000000)," +
                    "Max=(X=0.0000000,Y=2.0000000,Z=1.0000000),IsValid=1)");
                AssertPropertyTagFormat(defaults, "Matrix",
                    "(XPlane=(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)," +
                    "YPlane=(W=4.0000000,X=5.0000000,Y=6.0000000,Z=7.0000000)," +
                    "ZPlane=(W=8.0000000,X=9.0000000,Y=10.0000000,Z=11.0000000)," +
                    "WPlane=(W=12.0000000,X=13.0000000,Y=14.0000000,Z=15.0000000))");
            }

            void AssertFunctionDelegateTokens(UnrealPackage linker)
            {
                var delegateTokensFunc = linker.FindObject<UFunction>("DelegateTokens");
                delegateTokensFunc.BeginDeserializing();

                var script = delegateTokensFunc.ByteCodeManager;
                script.Deserialize();
                script.CurrentTokenIndex = -1;

                // OnDelegate();
                AssertTokens(script,
                    typeof(DelegateFunctionToken),
                    typeof(EndFunctionParmsToken));

                // OnDelegate = InternalOnDelegate;
                AssertTokens(script,
                    typeof(LetDelegateToken),
                    typeof(InstanceVariableToken),
                    typeof(DelegatePropertyToken));

                // OnDelegate = none;
                AssertTokens(script,
                    typeof(LetDelegateToken),
                    typeof(InstanceVariableToken),
                    typeof(DelegatePropertyToken));

                // if (OnDelegate == InstanceDelegate);
                AssertTokens(script,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceVariableToken),
                    typeof(EndFunctionParmsToken));

                // if (OnDelegate != InstanceDelegate);
                AssertTokens(script,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpNeToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceVariableToken),
                    typeof(EndFunctionParmsToken));

                // if (OnDelegate == InternalOnDelegate);
                AssertTokens(script,
                    typeof(JumpIfNotToken),
                    typeof(DelegateFunctionCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceDelegateToken),
                    typeof(EndFunctionParmsToken));

                // if (OnDelegate != InternalOnDelegate);
                AssertTokens(script,
                    typeof(JumpIfNotToken),
                    typeof(DelegateFunctionCmpNeToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceDelegateToken),
                    typeof(EndFunctionParmsToken));

                // if (OnDelegate == none);
                AssertTokens(script,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(EmptyDelegateToken),
                    typeof(EndFunctionParmsToken));

                // (return)
                AssertTokens(script,
                    typeof(ReturnToken),
                    typeof(NothingToken),
                    typeof(EndOfScriptToken));

                Assert.AreEqual(script.DeserializedTokens.Last(), script.CurrentToken);
            }

            using var linker = GetScriptPackageLinker();
            Assert.IsNotNull(linker);
            linker.InitializePackage();
            AssertTestClass(linker);

            var tokensClass = linker.FindObject<UClass>("ExprTokens");
            Assert.IsNotNull(tokensClass);

            // Test a series of expected tokens
            AssertFunctionDelegateTokens(linker);
            AssertScriptDecompile(tokensClass);
            AssertDefaults(linker);
        }
    }
}
