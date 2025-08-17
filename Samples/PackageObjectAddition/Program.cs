// Adds a new function to an existing class, by appending it to the end of the package file.

using System.Diagnostics;
using UELib;
using UELib.Core;
using UELib.Flags;
using static UELib.Core.UStruct.UByteCodeDecompiler;

// Has to be a decompressed(and decrypted if any) package!
const string packagePath = "Assets/TestUC3.u";

using var fileStream = new FileStream(packagePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
using var package = new UnrealPackage(fileStream);

var packageStream = package.Stream;
package.BuildTarget = UnrealPackage.GameBuild.BuildName.Unset;
package.Deserialize(packageStream);

// In order for FindObjectByGroup to work, we need to initialize the export objects.
package.InitializePackage(UnrealPackage.InitFlags.RegisterClasses | UnrealPackage.InitFlags.Construct);

const string objectTarget = "UC3Test";
var objectResult = package.FindObjectByGroup(objectTarget);
Console.WriteLine(objectResult == null
    ? $"Object '{objectTarget}' not found in package."
    : $"Found object: {objectResult.Name} of type {objectResult.Class?.Name ?? "Class"}");

if (objectResult is UClass targetClass)
{
    // Load the fields, so we can modify them.
    targetClass.Load();

    var export = targetClass.ExportResource;
    Debug.Assert(export != null, "Object must be an export");

    var newFunctionName = new UName("MyNewAddedFunctionName");
    var tokenFactory = package.Branch.GetTokenFactory(package);
    // Let's call a function, recursively!
    var funcCall = new VirtualFunctionToken
    {
        OpCode = tokenFactory.GetOpCodeFromTokenType<VirtualFunctionToken>(),
        FunctionName = newFunctionName,
        Arguments = [tokenFactory.CreateToken<EndFunctionParmsToken>()]
    };

    var returnToken = tokenFactory.CreateToken<ReturnToken>();
    returnToken.ReturnExpression = tokenFactory.CreateToken<NothingToken>();
    var endOfScript = tokenFactory.CreateToken<EndOfScriptToken>();

    UnrealPackageBuilder
        .Operate(package)
        .AddName(newFunctionName)
        .AddResource(UnrealObjectBuilder
                     .CreateFunction(package)
                     .FriendlyName(newFunctionName)
                     .FunctionFlags(FunctionFlag.Public, FunctionFlag.Defined)
                     .Outer(targetClass) // We need to set this before the call to AddResource finalizes.
                     .WithStatements(funcCall, returnToken, endOfScript)
                     .Build(out var newFunction));

    // Register the new function in the target class.
    var newClass = UnrealObjectBuilder
                   .Operate(targetClass)
                   .AddField(newFunction)
                   .Build();

    // Ensure that the internal UnrealName.None can be mapped back to the existing 'None' index in the current package.
    package.Archive.NameIndices[UnrealName.None.Index] = package.Names.FindIndex(name => ((IndexName)name).Index == UnrealName.None.Index);
    package.Summary.NameOffset = (int)packageStream.Length; // Write out the name table and other tables to the end of the stream.
    packageStream.Seek(0, SeekOrigin.Begin);
    package.Serialize(packageStream);

    // Re-write the updated class, hopefully the class hasn't changed in size. (for UE2 and older this may cause change in size)
    packageStream.Seek(newClass.ExportResource!.SerialOffset, SeekOrigin.Begin);
    newClass.Save(packageStream);

    // Write out the new function to the end of the stream.
    packageStream.Seek(0, SeekOrigin.End);
    int serialOffset = (int)packageStream.Position;
    newFunction.Save(packageStream); // save the new function at the end of the stream
    newFunction.ExportResource!.SerialOffset = serialOffset;
    newFunction.ExportResource!.SerialSize = (int)packageStream.Position - serialOffset;

    // Move the entire export table to the end of the stream (to make room for the new function)
    package.Summary.ExportOffset = (int)packageStream.Length;

    packageStream.Seek(0, SeekOrigin.Begin);
    package.Serialize(packageStream); // also writes out all the other tables and dependencies table.

    packageStream.Flush(); // Ensure all changes are written to the file

    // sanity test
    //package.InitializePackage();

    Debug.Assert(package.FindObjectByGroup(newFunctionName) != null, "New function was not added successfully.");
}
