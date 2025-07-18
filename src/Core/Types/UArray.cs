using System.Collections.Generic;

namespace UELib.Core;

/// <summary>
///     Implements TArray.
///     A derived class of List to help with the serialization of Unreal arrays.
///     Typically, an array is serialized with the count as a CompactInteger (<see cref="IUnrealStream.ReadIndex" />) (Or
///     Int32 in UE3 builds)
///     Followed by a series of that array's element type. For example, for an array of objects, the data would be a series
///     of CompactIntegers.
///     But for a struct type such as FColor, the data is not referenced, but inlined directly in an atomic order.
///     To read an array of objects (referenced by index):
///     <example>
///         UArray<<see cref="UObject" />> objects;
///         stream.ReadArray(out objects);
///     </example>
///     For an array of structs (inlined), you will have to declare a new struct and implement the
///     <see cref="IUnrealSerializableClass"></see> interface
///     <example>
///         UArray<<see cref="UFont.FontCharacter" />> characters;
///         stream.ReadArray(out characters);
///     </example>
/// </summary>
public class UArray<T> : List<T>
{
    public UArray()
    {
    }

    public UArray(int capacity) : base(capacity)
    {
    }

    public UArray(IEnumerable<T> collection) : base(collection)
    {
    }

    public override string ToString()
    {
        return $"<{typeof(T).Name}>[{Count}]";
        //return $"[{string.Join(",", this.Select(t => t.ToString()))}]";
    }
}