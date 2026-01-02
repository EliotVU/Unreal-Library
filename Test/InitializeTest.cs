using UELib;
using UELib.Services;

namespace Eliot.UELib.Test;

[TestClass]
public class InitializeTest
{
    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        LibServices.LogService = new TestLogService();

        // Decompilation defaults.
        UnrealConfig.Indention = "    ";
        UnrealConfig.SuppressComments = true;
    }
}
