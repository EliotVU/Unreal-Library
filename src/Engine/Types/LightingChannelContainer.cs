using System.Runtime.InteropServices;

namespace UELib.Engine
{
    /// <summary>
    ///     See LightingChannelContainer in Engine/Classes/LightComponent.uc
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public struct LightingChannelContainer : IUnrealAtomicStruct
    {
        [MarshalAs(UnmanagedType.I1)] public bool Initialized;
        [MarshalAs(UnmanagedType.I1)] public bool BSP;
        [MarshalAs(UnmanagedType.I1)] public bool Static;
        [MarshalAs(UnmanagedType.I1)] public bool Dynamic;
    }
}