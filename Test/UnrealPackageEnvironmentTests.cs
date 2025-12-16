using System.Reflection;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Test;

[TestClass]
public class UnrealPackageEnvironmentTests
{
    [TestMethod]
    public void TransientEnvironmentTest()
    {
        // Created statically, we're basically just testing for any thrown exceptions during its creation.
        Assert.IsNotNull(UnrealLoader.TransientPackageEnvironment);
    }

    [TestMethod]
    public void EnvironmentStaticClassesTest()
    {
        // Ensure that StandardClasses works as expected.
        var objectMap = new UnrealObjectContainer();
        using var packageEnvironment = new UnrealPackageEnvironment("Test", objectMap);

        // add the standard classes (because we have initialized the container ourselves)
        packageEnvironment.AddUnrealClasses();

        var corePackage = objectMap.Find<UPackage>(in UnrealName.Core);

        var packageStaticClass = objectMap.Find<UClass>(in UnrealName.Package);
        Assert.IsNotNull(packageStaticClass, "Class UPackage doesn't exist");

        var classStaticClass = objectMap.Find<UClass>(in UnrealName.Class);
        Assert.IsNotNull(classStaticClass, "Class UClass doesn't exist");
        Assert.IsNotNull(classStaticClass.Super, "Class UClass' super doesn't exist");
        Assert.IsNotNull(classStaticClass.Class);
        Assert.AreEqual(corePackage, classStaticClass.Outer);

        var classObject = objectMap.Find<UClass>(in UnrealName.Class);
        Assert.IsNotNull(classObject);

        // Ensure that we can also retrieve a Function class when specifying a static UClass.
        // This is necessary in dynamic circumstances where do not have an explicit TypeParameter.
        var functionClass = objectMap.Find<UClass>(in UnrealName.Function, corePackage.Name);
        Assert.IsNotNull(functionClass);
    }

    [TestMethod]
    public void TestClassTypeOverrideUsingEnvironment()
    {
        using var assemblyEnvironment = new UnrealPackageEnvironment("ByAssembly", RegisterUnrealClassesStrategy.EssentialClasses);

        assemblyEnvironment.AddUnrealClasses(Assembly.GetExecutingAssembly());
        Assert.AreEqual(typeof(UMyClass), (Type)assemblyEnvironment.GetStaticClass<UMyClass>()!);

        using var manualEnvironment = new UnrealPackageEnvironment("ByHand", RegisterUnrealClassesStrategy.EssentialClasses);

        // With class attribute
        manualEnvironment.AddUnrealClass<UMyClass>();
        Assert.AreEqual(typeof(UMyClass), (Type)manualEnvironment.GetStaticClass<UMyClass>()!);

        // Without class attribute
        manualEnvironment.AddUnrealClasses(Assembly.GetAssembly(typeof(UnrealPackage))!); // Required base classes
        manualEnvironment.AddUnrealClass<UnrealPackageTests.MyUModel>("Model", "Engine", "Object");
        Assert.AreEqual(typeof(UnrealPackageTests.MyUModel), (Type)manualEnvironment.GetStaticClass(new UName("Model"))!);
    }
}
