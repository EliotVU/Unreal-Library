using System.Numerics;
using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FVector4/UObject.Vector4
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UVector4 : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public float X, Y, Z, W;

        public UVector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out X);
            stream.Read(out Y);
            stream.Read(out Z);
            stream.Read(out W);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(X);
            stream.Write(Y);
            stream.Write(Z);
            stream.Write(W);
        }

        public static unsafe explicit operator Vector4(UVector4 u)
        {
            return *(Vector4*)&u;
        }

        public static unsafe explicit operator UVector4(Vector4 m)
        {
            return *(UVector4*)&m;
        }

        public override int GetHashCode()
        {
            return ((Vector4)this).GetHashCode();
        }
    }
}