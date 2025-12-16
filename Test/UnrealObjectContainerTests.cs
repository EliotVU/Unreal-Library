using UELib;
using UELib.Core;

namespace Eliot.UELib.Test;

[TestClass]
public class UnrealObjectContainerTests
{
    [TestMethod]
    public void ObjectHashingTest()
    {
        var objectMap = new UnrealObjectContainer();
        var obj = new UObject
        {
            Name = new UName("MyObject")
        };

        // Verify if the object is hashed.
        objectMap.Add(obj);
        Assert.IsTrue(objectMap.Contains(obj.Name));

        // Can we find the object?
        Assert.AreEqual(obj, objectMap.Find(obj.Name));
        Assert.AreEqual(obj, objectMap.Find<UObject>(obj.Name));
        Assert.AreEqual(obj, objectMap.Find<UObject>(obj.Name));

        Assert.IsNull(objectMap.Find<UPackage?>(obj.Name));
        Assert.IsNull(objectMap.Find<UPackage?>(obj.Name));

        // Verify no duplications
        objectMap.Add(obj);
        Assert.AreEqual(obj, objectMap.Find(obj.Name));

        objectMap.Add(new UObject { Name = new UName("Dummy") });

        objectMap.Remove(obj);
        Assert.IsFalse(objectMap.Contains(obj.Name));
        // Ensure removal didn't remove ALL.
        Assert.IsNotEmpty(objectMap.Enumerate());

        objectMap.Dispose();
        Assert.IsEmpty(objectMap.Enumerate());
    }
}
