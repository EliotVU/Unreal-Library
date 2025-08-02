using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTextureMovie/Engine.TextureMovie
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTextureMovie : UTexture
    {
        #region Serialized Members

        [StreamRecord]
        public UBulkData<byte> RawData { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            RawData = stream.ReadStruct<UBulkData<byte>>();
            stream.Record(nameof(RawData), RawData);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
            
            stream.WriteStruct(RawData);
        }
    }
}
