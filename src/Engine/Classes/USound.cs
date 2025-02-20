using System.Collections.Generic;
using UELib.Branch;

namespace UELib.Core
{
    /// <summary>
    ///     Implements USound/Engine.Sound
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE2_5)] // Re-branded in UE3 to USoundNodeWave
    public class USound : UObject, IUnrealExportable
    {
        #region Serialized Members

        public UName FileType;

        /// <summary>
        /// The likely hood that this sound will be selected from an array of sounds, see "USoundGroup".
        /// Null if not serialized.
        /// </summary>
        public float? Likelihood;

        public UBulkData<byte> RawData;

        #endregion

        public IEnumerable<string> ExportableExtensions => new List<string> { FileType };

        public USound()
        {
            ShouldDeserializeOnDemand = true;
        }

        public bool CanExport()
        {
            return RawData.StorageSize != -1;
        }

        public void SerializeExport(string desiredExportExtension, System.IO.Stream exportStream)
        {
            exportStream.Write(RawData.ElementData, 0, RawData.ElementData.Length);
        }

        protected override void Deserialize()
        {
            base.Deserialize();

#if SPLINTERCELLX
            if (Package.Build == BuildGeneration.SCX)
            {
                // Always 0x0100BB00 but the first four digits appear to increment by 1 per object;
                // -- BB00 is always the same for all sounds, but differs per package.
                // Probably representing SoundFlags
                _Buffer.Read(out uint v30);
                Record(nameof(v30), v30);

                if (_Buffer.LicenseeVersion < 31)
                {
                    // bBinData?
                    // same as v40
                    _Buffer.Read(out bool v04);
                    Record(nameof(v04), v04);

                    // Relative path to the sound's .bin file
                    // same as v48
                    _Buffer.Read(out string v1c);
                    Record(nameof(v1c), v1c);
                }

                if (_Buffer.LicenseeVersion >= 72)
                {
                    // 0xFFFFFFFF
                    _Buffer.Read(out int v38);
                    Record(nameof(v38), v38);
                }

                if (_Buffer.LicenseeVersion >= 98)
                {
                    _Buffer.Read(out UObject v3c);
                    Record(nameof(v3c), v3c);
                }

                if (_Buffer.LicenseeVersion >= 117)
                {
                    _Buffer.Read(out int v44);
                    Record(nameof(v44), v44);
                }

                if (_Buffer.LicenseeVersion >= 81)
                {
                    // bBinData?
                    _Buffer.Read(out bool v40);
                    Record(nameof(v40), v40);

                    // Relative path to the sound's .bin file
                    _Buffer.Read(out string v48);
                    Record(nameof(v48), v48);

                    // No native serialization, maybe as a ScriptProperty
                    // bLipsyncData?
                    bool v4c = false;
                    if (v40 && v4c)
                    {
                        // lip-sync data path
                        _Buffer.Read(out string dataPath);
                        Record(nameof(dataPath), dataPath);
                    }
                }

                if (_Buffer.LicenseeVersion > 0)
                {
                    return;
                }
            }
#endif
            FileType = _Buffer.ReadNameReference();
            Record(nameof(FileType), FileType);
#if HP
            if (Package.Build == BuildGeneration.HP)
            {
                _Buffer.Read(out uint flags);
                Record(nameof(flags), flags);
                _Buffer.Read(out float duration);
                Record(nameof(duration), duration);

                if (_Buffer.Version >= 77)
                {
                    _Buffer.Read(out int numSamples);
                    Record(nameof(numSamples), numSamples);
                }

                if (_Buffer.Version >= 78)
                {
                    _Buffer.Read(out int bitsPerSample);
                    Record(nameof(bitsPerSample), bitsPerSample);
                    _Buffer.Read(out int numChannels);
                    Record(nameof(numChannels), numChannels);
                }

                if (_Buffer.Version >= 79)
                {
                    _Buffer.Read(out int sampleRate);
                    Record(nameof(sampleRate), sampleRate);
                }
            }
#endif
#if UNDYING
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Undying)
            {
                if (_Buffer.Version >= 77)
                {
                    _Buffer.Read(out int v7c);
                    Record(nameof(v7c), v7c);
                    _Buffer.Read(out int v74);
                    Record(nameof(v74), v74);
                    _Buffer.Read(out float v6c);
                    Record(nameof(v6c), v6c);
                }

                if (_Buffer.Version >= 79)
                {
                    _Buffer.Read(out uint v78);
                    Record(nameof(v78), v78);
                    _Buffer.Read(out float v68); // Radius?
                    Record(nameof(v68), v68);
                }

                if (_Buffer.Version >= 80)
                {
                    _Buffer.Read(out float v80);
                    Record(nameof(v80), v80);
                }

                if (_Buffer.Version >= 82)
                {
                    _Buffer.Read(out int v84);
                    Record(nameof(v84), v84);
                }
            }
#endif
#if DEVASTATION
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Devastation
                && _Buffer.LicenseeVersion >= 6
                && _Buffer.LicenseeVersion < 8)
            {
                _Buffer.Read(out int l14);
                Record(nameof(l14), l14);
            }
#endif
#if UT
            if ((Package.Build == UnrealPackage.GameBuild.BuildName.UT2004 ||
                 Package.Build == UnrealPackage.GameBuild.BuildName.UT2003)
                && _Buffer.LicenseeVersion >= 2)
            {
                Likelihood = _Buffer.ReadFloat();
                Record(nameof(Likelihood), Likelihood);
            }
#endif
            // Resource Interchange File Format
            _Buffer.Read(out RawData);
            Record(nameof(RawData), RawData);
#if UNDYING
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Undying)
            {
                if (_Buffer.Version >= 76)
                {
                    _Buffer.Read(out uint v5c);
                    Record(nameof(v5c), v5c);

                    int undyingExtraDataLength = _Buffer.ReadIndex();
                    Record(nameof(undyingExtraDataLength), undyingExtraDataLength);
                    if (undyingExtraDataLength > 0)
                    {
                        var undyingExtraData = new byte[undyingExtraDataLength];
                        _Buffer.Read(undyingExtraData, 0, undyingExtraDataLength);
                        Record(nameof(undyingExtraData), undyingExtraData);
                    }
                }

                if (_Buffer.Version >= 85)
                {
                    _Buffer.Read(out uint v8c);
                    Record(nameof(v8c), v8c);
                }
            }
#endif
        }
    }
}
