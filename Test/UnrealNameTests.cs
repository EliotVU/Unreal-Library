using UELib.Core;

namespace Eliot.UELib.Test;

[TestClass]
public class UnrealNameTests
{
    [TestMethod]
    public void TestDefaultIsNone()
    {
        // Ensure that the default UName is "None"
        UName name = default;
        Assert.IsTrue(name.IsNone());
        Assert.AreEqual("None", name.ToString());
    }

    [TestMethod]
    public void TestNoneIsNone()
    {
        var name = UnrealName.None;
        Assert.IsTrue(name.IsNone());
        Assert.AreEqual("None", name.ToString());
    }

    [TestMethod]
    public void TestRawIsNone()
    {
        // Pretend we parsed "None" from a file.
        var name = new UName("None");
        Assert.IsTrue(name.IsNone());
        Assert.AreEqual("None", name.ToString());
    }

    [TestMethod]
    public void TestNameNumber()
    {
        // Pretend we parsed "None" from a file.
        var name = new UName("None", 1);
        Assert.IsFalse(name.IsNone());
        Assert.AreEqual("None_0", name.ToString());
    }
}
