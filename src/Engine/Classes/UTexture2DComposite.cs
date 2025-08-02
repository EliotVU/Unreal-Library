using System.Runtime.InteropServices;
using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture2DComposite/Engine.Texture2DComposite
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTexture2DComposite : UTexture2D
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void _Deserialize(IUnrealStream stream);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void _Serialize(IUnrealStream stream);

        public override void Deserialize(IUnrealStream stream)
        {
            // Deserialize from UTexture instead of UTexture2D
            if (stream.Version >= 600)
            {
                var ptr = typeof(UTexture).GetMethod(nameof(Deserialize)).MethodHandle.GetFunctionPointer();
                var deserializeFunc = Marshal.GetDelegateForFunctionPointer<_Deserialize>(ptr);
                deserializeFunc(stream);

                return;
            }

            base.Deserialize(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            // Serialize from UTexture instead of UTexture2D
            if (stream.Version >= 600)
            {
                var ptr = typeof(UTexture).GetMethod(nameof(Serialize)).MethodHandle.GetFunctionPointer();
                var deserializeFunc = Marshal.GetDelegateForFunctionPointer<_Serialize>(ptr);
                deserializeFunc(stream);

                return;
            }

            base.Serialize(stream);
        }
    }
}
