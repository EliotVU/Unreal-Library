using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Core;

namespace UELib.Test
{
    /// <summary>
    /// The following tests requires UELib to be built without WinForms dependencies.
    /// You will have to change your active solution configuration to Test in order to run the tests without WinForms.
    /// </summary>
    [TestClass]
    public class UnrealPackageTests
    {
        [TestMethod]
        public void LoadPackageTest()
        {
            string testPackageUC2Path = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "upk",
                "TestUC2", "TestUC2.u");
            var packageUC2 = UnrealLoader.LoadPackage(testPackageUC2Path);
            Assert.IsNotNull(packageUC2);
            AssertTestPackage(packageUC2);
            AssertDefaultPropertiesClass(packageUC2);

            string testPackageUC3Path = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "upk",
                "TestUC3", "TestUC3.u");
            var packageUC3 = UnrealLoader.LoadPackage(testPackageUC3Path);
            Assert.IsNotNull(packageUC3);
            AssertTestPackage(packageUC3);
        }

        private static void AssertTestPackage(UnrealPackage package)
        {
            package.InitializePackage();

            AssertTestClass(package);
        }

        private static void AssertTestClass(UnrealPackage package)
        {
            var testClass = package.FindObject<UClass>("Test");
            Assert.IsNotNull(testClass);

            // Validate that Public/Protected/Private are correct and distinguishable.
            var publicProperty = package.FindObject<UIntProperty>("Public");
            Assert.IsNotNull(publicProperty);
            Assert.IsTrue(publicProperty.IsPublic());
            Assert.IsFalse(publicProperty.IsProtected());
            Assert.IsFalse(publicProperty.IsPrivate());

            var protectedProperty = package.FindObject<UIntProperty>("Protected");
            Assert.IsNotNull(protectedProperty);
            Assert.IsTrue(protectedProperty.IsPublic());
            Assert.IsTrue(protectedProperty.IsProtected());
            Assert.IsFalse(protectedProperty.IsPrivate());

            var privateProperty = package.FindObject<UIntProperty>("Private");
            Assert.IsNotNull(privateProperty);
            Assert.IsFalse(privateProperty.IsPublic());
            Assert.IsFalse(privateProperty.IsProtected());
            Assert.IsTrue(privateProperty.IsPrivate());
        }

        private static void AssertDefaultPropertiesClass(UnrealPackage package)
        {
            var testClass = package.FindObject<UClass>("DefaultProperties");
            Assert.IsNotNull(testClass);
            
            Assert.IsNotNull(testClass.Properties);
            string stringValue = testClass.Properties.Find("String").DeserializeValue();
            Assert.AreEqual("\"String_\\\"\\\\0abfnrtv\"", stringValue);

            Assert.IsNotNull(testClass.Properties);
            string floatValue = testClass.Properties.Find("Float").DeserializeValue();
            // 0.0123456789 in its compiled form
            Assert.AreEqual("0.012345679", floatValue);
        }
    }
}