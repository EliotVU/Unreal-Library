using System;
using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FGuid/UObject.Guid
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UGuid : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public uint A, B, C, D;

        public UGuid(uint a, uint b, uint c, uint d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out A);
            stream.Read(out B);
            stream.Read(out C);
            stream.Read(out D);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(A);
            stream.Write(B);
            stream.Write(C);
            stream.Write(D);
        }

        public static unsafe explicit operator Guid(UGuid u)
        {
            return *(Guid*)&u;
        }

        public static unsafe explicit operator UGuid(Guid m)
        {
            return *(UGuid*)&m;
        }

        public override int GetHashCode()
        {
            return ((Guid)this).GetHashCode();
        }

        public override string ToString()
        {
            return $"{A:X8}{B:X8}{C:X8}{D:X8}";
        }
    }
}