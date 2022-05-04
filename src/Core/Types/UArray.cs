using System;
using System.Collections.Generic;

namespace UELib.Core
{
    /// <summary>
    /// Implements TArray.
    /// 
    /// A derived class of List to help with the serialization of Unreal arrays.
    /// 
    /// 
    /// Typically an array is serialized with the count as a CompactInteger (<see cref="IUnrealStream.ReadIndex" />) (Or Int32 in UE3 builds)
    /// Followed by a series of that array's element type. For example, for an array of objects, the data would be a series of CompactIntegers.
    /// But for a struct type such as FColor, the data is not referenced, but inlined directly in an atomic order.
    ///
    /// To read an array of objects (referenced by index):
    /// <example>
    /// UArray<<see cref="UObject" />> objects;
    /// stream.ReadArray(out objects);
    /// </example>
    ///
    /// For an array of structs (inlined), you will have to declare a new struct and implement the <see cref="IUnrealSerializableClass"></see> interface
    /// <example>
    /// UArray<<see cref="UFont.FontCharacter" />> characters;
    /// stream.ReadArray(out characters);
    /// </example>
    /// </summary>
    public class UArray<T> : List<T>
    {
        public UArray()
        {
        }

        public UArray(int capacity) : base(capacity)
        {
        }

        [Obsolete("Deprecated, see IUnrealStream.ReadArray")]
        public UArray(IUnrealStream stream)
        {
        }

        [Obsolete("Deprecated, see IUnrealStream.ReadArray")]
        public UArray(IUnrealStream stream, int count)
        {
        }

        [Obsolete]
        public void Serialize<ST>(IUnrealStream stream) where ST : T, IUnrealSerializableClass
        {
            stream.WriteIndex(Count);
            for (var i = 0; i < Count; ++i)
            {
                ((IUnrealSerializableClass)this[i]).Serialize(stream);
            }
        }

        [Obsolete]
        public void Deserialize<ST>(IUnrealStream stream) where ST : T, IUnrealSerializableClass, new()
        {
            int c = stream.ReadIndex();
            Capacity = c;
            for (var i = 0; i < c; ++i)
            {
                var item = new ST();
                item.Deserialize(stream);
                Add(item);
            }
        }

        [Obsolete]
        public void Deserialize<ST>(IUnrealStream stream, int count) where ST : T, IUnrealSerializableClass, new()
        {
            Capacity = count;
            for (var i = 0; i < count; ++i)
            {
                var item = new ST();
                item.Deserialize(stream);
                Add(item);
            }
        }

        [Obsolete]
        public void Deserialize<ST>(IUnrealStream stream, Action<ST> action) where ST : T, IUnrealSerializableClass, new()
        {
            int c = stream.ReadIndex();
            Capacity = c;
            for (var i = 0; i < c; ++i)
            {
                var item = new ST();
                action.Invoke(item);
                item.Deserialize(stream);
                Add(item);
            }
        }

        public override string ToString()
        {
            return $"<{typeof(T).Name}>[{Count}]";
            //return $"[{string.Join(",", this.Select(t => t.ToString()))}]";
        }
    }
}