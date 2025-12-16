using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using UELib.Decoding;
using UELib.IO;

namespace UELib
{
    /// <summary>
    /// Provides static methods for loading unreal packages.
    /// </summary>
    public static class UnrealLoader
    {
        public static readonly UnrealPackageEnvironment TransientPackageEnvironment = new("Transient", RegisterUnrealClassesStrategy.StandardClasses);

        public static UnrealPackage LoadPackage(
            FileStream fileStream,
            UnrealPackageEnvironment? environment = null)
        {
            string packagePath = fileStream.Name;

            var package = new UnrealPackage(fileStream, packagePath, environment ?? TransientPackageEnvironment);
            package.Deserialize();

            return package;
        }

        public static UnrealPackage LoadPackage(
            string packagePath,
            FileAccess fileAccess = FileAccess.Read,
            UnrealPackageEnvironment? environment = null)
        {
            return LoadPackage(
                packagePath,
                UnrealPackage.GameBuild.BuildName.Unset,
                fileAccess,
                environment
            );
        }

        /// <summary>
        /// Loads the given file specified by PackagePath and
        /// returns the serialized UnrealPackage.
        /// </summary>
        public static UnrealPackage LoadPackage(
            string packagePath,
            UnrealPackage.GameBuild.BuildName buildTarget,
            FileAccess fileAccess = FileAccess.Read,
            UnrealPackageEnvironment? environment = null)
        {
            var fileStream = new FileStream(packagePath, FileMode.Open, fileAccess, FileShare.Read);

            var package = new UnrealPackage(fileStream, packagePath, environment ?? TransientPackageEnvironment);
            package.BuildTarget = buildTarget;
            package.Deserialize();

            return package;
        }

        /// <summary>
        /// Loads the given file specified by PackagePath and
        /// returns the serialized UnrealPackage.
        /// </summary>
        public static UnrealPackage LoadPackage(
            string packagePath,
            IBufferDecoder decoder,
            FileAccess fileAccess = FileAccess.Read,
            UnrealPackageEnvironment? environment = null)
        {
            var fileStream = new FileStream(packagePath, FileMode.Open, fileAccess, FileShare.Read);
            var baseStream = new EncodedStream(fileStream, decoder);

            var package = new UnrealPackage(baseStream, packagePath, environment ?? TransientPackageEnvironment);
            package.Deserialize();

            return package;
        }

        /// <summary>
        /// Loads the given file specified by PackagePath and
        /// returns the serialized UnrealPackage with deserialized objects.
        /// </summary>
        public static UnrealPackage LoadFullPackage(
            string packagePath,
            FileAccess fileAccess = FileAccess.Read,
            UnrealPackageEnvironment? environment = null)
        {
            var package = LoadPackage(packagePath, fileAccess, environment);

            // TODO: Acquire extensions from the detected game build.
            string? packageDirectory = Path.GetDirectoryName(packagePath);
            var packageProvider = packageDirectory != null
                ? new UnrealFilePackageProvider([packageDirectory], UnrealExtensions.Common)
                : null;
            package.InitializePackage(packageProvider);

            return package;
        }

        /// <summary>
        /// Looks if the package is already loaded before by looking into the CachedPackages list first.
        /// If it is not found then it loads the given file specified by PackagePath and returns the serialized UnrealPackage.
        /// </summary>
        [Obsolete("Deprecated", true)]
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
            return UnrealExtensions
                .Common
                .Concat(UnrealExtensions.Legacy)
                .Any(ext => string.Compare(fileExt, ext, StringComparison.OrdinalIgnoreCase) == 0);
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
