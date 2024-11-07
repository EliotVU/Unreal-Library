using System;
using System.IO;
using UELib;

namespace Eliot.UELib.Test
{
    internal static class UnrealPackageUtilities
    {
        // HACK: Ugly workaround the issues with UPackageStream
        public static UPackageStream CreateTempPackageStream()
        {
            string tempFilePath = Path.Join(Path.GetTempFileName());
            File.WriteAllBytes(tempFilePath, BitConverter.GetBytes(UnrealPackage.Signature));

            var stream = new UPackageStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite);
            return stream;
        }
    }
}
