using System;
using System.IO;
using System.Linq;

namespace UELib
{
    public static class UnrealFile
    {
        /// <summary>
        /// The signature of an 'Unreal' package file.
        /// </summary>
        public const uint Signature = 0x9E2A83C1;

        /// <summary>
        /// The signature of an 'Unreal' package file when serialized in big-endian format.
        /// </summary>
        public const uint BigEndianSignature = 0xC1832A9E;

        public static uint GetSignature(Stream stream)
        {
            return GetSignature(stream, [Signature, BigEndianSignature]);
        }

        public static uint GetSignature(Stream stream, uint[] signatures)
        {
            byte[] buffer = new byte[4];
            int read = stream.Read(buffer, 0, 4);

            uint signature = BitConverter.ToUInt32(buffer, 0);
            // Naive and a bit slow, but this works for most standard files.
            return read == 4 && signatures.Contains(signature) ? signature : 0;
        }
    }
}
