// Expands an object's size using a path to find the object in a package.
// The example assumes an UE3 Package, for UE2 a different approach will be needed.

using System.Diagnostics;
using UELib;

// Has to be a decompressed(and decrypted if any) package!
const string packagePath = "Assets/TestUC3.u";

using var fileStream = new FileStream(packagePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
using var package = new UnrealPackage(fileStream);
var stream = package.Stream;
package.BuildTarget = UnrealPackage.GameBuild.BuildName.Unset;
package.Deserialize(stream);

// In order for FindObjectByGroup to work, we need to initialize the export objects.
package.InitializePackage(UnrealPackage.InitFlags.RegisterClasses | UnrealPackage.InitFlags.Construct);

const string objectToExpandName = "UC3Test.Test"; // ClassName.FunctionName
var testFunction = package.FindObjectByGroup(objectToExpandName);
Console.WriteLine(testFunction == null
    ? $"Object '{objectToExpandName}' not found in package."
    : $"Found object: {testFunction.Name} of type {testFunction.Class?.Name ?? "Class"}");

if (testFunction != null)
{
    var export = testFunction.ExportResource;
    Debug.Assert(export != null, "Object must be an export");

    int newSerialSize = export.SerialSize + 100; // Increase size by 100 bytes
    byte[] newBuffer = new byte[newSerialSize];

    stream.Seek(export.SerialOffset, SeekOrigin.Begin);
    int copy = stream.Read(newBuffer, 0, export.SerialSize); // read the old data
    Debug.Assert(copy == export.SerialSize, "Failed to read the old data");

    stream.Position = stream.Length; // Move to the end of the stream
    export.SerialOffset = (int)stream.Position; // Update the offset to the end of the stream
    export.SerialSize = newSerialSize; // Update the size of the export
    stream.Write(newBuffer); // Write the old data to the end of the stream

    // Write the new serial offset and size to the export
    stream.Seek(export.Offset, SeekOrigin.Begin); // Offset to the export item.
    export.Serialize(stream); // Because the export item size itself hasn't changed, we can cheat and serialize it in place
    Debug.Assert(export.Offset + export.Size == (int)stream.Position, "Export size cannot change.");
    
    stream.Flush(); // Ensure all changes are written to the file
}
