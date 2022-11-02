using System.Numerics;
using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FQuat/UObject.Quat
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UQuat : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public float X, Y, Z, W;

        public UQuat(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public UQuat(ref UVector v, float w)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
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

        public static unsafe explicit operator Quaternion(UQuat u)
        {
            return *(Quaternion*)&u;
        }

        public static unsafe explicit operator UQuat(Quaternion m)
        {
            return *(UQuat*)&m;
        }

        public override int GetHashCode()
        {
            return ((Quaternion)this).GetHashCode();
        }
    }
}