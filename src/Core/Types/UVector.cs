using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    /// Implements FVector/UObject.Vector
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
    }
}