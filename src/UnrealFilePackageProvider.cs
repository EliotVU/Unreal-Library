using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using UELib.Core;
using UELib.Services;

namespace UELib;

public interface IUnrealPackageProvider
{
    /// <summary>
    ///     Resolves and returns a package by name, using the provided linker to resolve any dependencies.
    /// </summary>
    /// <param name="packageName">The package name to look for (may include the relative path)</param>
    /// <param name="linker">The linker to use to retrieve or construct (and load the <see cref="UPackage"/>) instance.</param>
    /// <returns>The constructed and (possibly) loaded <see cref="UPackage"/>.</returns>
    public UPackage GetPackage(string packageName, UnrealPackageLinker linker);
}

public sealed class UnrealFilePackageProvider : IUnrealPackageProvider
{
    private readonly UnrealPackageEnvironment _Environment;
    private readonly string[] _PackageExtensions;

    public UnrealFilePackageProvider(UnrealPackageEnvironment environment, string[] packageExtensions)
    {
        _Environment = environment;
        _PackageExtensions = packageExtensions;

        foreach (string directory in environment.Directories)
        {
            Contract.Assert(
                Directory.Exists(directory),
                $"Invalid directory '{directory}' passed to environment '{environment.Name}'."
            );
        }
    }

    public UPackage GetPackage(string packageName, UnrealPackageLinker linker)
    {
        if (_Environment.Directories.Length == 0)
        {
            return linker.GetRootPackage(packageName);
        }

        // Build search patterns for each extension, e.g. "Core.u", "Core.upk"
        string[] patterns = _PackageExtensions
                            .Select(ext => packageName + ext)
                            .ToArray();

        foreach (string root in _Environment.Directories)
        {
            foreach (string pattern in patterns)
            {
                // Search recursively for files matching the pattern
                var files = Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories);

                string? file = files.FirstOrDefault();
                if (file == null)
                {
                    continue;
                }

                var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                var archive = new UnrealPackageArchive(stream, file, _Environment);

                LibServices.Debug("Found import package '{0}' at '{1}'", packageName, root);

                var pkg = archive.Package.RootPackage;
                pkg.Package = archive.Package;

                // Load the package's header and tables
                archive.Package.Deserialize();


                var otherLinker = new UnrealPackageLinker(archive.Package, this);
                archive.Package.Linker = otherLinker;

                // Load the package's exports
                //archive.Package.InitializePackage(UnrealPackage.InitFlags.Construct | UnrealPackage.InitFlags.Deserialize, otherLinker);
                otherLinker.Preload();

                return pkg;
            }
        }

        LibServices.Debug("Couldn't find import package '{0}'", packageName);
        foreach (string directory in _Environment.Directories)
        {
            LibServices.Debug("  Searched in '{0}'", directory);
        }

        return linker.GetRootPackage(packageName);
    }
}
