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
public sealed class UnrealObjectContainer
{
    /// <summary>
    ///     All persistent objects, mapped by hash
    /// </summary>
    private readonly Dictionary<int, ObjectLink> _ObjectNameHashMap = new(1024);

    private readonly Dictionary<int, ObjectLink> _ObjectNameOuterHashMap = new(1024);

    /// <summary>
    ///     Enumerates over all indexed objects.
    /// </summary>
    public IEnumerable<T> Enumerate<T>() where T : UObject
    {
        foreach (var obj in _ObjectNameHashMap.Values.SelectMany(EnumerateObjectLink))
        {
            if (obj is T tObj)
            {
                yield return tObj;
            }
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
        Debug.Assert(newObject != null, "Object cannot be null");
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

    private sealed class ObjectLink(UObject @object, ObjectLink? next)
    {
        public UObject Object => @object;

        public ObjectLink? Next => next;
    }
}
