using System.Collections.Generic;

namespace UELib.Core
{
    /// <summary>
    /// Implements TMap.
    ///
    /// A derived class of Dictionary to help with the serialization of Unreal maps.
    ///
    /// <example>
    /// UMap<<see cref="UName" />, <see cref="UMap" />> objMap;
    /// stream.ReadMap(out objMap);
    /// </example>
    /// </summary>
    public class UMap<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public UMap() : base()
        {
            
        }
        
        public UMap(int capacity) : base(capacity)
        {
            
        }

        public override string ToString()
        {
            return $"<{typeof(TKey).Name}, {typeof(TValue).Name}>[{Count}]";
        }
    }
}
