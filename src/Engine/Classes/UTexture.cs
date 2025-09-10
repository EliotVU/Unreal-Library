using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture/Engine.Texture
    /// </summary>
    [UnrealRegisterClass]
    public class UTexture : UBitmapMaterial
    {
        #region Serialized Members

        [StreamRecord, BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE2_5)]
        public UArray<LegacyMipMap> Mips { get; set; } = [];

        [StreamRecord, BuildGeneration(BuildGeneration.UE3)]
        public UBulkData<byte> SourceArt { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            // This kind of data was moved to UTexture2D
            if (stream.Version >= (uint)PackageObjectLegacyVersion.UE3)
            {
#if BORDERLANDS2 || BATTLEBORN
                if (this is not UTexture2D && (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                    stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn))
                {
                    return;
                }
#endif
                SourceArt = stream.ReadStruct<UBulkData<byte>>();
                stream.Record(nameof(SourceArt), SourceArt);

                return;
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.CompMipsDeprecated)
            {
                var bHasCompProperty = Properties.Find("bHasComp");
                if (bHasCompProperty != null)
                {
                    bool bHasComp = bool.Parse(bHasCompProperty.Value);
                    if (bHasComp)
                    {
                        stream.ReadArray(out UArray<LegacyMipMap> oldMips);
                        stream.Record(nameof(oldMips), oldMips);
                    }
                }
            }

            Mips = stream.ReadArray<LegacyMipMap>();
            stream.Record(nameof(Mips), Mips);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            // This kind of data was moved to UTexture2D
            if (stream.Version >= (uint)PackageObjectLegacyVersion.UE3)
            {
#if BORDERLANDS2 || BATTLEBORN
                if (this is not UTexture2D && (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                                               stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn))
                {
                    return;
                }
#endif
                stream.WriteStruct(SourceArt);

                return;
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.CompMipsDeprecated)
            {
                var bHasCompProperty = Properties.Find("bHasComp");
                if (bHasCompProperty != null)
                {
                    bool bHasComp = bool.Parse(bHasCompProperty.Value);
                    if (bHasComp)
                    {
                        stream.WriteArray(new UArray<LegacyMipMap>(0));
                    }
                }
            }

            stream.Write(Mips);
        }

        public struct LegacyMipMap : IUnrealSerializableClass
        {
            public UBulkData<byte> Data;
            public int USize;
            public int VSize;
            public byte UBits;
            public byte VBits;

            public void Deserialize(IUnrealStream stream)
            {
                stream.Read(out Data);
                stream.Read(out USize);
                stream.Read(out VSize);
                stream.Read(out UBits);
                stream.Read(out VBits);
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(ref Data);
                stream.Write(USize);
                stream.Write(VSize);
                stream.Write(UBits);
                stream.Write(VBits);
            }
        }
    }
}
