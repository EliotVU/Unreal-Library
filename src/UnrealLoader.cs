using System.Collections.Generic;
using System.IO;
using UELib.Decoding;

namespace UELib
{
    /// <summary>
    /// Provides static methods for loading unreal packages.
    /// </summary>
    public static class UnrealLoader
    {
        /// <summary>
        /// Stored packages that were imported by certain objects. Kept here that in case re-use is necessary, that it will be loaded faster.
        /// The packages and the list is closed and cleared by the main package that loaded them with ImportObjects().
        /// In any other case the list needs to be cleared manually.
        /// </summary>
        private static readonly List<UnrealPackage> _CachedPackages = new List<UnrealPackage>();

        /// <summary>
        /// Loads the given file specified by PackagePath and
        /// returns the serialized UnrealPackage.
        /// </summary>
        public static UnrealPackage LoadPackage(string packagePath, FileAccess fileAccess = FileAccess.Read)
        {
            var stream = new UPackageStream(packagePath, FileMode.Open, fileAccess);
            var package = new UnrealPackage(stream);
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
            var stream = new UPackageStream(packagePath, FileMode.Open, fileAccess)
            {
                Decoder = decoder
            };
            var package = new UnrealPackage(stream);
            package.Deserialize(stream);
            return package;
        }

        /// <summary>
        /// Looks if the package is already loaded before by looking into the CachedPackages list first.
        /// If it is not found then it loads the given file specified by PackagePath and returns the serialized UnrealPackage.
        /// </summary>
        public static UnrealPackage LoadCachedPackage(string packagePath, FileAccess fileAccess = FileAccess.Read)
        {
            var package = _CachedPackages.Find(pkg => pkg.PackageName == Path.GetFileNameWithoutExtension(packagePath));
            if (package != null)
                return package;

            package = LoadPackage(packagePath, fileAccess);
            if (package != null)
            {
                _CachedPackages.Add(package);
            }

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
    }
}