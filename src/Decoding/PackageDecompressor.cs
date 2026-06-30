using System.IO.Compression;
using UELib.Core;
using UELib.Flags;

namespace UELib.Decoding;

/// <summary>
/// Decompresses compressed Unreal Engine package data.
/// </summary>
public static class PackageDecompressor
{
    public static byte[] DecompressPackage(string packagePath,
                                            UArray<CompressedChunk> chunks,
                                            int headerSize,
                                            uint flags)
    {
        int totalSize = headerSize;
        foreach (var chunk in chunks)
        {
            int chunkEnd = (int)(chunk.UncompressedOffset + chunk.UncompressedSize);
            if (chunkEnd > totalSize)
                totalSize = chunkEnd;
        }

        long firstCompressedOffset = chunks.Min(c => c.CompressedOffset);

        Console.WriteLine($"  Decompressing: headerSize={headerSize} total={totalSize} chunks={chunks.Count}");

        byte[] result = new byte[totalSize];

        using (var fs = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            int uncompressedLen = (int)firstCompressedOffset;
            int headerRead = fs.Read(result, 0, uncompressedLen);

            foreach (var chunk in chunks)
            {

                byte[] compressed = new byte[chunk.CompressedSize];
                fs.Seek(chunk.CompressedOffset, SeekOrigin.Begin);
                int read = fs.Read(compressed, 0, chunk.CompressedSize);
                if (read != chunk.CompressedSize)
                    Console.Error.WriteLine($"  WARNING: read {read} != {chunk.CompressedSize}");

                DecompressChunkHeader(compressed, chunk, ref result, flags);
            }
        }

        return result;
    }

    private static void DecompressChunkHeader(byte[] data, CompressedChunk chunk,
                                               ref byte[] result, uint flags)
    {
        // Parse CompressedChunkHeader (32 bytes minimum)
        var header = new CompressedChunkHeader();
        using (var ms = new MemoryStream(data, false))
        using (var reader = new BinaryReader(ms))
        {
            header.Tag = reader.ReadUInt32();
            header.ChunkSize = reader.ReadInt32();
            header.Summary.CompressedSize = reader.ReadInt32();
            header.Summary.UncompressedSize = reader.ReadInt32();

            int numBlocks = (header.Summary.UncompressedSize + header.ChunkSize - 1) / header.ChunkSize;

            var blocks = new (int compSize, int uncompSize)[numBlocks];
            for (int i = 0; i < numBlocks; i++)
            {
                blocks[i] = (reader.ReadInt32(), reader.ReadInt32());
            }

            int headerEnd = (int)ms.Position;
            int destOffset = (int)chunk.UncompressedOffset;

            int blockDataOffset = headerEnd;
            for (int i = 0; i < numBlocks; i++)
            {
                var (blockCompSize, blockUncompSize) = blocks[i];

                if (blockDataOffset + blockCompSize > data.Length)
                {
                    Console.Error.WriteLine($"    Block[{i}]: data overflow");
                    break;
                }

                byte[] blockInput = new byte[blockCompSize];
                Buffer.BlockCopy(data, blockDataOffset, blockInput, 0, blockCompSize);
                blockDataOffset += blockCompSize;

                byte[] blockOutput = new byte[blockUncompSize];
                int decompressedLen;

                if ((flags & (uint)CompressionFlags.ZLIB) != 0)
                {
                    decompressedLen = DecompressZlib(blockInput, blockOutput);
                }
                else if ((flags & (uint)CompressionFlags.ZLO) != 0)
                {
                    decompressedLen = LZODecompressor.Decompress(blockInput, blockOutput);
                }
                else
                {
                    throw new NotImplementedException($"Unsupported compression flags: 0x{flags:X}");
                }

                if (decompressedLen < 0 || decompressedLen > blockUncompSize)
                    throw new InvalidDataException(
                        $"Block[{i}] decompression failed: expected {blockUncompSize}, got {decompressedLen}");

                Buffer.BlockCopy(blockOutput, 0, result, destOffset, decompressedLen);
                destOffset += decompressedLen;
            }
        }
    }

    private static int DecompressZlib(byte[] input, byte[] output)
    {
        using var compressedStream = new MemoryStream(input, 2, input.Length - 2, false);
        using var decompressor = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream(output, true);
        decompressor.CopyTo(outputStream);
        return (int)outputStream.Position;
    }
}
