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
