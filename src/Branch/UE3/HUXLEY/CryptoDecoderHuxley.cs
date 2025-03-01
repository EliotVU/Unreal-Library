using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using UELib.Core;
using UELib.Decoding;
using UELib.Annotations;

namespace UELib.Branch.UE3.HUXLEY
{
    public class CryptoDecoderHuxley : IBufferDecoder
    {
        private uint Key;

        public CryptoDecoderHuxley(string name)
        {
            for (var i = 0; i < name.Length; i++)
            {
                Key *= 16;
                Key ^= name[i];
            }
        }

        public CryptoDecoderHuxley(uint key)
        {
            Key = key;
        }

        public void PreDecode(IUnrealStream stream)
        {
        }

        public void DecodeBuild(IUnrealStream stream, UnrealPackage.GameBuild build)
        {
        }

        public void DecodeRead(long position, byte[] buffer, int index, int count)
        {
            for (var i = index; i + 4 <= count; i += 4)
            {
                for (var j = 0; j < 4; j++)
                    buffer[i + j] = (byte)(Key >> (j * 8) ^ buffer[i + j]);

                for (var j = 0; j < 4; j++)
                    buffer[i + j] = (byte)(count >> (j * 8) ^ buffer[i + j]);
            }
        }

        public unsafe void DecodeByte(long position, byte* b)
        {
        }
    }
}