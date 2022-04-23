using System.Runtime.CompilerServices;
using UELib.Annotations;

namespace UELib.Decoding
{
    [PublicAPI]
    public static class CryptoCore
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte RotateRight(byte value, int count)
        {
            return (byte)((byte)(value >> count) | (byte)(value << (8 - count)));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort RotateRight(ushort value, int count)
        {
            return (ushort)((ushort)(value >> count) | (ushort)(value << (16 - count)));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte RotateLeft(byte value, int count)
        {
            return (byte)((value << count) | (value >> (8 - count)));
        }
    }
}