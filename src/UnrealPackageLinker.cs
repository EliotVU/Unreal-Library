using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

// TODO: Remove the @Environment from the archive and pass it to this linker instead.
// Or require it to be assigned to the UnrealPackage?
public sealed class UnrealPackageLinker(UnrealPackage package, UnrealPackageEnvironment packageEnvironment, IUnrealPackageProvider? packageProvider = null)
{
    public UnrealPackage Package { get; } = package;
    public UnrealPackageEnvironment PackageEnvironment { get; } = packageEnvironment;

    public IUnrealPackageProvider? PackageProvider { get; set; } = packageProvider;
    public IUnrealPackageEventEmitter? EventEmitter { get; set; }

    private InternalClassFlags _InternalPreloadFlags = InternalClassFlags.Default;
    private readonly UnrealObjectContainer _ObjectContainer = packageEnvironment.ObjectContainer;

#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _PreloadLock = new();
#else
    private readonly object _PreloadLock = new();
#endif

    public UClass GetStaticClass<T>()
        where T : UObject
    {
        // FIXME: Stupid workaround
        return GetStaticClass(new UName(typeof(T).Name.Substring(1)));
        var classAttr = typeof(T).GetCustomAttribute<UnrealClassAttribute>();
        Debug.Assert(classAttr != null, "Internal type is missing the static class attribute.");

        var classPackage = FindObject<UPackage>(classAttr.ClassPackageName);
        var @class = FindObject<UClass>(classAttr.ClassName, classPackage);
        Debug.Assert(@class != null, $"Couldn't find the static class for internal type in package {classPackage}.");

        return @class;
    }

    public UClass GetStaticClass(in UName className)
    {
        return FindObject<UClass>(className) ?? throw new ArgumentNullException();
    }

    public UPackage GetRootPackage(in UName packageName)
    {
        // Caveat: "Core" and "Engine" will return the integrated root packages
        return FindObject<UPackage>(packageName) ?? CreateObject<UPackage>(packageName);
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

        var externalRootPackage = FindObject<UPackage?>(packageName);
        bool isPackageLinked = externalRootPackage?.Package != null;
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
    public T? IndexToObject<T>(UPackageIndex packageIndex)
        where T : UObject
    {
        switch (packageIndex)
        {
            case < 0:
                {
                    var import = Package.Imports[packageIndex.ImportIndex];
                    return (T)import.Object ?? (T)CreateObject(import);
                }

            case > 0:
                {
                    var export = Package.Exports[packageIndex.ExportIndex];
                    return (T)export.Object ?? (T)CreateObject(export);
                }

            default:
                return null;
        }
    }

    private UObject CreateObject(UImportTableItem import)
    {
        Debug.Assert(import.Object == null);

        var objName = import.ObjectName;
        if (import.OuterIndex.IsNull)
        {
            Trace.WriteLine($"Root import {import.GetReferencePath()}");

            var pkg = ImportExternalRootPackage(objName);
            import.Object = pkg;

            return pkg;
        }

        var classPackage = FindObject<UPackage?>(import.ClassPackageName);
        //Debug.Assert(classPackage != null, $"Couldn't find package '{import.ClassPackageName}'");

        var importClass = FindObject<UClass?>(import.ClassName, classPackage);
        if (classPackage != null)
        {
            if (importClass == null)
            {
                LibServices.Debug($"Couldn't find import class '{import.ClassName}' in '{classPackage.GetPath()}'");
            }
            //Debug.Assert(importClass != null, $"Couldn't find class '{import.ClassName}' in package '{classPackage.GetPath()}'");
        }

        var objClass = importClass ?? GetStaticClass(UnrealName.Class);

        // Two-way imports (Occurs for UT2004 and perhaps any other game too)
        //Debug.Assert(import.OuterIndex.IsExport, "Found an import with an export outer!");

        var objOuter = IndexToObject<UObject?>(import.OuterIndex);
        var crossObject = FindObject<UObject>(objName, objOuter, objClass);
        if (crossObject != null)
        {
            if (objClass.Name != UnrealName.Class)
            {
                Debug.Assert(
                    crossObject.InheritsStaticClass(objClass),
                    $"Mismatched object {crossObject.GetReferencePath()} with class {objClass.GetReferencePath()} '{objName}'"
                );
            }

            //LibServices.Debug("Cross-reference found {0} for {1}", crossObject.GetReferencePath(), package.RootPackage.GetReferencePath());
            import.Object = crossObject;

            return crossObject;
        }

        Trace.WriteLine($"Creating imposter {import.GetReferencePath()}");

        var internalClassType = objClass.InternalType;
        Debug.Assert(internalClassType != null, "Internal class for imposter should not be null");

        var obj = (UObject)Activator.CreateInstance(internalClassType);
        Debug.Assert(obj != null);

        if (internalClassType == typeof(UClass))
        {
            internalClassType = typeof(UnknownObject);

            Trace.WriteLine($"Binding internal type {internalClassType.ToString()} for imposter user-class {objName}");
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

        _ObjectContainer.Add(obj);
        EventEmitter?.OnAdded(obj);

        return obj;
    }

    internal UObject CreateObject(UExportTableItem export)
    {
        Debug.Assert(export.Object == null);

        var objName = export.ObjectName;
        if (export.OuterIndex.IsNull && (export.ExportFlags & (uint)ExportFlags.ForcedExport) != 0)
        {
            Trace.WriteLine($"Root export {export.GetReferencePath()}");

            var pkg = FindObject<UPackage?>(objName);
            if (pkg == null)
            {
                pkg = CreateObject<UPackage>(objName);
                pkg.PackageIndex = export.Index + 1;
                pkg.PackageResource = export;
            }

            export.Object = pkg;

            return pkg;
        }

        var objFlags = new UnrealFlags<ObjectFlag>(export.ObjectFlags, GetInternalObjectFlagsMap());
        var objClass = export.ClassIndex
            ? IndexToObject<UClass>(export.ClassIndex)
            // User-class, see if we have an internal class for it.
            : GetStaticClass(UnrealName.Class);

        // Perhaps the external user-class no longer exists?
        if (objClass == null)
        {
            LibServices.LogService.Log($"Failed to resolve class for export {export.GetReferencePath()}");
            objClass = GetStaticClass(UnrealName.Class);
        }

        var objOuter = IndexToObject<UObject?>(export.OuterIndex) ?? Package.RootPackage;
        // May occur if the outer was preloaded and has a dependency on this export.
        if (export.Object != null)
        {
            return export.Object;
        }

        var internalClassType = objClass.InternalType;
        Debug.Assert(internalClassType != null, $"Internal type should not be null {objClass.GetReferencePath()} {objClass.Class.GetReferencePath()}");

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
        }

        var obj = (UObject)Activator.CreateInstance(internalClassType);
        Debug.Assert(obj != null);

        if (export.ClassIndex.IsNull)
        {
            // Hacky solution for binding, let's say we have a user-class 'Engine.Palette' and the internal class type 'UELib.Engine.UPalette'.
            // Then we wish to construct the user-class 'Palette' as a 'UELib.Core.UClass' type, but any instance of it as the internal class type 'UPalette'.
            // TODO: Find by outer? (We want to only match Engine.Palette to UELib.UPalette)
            var internalClass = FindObject<UClass?>(objName); // Either a static class or a user-class with an internal type.

            // Safety guard, in case we mismatch a user-class instead of the intrinsic static class.
            bool hasIntrinsicClass = internalClass != null && internalClass.InternalFlags.HasFlag(InternalClassFlags.Intrinsic);
            internalClassType = hasIntrinsicClass
                ? (Type)internalClass
                // for instances of a user-class, like say MatchInfo'Engine.CTFMatchInfo1' should instantiate as 'UObject'.
                // UnknownObject?
                : typeof(UObject);

            Debug.Assert(internalClassType != null, $"Failed to resolve internal type for user-class {objName}; internal class {internalClass?.GetReferencePath()}");

            Trace.WriteLine($"Binding internal type {internalClassType.ToString()} for user-class {objName}");
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

        _ObjectContainer.Add(obj);
        EventEmitter?.OnAdded(obj);

        if (objClass.InternalFlags.HasFlag(_InternalPreloadFlags))
        {
            // FIXME: Stackoverflow on UT99
            //Preload(obj);
        }

        return obj;
    }

    public T CreateObject<T>(in UName name, UClass @class, UObject? outer = null)
        where T : UObject, new()
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(@class, nameof(@class));
#endif

        var newObject = new T
        {
            Package = Package,
            PackageIndex = UPackageIndex.Null,

            Name = name,
            Class = @class,
            Outer = outer
        };

        _ObjectContainer.Add(newObject);
        EventEmitter?.OnAdded(newObject);

        return newObject;
    }

    public UClass CreateObject<T>(in UName name, UClass? super, ClassFlag[] classFlagIndices, UObject? outer = null)
        where T : UClass, new()
    {
        var @class = CreateObject<T>(name, GetStaticClass(UnrealName.Class), outer);
        @class.Super = super;
        @class.ClassFlags = new UnrealFlags<ClassFlag>(GetInternalClassFlagsMap(), classFlagIndices);

        return @class;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UClass CreateObject<T>(string name, UClass? super, ClassFlag[] classFlagIndices, UObject? outer = null)
        where T : UClass, new()
    {
        return CreateObject<T>(new UName(name), super, classFlagIndices, outer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T CreateObject<T>(string name, UClass @class, UObject? outer = null)
        where T : UObject, new()
    {
        return CreateObject<T>(new UName(name), @class, outer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T CreateObject<T>(string name, UObject? outer = null)
        where T : UObject, new()
    {
        return CreateObject<T>(new UName(name), outer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T CreateObject<T>(in UName name, UObject? outer = null)
        where T : UObject, new()
    {
        var @class = GetStaticClass<T>();
        return CreateObject<T>(name, @class, outer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? FindObject<T>(string name)
        where T : UObject
    {
        return _ObjectContainer.Find<T>(IndexName.ToIndex(name));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? FindObject<T>(in UName name)
        where T : UObject
    {
        return _ObjectContainer.Find<T>(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? FindObject<T>(string name, UClass @class)
        where T : UObject
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(@class, nameof(@class));
#endif

        return _ObjectContainer.Find<T>(IndexName.ToIndex(name), @class);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? FindObject<T>(in UName name, UObject? outer)
        where T : UObject
    {
        return outer == null
            ? _ObjectContainer.Find<T>(name)
            : _ObjectContainer.Find<T>(name, outer.Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? FindObject<T>(in UName name, UObject? outer, UClass @class)
        where T : UObject
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(@class, nameof(@class));
#endif

        return outer == null
            ? _ObjectContainer.Find<T>(name.GetHashCode(), @class)
            : _ObjectContainer.Find<T>(name.GetHashCode(), outer.Name.GetHashCode(), @class);
    }

    /// <summary>
    /// Enumerates over all objects that belong to this linker associated package.
    /// </summary>
    /// <typeparam name="T">The class type to constrain the search to.</typeparam>
    /// <returns>Enumerable objects</returns>
    public IEnumerable<T> EnumerateObjects<T>()
        where T : UObject
    {
        return _ObjectContainer
            .Enumerate()
            .OfType<T>()
            .Where(obj => obj.Package == Package);
    }

    public void PreloadExports(InternalClassFlags loadFlags = InternalClassFlags.Preload)
    {
        Contract.Assert(loadFlags != InternalClassFlags.Default);

        lock (_PreloadLock)
        {
            _InternalPreloadFlags = loadFlags;

            // And load!
            ConstructExports();
        }
    }

    public void PreloadExport(UExportTableItem export, InternalClassFlags loadFlags = InternalClassFlags.Preload)
    {
        Contract.Assert(export.Object == null);
        Contract.Assert(loadFlags != InternalClassFlags.Default);

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
        LibServices.Debug("Loading exports for '{0}'", Package.FullPackageName);

        foreach (var exp in exports)
        {
            try
            {
                var obj = exp.Object ?? CreateObject(exp);
            }
            catch (Exception exception)
            {
                LibServices.LogService.SilentException(new UnrealException($"Couldn't create object for export '{exp}'", exception));
            }
        }
    }
    
    /// <summary>
    /// Loads all exported objects in the package that have been constructed.
    /// </summary>
    public void LoadExports() =>
        LoadExports(Package
            .Exports
            .Select(exp => exp.Object)
            .Where<UObject>(obj => obj != null));
    
    /// <summary>
    /// Loads all exported objects in the package that have been constructed and that satisfy the load flags.
    /// </summary>
    public void LoadExports(InternalClassFlags loadFlags)
    {
        Contract.Assert(loadFlags != InternalClassFlags.Default);

        var exports = Package
            .Exports
            .Select(exp => exp.Object)
            .Where<UObject>(obj => obj != null && (obj.Class.InternalFlags & loadFlags) != 0);
        LoadExports(exports);
    }

    /// <summary>
    /// Loads all enumerable objects that have not yet been loaded.
    /// </summary>
    public void LoadExports(IEnumerable<UObject> exportObjects)
    {
        foreach (var obj in exportObjects)
        {
            // Already loaded, skip.
            if (obj.DeserializationState != default || obj.PackageResource == null)
            {
                continue;
            }

            LoadObject(obj);
        }
    }

    public void LoadObject(UObject obj)
    {
        obj.Load();
        EventEmitter?.OnLoaded(obj);
    }
}
