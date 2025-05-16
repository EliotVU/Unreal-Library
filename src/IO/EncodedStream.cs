using System;
using System.IO;
using UELib.Decoding;

namespace UELib.IO;

/// <summary>
/// </summary>
internal sealed class EncodedStream(Stream baseStream, IBufferDecoder decoder, long basePosition = 0)
    : PipedStream(baseStream)
{
    public override int Read(byte[] buffer, int index, int count)
    {
        long absolutePosition = Position + basePosition;
        int byteCount = BaseStream.Read(buffer, index, count);

        decoder.DecodeRead(absolutePosition, buffer, index, count);

        return byteCount;
    }

    public override int ReadByte()
    {
        unsafe
        {
            byte[] buffer = new byte[1];
            long absolutePosition = Position + basePosition;
            int byteCount = BaseStream.Read(buffer, 0, 1);
            if (byteCount == 0)
            {
                return -1;
            }

            fixed (byte* ptr = &buffer[0])
            {
                decoder.DecodeByte(absolutePosition, ptr);

                return *ptr;
            }
        }
    }

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException("Cannot write to an encoded stream");
}
