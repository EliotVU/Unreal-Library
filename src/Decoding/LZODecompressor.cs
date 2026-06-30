namespace UELib.Decoding;

/// <summary>
/// LZO decompression dispatcher. Tries native liblzo2 first, falls back to managed.
/// </summary>
public static class LZODecompressor
{
    public static int Decompress(byte[] input, byte[] output)
    {
        if (NativeLZO.Available)
            return NativeLZO.Decompress(input, output);

        return LZO.Decompress(input, output);
    }
}
