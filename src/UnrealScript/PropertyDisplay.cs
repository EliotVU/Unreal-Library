using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

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
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    default: literal.Append(c); break;
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
        public static string FormatLiteral(int input)
        {
            return input.ToString(CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(long input)
        {
            return input.ToString(CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatLiteral(float input)
        {
            return input.ToString(CultureInfo.InvariantCulture);
        }
    }
}