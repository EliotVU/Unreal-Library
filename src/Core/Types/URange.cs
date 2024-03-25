using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FRange/UObject.Range
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct URange : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public float A, B;

        public URange(float a, float b)
        {
            A = a;
            B = b;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out A);
            stream.Read(out B);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(A);
            stream.Write(B);
        }
    }
}