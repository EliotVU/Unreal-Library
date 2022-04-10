using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Core;

namespace UELib.Test
{
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

            string testPackageUC3Path = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "upk",
                "TestUC3", "TestUC3.u");
            var packageUC3 = UnrealLoader.LoadPackage(testPackageUC3Path);
            Assert.IsNotNull(packageUC3);
            AssertTestPackage(packageUC3);
        }

        private static void AssertTestPackage(UnrealPackage package)
        {
            package.InitializePackage(UnrealPackage.InitFlags.Construct);

            var testClass = package.FindObject("Test", typeof(UnknownObject));
            Assert.IsNotNull(testClass);

            // Validate that Public/Protected/Private are correct and distinguishable.
            var publicProperty = package.FindObject("Public", typeof(UnknownObject));
            Assert.IsNotNull(publicProperty);
            Assert.IsTrue(publicProperty.IsPublic());
            Assert.IsFalse(publicProperty.IsProtected());
            Assert.IsFalse(publicProperty.IsPrivate());

            var protectedProperty = package.FindObject("Protected", typeof(UnknownObject));
            Assert.IsNotNull(protectedProperty);
            Assert.IsTrue(protectedProperty.IsPublic());
            Assert.IsTrue(protectedProperty.IsProtected());
            Assert.IsFalse(protectedProperty.IsPrivate());

            var privateProperty = package.FindObject("Private", typeof(UnknownObject));
            Assert.IsNotNull(privateProperty);
            Assert.IsFalse(privateProperty.IsPublic());
            Assert.IsFalse(privateProperty.IsProtected());
            Assert.IsTrue(privateProperty.IsPrivate());

            testClass.BeginDeserializing();
        }
    }
}