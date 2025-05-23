using System;
using System.IO;
using System.Linq;
using UELib.Decoding;

namespace UELib
{
    /// <summary>
    /// Provides static methods for loading unreal packages.
    /// </summary>
    public static class UnrealLoader
    {
        public static UnrealPackage LoadPackage(string packagePath, FileAccess fileAccess = FileAccess.Read)
        {
            return LoadPackage(packagePath, UnrealPackage.GameBuild.BuildName.Unset, fileAccess);
        }

        /// <summary>
        /// Loads the given file specified by PackagePath and
        /// returns the serialized UnrealPackage.
        /// </summary>
        public static UnrealPackage LoadPackage(string packagePath, UnrealPackage.GameBuild.BuildName buildTarget,
            FileAccess fileAccess = FileAccess.Read)
        {
            var stream = new UPackageStream(packagePath, FileMode.Open, fileAccess);
            var package = new UnrealPackage(stream);
            package.BuildTarget = buildTarget;
            package.Deserialize(stream);
            return package;
        }

        /// <summary>
        /// Loads the given file specified by PackagePath and
        /// returns the serialized UnrealPackage.
        /// </summary>
        public static UnrealPackage LoadPackage(string packagePath, IBufferDecoder decoder,
            FileAccess fileAccess = FileAccess.Read)
        {
            var stream = new UPackageStream(packagePath, FileMode.Open, fileAccess) { Decoder = decoder };
            var package = new UnrealPackage(stream);
            package.Deserialize(stream);
            return package;
        }

        /// <summary>
        /// Loads the given file specified by PackagePath and
        /// returns the serialized UnrealPackage with deserialized objects.
        /// </summary>
        public static UnrealPackage LoadFullPackage(string packagePath, FileAccess fileAccess = FileAccess.Read)
        {
            var package = LoadPackage(packagePath, fileAccess);
            package?.InitializePackage();

            return package;
        }

        /// <summary>
        /// Looks if the package is already loaded before by looking into the CachedPackages list first.
        /// If it is not found then it loads the given file specified by PackagePath and returns the serialized UnrealPackage.
        /// </summary>
        [Obsolete]
        public static UnrealPackage LoadCachedPackage(string packagePath, FileAccess fileAccess = FileAccess.Read)
        {
            throw new NotImplementedException("Deprecated");
        }

        /// <summary>
        /// Tests if the given file path has an Unreal extension.
        /// </summary>
        /// <param name="filePath">The path to the Unreal file.</param>
        public static bool IsUnrealFileExtension(string filePath)
        {
            string fileExt = Path.GetExtension(filePath);
            return UnrealExtensions.Common.Concat(UnrealExtensions.Legacy).Any(ext => fileExt == ext);
        }

        /// <summary>
        /// Tests if the given file has an Unreal signature.
        /// </summary>
        /// <param name="filePath">The path to the Unreal file.</param>
        public static bool IsUnrealFileSignature(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            return UnrealFile.GetSignature(stream) != 0;
        }
    }
}
