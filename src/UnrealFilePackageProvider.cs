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
    /// <param name="packageLinker">The linker to use to retrieve or construct (and load the <see cref="UPackage"/>) instance.</param>
    /// <returns>The constructed and (possibly) loaded <see cref="UPackage"/>.</returns>
    public UPackage GetPackage(string packageName, UnrealPackageLinker packageLinker);
}

public sealed class UnrealFilePackageProvider : IUnrealPackageProvider
{
    private readonly string[] _Directories;
    private readonly string[] _PackageExtensions;

    public UnrealFilePackageProvider(string[] directories, string[] packageExtensions)
    {
        _Directories = directories;
        _PackageExtensions = packageExtensions;

        foreach (string extension in _PackageExtensions)
        {
            Contract.Assert(
                extension.StartsWith("."),
                $"Invalid extension '{extension}' passed to package provider."
            );
        }

        foreach (string directory in _Directories)
        {
            Contract.Assert(
                Directory.Exists(directory),
                $"Invalid directory '{directory}' passed to package provider."
            );
        }
    }

    public UPackage GetPackage(string packageName, UnrealPackageLinker sourceLinker)
    {
        if (_Directories.Length == 0)
        {
            return sourceLinker.GetRootPackage(new UName(packageName));
        }

        // Build search patterns for each extension, e.g. "Core.u", "Core.upk"
        string[] patterns = _PackageExtensions
                            .Select(ext => packageName + ext)
                            .ToArray();

        LibServices.Trace(
            "Scanning directories [{0}] for external package '{1}' invoked by package {2}",
            _Directories.Aggregate("", (a, b) => a + "," + b),
            packageName,
            sourceLinker.Package.RootPackage
        );

        foreach (string root in _Directories)
        {
            foreach (string pattern in patterns)
            {
                // Search recursively for files matching the pattern
                var files = Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories);

                string? filePath = files.FirstOrDefault();
                if (filePath == null)
                {
                    continue;
                }

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var otherPackage = new UnrealPackage(fileStream, filePath, sourceLinker.PackageEnvironment);

                LibServices.Trace("Found import package '{0}' at '{1}'", packageName, filePath);

                var otherRootPackage = otherPackage.RootPackage;
                otherRootPackage.Package = otherPackage;

                otherPackage.NTLPackage = sourceLinker.Package.NTLPackage;

                // Load the package's header and tables
                otherPackage.Deserialize();

                otherPackage.Linker.EventEmitter = sourceLinker.EventEmitter;
                // TODO: We shouldn't have to do this, instead we should retrieve the dependency and lazy-link it as well.
                otherPackage.InitializePackage(this);

                return otherRootPackage;
            }
        }

        LibServices.Debug("Couldn't find import package '{0}'", packageName);
        foreach (string directory in _Directories)
        {
            LibServices.Debug("  Searched in '{0}'", directory);
        }

        return sourceLinker.GetRootPackage(new UName(packageName));
    }
}
