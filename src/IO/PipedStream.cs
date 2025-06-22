using System.IO;

namespace UELib.IO;

public abstract class PipedStream(Stream baseStream) : Stream
{
    public readonly Stream BaseStream = baseStream;

    /// <inheritdoc />
    public override bool CanRead => BaseStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => BaseStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => BaseStream.CanWrite;

    /// <inheritdoc />
    public override long Length => BaseStream.Length;

    /// <inheritdoc />
    public override long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    /// <inheritdoc />
    public override void Flush() => BaseStream.Flush();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) => BaseStream.SetLength(value);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int index, int count) => BaseStream.Read(buffer, index, count);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);

    /// <inheritdoc />
    protected override void Dispose(bool disposing) => BaseStream.Dispose();
}
