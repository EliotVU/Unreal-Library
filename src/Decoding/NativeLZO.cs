using System.Runtime.InteropServices;

namespace UELib.Decoding;

public static class NativeLZO
{
    private static readonly bool _available;
    private static IntPtr _libHandle;
    private static IntPtr _decompressPtr;

    static NativeLZO()
    {
        try
        {
            string? libPath = FindLibrary();
            if (libPath == null)
            {
                Console.Error.WriteLine("  Native LZO: liblzo2 not found");
                return;
            }

            if (!NativeLibrary.TryLoad(libPath, typeof(NativeLZO).Assembly, null, out _libHandle))
            {
                Console.Error.WriteLine("  Native LZO: failed to load library");
                return;
            }

            // Try with and without leading underscore (macOS dlsym adds one)
            foreach (var name in new[] {
                "lzo1x_decompress_safe",
                "_lzo1x_decompress_safe"
            })
            {
                if (NativeLibrary.TryGetExport(_libHandle, name, out _decompressPtr))
                {
                    _available = true;
                    return;
                }
            }

            Console.Error.WriteLine("  Native LZO: decompress entry point not found");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  Native LZO: error - {ex.Message}");
        }
    }

    public static bool Available => _available;

    public static unsafe int Decompress(byte[] input, byte[] output)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<DecompressDelegate>(_decompressPtr);
        fixed (byte* src = input)
        fixed (byte* dst = output)
        {
            nuint dstLen = (nuint)output.Length;
            int result = fn(src, (nuint)input.Length, dst, &dstLen, null);
            return result == 0 ? (int)dstLen : -1;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate int DecompressDelegate(
        byte* src, nuint src_len, byte* dst, nuint* dst_len, void* wrkmem);

    private static string? FindLibrary()
    {
        // Check common env vars
        foreach (var env in new[] { "DYLD_LIBRARY_PATH", "LD_LIBRARY_PATH" })
        {
            var val = Environment.GetEnvironmentVariable(env);
            if (val != null)
            {
                foreach (var dir in val.Split(':'))
                {
                    var full = Path.Combine(dir, "liblzo2.dylib");
                    if (File.Exists(full)) return full;
                    full = Path.Combine(dir, "liblzo2.2.dylib");
                    if (File.Exists(full)) return full;
                }
            }
        }

        // Search nix store
        foreach (var nixDir in new[] { "/nix/store" })
        {
            if (!Directory.Exists(nixDir)) continue;
            try
            {
                foreach (var dir in Directory.GetDirectories(nixDir, "*lzo*"))
                {
                    var dylib = Path.Combine(dir, "lib", "liblzo2.dylib");
                    if (File.Exists(dylib)) return dylib;
                    dylib = Path.Combine(dir, "lib", "liblzo2.2.dylib");
                    if (File.Exists(dylib)) return dylib;
                }
            }
            catch { }
        }

        return null;
    }
}
