using System.Numerics;
using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FLinearColor/UObject.LinearColor
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct ULinearColor : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public float R, G, B, A;

        public ULinearColor(float r, float g, float b, float a = 1.0f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out R);
            stream.Read(out G);
            stream.Read(out B);
            stream.Read(out A);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(R);
            stream.Write(G);
            stream.Write(B);
            stream.Write(A);
        }

        public static unsafe explicit operator Vector4(ULinearColor u)
        {
            return *(Vector4*)&u;
        }

        public static unsafe explicit operator ULinearColor(Vector4 m)
        {
            return *(ULinearColor*)&m;
        }

        public override int GetHashCode()
        {
            return ((Vector4)this).GetHashCode();
        }
    }
}