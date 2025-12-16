using System.Reflection;
using UELib;
using UELib.Core;
using UELib.Engine;
using UELib.ObjectModel.Annotations;

namespace Eliot.UELib.Test
{
    [UnrealRegisterClass]
    public class UMyClass : UClass;

    [TestClass]
    public class UnrealPackageTests
    {
        public class MyUModel : UModel;

        // Legacy approach
        [TestMethod]
        [Obsolete("Legacy")]
        public void TestClassTypeOverride()
        {
            using var package1 = new UnrealPackage("package1");

            Assert.IsTrue(package1.GetClassType("Model") == typeof(UnknownObject));
            package1.AddClassType("Model", typeof(MyUModel));
            Assert.IsTrue(package1.GetClassType("Model") == typeof(MyUModel));
            package1.InitializePackage(UnrealPackage.InitFlags.RegisterClasses);
            Assert.IsTrue(package1.GetClassType("Model") == typeof(UModel));

            using var package2 = new UnrealPackage("package2");

            // Swapped order...
            Assert.IsTrue(package2.GetClassType("Model") == typeof(UnknownObject));
            package2.InitializePackage(UnrealPackage.InitFlags.RegisterClasses);
            Assert.IsTrue(package2.GetClassType("Model") == typeof(UModel));
            package2.AddClassType("Model", typeof(MyUModel));
            Assert.IsTrue(package2.GetClassType("Model") == typeof(MyUModel));

            // Using attributes in a custom assembly.
            package2.Environment.AddUnrealClasses(Assembly.GetExecutingAssembly());
            Assert.AreEqual(typeof(UMyClass), package2.GetClassType("MyClass"));
        }
    }
}
