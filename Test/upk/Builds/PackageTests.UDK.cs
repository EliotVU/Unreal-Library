using System.Reflection;
using UELib;
using UELib.Core;
using UELib.Flags;
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
                    "0.012346");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "NameProperty",
                    "\"Name\"");
                //UnrealPackageTests.AssertPropertyTagFormat(defaults, "String",
                //    "\"String_\\\"\\\\0abf\\\\n\\\\rtv\"");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Vector",
                    "(X=1.000000,Y=2.000000,Z=3.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Vector4",
                    "(X=1.000000,Y=2.000000,Z=3.000000,W=4.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Vector2D",
                    "(X=1.000000,Y=2.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Rotator",
                    "(Pitch=180,Yaw=90,Roll=45)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Quat",
                    "(X=1.000000,Y=2.000000,Z=3.000000,W=4.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Plane",
                    "(W=0.000000,X=1.000000,Y=2.000000,Z=3.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Color",
                    "(R=80,G=40,B=20,A=160)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "LinearColor",
                    "(R=0.200000,G=0.400000,B=0.600000,A=0.800000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Box",
                    "(Min=(X=0.000000,Y=1.000000,Z=2.000000)," +
                    "Max=(X=0.000000,Y=2.000000,Z=1.000000),IsValid=1)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Matrix",
                    "(XPlane=(W=0.000000,X=1.000000,Y=2.000000,Z=3.000000)," +
                    "YPlane=(W=4.000000,X=5.000000,Y=6.000000,Z=7.000000)," +
                    "ZPlane=(W=8.000000,X=9.000000,Y=10.000000,Z=11.000000)," +
                    "WPlane=(W=12.000000,X=13.000000,Y=14.000000,Z=15.000000))");
            }

            void AssertFunctionDelegateTokens(UnrealPackageLinker packageLinker)
            {
                var delegateTokensFunc = packageLinker.PackageEnvironment.FindObject<UFunction>("DelegateTokens");
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
                // if (OnDelegate == InstanceDelegate);
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceVariableToken),
                    typeof(EndFunctionParmsToken));
                // if (OnDelegate != InstanceDelegate);
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpNeToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceVariableToken),
                    typeof(EndFunctionParmsToken));
                // if (OnDelegate == InternalOnDelegate);
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateFunctionCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceDelegateToken),
                    typeof(EndFunctionParmsToken));
                // if (OnDelegate != InternalOnDelegate);
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateFunctionCmpNeToken),
                    typeof(InstanceVariableToken),
                    typeof(InstanceDelegateToken),
                    typeof(EndFunctionParmsToken));
                // if (OnDelegate == none);
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(JumpIfNotToken),
                    typeof(DelegateCmpEqToken),
                    typeof(InstanceVariableToken),
                    typeof(EmptyDelegateToken),
                    typeof(EndFunctionParmsToken));
                // (return)
                UnrealPackageUtilities.AssertTokens(decompiler,
                    typeof(ReturnToken),
                    typeof(NothingToken),
                    typeof(EndOfScriptToken));

                Assert.AreEqual(decompiler.DeserializedTokens.Last(), decompiler.CurrentToken);
            }

            using var package = GetScriptPackage();
            var packageLinker = package.Linker;
            package.InitializePackage(null);

            // Register these BEFORE any decompilation begins.
            var obj = package.Environment.FindObject<UClass>(in UnrealName.Object);
            obj.AddField(new UFunction
            {
                Name = new UName("EqualEqual_IntInt"),
                FriendlyName = new UName("=="),
                NativeToken = 154,
                OperPrecedence = 24,
                FunctionFlags = new UnrealFlags<FunctionFlag>(package.Branch.EnumFlagsMap[typeof(FunctionFlag)], FunctionFlag.Operator, FunctionFlag.Native)
            });

            UnrealPackageUtilities.AssertTestClass(package);

            var tokensClass = package.Environment.FindObject<UClass>("ExprTokens");
            Assert.IsNotNull(tokensClass);

            // Test a series of expected tokens
            AssertFunctionDelegateTokens(packageLinker);
            UnrealPackageUtilities.AssertScriptDecompile(tokensClass);
            AssertDefaults(packageLinker);

            // Test Enum tag output

            var enumTagTest = package.Environment.FindObject<UFunction>("EnumTagTest");
            Assert.AreEqual(
                """
                function EnumTagSamples.MyEnum EnumTagTest(optional EnumTagSamples.MyEnum EnumParam = MyEnum.ME_One)
                {
                    local EnumTagSamples sampler;

                    EnumProperty = MyEnum.ME_One;
                    self.EnumProperty = MyEnum.ME_One;
                    sampler.EnumProperty = MyEnum.ME_One;
                    if(int(EnumProperty) == int(MyEnum.ME_One))
                    {
                        EnumProperty = MyEnum.ME_One;
                    }
                    EnumTagTest(MyEnum.ME_One);
                    sampler.EnumTagTest(MyEnum.ME_One);
                    switch(EnumProperty)
                    {
                        case MyEnum.ME_One:
                            switch(OtherEnumProperty)
                            {
                                case OtherEnum.OE_One:
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case MyEnum.ME_Two:
                            break;
                        default:
                            break;
                    }
                    return MyEnum.ME_One;
                }
                """,
                enumTagTest.Decompile()
            );
        }
    }
}
