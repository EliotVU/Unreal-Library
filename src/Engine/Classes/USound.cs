using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements USound/Engine.Sound
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE2_5)] // Re-branded in UE3 to USoundNodeWave
    public class USound : UObject, IUnrealExportable
    {
        #region Serialized Members

        /// <summary>
        ///     The sound type of <see cref="RawData"/>, e.g. 'WAV', 'OGG', etc.
        /// </summary>
        [StreamRecord]
        public UName FileType { get; set; } = UnrealName.None;

        /// <summary>
        ///     The likelihood that this sound will be selected from an array of sounds, when part of a <see cref="USoundGroup"/>.
        ///     Null if not serialized.
        /// </summary>
        [StreamRecord]
        public float? Likelihood { get; set; }

        [StreamRecord]
        public UBulkData<byte> RawData { get; set; }

        #endregion

        public IEnumerable<string> ExportableExtensions => new List<string> { FileType };

        public USound()
        {
            ShouldDeserializeOnDemand = true;
        }

        public bool CanExport()
        {
            return RawData.StorageSize > 0;
        }

        public void SerializeExport(string desiredExportExtension, Stream exportStream)
        {
            var stream = Package.Stream;
            RawData.LoadData(stream);
            Contract.Assert(RawData.ElementData is { Length: > 0 }, "No sound data.");
            exportStream.Write(RawData.ElementData, 0, RawData.ElementData.Length);
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
#if SPLINTERCELLX
            if (stream.Build == BuildGeneration.SCX)
            {
                // Always 0x0100BB00 but the first four digits appear to increment by 1 per object;
                // -- BB00 is always the same for all sounds, but differs per package.
                // Probably representing SoundFlags
                stream.Read(out uint v30);
                stream.Record(nameof(v30), v30);

                if (stream.LicenseeVersion < 31)
                {
                    // bBinData?
                    // same as v40
                    stream.Read(out bool v04);
                    stream.Record(nameof(v04), v04);

                    // Relative path to the sound's .bin file
                    // same as v48
                    stream.Read(out string v1c);
                    stream.Record(nameof(v1c), v1c);
                }

                if (stream.LicenseeVersion >= 72)
                {
                    // 0xFFFFFFFF
                    stream.Read(out int v38);
                    stream.Record(nameof(v38), v38);
                }

                if (stream.LicenseeVersion >= 98)
                {
                    stream.Read(out UObject v3c);
                    stream.Record(nameof(v3c), v3c);
                }

                if (stream.LicenseeVersion >= 117)
                {
                    stream.Read(out int v44);
                    stream.Record(nameof(v44), v44);
                }

                if (stream.LicenseeVersion >= 81)
                {
                    // bBinData?
                    stream.Read(out bool v40);
                    stream.Record(nameof(v40), v40);

                    // Relative path to the sound's .bin file
                    stream.Read(out string v48);
                    stream.Record(nameof(v48), v48);

                    // No native serialization, maybe as a ScriptProperty
                    // bLipsyncData?
                    bool v4c = false;
                    if (v40 && v4c)
                    {
                        // lip-sync data path
                        stream.Read(out string dataPath);
                        stream.Record(nameof(dataPath), dataPath);
                    }
                }

                if (stream.LicenseeVersion > 0)
                {
                    return;
                }
            }
#endif
            FileType = stream.ReadName();
            stream.Record(nameof(FileType), FileType);
#if R6
            if (stream.Build == UnrealPackage.GameBuild.BuildName.R6RS)
            {
                // Same system as SCX
                if (FileType == "DareEvent" || FileType == "DareGen")
                {
                    if (stream.LicenseeVersion >= 3)
                    {
                        // same as SCX v30
                        stream.Read(out uint v68);
                        stream.Record(nameof(v68), v68);
                    }

                    if (stream.LicenseeVersion >= 8)
                    {
                        // bLipsyncData?
                        stream.Read(out bool v6c);
                        stream.Record(nameof(v6c), v6c);

                        // path
                        stream.Read(out string v70);
                        stream.Record(nameof(v70), v70);

                        if (v6c)
                        {
                            // Load CCompressedLipDescData...
                        }
                    }

                    return;
                }

                // Read RawData
            }
#endif
#if HP
            if (stream.Build == BuildGeneration.HP)
            {
                stream.Read(out uint flags);
                stream.Record(nameof(flags), flags);
                stream.Read(out float duration);
                stream.Record(nameof(duration), duration);

                if (stream.Version >= 77)
                {
                    stream.Read(out int numSamples);
                    stream.Record(nameof(numSamples), numSamples);
                }

                if (stream.Version >= 78)
                {
                    stream.Read(out int bitsPerSample);
                    stream.Record(nameof(bitsPerSample), bitsPerSample);
                    stream.Read(out int numChannels);
                    stream.Record(nameof(numChannels), numChannels);
                }

                if (stream.Version >= 79)
                {
                    stream.Read(out int sampleRate);
                    stream.Record(nameof(sampleRate), sampleRate);
                }
            }
#endif
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying)
            {
                if (stream.Version >= 77)
                {
                    stream.Read(out int v7c);
                    stream.Record(nameof(v7c), v7c);
                    stream.Read(out int v74);
                    stream.Record(nameof(v74), v74);
                    stream.Read(out float v6c);
                    stream.Record(nameof(v6c), v6c);
                }

                if (stream.Version >= 79)
                {
                    stream.Read(out uint v78);
                    stream.Record(nameof(v78), v78);
                    stream.Read(out float v68); // Radius?
                    stream.Record(nameof(v68), v68);
                }

                if (stream.Version >= 80)
                {
                    stream.Read(out float v80);
                    stream.Record(nameof(v80), v80);
                }

                if (stream.Version >= 82)
                {
                    stream.Read(out int v84);
                    stream.Record(nameof(v84), v84);
                }
            }
#endif
#if DEVASTATION
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Devastation
                && stream.LicenseeVersion is >= 6 and < 8)
            {
                // l14
                Likelihood = stream.ReadFloat();
                stream.Record(nameof(Likelihood), Likelihood);
            }
#endif
#if UT
            if ((stream.Build == UnrealPackage.GameBuild.BuildName.UT2004 ||
                 stream.Build == UnrealPackage.GameBuild.BuildName.UT2003)
                && stream.LicenseeVersion >= 2)
            {
                Likelihood = stream.ReadFloat();
                stream.Record(nameof(Likelihood), Likelihood);
            }
#endif
            // Resource Interchange File Format
            RawData = stream.ReadStruct<UBulkData<byte>>();
            stream.Record(nameof(RawData), RawData);
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying)
            {
                if (stream.Version >= 76)
                {
                    stream.Read(out uint v5c);
                    stream.Record(nameof(v5c), v5c);

                    int undyingExtraDataLength = stream.ReadIndex();
                    stream.Record(nameof(undyingExtraDataLength), undyingExtraDataLength);
                    if (undyingExtraDataLength > 0)
                    {
                        var undyingExtraData = new byte[undyingExtraDataLength];
                        stream.Read(undyingExtraData, 0, undyingExtraDataLength);
                        stream.Record(nameof(undyingExtraData), undyingExtraData);
                    }
                }

                if (stream.Version >= 85)
                {
                    stream.Read(out uint v8c);
                    stream.Record(nameof(v8c), v8c);
                }
            }
#endif
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if SPLINTERCELLX
            if (stream.Build == BuildGeneration.SCX)
            {
                throw new NotSupportedException("This package version is not supported!");

                if (stream.LicenseeVersion > 0)
                {
                    return;
                }
            }
#endif
            stream.Write(FileType);
#if R6
            if (stream.Build == UnrealPackage.GameBuild.BuildName.R6RS)
            {
                // Same system as SCX
                if (FileType == "DareEvent" || FileType == "DareGen")
                {
                    if (stream.LicenseeVersion >= 3)
                    {
                        throw new NotSupportedException("This package version is not supported!");
                    }

                    if (stream.LicenseeVersion >= 8)
                    {
                        throw new NotSupportedException("This package version is not supported!");
                    }

                    return;
                }
            }
#endif
#if HP
            if (stream.Build == BuildGeneration.HP)
            {
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying)
            {
                if (stream.Version >= 77)
                {
                    throw new NotSupportedException("This package version is not supported!");
                }

                if (stream.Version >= 79)
                {
                    throw new NotSupportedException("This package version is not supported!");
                }

                if (stream.Version >= 80)
                {
                    throw new NotSupportedException("This package version is not supported!");
                }

                if (stream.Version >= 82)
                {
                    throw new NotSupportedException("This package version is not supported!");
                }
            }
#endif
#if DEVASTATION
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Devastation
                && stream.LicenseeVersion is >= 6 and < 8)
            {
                // l14
                stream.Write(Likelihood.GetValueOrDefault(1.0f));
            }
#endif
#if UT
            if ((stream.Build == UnrealPackage.GameBuild.BuildName.UT2004 ||
                 stream.Build == UnrealPackage.GameBuild.BuildName.UT2003)
                && stream.LicenseeVersion >= 2)
            {
                stream.Write(Likelihood.GetValueOrDefault(1.0f));
            }
#endif
            stream.WriteStruct(RawData);
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying)
            {
                if (stream.Version >= 76)
                {
                    throw new NotSupportedException("This package version is not supported!");
                }

                if (stream.Version >= 85)
                {
                    throw new NotSupportedException("This package version is not supported!");
                }
            }
#endif
        }
    }

    /// <summary>
    ///     Implements UMusic/Engine.Music
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE1)]
    public class UMusic : USound;
}
