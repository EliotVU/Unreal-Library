using System.Runtime.CompilerServices;
using UELib.Decoding;

namespace UELib.Branch.UE2.AA2
{
    // TODO: Re-implement as a BaseStream wrapper (in UELib 2.0)
    public class CryptoDecoderAA2 : IBufferDecoder
    {
        public void PreDecode(IUnrealStream stream)
        {
        }

        public void DecodeBuild(IUnrealStream stream, UnrealPackage.GameBuild build)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte DecryptByte(long position, byte scrambledByte)
        {
            long offsetScramble = (position >> 8) ^ position;
            scrambledByte ^= (byte)offsetScramble;
            return (offsetScramble & 0x02) != 0
                ? CryptoCore.RotateLeft(scrambledByte, 1)
                : scrambledByte;
        }

        public void DecodeRead(long position, byte[] buffer, int index, int count)
        {
            for (int i = index; i < count; ++i) buffer[i] = DecryptByte(position + i, buffer[i]);
        }

        public unsafe void DecodeByte(long position, byte* b)
        {
            *b = DecryptByte(position, *b);
        }
    }

    public class CryptoDecoderWithKeyAA2 : IBufferDecoder
    {
        public byte Key = 0x05;

        public void PreDecode(IUnrealStream stream)
        {
        }

        public void DecodeBuild(IUnrealStream stream, UnrealPackage.GameBuild build)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte DecryptByte(long position, byte scrambledByte)
        {
            long offsetScramble = (position >> 8) ^ position;
            scrambledByte ^= (byte)offsetScramble;
            if ((offsetScramble & 0x02) != 0)
            {
                if ((sbyte)scrambledByte < 0)
                    scrambledByte = (byte)((scrambledByte << 1) | 1);
                else
                    scrambledByte <<= 1;
            }

            return (byte)(Key ^ scrambledByte);
        }

        public void DecodeRead(long position, byte[] buffer, int index, int count)
        {
            for (int i = index; i < count; ++i) buffer[i] = DecryptByte(position + i, buffer[i]);
        }

        public unsafe void DecodeByte(long position, byte* b)
        {
            *b = DecryptByte(position, *b);
        }
    }
}