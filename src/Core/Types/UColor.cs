using System.Drawing;
using System.Runtime.InteropServices;
using UELib.Annotations;

namespace UELib.Core.Types
{
    /// <summary>
    /// Implements FColor/UObject.Color
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UColor : IUnrealAtomicStruct
    {
        // The order may change based on compile-time constants.
        // Intel Win32 x86
        public byte B, G, R, A;
        // Non-intel
        //public byte A, R, G, B;

        [PublicAPI]
        public Color ToColor()
        {
            return Color.FromArgb(A, R, G, B);
        }
    }
}
