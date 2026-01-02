using System.Reflection;
using UELib;
using UELib.Core;
using UELib.Flags;
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
                    "0.012346");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "NameProperty",
                    "\"Name\"");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "String",
                    "\"String_\\\"\\\\0abfnrtv\"");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Vector",
                    "(X=1.000000,Y=2.000000,Z=3.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Plane",
                    "(W=0.000000,X=1.000000,Y=2.000000,Z=3.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Rotator",
                    "(Pitch=180,Yaw=90,Roll=45)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Coords",
                    "(Origin=(X=0.200000,Y=0.400000,Z=1.000000)," +
                    "XAxis=(X=1.000000,Y=0.000000,Z=0.000000)," +
                    "YAxis=(X=0.000000,Y=1.000000,Z=0.000000)," +
                    "ZAxis=(X=0.000000,Y=0.000000,Z=1.000000))");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Quat",
                    "(X=1.000000,Y=2.000000,Z=3.000000,W=4.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Range",
                    "(Min=80.000000,Max=40.000000)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Scale",
                    "(Scale=(X=1.000000,Y=2.000000,Z=3.000000),SheerRate=5.000000,SheerAxis=6)");
                UnrealPackageUtilities.AssertPropertyTagFormat(defaults, "Color",
                    "(R=80,G=40,B=20,A=160)");
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
                var delegateTokensFunc = packageLinker.PackageEnvironment.FindObject<UFunction>("DelegateTokens")!;
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

            // Register these BEFORE any decompilation begins.
            var obj = package.Environment.FindObject<UClass>(in UnrealName.Object);
            obj.AddField(new UFunction
            {
                Name = new UName("Not_PreBool"),
                FriendlyName = new UName("!"),
                NativeToken = 129,
                FunctionFlags = new UnrealFlags<FunctionFlag>(package.Branch.EnumFlagsMap[typeof(FunctionFlag)], FunctionFlag.Operator, FunctionFlag.PreOperator, FunctionFlag.Native)
            });

            obj.AddField(new UFunction
            {
                Name = new UName("EqualEqual_BoolBool"),
                FriendlyName = new UName("=="),
                NativeToken = 242,
                OperPrecedence = 24,
                FunctionFlags = new UnrealFlags<FunctionFlag>(package.Branch.EnumFlagsMap[typeof(FunctionFlag)], FunctionFlag.Operator, FunctionFlag.Native)
            });

            obj.AddField(new UFunction
            {
                Name = new UName("EqualEqual_IntInt"),
                FriendlyName = new UName("=="),
                NativeToken = 154,
                OperPrecedence = 24,
                FunctionFlags = new UnrealFlags<FunctionFlag>(package.Branch.EnumFlagsMap[typeof(FunctionFlag)], FunctionFlag.Operator, FunctionFlag.Native)
            });

            obj.AddField(new UFunction
            {
                Name = new UName("AndAnd_BoolBool"),
                FriendlyName = new UName("&&"),
                NativeToken = 130,
                OperPrecedence = 30,
                FunctionFlags = new UnrealFlags<FunctionFlag>(package.Branch.EnumFlagsMap[typeof(FunctionFlag)], FunctionFlag.Operator, FunctionFlag.Native)
            });

            obj.AddField(new UFunction
            {
                Name = new UName("Multiply_FloatFloat"),
                FriendlyName = new UName("*"),
                NativeToken = 171,
                OperPrecedence = 16,
                FunctionFlags = new UnrealFlags<FunctionFlag>(package.Branch.EnumFlagsMap[typeof(FunctionFlag)], FunctionFlag.Operator, FunctionFlag.Native)
            });

            obj.AddField(new UFunction
            {
                Name = new UName("Subtract_FloatFloat"),
                FriendlyName = new UName("-"),
                NativeToken = 175,
                OperPrecedence = 20,
                FunctionFlags = new UnrealFlags<FunctionFlag>(package.Branch.EnumFlagsMap[typeof(FunctionFlag)], FunctionFlag.Operator, FunctionFlag.Native)
            });

            UnrealPackageUtilities.AssertTestClass(package);

            var tokensClass = package.Environment.FindObject<UClass>("ExprTokens");
            Assert.IsNotNull(tokensClass);

            // Test a series of expected tokens
            AssertFunctionDelegateTokens(package.Linker);
            UnrealPackageUtilities.AssertScriptDecompile(tokensClass);
            AssertDefaults(package.Linker);

            // Test precedence output

            var binaryOperatorPrecedenceTest = package.Environment.FindObject<UFunction>("BinaryOperatorPrecedenceTest");
            Assert.AreEqual(
                """
                function float BinaryOperatorPrecedenceTest()
                {
                    return 1.000000 * (1.000000 - 1.000000);
                }
                """,
                binaryOperatorPrecedenceTest.Decompile()
            );

            var preOperatorPrecedenceTest = package.Environment.FindObject<UFunction>("PreOperatorPrecedenceTest");
            Assert.AreEqual(
                """
                function bool PreOperatorPrecedenceTest()
                {
                    return !(true == true && false == false);
                }
                """,
                preOperatorPrecedenceTest.Decompile()
            );
        }
    }
}
