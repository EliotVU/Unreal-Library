using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using UELib.Core;

namespace UELib;

// <summary>
///     An <see cref="UObject"/> container that allows for fast lookups by name.
/// </summary>
public sealed class UnrealObjectContainer : IDisposable
{
    /// <summary>
    ///     All persistent objects, mapped by hash
    /// </summary>
    private readonly Dictionary<int, ObjectLink> _ObjectNameHashMap = new(1024);

    private readonly Dictionary<int, ObjectLink> _ObjectNameOuterHashMap = new(1024);

    /// <summary>
    ///     Enumerates over all indexed objects.
    /// </summary>
    public IEnumerable<UObject> Enumerate()
    {
        foreach (var obj in _ObjectNameHashMap.Values.SelectMany(EnumerateObjectLink))
        {
            yield return obj;
        }
    }

    /// <summary>
    ///     Enumerates over all indexed objects that belong to the package.
    /// </summary>
    public IEnumerable<UObject> Enumerate(UnrealPackage package)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(package, nameof(package));
#endif

        foreach (var obj in _ObjectNameHashMap.Values.SelectMany(EnumerateObjectLink))
        {
            if (obj.Package != package)
            {
                continue;
            }

            yield return obj;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UObject? Find(in UName objectName)
    {
        return Find(objectName.GetHashCode());
    }

    internal UObject? Find(int objectNameHash)
    {
        _ObjectNameHashMap.TryGetValue(objectNameHash, out var objectLink);

        return EnumerateObjectLink(objectLink)
            .FirstOrDefault(obj => obj.Name.GetHashCode() == objectNameHash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? Find<T>(in UName objectName)
        where T : UObject
    {
        return Find<T>(objectName.GetHashCode());
    }

    internal T? Find<T>(int objectNameHash)
        where T : UObject
    {
        _ObjectNameHashMap.TryGetValue(objectNameHash, out var objectLink);

        return EnumerateObjectLink(objectLink)
            .OfType<T>()
            .FirstOrDefault(obj => obj.Name.GetHashCode() == objectNameHash);
    }

    internal T? Find<T>(int objectNameHash, UClass @class)
        where T : UObject
    {
        _ObjectNameHashMap.TryGetValue(objectNameHash, out var objectLink);

        return (T?)EnumerateObjectLink(objectLink)
            // FIXME: For if we are looking for a class object by using a static UClass.
            .Where(obj => ((Type?)@class == typeof(UClass) && obj.GetType() == typeof(UClass)) || obj.InheritsStaticClass(@class))
            .FirstOrDefault(obj => obj.Name.GetHashCode() == objectNameHash);
    }

    public T? Find<T>(in UName objectName, in UName outerName)
        where T : UObject
    {
        int objectNameHash = objectName.GetHashCode();
        _ObjectNameOuterHashMap.TryGetValue(objectNameHash ^ outerName.GetHashCode(), out var objectLink);

        return EnumerateObjectLink(objectLink)
            .OfType<T>()
            .FirstOrDefault(obj => obj.Name == objectNameHash);
    }

    internal T? Find<T>(int objectNameHash, int outerNameHash, UClass @class)
        where T : UObject
    {
        _ObjectNameOuterHashMap.TryGetValue(objectNameHash ^ outerNameHash, out var objectLink);

        return (T?)EnumerateObjectLink(objectLink)
            // FIXME: For if we are looking for a class object by using a static UClass.
            .Where(obj => ((Type?)@class == typeof(UClass) && obj.GetType() == typeof(UClass)) || obj.InheritsStaticClass(@class))
            .FirstOrDefault(obj => obj.Name.GetHashCode() == objectNameHash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<UObject> EnumerateObjectLink(ObjectLink? objectLink)
    {
        for (var link = objectLink; link != null; link = link.Next)
        {
            yield return link.Object;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(in UName objectName) => _ObjectNameHashMap.ContainsKey(objectName.GetHashCode());

#if NET8_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
    public void Add(UObject newObject)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(newObject, nameof(newObject));
#endif
        int objectHash = newObject.GetHashCode();

        if (_ObjectNameHashMap.TryGetValue(objectHash, out var objectLink))
        {
            // Already linked up?
            var hashedObject = EnumerateObjectLink(objectLink).FirstOrDefault(obj => obj == newObject);
            if (hashedObject != null)
            {
                return;
            }

            // Swap the link
            var newObjectLink = new ObjectLink(newObject, objectLink);
            _ObjectNameHashMap[objectHash] = newObjectLink;
        }
        else
        {
            _ObjectNameHashMap.Add(objectHash, new ObjectLink(newObject, null));
        }

        if (newObject.Outer == null)
        {
            return;
        }

        objectHash ^= newObject.Outer.GetHashCode();
        if (_ObjectNameOuterHashMap.TryGetValue(objectHash, out objectLink))
        {
            // Already linked up?
            var hashedObject = EnumerateObjectLink(objectLink).FirstOrDefault(obj => obj == newObject);
            if (hashedObject != null)
            {
                return;
            }

            // Swap the link
            var newObjectLink = new ObjectLink(newObject, objectLink);
            _ObjectNameOuterHashMap[objectHash] = newObjectLink;
        }
        else
        {
            _ObjectNameOuterHashMap.Add(objectHash, new ObjectLink(newObject, null));
        }
    }

    public void Remove(UObject disposedObject)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(disposedObject, nameof(disposedObject));
#endif

        int objectHash = disposedObject.GetHashCode();
        UnlinkFromMap(_ObjectNameHashMap, objectHash, disposedObject);

        if (disposedObject.Outer == null)
        {
            return;
        }

        int outerHash = objectHash ^ disposedObject.Outer.GetHashCode();
        UnlinkFromMap(_ObjectNameOuterHashMap, outerHash, disposedObject);

        return;

        static void UnlinkFromMap(Dictionary<int, ObjectLink> map, int key, UObject target)
        {
            var objectLink = map[key];
            if (objectLink.Object == target)
            {
                if (objectLink.Next == null)
                {
                    map.Remove(key);

                    return;
                }

                // Unshift
                map[key] = new ObjectLink(objectLink.Next.Object, objectLink.Next.Next);

                return;
            }

            do
            {
                if (objectLink.Next.Object == target)
                {
                    // Skip over it.
                    objectLink.Next = objectLink.Next.Next;

                    return;
                }

                objectLink = objectLink.Next;
            } while (objectLink != null);

            throw new InvalidOperationException("Failed to remove the object from its object link.");
        }
    }

    private sealed class ObjectLink(UObject @object, ObjectLink? next)
    {
        public UObject Object => @object;
        public ObjectLink? Next { get; internal set; } = next;
    }

    public void Dispose()
    {
        Trace.WriteLine($"Disposing of {nameof(UnrealObjectContainer)} objects");

        var objectsToDisposeOf = Enumerate().ToArray();
        foreach (var obj in objectsToDisposeOf)
        {
            obj.Dispose();

            if (obj.PackageResource != null)
            {
                obj.PackageResource.Object = null;
            }
        }

        _ObjectNameHashMap.Clear();
        _ObjectNameOuterHashMap.Clear();
    }

    public void Dispose(UnrealPackage package)
    {
        Trace.WriteLine($"Disposing of {nameof(UnrealObjectContainer)} objects for package {package.FullPackageName}");

        var objectsToDisposeOf = Enumerate(package).ToArray();
        foreach (var obj in objectsToDisposeOf)
        {
            obj.Dispose();

            if (obj.PackageResource != null)
            {
                obj.PackageResource.Object = null;
            }

            // Remove one by one
            Remove(obj);
        }
    }
}
