using System.Text;

#if !NETFRAMEWORK
using static System.Text.CodePagesEncodingProvider;
#endif

namespace UELib
{
    public static class UnrealEncoding
    {
#if NETFRAMEWORK
        // ReSharper disable once InconsistentNaming
        public static readonly Encoding ANSI = Encoding.GetEncoding("Windows-1252");
#else
        // ReSharper disable once InconsistentNaming
        private static Encoding? s_ANSI;

        // ReSharper disable once InconsistentNaming
        public static Encoding ANSI
        {
            get
            {
                if (s_ANSI != null)
                    return s_ANSI;

                // Provide 1252 encoding (ANSI) for .NET Core and .NET 5+
                Encoding.RegisterProvider(Instance);
                s_ANSI = Encoding.GetEncoding(1252);

                return s_ANSI;
            }
        }
#endif
        public static readonly Encoding Unicode = Encoding.Unicode;
    }
}
