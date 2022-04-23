using UELib.Annotations;

namespace UELib.Decoding
{
    [PublicAPI]
    public interface IBufferDecoder
    {
        void PreDecode(IUnrealStream stream);
        void DecodeBuild(IUnrealStream stream, UnrealPackage.GameBuild build);
        void DecodeRead(long position, byte[] buffer, int index, int count);
        unsafe void DecodeByte(long position, byte* b);
    }
}
