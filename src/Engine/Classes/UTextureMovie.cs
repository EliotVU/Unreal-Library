using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTextureMovie/Engine.TextureMovie
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTextureMovie : UTexture
    {
        public UBulkData<byte> RawData;

        protected override void Deserialize()
        {
            base.Deserialize();

            _Buffer.Read(out RawData);
            Record(nameof(RawData), RawData);
        }
    }
}
