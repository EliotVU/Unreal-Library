using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UELib.IO;

public static class UnrealBinaryPrimitives
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadCompactIndex(ReadOnlySpan<byte> source)
    {
        int index = 0;

        byte b0 = MemoryMarshal.Read<byte>(source);
        if ((b0 & 0x40) != 0)
        {
            byte b1 = MemoryMarshal.Read<byte>(source);
            if ((b1 & 0x80) != 0)
            {
                byte b2 = MemoryMarshal.Read<byte>(source);
                if ((b2 & 0x80) != 0)
                {
                    byte b3 = MemoryMarshal.Read<byte>(source);
                    if ((b3 & 0x80) != 0)
                    {
                        byte b4 = MemoryMarshal.Read<byte>(source);
                        index = b4;
                    }

                    index = (index << 7) | (b3 & 0x7F);
                }

                index = (index << 7) | (b2 & 0x7F);
            }

            index = (index << 7) | (b1 & 0x7F);
        }

        if ((b0 & 0x80) != 0)
        {
            index *= -1;
        }

        return index;
    }
}
