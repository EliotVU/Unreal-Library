using System.IO;
using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture2D/Engine.Texture2D
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTexture2D : UTexture
    {
        public uint SizeX, SizeY;

        public UArray<MipMap2D> Mips;

        /// <summary>
        /// PVR Texture Compression
        /// </summary>
        public UArray<MipMap2D> CachedPVRTCMips;

        /// <summary>
        /// ATI Texture Compression
        /// </summary>
        public UArray<MipMap2D> CachedATITCMips;

        /// <summary>
        /// Ericsson Texture Compression
        /// </summary>
        public UArray<MipMap2D> CachedETCMips;

        public int CachedFlashMipMaxResolution;
        public UBulkData<byte> CachedFlashMipData;

        public UGuid TextureFileCacheGuid;

        protected override void Deserialize()
        {
            base.Deserialize();
#if BATTLEBORN
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn &&
                _Buffer.LicenseeVersion >= 47) // version 47-49 is not properly supported.
            {
                DeserializeTextureBaseGbx(_Buffer);

                return;
            }
#endif
            // These properties have been moved to ScriptProperties.
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.DisplacedUTextureProperties)
            {
                _Buffer.Read(out SizeX);
                Record(nameof(SizeX), SizeX);

                _Buffer.Read(out SizeY);
                Record(nameof(SizeY), SizeY);

                _Buffer.Read(out int format);
                Format = (TextureFormat)format;
                Record(nameof(Format), Format);
            }
#if TERA
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Tera &&
                _Buffer.LicenseeVersion >= 3)
            {
                _Buffer.Read(out string fileSourcePath);
                Record(nameof(fileSourcePath), fileSourcePath);
            }
#endif
#if BORDERLANDS
            if ((_Buffer.Package.Build == BuildGeneration.GB &&
                 _Buffer.LicenseeVersion >= 55) ||
                _Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn &&
                _Buffer.LicenseeVersion < 49)
            {
                _Buffer.ReadStruct(out UGuid constantGuid);
                Record(nameof(constantGuid), constantGuid);
            }
#endif
            _Buffer.ReadArray(out Mips);
            Record(nameof(Mips), Mips);
#if BORDERLANDS || BORDERLANDS2
            if (_Buffer.Package.Build == BuildGeneration.GB ||
                _Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 /*no version check*/ ||
                (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn &&
                 _Buffer.LicenseeVersion < 49))
            {
                _Buffer.ReadStruct(out UGuid constantGuid);
                Record(nameof(constantGuid), constantGuid);
            }
#endif
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedTextureFileCacheGuidToTexture2D)
            {
                _Buffer.ReadStruct(out TextureFileCacheGuid);
                Record(nameof(TextureFileCacheGuid), TextureFileCacheGuid);
            }
#if BORDERLANDS
            if (_Buffer.Package.Build == BuildGeneration.GB &&
                _Buffer.LicenseeVersion >= 55)
            {
                return;
            }
#endif
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedPVRTCToUTexture2D)
            {
                _Buffer.ReadArray(out CachedPVRTCMips);
                Record(nameof(CachedPVRTCMips), CachedPVRTCMips);
            }
#if BORDERLANDS2 || BATTLEBORN
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 /*VR*/ ||
                _Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                // Missed UDK upgrades?
                return;
            }
#endif
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedATITCToUTexture2D)
            {
                _Buffer.Read(out CachedFlashMipMaxResolution);
                Record(nameof(CachedFlashMipMaxResolution), CachedFlashMipMaxResolution);

                _Buffer.ReadArray(out CachedATITCMips);
                Record(nameof(CachedATITCMips), CachedATITCMips);

                _Buffer.ReadStruct(out CachedFlashMipData);
                Record(nameof(CachedFlashMipData), CachedFlashMipData);
            }

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedETCToUTexture2D)
            {
                _Buffer.ReadArray(out CachedETCMips);
                Record(nameof(CachedETCMips), CachedETCMips);
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
