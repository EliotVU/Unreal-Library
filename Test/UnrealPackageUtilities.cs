using System;
using System.IO;
using UELib;
using UELib.Branch;

namespace Eliot.UELib.Test
{
    public static class UnrealPackageUtilities
    {
        public static FileStream CreateTempPackageStream()
        {
            string tempFilePath = Path.Join(Path.GetTempFileName());
            var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite);

            return fileStream;
        }

        public static UnrealPackageArchive CreateTempArchive(PackageObjectLegacyVersion version,
            ushort licenseeVersion = 0) => CreateTempArchive((uint)version, licenseeVersion);
        public static UnrealPackageArchive CreateTempArchive(uint version, ushort licenseeVersion = 0)
        {
            var fileStream = CreateTempPackageStream();

            var archive = new UnrealPackageArchive(fileStream);
            var package = archive.Package;
            package.Build = new UnrealPackage.GameBuild(package);
            package.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = version,
                LicenseeVersion = licenseeVersion
            };

            return archive;
        }
        
        public static UnrealPackageArchive CreateMemoryArchive(PackageObjectLegacyVersion version,
            ushort licenseeVersion = 0) => CreateMemoryArchive((uint)version, licenseeVersion);
        public static UnrealPackageArchive CreateMemoryArchive(uint version, ushort licenseeVersion = 0)
        {
            var memoryStream = new MemoryStream();
            var archive = new UnrealPackageArchive(memoryStream, "Transient");
            var package = archive.Package;
            package.Build = new UnrealPackage.GameBuild(package);
            package.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = version,
                LicenseeVersion = licenseeVersion
            };

            return archive;
        }
    }
}
