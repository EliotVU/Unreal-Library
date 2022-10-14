using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    /// Implements FPlane/UObject.Plane
    /// 
    /// Extends Vector, but we can't do this in C#
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UPlane : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public float W, X, Y, Z;
        
        public UPlane(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        
        public UPlane(ref UVector v, float w)
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
    }
}