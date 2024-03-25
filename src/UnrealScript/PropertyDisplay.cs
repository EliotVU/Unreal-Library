using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using UELib.Core;

namespace UELib.UnrealScript
{
    public static class PropertyDisplay
    {
        /// <summary>
        /// Recodes escaped characters
        /// https://stackoverflow.com/a/14087738/617087
        /// </summary>
        public static string FormatLiteral(string input)
        {
            var literal = new StringBuilder(input.Length + 2);
            literal.Append("\"");
            foreach (char c in input)
            {
                switch (c)
                {
                    case '\"':
                        literal.Append("\\\"");
                        break;
                    case '\\':
                        literal.Append(@"\\");
                        break;
                    case '\n':
                        literal.Append(@"\n");
                        break;
                    case '\r':
                        literal.Append(@"\r");
                        break;
                    default:
                        literal.Append(c);
                        break;
                }
            }

            literal.Append("\"");
            return literal.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(UName input)
        {
            return $"'{input}'";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(byte input)
        {
            return input.ToString(CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(short input)
        {
            return input.ToString("D", CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(ushort input)
        {
            return input.ToString("D", CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(int input)
        {
            return input.ToString("D", CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(uint input)
        {
            return input.ToString("D", CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(float input)
        {
            return input.ToString("F7", CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(long input)
        {
            return input.ToString("F15", CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(ulong input)
        {
            return input.ToString("F15", CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(UObject input)
        {
            return input != null 
                ? input.GetReferencePath() 
                : "none";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatExport(float input)
        {
            return input.ToString("+00000.000000;-00000.000000", CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatExport(ref UVector input)
        {
            return $"{FormatExport(input.X)}," +
                   $"{FormatExport(input.Y)}," +
                   $"{FormatExport(input.Z)}";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatOffset(short input)
        {
            return $"{input:X3}h";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatOffset(int input)
        {
            return $"{input:X4}h";
        }
    }
}
