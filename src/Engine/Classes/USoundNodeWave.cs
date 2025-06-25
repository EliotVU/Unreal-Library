using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UELib.Branch;

namespace UELib.Core
{
    /// <summary>
    ///     Implements USoundNodeWave/Engine.SoundNodeWave
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USoundNodeWave : USoundNode, IUnrealExportable
    {
        // UnrealProperty
        public float Volume = 0.25f;
        public float Pitch = 1.0f;
        public float Duration;

        // Serialized//UnrealProperty

        public UArray<int> ChannelOffsets;
        public UArray<int> ChannelSizes;
        public int ChannelCount;

        public UName AudioFileCacheName;
        public UGuid AudioFileCacheGuid;

        #region Serialized Members

        // Serialized
        public UName FileType;
        public UBulkData<byte> RawData;

        // What the hell was Epic thinking with all these?

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedPCSoundData"/>
        /// </summary>
        public UBulkData<byte> CompressedPCData;

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedXenonSoundData"/>
        /// </summary>
        public UBulkData<byte> CompressedXbox360Data; // CachedCookedXbox360Data in older UnrealScript.

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedPS3SoundData"/>
        /// </summary>
        public UBulkData<byte> CompressedPS3Data;

        public UBulkData<byte> CompressedDingoData;

        /// <summary>
        /// PS4?
        /// </summary>
        public UBulkData<byte> CompressedOrbisData;

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedWiiUSoundData"/>
        /// </summary>
        public UBulkData<byte> CompressedWiiUData;

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedIPhoneSoundData"/>
        /// </summary>
        public UBulkData<byte> CompressedIPhoneData;

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedFlashSoundData"/>
        /// </summary>
        public UBulkData<byte> CompressedFlashData;

        #endregion

        public IEnumerable<string> ExportableExtensions => new List<string> { FileType };

        public bool CanExport()
        {
            // Checking for != -1 means that RawData has been deserialized, but does not mean that there is any data.
            return Package.Stream != null && RawData.StorageSize != -1 && GetExportableAudioData().ElementCount > 0;
        }

        private UBulkData<byte> GetExportableAudioData()
        {
            var exportData = RawData;
            if (exportData.ElementCount != 0)
            {
                return exportData;
            }

            if (CompressedPCData.ElementCount > 0)
            {
                exportData = CompressedPCData;
            }
            else if (CompressedXbox360Data.ElementCount > 0)
            {
                exportData = CompressedXbox360Data;
            }
            else if (CompressedPS3Data.ElementCount > 0)
            {
                exportData = CompressedPS3Data;
            }
            else if (CompressedDingoData.ElementCount > 0)
            {
                exportData = CompressedDingoData;
            }
            else if (CompressedOrbisData.ElementCount > 0)
            {
                exportData = CompressedOrbisData;
            }
            else if (CompressedWiiUData.ElementCount > 0)
            {
                exportData = CompressedWiiUData;
            }
            else if (CompressedIPhoneData.ElementCount > 0)
            {
                exportData = CompressedIPhoneData;
            }
            else if (CompressedFlashData.ElementCount > 0)
            {
                exportData = CompressedFlashData;
            }

            return exportData;
        }

        public void SerializeExport(string desiredExportExtension, System.IO.Stream exportStream)
        {
            var stream = Package.Stream;

            var exportData = GetExportableAudioData();
            exportData.LoadData(stream);
            Contract.Assert(exportData.ElementData is { Length: > 0 }, "No sound data.");
            exportStream.Write(exportData.ElementData, 0, exportData.ElementData.Length);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (stream.Version < (uint)PackageObjectLegacyVersion.AddedPCSoundData)
            {
                stream.Write(FileType);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedChannelsSoundInfo &&
                stream.Version < (uint)PackageObjectLegacyVersion.DisplacedSoundChannelProperties)
            {
                stream.Write(ChannelOffsets); //v7c
                stream.Write(ChannelSizes); //v88
            }

            stream.Write(ref RawData);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedChannelCountSoundInfo &&
                stream.Version < (uint)PackageObjectLegacyVersion.DisplacedSoundChannelProperties)
            {
                stream.Write(ChannelCount); //v70
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPCSoundData)
            {
                stream.Write(ref CompressedPCData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedXenonSoundData)
            {
                stream.Write(ref CompressedXbox360Data);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPS3SoundData)
            {
                stream.Write(ref CompressedPS3Data);
            }
#if DD2
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.DD2)
            {
                if (stream.LicenseeVersion >= 11)
                {

                }

                if (stream.LicenseeVersion >= 12)
                {

                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= 686)
                {
                    stream.Write(ref CompressedDingoData);
                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= 688)
                {
                    stream.Write(ref CompressedOrbisData);
                }
            }
#endif
#if BORDERLANDS
            if (stream.Package.Build == BuildGeneration.GB)
            {
                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDingoSoundData)
                {
                    stream.Write(ref CompressedDingoData);
                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedOrbisSoundData)
                {
                    stream.Write(ref CompressedOrbisData);
                }
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedWiiUSoundData)
            {
                stream.Write(ref CompressedWiiUData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedIPhoneSoundData)
            {
                stream.Write(ref CompressedIPhoneData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedFlashSoundData)
            {
                stream.Write(ref CompressedFlashData);
            }
#if BORDERLANDS
            if (stream.Package.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 43)
            {
                stream.WriteStruct(ref AudioFileCacheGuid);
            }
#endif
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (stream.Version < (uint)PackageObjectLegacyVersion.AddedPCSoundData)
            {
                stream.Read(out FileType);
                stream.Record(nameof(FileType), FileType);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedChannelsSoundInfo &&
                stream.Version < (uint)PackageObjectLegacyVersion.DisplacedSoundChannelProperties)
            {
                stream.Read(out ChannelOffsets); //v7c
                stream.Record(nameof(ChannelOffsets), ChannelOffsets);

                stream.Read(out ChannelSizes); //v88
                stream.Record(nameof(ChannelSizes), ChannelSizes);
            }

            stream.Read(out RawData);
            stream.Record(nameof(RawData), RawData);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedChannelCountSoundInfo &&
                stream.Version < (uint)PackageObjectLegacyVersion.DisplacedSoundChannelProperties)
            {
                stream.Read(out ChannelCount); //v70
                stream.Record(nameof(ChannelCount), ChannelCount);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPCSoundData)
            {
                stream.Read(out CompressedPCData);
                stream.Record(nameof(CompressedPCData), CompressedPCData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedXenonSoundData)
            {
                stream.Read(out CompressedXbox360Data);
                stream.Record(nameof(CompressedXbox360Data), CompressedXbox360Data);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPS3SoundData)
            {
                stream.Read(out CompressedPS3Data);
                stream.Record(nameof(CompressedPS3Data), CompressedPS3Data);
            }
#if DD2
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.DD2)
            {
                if (stream.LicenseeVersion >= 11)
                {

                }

                if (stream.LicenseeVersion >= 12)
                {

                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= 686)
                {
                    stream.Read(out CompressedDingoData);
                    stream.Record(nameof(CompressedDingoData), CompressedDingoData);
                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= 688)
                {
                    stream.Read(out CompressedOrbisData);
                    stream.Record(nameof(CompressedOrbisData), CompressedOrbisData);
                }
            }
#endif
#if BORDERLANDS
            if (stream.Package.Build == BuildGeneration.GB)
            {
                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDingoSoundData)
                {
                    stream.Read(out CompressedDingoData);
                    stream.Record(nameof(CompressedDingoData), CompressedDingoData);
                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedOrbisSoundData)
                {
                    stream.Read(out CompressedOrbisData);
                    stream.Record(nameof(CompressedOrbisData), CompressedOrbisData);
                }
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedWiiUSoundData)
            {
                stream.Read(out CompressedWiiUData);
                stream.Record(nameof(CompressedWiiUData), CompressedWiiUData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedIPhoneSoundData)
            {
                stream.Read(out CompressedIPhoneData);
                stream.Record(nameof(CompressedIPhoneData), CompressedIPhoneData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedFlashSoundData)
            {
                stream.Read(out CompressedFlashData);
                stream.Record(nameof(CompressedFlashData), CompressedFlashData);
            }
#if BORDERLANDS
            // Borderlands2 only has backwards compatibility for version 584 and licensee 57 or 58.
            if (stream.Package.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 43)
            {
                stream.ReadStruct(out AudioFileCacheGuid);
                stream.Record(nameof(AudioFileCacheGuid), AudioFileCacheGuid);
            }
#endif
        }
    }
}
