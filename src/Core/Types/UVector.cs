using System.Numerics;
using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FVector/UObject.Vector
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UVector : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public float X, Y, Z;

        public UVector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out X);
            stream.Read(out Y);
            stream.Read(out Z);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(X);
            stream.Write(Y);
            stream.Write(Z);
        }

        public static unsafe explicit operator Vector3(UVector u)
        {
            return *(Vector3*)&u;
        }

        public static unsafe explicit operator UVector(Vector3 m)
        {
            return *(UVector*)&m;
        }

        public override int GetHashCode()
        {
            return ((Vector3)this).GetHashCode();
        }

        public override string ToString()
        {
            return $"vect({X:F},{Y:F},{Z:F})";
        }
    }
}