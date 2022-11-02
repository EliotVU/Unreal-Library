using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FRangeVector/UObject.RangeVector
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct URangeVector : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public URange X, Y, Z;

        public URangeVector(URange x, URange y, URange z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.ReadStruct(out X);
            stream.ReadStruct(out Y);
            stream.ReadStruct(out Y);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.WriteStruct(ref X);
            stream.WriteStruct(ref Y);
            stream.WriteStruct(ref Z);
        }
    }
}