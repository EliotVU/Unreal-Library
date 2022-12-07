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
        private delegate void _Deserialize();
        
        protected override void Deserialize()
        {
            // Deserialize from UTexture instead of UTexture2D
            if (_Buffer.Version >= 600)
            {
                var ptr = typeof(UTexture).GetMethod(nameof(Deserialize)).MethodHandle.GetFunctionPointer();
                var deserializeFunc = Marshal.GetDelegateForFunctionPointer<_Deserialize>(ptr);
                deserializeFunc();
                return;
            }

            base.Deserialize();
        }
    }
}
