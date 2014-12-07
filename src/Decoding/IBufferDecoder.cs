namespace UELib.Decoding
{
    public interface IBufferDecoder
    {
        void PreDecode( IUnrealStream stream );
        void DecodeBuild( IUnrealStream stream, UnrealPackage.GameBuild build );
        int DecodeRead(byte[] array, int offset, int count);
    }
}
