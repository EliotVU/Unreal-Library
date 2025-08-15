using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements USoundNodeWave/Engine.SoundNodeWave
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USoundNodeWave : USoundNode, IUnrealExportable
    {
        #region Script Properties

        [UnrealProperty]
        public float Volume { get; set; } = 0.25f;

        [UnrealProperty]
        public float Pitch { get; set; } = 1.0f;

        [UnrealProperty]
        public float Duration { get; set; }

        #endregion

        #region Serialized Members

        [StreamRecord]
        public UName FileType { get; set; } = UnrealName.None;

        [StreamRecord]
        public UBulkData<byte> RawData { get; set; }

        // What the hell was Epic thinking with all these?

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedPCSoundData"/>
        /// </summary>
        [StreamRecord]
        public UBulkData<byte> CompressedPCData { get; set; }

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedXenonSoundData"/>
        /// </summary>
        [StreamRecord]
        public UBulkData<byte> CompressedXbox360Data { get; set; } // CachedCookedXbox360Data in older UnrealScript.

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedPS3SoundData"/>
        /// </summary>
        [StreamRecord]
        public UBulkData<byte> CompressedPS3Data { get; set; }

        [StreamRecord]
        public UBulkData<byte> CompressedDingoData { get; set; }

        /// <summary>
        /// PS4?
        /// </summary>
        [StreamRecord]
        public UBulkData<byte> CompressedOrbisData { get; set; }

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedWiiUSoundData"/>
        /// </summary>
        [StreamRecord]
        public UBulkData<byte> CompressedWiiUData { get; set; }

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedIPhoneSoundData"/>
        /// </summary>
        [StreamRecord]
        public UBulkData<byte> CompressedIPhoneData { get; set; }

        /// <summary>
        /// Null if version &lt; <see cref="PackageObjectLegacyVersion.AddedFlashSoundData"/>
        /// </summary>
        [StreamRecord]
        public UBulkData<byte> CompressedFlashData { get; set; }

        [StreamRecord, UnrealProperty]
        public UArray<int> ChannelOffsets { get; set; }

        [StreamRecord, UnrealProperty]
        public UArray<int> ChannelSizes { get; set; }

        [StreamRecord, UnrealProperty]
        public int ChannelCount { get; set; }

        [StreamRecord, UnrealProperty]
        public UName AudioFileCacheName { get; set; } = UnrealName.None;

        [StreamRecord, UnrealProperty]
        public UGuid AudioFileCacheGuid { get; set; }

        #endregion

        public IEnumerable<string> ExportableExtensions => new List<string> { FileType };

        public bool CanExport()
        {
            // Checking for != -1 means that RawData has been deserialized, but does not mean that there is any data.
            return RawData.StorageSize != -1 && GetExportableAudioData().ElementCount > 0;
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

            stream.WriteStruct(RawData);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedChannelCountSoundInfo &&
                stream.Version < (uint)PackageObjectLegacyVersion.DisplacedSoundChannelProperties)
            {
                stream.Write(ChannelCount); //v70
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPCSoundData)
            {
                stream.WriteStruct(CompressedPCData);
            }
#if SA2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SA2 &&
                stream.LicenseeVersion >= 105)
            {
                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedXenonSoundData)
            {
                stream.WriteStruct(CompressedXbox360Data);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPS3SoundData)
            {
                stream.WriteStruct(CompressedPS3Data);
            }
#if DD2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DD2)
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
                    stream.WriteStruct(CompressedDingoData);
                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= 688)
                {
                    stream.WriteStruct(CompressedOrbisData);
                }
            }
#endif
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB)
            {
                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDingoSoundData)
                {
                    stream.WriteStruct(CompressedDingoData);
                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedOrbisSoundData)
                {
                    stream.WriteStruct(CompressedOrbisData);
                }
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedWiiUSoundData)
            {
                stream.WriteStruct(CompressedWiiUData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedIPhoneSoundData)
            {
                stream.WriteStruct(CompressedIPhoneData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedFlashSoundData)
            {
                stream.WriteStruct(CompressedFlashData);
            }
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 43)
            {
                stream.WriteStruct(AudioFileCacheGuid);
            }
#endif
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (stream.Version < (uint)PackageObjectLegacyVersion.AddedPCSoundData)
            {
                FileType = stream.ReadName();
                stream.Record(nameof(FileType), FileType);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedChannelsSoundInfo &&
                stream.Version < (uint)PackageObjectLegacyVersion.DisplacedSoundChannelProperties)
            {
                ChannelOffsets = stream.ReadIntArray(); //v7c
                stream.Record(nameof(ChannelOffsets), ChannelOffsets);

                ChannelSizes = stream.ReadIntArray(); //v88
                stream.Record(nameof(ChannelSizes), ChannelSizes);
            }

            RawData = stream.ReadStruct<UBulkData<byte>>();
            stream.Record(nameof(RawData), RawData);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedChannelCountSoundInfo &&
                stream.Version < (uint)PackageObjectLegacyVersion.DisplacedSoundChannelProperties)
            {
                ChannelCount = stream.ReadInt32(); //v70
                stream.Record(nameof(ChannelCount), ChannelCount);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPCSoundData)
            {
                CompressedPCData = stream.ReadStruct<UBulkData<byte>>();
                stream.Record(nameof(CompressedPCData), CompressedPCData);
            }
#if SA2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SA2 &&
                stream.LicenseeVersion >= 105)
            {
                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedXenonSoundData)
            {
                CompressedXbox360Data = stream.ReadStruct<UBulkData<byte>>();
                stream.Record(nameof(CompressedXbox360Data), CompressedXbox360Data);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedPS3SoundData)
            {
                CompressedPS3Data = stream.ReadStruct<UBulkData<byte>>();
                stream.Record(nameof(CompressedPS3Data), CompressedPS3Data);
            }
#if DD2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DD2)
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
                    CompressedDingoData = stream.ReadStruct<UBulkData<byte>>();
                    stream.Record(nameof(CompressedDingoData), CompressedDingoData);
                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= 688)
                {
                    CompressedOrbisData = stream.ReadStruct<UBulkData<byte>>();
                    stream.Record(nameof(CompressedOrbisData), CompressedOrbisData);
                }
            }
#endif
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB)
            {
                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDingoSoundData)
                {
                    CompressedDingoData = stream.ReadStruct<UBulkData<byte>>();
                    stream.Record(nameof(CompressedDingoData), CompressedDingoData);
                }

                // FIXME: Deprecation version; not attested in the latest UDK build.
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedOrbisSoundData)
                {
                    CompressedOrbisData = stream.ReadStruct<UBulkData<byte>>();
                    stream.Record(nameof(CompressedOrbisData), CompressedOrbisData);
                }
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedWiiUSoundData)
            {
                CompressedWiiUData = stream.ReadStruct<UBulkData<byte>>();
                stream.Record(nameof(CompressedWiiUData), CompressedWiiUData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedIPhoneSoundData)
            {
                CompressedIPhoneData = stream.ReadStruct<UBulkData<byte>>();
                stream.Record(nameof(CompressedIPhoneData), CompressedIPhoneData);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedFlashSoundData)
            {
                CompressedFlashData = stream.ReadStruct<UBulkData<byte>>();
                stream.Record(nameof(CompressedFlashData), CompressedFlashData);
            }
#if BORDERLANDS
            // Borderlands2 only has backwards compatibility for version 584 and licensee 57 or 58.
            if (stream.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 43)
            {
                AudioFileCacheGuid = stream.ReadStruct<UGuid>();
                stream.Record(nameof(AudioFileCacheGuid), AudioFileCacheGuid);
            }
#endif
        }
    }
}
