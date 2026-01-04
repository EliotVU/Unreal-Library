using System.Diagnostics.Contracts;
using Microsoft.Extensions.FileSystemGlobbing;
using UELib.Core;
using UELib.Services;

namespace UELib;

public interface IUnrealPackageProvider
{
    /// <summary>
    ///     Resolves and returns a package by name, using the provided linker to resolve any dependencies.
    /// </summary>
    /// <param name="packageName">The package name to look for (may include the relative path to root e.g. "/Script/Engine")</param>
    /// <param name="packageLinker">The linker to use to retrieve or construct (and load the <see cref="UPackage"/>) instance.</param>
    /// <returns>The constructed and (possibly) loaded <see cref="UPackage"/>.</returns>
    public UPackage GetPackage(in UName packageName, UnrealPackageLinker packageLinker);
}

public sealed class UnrealFilePackageProvider : IUnrealPackageProvider
{
    private readonly string[] _Directories;
    private readonly string[] _PackageExtensions;

    private Dictionary<UName, List<string>>? _VirtualFileIndex;

    private readonly
#if NET5_0_OR_GREATER
        Lock
#else
        object
#endif
        _FileIndexLock = new();

    public UnrealFilePackageProvider(string[] directories, string[] packageExtensions)
    {
        _Directories = directories;
        _PackageExtensions = packageExtensions;

        foreach (string directory in _Directories)
        {
            Contract.Assert(
                Directory.Exists(directory),
                $"Invalid directory '{directory}' passed to package provider."
            );
        }

        // Include all extensions for any file.
        foreach (string extension in packageExtensions)
        {
            Contract.Assert(
                extension.StartsWith("."),
                $"Invalid extension '{extension}' passed to package provider."
            );
        }
    }

    /// <inheritdoc/>
    public UPackage GetPackage(in UName packageName, UnrealPackageLinker sourceLinker)
    {
        if (_Directories.Length == 0)
        {
            return sourceLinker.GetRootPackage(packageName);
        }

        // Make all directories accessible.
        lock (_FileIndexLock)
        {
            _VirtualFileIndex ??= BuildIndex(_Directories, _PackageExtensions);
        }

        LibServices.Trace(
            "Scanning directories [{0}] for external package '{1}' invoked by package {2}",
            _Directories.Aggregate("", (a, b) => a + "," + b),
            packageName,
            sourceLinker.Package.RootPackage
        );

        if (TryResolve(_VirtualFileIndex, packageName, out string filePath))
        {
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var otherPackage = new UnrealPackage(fileStream, filePath, sourceLinker.PackageEnvironment);

            LibServices.Trace("Found import package '{0}' at '{1}'", packageName, filePath);

            var otherRootPackage = otherPackage.RootPackage;
            otherRootPackage.Package = otherPackage;

            otherPackage.NTLPackage = sourceLinker.Package.NTLPackage;
            // Would be nice for non-auto-detected builds, but also easily fatal,
            // for some games we don't want to apply the build-specific logic to its linked packages.
            //otherPackage.Build = sourceLinker.Package.Build;
            // TODO: Branch cache, pick the same one if they are determined a 'match'
            otherPackage.Branch = sourceLinker.Package.Branch; // Could be fatal if the package is not actually compatible.

            // Load the package's header and tables
            otherPackage.Deserialize();

            otherPackage.Linker.EventEmitter = sourceLinker.EventEmitter;
            // TODO: We shouldn't have to do this, instead we should retrieve the dependency and lazy-link it as well.
            otherPackage.InitializePackage(this);

            return otherRootPackage;
        }

        LibServices.Debug("Couldn't find import package '{0}'", packageName);
        foreach (string directory in _Directories)
        {
            LibServices.Debug("  Searched in '{0}'", directory);
        }

        return sourceLinker.GetRootPackage(packageName);
    }

    private static Dictionary<UName, List<string>> BuildIndex(
        string[] directoryRoots,
        string[] includeExtensions)
    {
        var index = new Dictionary<UName, List<string>>();

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        // **/*.{u,upk,etc}
        //string fileGlobPattern = $"**/*.{{{string.Join(",", includeExtensions.Select(e => e.Substring(1)))}}}";
        //matcher.AddInclude(fileGlobPattern);
        matcher.AddIncludePatterns(includeExtensions.Select(ext => $"**/*{ext}"));

        foreach (string directoryRoot in directoryRoots)
        {
            var files = matcher.GetResultsInFullPath(directoryRoot);
            foreach (string file in files)
            {
                var key = new UName(Path.GetFileNameWithoutExtension(file));
                if (!index.TryGetValue(key, out var list))
                {
                    list = [];
                    index[key] = list;
                }

                list.Add(file);
            }
        }

        return index;
    }

    private static bool TryResolve(
        Dictionary<UName, List<string>> index,
        in UName packageName,
        out string resolvedPath)
    {
        if (!index.TryGetValue(packageName, out var matches))
        {
            resolvedPath = null;
            return false;
        }

        // Policy decision:
        // pick first, shortest, preferred folder, etc.
        resolvedPath = matches[0];
        return true;
    }
}
