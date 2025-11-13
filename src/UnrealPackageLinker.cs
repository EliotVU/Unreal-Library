using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public void OnAdd(UObject obj);
}

public sealed class UnrealPackageLinker(UnrealPackage package, IUnrealPackageProvider packageProvider)
{
    private readonly UnrealObjectContainer _ObjectContainer = package.Archive.Environment.ObjectContainer;

    public IUnrealPackageEventEmitter? EventEmitter { get; set; }

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

    internal UPackage GetRootPackage(string packageName)
    {
        return FindObject<UPackage>(packageName) ?? CreateObject<UPackage>(packageName);
    }

    private UPackage ImportPackage(in UName packageName)
    {
        var pkg = FindObject<UPackage?>(packageName);
        return pkg?.Package == null ? packageProvider.GetPackage(packageName, this) : pkg;
    }

    // TODO: Cache result
    private ulong[] GetInternalObjectFlagsMap()
    {
        return package.Branch.EnumFlagsMap[typeof(ObjectFlag)];
    }

    // TODO: Cache result
    private ulong[] GetInternalClassFlagsMap()
    {
        return package.Branch.EnumFlagsMap[typeof(ClassFlag)];
    }

    /// <summary>
    /// Returns a <see cref="UObject"/> from a package index.
    /// </summary>
    public T? IndexToObject<T>(int packageIndex)
        where T : UObject
    {
        switch (packageIndex)
        {
            case < 0:
                {
                    var import = package.Imports[-packageIndex - 1];
                    return (T)import.Object ?? (T)CreateObject(import);
                }

            case > 0:
                {
                    var export = package.Exports[packageIndex - 1];
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
            LibServices.Debug("Root import {0}", import.GetReferencePath());

            var pkg = ImportPackage(objName);
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

        LibServices.Debug("Creating imposter {0}", import.GetReferencePath());

        var internalClassType = objClass.InternalType;
        Debug.Assert(internalClassType != null, "Internal class for imposter should not be null");

        var obj = (UObject)Activator.CreateInstance(internalClassType);
        Debug.Assert(obj != null);

        if (internalClassType == typeof(UClass))
        {
            internalClassType = typeof(UnknownObject);

            LibServices.Debug("Binding internal type {0} for imposter user-class {1}", internalClassType.ToString(), objName);
            ((UClass)obj).InternalType = internalClassType;
        }

        import.Object = obj;

        var objFlags = new UnrealFlags<ObjectFlag>(GetInternalObjectFlagsMap(), ObjectFlag.Public);

        obj.Name = objName;
        obj.ObjectFlags = objFlags;
        obj.Package = package;
        obj.PackageIndex = -(import.Index + 1);
        obj.PackageResource = import;
        obj.Class = objClass;
        obj.Outer = objOuter;

        _ObjectContainer.Add(obj);
        EventEmitter?.OnAdd(obj);

        return obj;
    }

    internal UObject CreateObject(UExportTableItem export)
    {
        Debug.Assert(export.Object == null);

        var objName = export.ObjectName;
        if (export.OuterIndex.IsNull && (export.ExportFlags & (uint)ExportFlags.ForcedExport) != 0)
        {
            LibServices.Debug("Root export {0}", export.GetReferencePath());

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

        var objOuter = IndexToObject<UObject?>(export.OuterIndex) ?? package.RootPackage;
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

            LibServices.Debug("Binding internal type {0} for user-class {1}", internalClassType.ToString(), objName);
            ((UClass)obj).InternalType = internalClassType;
        }

        export.Object = obj;

        obj.InternalFlags |= objClass.InternalFlags & InternalClassFlags.Inherit;
        obj.Name = objName;
        obj.ObjectFlags = objFlags;
        obj.Package = package;
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
        EventEmitter?.OnAdd(obj);

        if (objClass.InternalFlags.HasFlag(InternalClassFlags.Preload) || obj is UField)
        {
            // FIXME: Stackoverflow on UT99
            //Preload(obj);
        }

        return obj;
    }

    public T CreateObject<T>(in UName name, UClass @class, UObject? outer = null)
        where T : UObject, new()
    {
        var newObject = new T
        {
            Package = package,
            PackageIndex = UPackageIndex.Null,

            Name = name,
            Class = @class,
            Outer = outer
        };

        _ObjectContainer.Add(newObject);
        EventEmitter?.OnAdd(newObject);

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
    public T? FindObject<T>(string name, UClass? @class)
        where T : UObject
    {
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
    public T? FindObject<T>(in UName name, UObject? outer, UClass? @class)
        where T : UObject
    {
        return outer == null
            ? _ObjectContainer.Find<T>(name.GetHashCode(), @class)
            : _ObjectContainer.Find<T>(name.GetHashCode(), outer.Name.GetHashCode(), @class);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<T> EnumerateObjects<T>()
        where T : UObject
    {
        return _ObjectContainer.Enumerate<T>();
    }

    /// <summary>
    /// Preloads all exports in the package that have the <see cref="InternalClassFlags.Preload"/> flag set.
    /// </summary>
    public void Preload(InternalClassFlags flags = InternalClassFlags.Preload)
    {
        var exports = package.Exports;

        LibServices.Debug("Loading exports for '{0}'", package.FullPackageName);

        foreach (var exp in exports)
        {
            // Skip deleted classes.
            if (exp.ObjectName == UnrealName.None)
            {
                continue;
            }

            try
            {
                var obj = exp.Object ?? CreateObject(exp);
            }
            catch (Exception exception)
            {
                LibServices.LogService.SilentException(new UnrealException($"Couldn't create object for export '{exp}'", exception));
            }
        }

        foreach (var obj in exports
                            .Select(exp => exp.Object)
                            .Where(obj => obj != null))
        {
            // Already loaded, skip.
            if (obj.DeserializationState != default)
            {
                continue;
            }

            if ((obj.Class.InternalFlags & flags) != 0)
            {
                Preload(obj);
            }
        }
    }

    public void Preload(UObject obj)
    {
        LibServices.Debug("Preloading {0}", obj.GetReferencePath());

        obj.Load();
    }
}
