using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FColor/UObject.Color
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct UColor : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        // The order may change based on compile-time constants.

        // <Intel-byte-order Win32 x86>
        public byte B, G, R, A;     // UE2, UE3
        //public byte R, G, B, A;   // UE1

        // <Intel-byte-order Linux x86>
        //public byte B, G, R, A;   // UE3

        // <Non-intel-byte-order>
        //public byte A, R, G, B;   // UE2, UE3
        //public byte A, B, G, R;   // UE1

        public UColor(byte b, byte g, byte r, byte a)
        {
            B = b;
            G = g;
            R = r;
            A = a;
        }

        // FIXME: RGBA UE1, UE2, UE3..
        // Always packed as one Int32 (order BGRA for Intel-byte-order) if serialized in bulk, and non bulk for later UE3 builds.
        // Packed as RGBA for UE1 unless not build intel-byte-order.
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
    }
}