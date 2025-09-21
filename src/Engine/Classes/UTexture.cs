using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture/Engine.Texture
    /// </summary>
    [UnrealRegisterClass]
    public class UTexture : UBitmapMaterial
    {
        public bool HasComp;

        [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE2_5)]
        public UArray<LegacyMipMap>? Mips;

        [BuildGeneration(BuildGeneration.UE3)]
        public UBulkData<byte> SourceArt;

        protected override void Deserialize()
        {
            base.Deserialize();

            // This kind of data was moved to UTexture2D
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.UE3)
            {
#if BORDERLANDS2 || BATTLEBORN
                if (this is not UTexture2D && (
                        Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                        Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn))
                {
                    return;
                }

                if (Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn &&
                    _Buffer.LicenseeVersion >= 47)
                {
                    return;
                }
#endif
                _Buffer.Read(out SourceArt);
                Record(nameof(SourceArt), SourceArt);
                return;
            }

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.CompMipsDeprecated)
            {
                var bHasCompProperty = Properties.Find("bHasComp");
                if (bHasCompProperty != null)
                {
                    HasComp = bool.Parse(bHasCompProperty.Value);
                    if (HasComp)
                    {
                        _Buffer.ReadArray(out UArray<LegacyMipMap> oldMips);
                        Record(nameof(oldMips), oldMips);
                    }
                }
            }

            _Buffer.ReadArray(out Mips);
            Record(nameof(Mips), Mips);
        }
#if BATTLEBORN
        // Deserializer for UTextureBaseGBX (the base for UTexture types starting with version 47)
        protected void DeserializeTextureBaseGbx(IUnrealStream stream)
        {
            // < 49
            // << constantGuid

            // >= 47
            // TextureBaseGBX.TextureArray
            int count = stream.ReadInt32();
            var v70 = new UArray<UArray<UTexture3D.MipMap3D>>(count);
            for (int i = 0; i < count; i++)
            {
                int c = stream.ReadInt32();
                var mips = new UArray<UTexture3D.MipMap3D>(c);
                for (int j = 0; j < c; ++j)
                {
                    var element = new UTexture3D.MipMap3D();
                    stream.Read(out element.Data);
                    stream.Read(out element.SizeX);
                    stream.Read(out element.SizeY);

                    if (stream.LicenseeVersion >= 47)
                    {
                        stream.Read(out element.SizeZ); // v50
                        stream.Read(out int v54);
                        stream.Read(out int v58);
                        stream.Read(out byte v5c);
                    }

                    mips.Add(element);
                }

                v70.Add(mips);
            }

            stream.Record(nameof(v70), v70);
            // else << mips

            // << v80 (ResourceInterface?)
            if (stream.LicenseeVersion >= 52)
            {
                // 2 for T2D, 3 for T3D.
                byte textureType = stream.ReadByte();
                stream.Record(nameof(textureType), textureType);

                // incomplete...
            }

            // < 49
            // << TextureFileCacheGuid
            // < 47
            // << CachedPVRTCMips
        }
#endif
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
