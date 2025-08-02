using UELib.Branch;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTextureRenderTargetCube/Engine.TextureRenderTargetCube
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTextureRenderTargetCube : UTexture
    {
        #region Serialized Members

        [StreamRecord, UnrealProperty]
        public uint SizeX { get; set; }

        [StreamRecord, UnrealProperty]
        public uint SizeY { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (stream.Version < (uint)PackageObjectLegacyVersion.DisplacedUTextureProperties)
            {
                SizeX = stream.ReadUInt32();
                stream.Record(nameof(SizeX), SizeX);
                SizeY = stream.ReadUInt32();
                stream.Record(nameof(SizeY), SizeY);

                stream.Read(out byte format);
                Format = (TextureFormat)format;
                stream.Record(nameof(Format), Format);

                stream.Read(out int numMips);
                stream.Record(nameof(numMips), numMips);
            }
        }
        
        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (stream.Version < (uint)PackageObjectLegacyVersion.DisplacedUTextureProperties)
            {
                stream.Write(SizeX);
                stream.Write(SizeY);

                stream.Write((byte)Format);
                stream.Write(Mips.Count);
            }
        }
    }
}
