using System.Reflection;
using UELib;
using UELib.Core;
using UELib.Engine;
using UELib.ObjectModel.Annotations;

namespace Eliot.UELib.Test
{
    [UnrealRegisterClass]
    public class UMyClass: UClass;
    
    [TestClass]
    public class UnrealPackageTests
    {
        public class MyUModel : UModel;

        // Legacy approach
        [TestMethod]
        public void TestClassTypeOverride()
        {
            using var package1 = new UnrealPackage();

            Assert.IsTrue(package1.GetClassType("Model") == typeof(UnknownObject));
            package1.AddClassType("Model", typeof(MyUModel));
            Assert.IsTrue(package1.GetClassType("Model") == typeof(MyUModel));
            package1.InitializePackage(UnrealPackage.InitFlags.RegisterClasses);
            Assert.IsTrue(package1.GetClassType("Model") == typeof(UModel));

            using var package2 = new UnrealPackage();

            // Swapped order...
            Assert.IsTrue(package2.GetClassType("Model") == typeof(UnknownObject));
            package2.InitializePackage(UnrealPackage.InitFlags.RegisterClasses);
            Assert.IsTrue(package2.GetClassType("Model") == typeof(UModel));
            package2.AddClassType("Model", typeof(MyUModel));
            Assert.IsTrue(package2.GetClassType("Model") == typeof(MyUModel));

            // Using attributes in a custom assembly.
            package2.Archive.Environment.AddUnrealClasses(Assembly.GetExecutingAssembly());
            Assert.AreEqual(typeof(UMyClass), package2.GetClassType("MyClass"));
        }

        [TestMethod]
        public void TestClassTypeOverrideUsingEnvironment()
        {
            using var assemblyEnvironment = new UnrealPackageEnvironment("", []);

            assemblyEnvironment.AddUnrealClasses(Assembly.GetExecutingAssembly());
            Assert.AreEqual(typeof(UMyClass), (Type)assemblyEnvironment.ObjectContainer.Find<UClass>(new UName("MyClass"))!);
            
            using var manualEnvironment = new UnrealPackageEnvironment("", []);

            // With class attribute
            manualEnvironment.AddUnrealClass<UMyClass>();
            Assert.AreEqual(typeof(UMyClass), (Type)manualEnvironment.ObjectContainer.Find<UClass>(new UName("MyClass"))!);

            // Without class attribute
            manualEnvironment.AddUnrealClasses(Assembly.GetAssembly(typeof(UnrealPackage))!); // Required base classes
            manualEnvironment.AddUnrealClass<MyUModel>("Model", "Engine", "Object");
            Assert.AreEqual(typeof(MyUModel), (Type)manualEnvironment.ObjectContainer.Find<UClass>(new UName("Model"))!);
        }

        internal static void AssertTestClass(UnrealPackage package)
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
    }
}
