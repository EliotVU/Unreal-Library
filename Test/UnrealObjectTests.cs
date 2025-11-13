using UELib.Core;

namespace Eliot.UELib.Test;

[TestClass]
public class UnrealObjectTests
{
    [TestMethod]
    public void TestStruct()
    {
        var uStruct = new UStruct();

        var f4 = new UField { Name = new("F4") };
        uStruct.AddField(f4);

        var f3 = new UField { Name = new("F3") };
        uStruct.AddField(f3);

        var f2 = new UField { Name = new("F2") };
        uStruct.AddField(f2);

        var f1 = new UField { Name = new("F1") };
        uStruct.AddField(f1);

        // Test order
        CollectionAssert.AreEqual(
            uStruct
                .EnumerateFields()
                .Select(uStruct => uStruct.Name.ToString())
                .ToList(),
            new List<string> { "F1", "F2", "F3", "F4" }
        );

        // Test removal of a field in-between
        uStruct.RemoveField(f2);
        CollectionAssert.AreEqual(
            uStruct
                .EnumerateFields()
                .Select(uStruct => uStruct.Name.ToString())
                .ToList(),
            new List<string> { "F1", "F3", "F4" }
        );

        // Test removal of the first field
        uStruct.RemoveField(f1);
        CollectionAssert.AreEqual(
            uStruct
                .EnumerateFields()
                .Select(uStruct => uStruct.Name.ToString())
                .ToList(),
            new List<string> { "F3", "F4" }
        );

        // Test removal of the last field.
        uStruct.RemoveField(f4);
        CollectionAssert.AreEqual(
            uStruct
                .EnumerateFields()
                .Select(uStruct => uStruct.Name.ToString())
                .ToList(),
            new List<string> { "F3" }
        );
    }
}
