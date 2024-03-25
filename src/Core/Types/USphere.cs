using System.Runtime.InteropServices;
using UELib.Branch;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FSphere/UObject.Sphere
    ///     Extends Plane, but we can't do this in C#
    ///     Cannot be serialized in bulk due the addition of the "W" field.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct USphere : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public float W, X, Y, Z;

        public USphere(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public USphere(ref UVector v, float w)
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
            if (stream.Version >= (uint)PackageObjectLegacyVersion.SphereExtendsPlane)
            {
                stream.Read(out W);
            }
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(X);
            stream.Write(Y);
            stream.Write(Z);
            if (stream.Version >= (uint)PackageObjectLegacyVersion.SphereExtendsPlane)
            {
                stream.Write(W);
            }
        }
    }
}