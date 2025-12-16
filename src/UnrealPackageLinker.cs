using System.Diagnostics;
using System.Diagnostics.Contracts;
using UELib.Core;
using UELib.Flags;
using UELib.ObjectModel.Annotations;
using UELib.Services;

namespace UELib;

public interface IUnrealPackageEventEmitter
{
    public void OnAdded(UObject obj);
    public void OnLoaded(UObject obj);
}

public sealed class UnrealPackageLinker
{
    public UnrealPackage Package { get; }
    public UnrealPackageEnvironment PackageEnvironment { get; }

    public IUnrealPackageProvider? PackageProvider { get; set; }
    public IUnrealPackageEventEmitter? EventEmitter { get; set; }

    private InternalClassFlags _InternalPreloadFlags = InternalClassFlags.Default;
    private readonly UClass _StaticUserClass;

#if NET9_0_OR_GREATER
    private readonly Lock _PreloadLock = new();
#else
    private readonly object _PreloadLock = new();
#endif

    internal UnrealPackageLinker(UnrealPackage package, UnrealPackageEnvironment packageEnvironment)
    {
        Package = package;
        PackageEnvironment = packageEnvironment;
        _StaticUserClass = packageEnvironment.GetStaticClass(UnrealName.Class);
    }

    public UPackage GetRootPackage(in UName packageName)
    {
        // Caveat: "Core" and "Engine" will return the integrated root packages
        return PackageEnvironment.FindObject<UPackage?>(packageName) ?? CreateRootPackage(packageName);
    }

    private UPackage CreateRootPackage(in UName packageName)
    {
        // outer: null, because, root packages should have no outer.
        return CreateObject<UPackage>(packageName, null);
    }

    private UPackage ImportExternalRootPackage(in UName packageName)
    {
        if (PackageProvider == null)
        {
            return GetRootPackage(packageName);
        }

        // e.g. Engine.u may have a root import dependency on "Engine" (with intrinsic classes as imports)
        if (packageName == Package.RootPackage.Name)
        {
            // Switch the package (assuming that it may have been assigned to the transient package)
            Package.RootPackage.Package = Package;
            return Package.RootPackage;
        }

        var externalRootPackage = PackageEnvironment.FindObject<UPackage?>(packageName);
        bool isPackageLinked = externalRootPackage != null &&
                               externalRootPackage.Package != UnrealPackage.TransientPackage;
        return isPackageLinked
            ? externalRootPackage
            : PackageProvider.GetPackage(packageName, this);
    }

    // TODO: Cache result
    private ulong[] GetInternalObjectFlagsMap()
    {
        return Package.Branch.EnumFlagsMap[typeof(ObjectFlag)];
    }

    // TODO: Cache result
    private ulong[] GetInternalClassFlagsMap()
    {
        return Package.Branch.EnumFlagsMap[typeof(ClassFlag)];
    }

    /// <summary>
    /// Returns a <see cref="UObject"/> from a package index.
    /// </summary>
    public T IndexToObject<T>(UPackageIndex packageIndex)
        where T : UObject?
    {
        switch (packageIndex)
        {
            case < 0:
                {
                    var import = Package.Imports[packageIndex.ImportIndex];
                    return (T?)import.Object ?? (T)CreateObject(import);
                }

            case > 0:
                {
                    var export = Package.Exports[packageIndex.ExportIndex];
                    return (T?)export.Object ?? (T)CreateObject(export);
                }

            default:
                return null;
        }
    }

    internal UObject CreateObject(UImportTableItem import)
    {
        Debug.Assert(import.Object == null);

        if (import.OuterIndex.IsNull)
        {
            LibServices.Trace("Root import {0}", import);

            var otherRootPackage = ImportExternalRootPackage(import.ObjectName);
            Contract.Assert(otherRootPackage != null, "Imported root package cannot be null");

            import.Object = otherRootPackage;

            return import.Object;
        }

        // Two-way imports (Occurs for UT2004 and perhaps any other game too)
        //Debug.Assert(import.OuterIndex.IsExport, "Found an import with an export outer!");

        var objOuter = IndexToObject<UObject?>(import.OuterIndex);
        Debug.Assert(objOuter != null, $"Couldn't resolve outer for import {import}");

        // May occur if the outer was preloaded and has a dependency on this import.
        // Attested issue with "PoplarGame.upk" (when dependency packages are missing)
        if (import.Object != null)
        {
            return import.Object;
        }

        var classPackage = PackageEnvironment.FindObject<UPackage?>(import.ClassPackageName);
        //Debug.Assert(classPackage != null, $"Couldn't find package '{import.ClassPackageName}'");

        var importClass = PackageEnvironment.FindObject<UClass?>(import.ClassName, classPackage);
        if (classPackage != null)
        {
            if (importClass == null)
            {
                LibServices.Debug($"Couldn't find import class '{import.ClassName}' in '{classPackage}'");
            }
            //Debug.Assert(importClass != null, $"Couldn't find class '{import.ClassName}' in package '{classPackage}'");
        }

        var objClass = importClass ?? _StaticUserClass;

        var objName = import.ObjectName;
        var crossObject = PackageEnvironment.FindObject<UObject?>(objName, objClass, objOuter);
        if (crossObject != null)
        {
            if (objClass.Name != UnrealName.Class)
            {
                Debug.Assert(
                    crossObject.InheritsStaticClass(objClass),
                    $"Mismatched object {crossObject} with class {objClass} '{objName}'"
                );
            }

            LibServices.Trace("Cross-reference found {0} for import {1}", crossObject, import);
            import.Object = crossObject;

            return crossObject;
        }

        LibServices.Trace("Creating imposter {0}", import);

        var internalClassType = objClass.InternalType;
        Debug.Assert(internalClassType != null, "Internal class for imposter should not be null");

        var obj = (UObject)Activator.CreateInstance(internalClassType);
        Debug.Assert(obj != null);

        if (internalClassType == typeof(UClass))
        {
            internalClassType = typeof(UnknownObject);

            LibServices.Trace("Binding internal type '{0}' for imposter user-class {1}", internalClassType, objName);
            ((UClass)obj).InternalType = internalClassType;
        }

        import.Object = obj;

        var objFlags = new UnrealFlags<ObjectFlag>(GetInternalObjectFlagsMap(), ObjectFlag.Public);

        obj.Name = objName;
        obj.ObjectFlags = objFlags;
        obj.Package = Package;
        obj.PackageIndex = -(import.Index + 1);
        obj.PackageResource = import;
        obj.Class = objClass;
        obj.Outer = objOuter;

        PackageEnvironment.AddObject(obj);

        return obj;
    }

    internal UObject CreateObject(UExportTableItem export)
    {
        Debug.Assert(export.Object == null);

        var objName = export.ObjectName;
        if (export.OuterIndex.IsNull && (export.ExportFlags & (uint)ExportFlags.ForcedExport) != 0)
        {
            LibServices.Trace("Root export {0}", export);

            var pkg = PackageEnvironment.FindObject<UPackage?>(objName);
            if (pkg == null)
            {
                pkg = CreateRootPackage(objName);
                pkg.PackageIndex = export.Index + 1;
                pkg.PackageResource = export;
            }

            export.Object = pkg;

            return pkg;
        }

        var objClass = IndexToObject<UClass?>(export.ClassIndex) ?? _StaticUserClass;

        var objOuter = export.OuterIndex.IsNull
            ? Package.RootPackage
            : IndexToObject<UObject>(export.OuterIndex);
        Debug.Assert(objOuter != null, $"Couldn't resolve outer for export {export}");

        // May occur if the outer was preloaded and has a dependency on this export.
        if (export.Object != null)
        {
            return export.Object;
        }

        var internalClassType = objClass.InternalType;
        Debug.Assert(internalClassType != null, $"Internal type should not be null {objClass} {objClass.Class}");

        var objFlags = new UnrealFlags<ObjectFlag>(export.ObjectFlags, GetInternalObjectFlagsMap());
        if (internalClassType == typeof(UnknownObject))
        {
            // Try one of the "super" classes for unregistered classes.
            internalClassType = objClass
                .EnumerateSuper<UClass>()
                // Don't fall back to UObject, because we already do for UnknownObject
                .TakeWhile(cls => cls.Name != UnrealName.Object)
                .Select(cls => cls.InternalType)
                .FirstOrDefault() ?? typeof(UnknownObject);

            // HACK: Workaround for unknown UComponent types.
            if (internalClassType == typeof(UnknownObject)
                // IsTemplate Detects:
                && objName.ToString().EndsWith("Component", StringComparison.OrdinalIgnoreCase)
                && (objFlags.HasFlag(ObjectFlag.TemplateObject)
                    || export
                       .EnumerateOuter()
                       .Cast<UExportTableItem>()
                       .Any(exp => (exp.ObjectFlags & GetInternalObjectFlagsMap()[(int)ObjectFlag.TemplateObject]) != 0)
                ))
            {
                internalClassType = typeof(UComponent);
            }

            LibServices.Trace("Falling back to internal type '{0}' for export {1}", internalClassType, export);
        }

        var obj = (UObject)Activator.CreateInstance(internalClassType);
        Debug.Assert(obj != null);

        if (export.ClassIndex.IsNull)
        {
            // Hacky solution for binding, let's say we have a user-class 'Engine.Palette' and the internal class type 'UELib.Engine.UPalette'.
            // Then we wish to construct the user-class 'Palette' as a 'UELib.Core.UClass' type, but any instance of it as the internal class type 'UPalette'.
            var internalClass = PackageEnvironment.FindObject<UClass?>(objName, objOuter); // Either a static class or a user-class with an internal type.

            // Safety guard, in case we mismatch a user-class instead of the intrinsic static class.
            bool hasIntrinsicClass = internalClass?.InternalFlags.HasFlag(InternalClassFlags.Intrinsic) == true;
            internalClassType = hasIntrinsicClass
                ? internalClass.InternalType
                // for instances of a user-class, like say MatchInfo'Engine.CTFMatchInfo1' should instantiate as 'UObject'.
                // UnknownObject?
                : typeof(UObject);

            Debug.Assert(internalClassType != null, $"Failed to resolve internal type for user-class '{objName}'; internal class {internalClass}");

            LibServices.Trace("Binding internal type '{0}' for user-class {1}", internalClassType, objName);
            ((UClass)obj).InternalType = internalClassType;
        }

        export.Object = obj;

        obj.InternalFlags |= objClass.InternalFlags & InternalClassFlags.Inherit;
        obj.Name = objName;
        obj.ObjectFlags = objFlags;
        obj.Package = Package;
        obj.PackageIndex = export.Index + 1;
        obj.PackageResource = export;
        obj.Class = objClass;
        obj.Outer = objOuter;

        if (export.ArchetypeIndex)
        {
            var objArchetype = IndexToObject<UObject?>(export.ArchetypeIndex); // ?? ClassDefaultObject
            obj.Archetype = objArchetype;
        }

        if (export.SuperIndex)
        {
            if (obj is not UStruct uStruct)
            {
                LibServices.Debug("Found a non-struct export with a super index {1}", objName);
            }
            else
            {
                var objSuper = IndexToObject<UStruct?>(export.SuperIndex);
                uStruct.Super = objSuper;
            }
        }

        LibServices.Trace("Constructed {0} using internal type '{1}'", obj, internalClassType);

        PackageEnvironment.AddObject(obj);

        if (objClass.InternalFlags.HasFlag(_InternalPreloadFlags))
        {
            // FIXME: Stackoverflow on UT99
            //Preload(obj);
        }

        return obj;
    }

    public T CreateObject<T>(string name)
        where T : UObject, new()
    {
        return CreateObject<T>(new UName(name));
    }

    public T CreateObject<T>(in UName name)
        where T : UObject, new()
    {
        var @class = PackageEnvironment.GetStaticClass<T>();
        return PackageEnvironment.CreateObject<T>(Package, name, @class, Package.RootPackage);
    }

    public T CreateObject<T>(in UName name, UObject? outer)
        where T : UObject, new()
    {
        var @class = PackageEnvironment.GetStaticClass<T>();
        return PackageEnvironment.CreateObject<T>(Package, name, @class, outer);
    }

    public UClass CreateObject<T>(in UName name, UClass? super, ClassFlag[] classFlagIndices, UObject? outer)
        where T : UClass, new()
    {
        var @class = PackageEnvironment.CreateObject<T>(Package, name, _StaticUserClass, outer);
        @class.Super = super;
        @class.ClassFlags = new UnrealFlags<ClassFlag>(GetInternalClassFlagsMap(), classFlagIndices);

        return @class;
    }

    public void PreloadExports(InternalClassFlags loadFlags = InternalClassFlags.Preloadable)
    {
        Contract.Assert(loadFlags != 0);

        lock (_PreloadLock)
        {
            _InternalPreloadFlags = loadFlags;

            // And load!
            ConstructExports();
        }
    }

    public void PreloadExport(UExportTableItem export, InternalClassFlags loadFlags = InternalClassFlags.Preloadable)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(export);
#endif

        Contract.Assert(export.Object == null);
        Contract.Assert(loadFlags != 0);

        lock (_PreloadLock)
        {
            _InternalPreloadFlags = loadFlags;
            CreateObject(export);
        }
    }

    /// <summary>
    /// Constructs all exported objects in the package.
    /// </summary>
    public void ConstructExports()
    {
        ConstructExports(Package.Exports);
    }

    /// <summary>
    /// Constructs all exported objects in the package that satisfy the load flags.
    /// </summary>
    public void ConstructExports(params ObjectFlag[] loadFlagIndices)
    {
        Contract.Assert(loadFlagIndices.Length > 0, "Missing load flags");

        var loadableObjectFlags = new UnrealFlags<ObjectFlag>(GetInternalObjectFlagsMap(), loadFlagIndices);
        var loadableExports = Package
            .Exports
            .Where(exp => loadableObjectFlags.HasFlags(exp.ObjectFlags));

        ConstructExports(loadableExports);
    }

    /// <summary>
    /// Constructs all enumerable exports that have not yet been constructed.
    /// </summary>
    public void ConstructExports(IEnumerable<UExportTableItem> exports)
    {
        LibServices.Trace("Loading exports for '{0}'", Package.FullPackageName);

        foreach (var exp in exports)
        {
            if (exp.Object != null)
            {
                continue;
            }

            try
            {
                CreateObject(exp);
            }
            catch (Exception exception)
            {
                LibServices.LogService.SilentException(new UnrealException($"Couldn't create object for export {exp}", exception));
            }
        }
    }

    /// <summary>
    /// Loads all exported objects in the package that have been constructed.
    /// </summary>
    public void LoadExports() =>
        LoadExports(Package
            .Exports
            .Where(exp => exp.Object != null)
            .Select<UExportTableItem, UObject>(exp => exp.Object));

    /// <summary>
    /// Loads all exported objects in the package that have been constructed and that satisfy the load flags.
    /// </summary>
    public void LoadExports(InternalClassFlags loadFlags)
    {
        Contract.Assert(loadFlags != 0);

        var exports = Package
            .Exports
            .Where(exp => exp.Object != null && (exp.Object.Class.InternalFlags & loadFlags) != 0)
            .Select<UExportTableItem, UObject>(exp => exp.Object)
            ;
        LoadExports(exports);
    }

    /// <summary>
    /// Loads all enumerable objects that have not yet been loaded.
    /// </summary>
    public void LoadExports(IEnumerable<UObject> exportObjects)
    {
        foreach (var obj in exportObjects)
        {
            if (obj.PackageResource == null)
            {
                continue;
            }

            LoadObject(obj);
        }
    }

    public void LoadObject(UObject obj)
    {
        if (obj.DeserializationState != 0)
        {
            return;
        }

        obj.Load();
        EventEmitter?.OnLoaded(obj);
    }
}
