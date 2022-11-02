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
        public float[,] M;

        public UMatrix(float[,] m)
        {
            M = m;
        }

        public UMatrix(ref UPlane x, ref UPlane y, ref UPlane z, ref UPlane w)
        {
            M = new float[4, 4];
            M[0, 0] = x.X; M[0, 1] = x.Y; M[0, 2] = x.Z; M[0, 3] = x.W;
            M[1, 0] = y.X; M[1, 1] = y.Y; M[1, 2] = y.Z; M[1, 3] = y.W;
            M[2, 0] = z.X; M[2, 1] = z.Y; M[2, 2] = z.Z; M[2, 3] = z.W;
            M[3, 0] = w.X; M[3, 1] = w.Y; M[3, 2] = w.Z; M[3, 3] = w.W;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out M[0, 0]); stream.Read(out M[0, 1]); stream.Read(out M[0, 2]); stream.Read(out M[0, 3]);
            stream.Read(out M[1, 0]); stream.Read(out M[1, 1]); stream.Read(out M[1, 2]); stream.Read(out M[1, 3]);
            stream.Read(out M[2, 0]); stream.Read(out M[2, 1]); stream.Read(out M[2, 2]); stream.Read(out M[2, 3]);
            stream.Read(out M[3, 0]); stream.Read(out M[3, 1]); stream.Read(out M[3, 2]); stream.Read(out M[3, 3]);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(M[0, 0]); stream.Write(M[0, 1]); stream.Write(M[0, 2]); stream.Write(M[0, 3]);
            stream.Write(M[1, 0]); stream.Write(M[1, 1]); stream.Write(M[1, 2]); stream.Write(M[1, 3]);
            stream.Write(M[2, 0]); stream.Write(M[2, 1]); stream.Write(M[2, 2]); stream.Write(M[2, 3]);
            stream.Write(M[3, 0]); stream.Write(M[3, 1]); stream.Write(M[3, 2]); stream.Write(M[3, 3]);
        }

        //public static unsafe explicit operator Matrix4x4(UMatrix u)
        //{
        //    return *(Matrix4x4*)&u;
        //}

        //public static unsafe explicit operator UMatrix(Matrix4x4 m)
        //{
        //    return *(UMatrix*)&m;
        //}

        //public override int GetHashCode()
        //{
        //    return ((Matrix4x4)this).GetHashCode();
        //}
    }
}