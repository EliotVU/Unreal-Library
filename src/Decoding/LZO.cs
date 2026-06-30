namespace UELib.Decoding;

/// <summary>
/// LZO1X-1 decompressor for Unreal Engine 3 compressed package data (managed fallback).
/// Based on the MiniLZO reference implementation.
/// </summary>
public static class LZO
{
    public static int Decompress(byte[] input, byte[] output)
        => Decompress(input, 0, input.Length, output, 0, output.Length);

    public static int Decompress(byte[] input, int inputOffset, int inputLength,
                                  byte[] output, int outputOffset, int maxOutputLength)
    {
        int ip = inputOffset;
        int ipEnd = inputOffset + inputLength;
        int op = outputOffset;
        int opEnd = outputOffset + maxOutputLength;

        if (inputLength <= 0) return -1;

        int t = input[ip++];

        if (t > 17)
        {
            t -= 17;
            if (t < 4)
            {
                if (t != 0)
                {
                    if (!DoMatch(input, ref ip, ipEnd, output, ref op, opEnd, outputOffset, t))
                        return -1;
                }
                if (t == 0 && ip < ipEnd)
                {
                    t = input[ip++];
                    if (!DoMatch(input, ref ip, ipEnd, output, ref op, opEnd, outputOffset, t))
                        return -1;
                }
            }
            else
            {
                if (op + t > opEnd || ip + t > ipEnd) return -1;
                Buffer.BlockCopy(input, ip, output, op, t);
                ip += t;
                op += t;
            }
        }

        while (ip < ipEnd)
        {
            t = input[ip++];

            if (t >= 16)
            {
                if (!DoMatch(input, ref ip, ipEnd, output, ref op, opEnd, outputOffset, t))
                    return -1;
                continue;
            }

            if (t == 0)
            {
                while (ip < ipEnd && input[ip] == 0) { t += 255; ip++; }
                if (ip >= ipEnd) return -1;
                t += 15 + input[ip++];
            }
            t += 3;
            if (op + t > opEnd || ip + t > ipEnd) return -1;
            Buffer.BlockCopy(input, ip, output, op, t);
            ip += t;
            op += t;

            if (ip >= ipEnd) break;
            t = input[ip++];
            if (t >= 16)
            {
                if (!DoMatch(input, ref ip, ipEnd, output, ref op, opEnd, outputOffset, t))
                    return -1;
            }
            else
            {
                if (ip >= ipEnd) break;
                int m_pos = op - 1 - 0x0800 - (t >> 2) - (input[ip++] << 2);
                if (m_pos < outputOffset) return -1;
                if (op + 3 > opEnd) return -1;
                output[op] = output[m_pos];
                output[op + 1] = output[m_pos + 1];
                output[op + 2] = output[m_pos + 2];
                op += 3;
            }
        }

        return op - outputOffset;
    }

    private static bool DoMatch(byte[] input, ref int ip, int ipEnd,
                                 byte[] output, ref int op, int opEnd,
                                 int outputOffset, int t)
    {
        int m_pos;

        if (t >= 64)
        {
            if (ip >= ipEnd) return false;
            m_pos = op - 1 - ((t >> 2) & 7) - (input[ip++] << 3);
            t = (t >> 5) - 1;
            if (t < 0) t = 0;
            t += 2;
            if (op + t > opEnd || m_pos < outputOffset) return false;
            for (int i = 0; i < t; i++)
                output[op + i] = output[m_pos + i];
            op += t;
            return true;
        }

        if (t >= 32)
        {
            t &= 31;
            if (t == 0)
            {
                while (ip < ipEnd && input[ip] == 0) { t += 255; ip++; }
                if (ip >= ipEnd) return false;
                t += 31 + input[ip++];
            }
            if (ip + 2 > ipEnd) return false;
            m_pos = op - 1 - ((input[ip] | (input[ip + 1] << 8)) >> 2);
            ip += 2;
        }
        else if (t >= 16)
        {
            m_pos = op - ((t & 8) << 11);
            t &= 7;
            if (t == 0)
            {
                while (ip < ipEnd && input[ip] == 0) { t += 255; ip++; }
                if (ip >= ipEnd) return false;
                t += 7 + input[ip++];
            }
            if (ip + 2 > ipEnd) return false;
            m_pos -= (input[ip] | (input[ip + 1] << 8)) >> 2;
            ip += 2;
            if (m_pos == op) return true;
            m_pos -= 0x4000;
        }
        else
        {
            if (ip >= ipEnd) return false;
            m_pos = op - 1 - 0x0800 - (t >> 2) - (input[ip++] << 2);
            if (m_pos < outputOffset) return false;
            if (op + 3 > opEnd) return false;
            output[op] = output[m_pos];
            output[op + 1] = output[m_pos + 1];
            output[op + 2] = output[m_pos + 2];
            op += 3;
            return true;
        }

        t += 2;
        if (op + t > opEnd || m_pos < outputOffset) return false;
        for (int i = 0; i < t; i++)
            output[op + i] = output[m_pos + i];
        op += t;
        return true;
    }
}
