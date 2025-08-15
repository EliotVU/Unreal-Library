using System;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture2D/Engine.Texture2D
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTexture2D : UTexture
    {
        #region Script Members

        [StreamRecord, UnrealProperty]
        public uint SizeX { get; set; }

        [StreamRecord, UnrealProperty]
        public uint SizeY { get; set; }

        [UnrealProperty]
        public UName TextureFileCacheName { get; set; }

        #endregion

        #region Serialized Members

        [StreamRecord]
        public UArray<MipMap2D> Mips { get; set; } = [];

        /// <summary>
        /// PVR Texture Compression
        /// </summary>
        [StreamRecord]
        public UArray<MipMap2D>? CachedPVRTCMips { get; set; }

        /// <summary>
        /// ATI Texture Compression
        /// </summary>
        [StreamRecord]
        public UArray<MipMap2D>? CachedATITCMips { get; set; }

        /// <summary>
        /// Ericsson Texture Compression
        /// </summary>
        [StreamRecord]
        public UArray<MipMap2D>? CachedETCMips { get; set; }

        [StreamRecord]
        public int CachedFlashMipMaxResolution { get; set; }

        [StreamRecord]
        public UBulkData<byte> CachedFlashMipData { get; set; }

        [StreamRecord]
        public UGuid TextureFileCacheGuid { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            // These properties have been moved to ScriptProperties.
            if (stream.Version < (uint)PackageObjectLegacyVersion.DisplacedUTextureProperties)
            {
                SizeX = stream.ReadUInt32();
                stream.Record(nameof(SizeX), SizeX);

                SizeY = stream.ReadUInt32();
                stream.Record(nameof(SizeY), SizeY);

                stream.Read(out int format);
                Format = (TextureFormat)format;
                stream.Record(nameof(Format), Format);
            }
#if TERA
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Tera)
            {
                // TODO: Not yet supported.
                return;
            }
#endif
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 55)
            {
                stream.ReadStruct(out UGuid constantGuid);
                stream.Record(nameof(constantGuid), constantGuid);
            }
#endif
            var mipMap2Ds = Mips;
            stream.ReadArray(out mipMap2Ds);
            stream.Record(nameof(Mips), Mips);
#if BORDERLANDS || BORDERLANDS2
            if (stream.Build == BuildGeneration.GB ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2/*no version check*/ ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                stream.ReadStruct(out UGuid constantGuid);
                stream.Record(nameof(constantGuid), constantGuid);
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedTextureFileCacheGuidToTexture2D)
            {
                TextureFileCacheGuid = stream.ReadStruct<UGuid>();
                stream.Record(nameof(TextureFileCacheGuid), TextureFileCacheGuid);
            }
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 55)
            {
                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPVRTCToUTexture2D)
            {
                CachedPVRTCMips = stream.ReadArray<MipMap2D>();
                stream.Record(nameof(CachedPVRTCMips), CachedPVRTCMips);
            }
#if BORDERLANDS2 || BATTLEBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2/*VR*/ ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                // Missed UDK upgrades?
                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedATITCToUTexture2D)
            {
                CachedFlashMipMaxResolution = stream.ReadInt32();
                stream.Record(nameof(CachedFlashMipMaxResolution), CachedFlashMipMaxResolution);

                CachedATITCMips = stream.ReadArray<MipMap2D>();
                stream.Record(nameof(CachedATITCMips), CachedATITCMips);

                CachedFlashMipData = stream.ReadStruct<UBulkData<byte>>();
                stream.Record(nameof(CachedFlashMipData), CachedFlashMipData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedETCToUTexture2D)
            {
                CachedETCMips = stream.ReadArray<MipMap2D>();
                stream.Record(nameof(CachedETCMips), CachedETCMips);
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            // These properties have been moved to ScriptProperties.
            if (stream.Version < (uint)PackageObjectLegacyVersion.DisplacedUTextureProperties)
            {
                stream.Write(SizeX);
                stream.Write(SizeY);
                stream.Write((int)Format);
            }
#if TERA
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Tera)
            {
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 55)
            {
                stream.WriteStruct(default(UGuid)); // Placeholder, as we don't have the value here
            }
#endif
            var mipMap2Ds = Mips;
            stream.WriteArray(mipMap2Ds);
#if BORDERLANDS || BORDERLANDS2
            if (stream.Build == BuildGeneration.GB ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                stream.WriteStruct(default(UGuid)); // Placeholder, as we don't have the value here
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedTextureFileCacheGuidToTexture2D)
            {
                stream.WriteStruct(TextureFileCacheGuid);
            }
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 55)
            {
                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPVRTCToUTexture2D)
            {
                var cachedPvrtcMips = CachedPVRTCMips;
                stream.WriteArray(cachedPvrtcMips);
            }
#if BORDERLANDS2 || BATTLEBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                // Missed UDK upgrades?
                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedATITCToUTexture2D)
            {
                stream.Write(CachedFlashMipMaxResolution);
                stream.WriteArray(CachedATITCMips);
                stream.WriteStruct(CachedFlashMipData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedETCToUTexture2D)
            {
                stream.WriteArray(CachedETCMips);
            }
        }

        public struct MipMap2D : IUnrealSerializableClass
        {
            public UBulkData<byte> Data;
            public uint SizeX;
            public uint SizeY;

            public void Deserialize(IUnrealStream stream)
            {
                stream.Read(out Data);
                stream.Read(out SizeX);
                stream.Read(out SizeY);
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(ref Data);
                stream.Write(SizeX);
                stream.Write(SizeY);
            }
        }
    }
}
