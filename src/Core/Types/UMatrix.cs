using System.Numerics;
using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FMatrix/UObject.Matrix
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UMatrix : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public UPlane M1, M2, M3, M4;

        public UMatrix(ref UPlane x, ref UPlane y, ref UPlane z, ref UPlane w)
        {
            M1 = x;
            M2 = y;
            M3 = z;
            M4 = w;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.ReadStruct(out M1);
            stream.ReadStruct(out M2);
            stream.ReadStruct(out M3);
            stream.ReadStruct(out M4);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.WriteStruct(ref M1);
            stream.WriteStruct(ref M2);
            stream.WriteStruct(ref M3);
            stream.WriteStruct(ref M4);
        }

        public static unsafe explicit operator Matrix4x4(UMatrix u)
        {
            return *(Matrix4x4*)&u;
        }

        public static unsafe explicit operator UMatrix(Matrix4x4 m)
        {
            return *(UMatrix*)&m;
        }

        public override int GetHashCode()
        {
            return ((Matrix4x4)this).GetHashCode();
        }
    }
}