using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace UELib.Core
{
    /// <summary>
    ///     Represents an index to an object resource within a package.
    /// </summary>
    /// <param name="Index">the object resource index in a package.</param>
    public readonly record struct UPackageIndex(int Index)
    {
        public readonly int Index = Index;

        [Pure] public bool IsNull => Index == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(UPackageIndex index) => index.IsNull == false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(UPackageIndex index) => Unsafe.As<UPackageIndex, int>(ref index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UPackageIndex(int index) => Unsafe.As<int, UPackageIndex>(ref index);
    }
}
