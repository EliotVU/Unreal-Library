using UELib.Decoding;

namespace UELib.Branch.UE3.HUXLEY
{
    public class CryptoDecoderHuxley : IBufferDecoder
    {
        private readonly uint _Key;

        public CryptoDecoderHuxley(string name)
        {
            for (int i = 0; i < name.Length; i++)
            {
                _Key *= 16;
                _Key ^= name[i];
            }
        }

        public CryptoDecoderHuxley(uint key) => _Key = key;

        public void PreDecode(IUnrealStream stream)
        {
        }

        public void DecodeBuild(IUnrealStream stream, UnrealPackage.GameBuild build)
        {
        }

        public void DecodeRead(long position, byte[] buffer, int index, int count)
        {
            for (int i = index; i + 4 <= count; i += 4)
            {
                for (int j = 0; j < 4; j++)
                {
                    buffer[i + j] = (byte)((_Key >> (j * 8)) ^ buffer[i + j]);
                }

                for (int j = 0; j < 4; j++)
                {
                    buffer[i + j] = (byte)((count >> (j * 8)) ^ buffer[i + j]);
                }
            }
        }

        public unsafe void DecodeByte(long position, byte* b)
        {
        }
    }
}
