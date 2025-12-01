using System.Diagnostics.Contracts;
using UELib;

// Expands an object's size using a path to find the object in a package.
// The example assumes an UE3 Package, for UE2 a different approach will be needed.

// Has to be a decompressed(and decrypted if any) package!
string packagePath = Path.Combine("Assets", "TestUC3.u");

using var packageEnvironment = new UnrealPackageEnvironment(Path.GetDirectoryName(packagePath)!, RegisterUnrealClassesStrategy.StandardClasses);

using var fileStream = new FileStream(packagePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
using var package = new UnrealPackage(fileStream, packagePath, packageEnvironment);

var packageStream = package.Stream;
package.BuildTarget = UnrealPackage.GameBuild.BuildName.Unset;
package.Deserialize(packageStream);

// In order for FindObjectByGroup to work, we need to initialize the export objects.
package.InitializePackage(null, UELib.ObjectModel.Annotations.InternalClassFlags.Default);

const string objectToExpandName = "UC3Test.Test"; // ClassName.FunctionName
var objectResult = package.FindObjectByGroup(objectToExpandName);
Console.WriteLine(objectResult == null
    ? $"Object '{objectToExpandName}' not found in package."
    : $"Found object: {objectResult.Name} of type {objectResult.Class.Name}");

if (objectResult != null)
{
    Contract.Assert(objectResult.PackageIndex.IsExport, "Object must be an export");

    const int expansionSizeInBytes = 100;

    var export = objectResult.ExportResource;
    int newSerialSize = export.SerialSize + expansionSizeInBytes;
    byte[] newBuffer = new byte[newSerialSize];

    packageStream.Seek(export.SerialOffset, SeekOrigin.Begin);
    int copy = packageStream.Read(newBuffer, 0, export.SerialSize); // read the old data
    Contract.Assert(copy == export.SerialSize, "Failed to read the old data");

    packageStream.Position = packageStream.Length; // Move to the end of the stream
    export.SerialOffset = (int)packageStream.Position; // Update the offset to the end of the stream
    export.SerialSize = newSerialSize; // Update the size of the export
    packageStream.Write(newBuffer); // Write the old data to the end of the stream

    // Write the new serial offset and size to the export
    packageStream.Seek(export.Offset, SeekOrigin.Begin); // Offset to the export item.
    export.Serialize(packageStream); // Because the export item size itself hasn't changed, we can cheat and serialize it in place
    Contract.Assert(export.Offset + export.Size == (int)packageStream.Position, "Export size cannot change.");

    fileStream.Flush(); // Ensure all changes are written to the file

    Console.WriteLine($"Successfully expanded object {objectResult.GetReferencePath()} by {expansionSizeInBytes} bytes to a total size of {export.SerialSize}.");
}
