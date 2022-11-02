using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using UELib;

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
            using var linker = GetScriptPackageLinker();
            Assert.IsNotNull(linker);
            linker.InitializePackage();
            UnrealPackageTests.AssertTestClass(linker);

            var defaults = UnrealPackageTests.AssertDefaultPropertiesClass(linker);
            //UnrealPackageTests.AssertPropertyTagFormat(defaults, "String",
            //    "\"String_\\\"\\\\0abf\\\\n\\\\rtv\"");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Float",
                "0.0123457");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Vector",
                "(X=1.0000000,Y=2.0000000,Z=3.0000000)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Vector4",
                "(X=1.0000000,Y=2.0000000,Z=3.0000000,W=4.0000000)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Vector2D",
                "(X=1.0000000,Y=2.0000000)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Rotator",
                "(Pitch=180,Yaw=90,Roll=45)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Quat",
                "(X=1.0000000,Y=2.0000000,Z=3.0000000,W=4.0000000)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Plane",
                "(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Color",
                "(R=80,G=40,B=20,A=160)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "LinearColor",
                "(R=0.2000000,G=0.4000000,B=0.6000000,A=0.8000000)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Box",
                "(Min=(X=0.0000000,Y=1.0000000,Z=2.0000000)," +
                "Max=(X=0.0000000,Y=2.0000000,Z=1.0000000),IsValid=1)");
            UnrealPackageTests.AssertPropertyTagFormat(defaults, "Matrix",
                "(XPlane=(W=0.0000000,X=1.0000000,Y=2.0000000,Z=3.0000000)," +
                "YPlane=(W=4.0000000,X=5.0000000,Y=6.0000000,Z=7.0000000)," +
                "ZPlane=(W=8.0000000,X=9.0000000,Y=10.0000000,Z=11.0000000)," +
                "WPlane=(W=12.0000000,X=13.0000000,Y=14.0000000,Z=15.0000000))");
        }
    }
}