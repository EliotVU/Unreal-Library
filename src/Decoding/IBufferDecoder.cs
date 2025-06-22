using System;

namespace UELib.Decoding
{
    public interface IBufferDecoder
    {
        [Obsolete("Deprecated", true)]
        void PreDecode(IUnrealStream stream);

        [Obsolete("Deprecated", true)]
        void DecodeBuild(IUnrealStream stream, UnrealPackage.GameBuild build);

        void DecodeRead(long position, byte[] buffer, int index, int count);
        unsafe void DecodeByte(long position, byte* b);
    }
}
