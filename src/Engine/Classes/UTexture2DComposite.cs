using System.Runtime.InteropServices;
using UELib.Branch;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture2DComposite/Engine.Texture2DComposite
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTexture2DComposite : UTexture2D // Actually UTexture, but we need to keep this inheritance for older builds.
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // Deserialize from UTexture instead of UTexture2D
            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassAdded)
            {
                DeserializeBase(stream, typeof(UTexture));

                return;
            }

            base.Deserialize(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            // Serialize from UTexture instead of UTexture2D
            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassAdded)
            {
                SerializeBase(stream, typeof(UTexture));

                return;
            }

            base.Serialize(stream);
        }
    }
}
