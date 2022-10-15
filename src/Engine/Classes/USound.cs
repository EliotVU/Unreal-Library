using System.Collections.Generic;

namespace UELib.Core
{
    /// <summary>
    /// Implements USound/Engine.Sound
    /// </summary>
    [UnrealRegisterClass]
    public class USound : UObject, IUnrealViewable, IUnrealExportable
    {
        #region Serialized Members

        public UName FileType;

        /// <summary>
        /// The likely hood that this sound will be selected from an array of sounds, see "USoundGroup".
        /// Null if not serialized.
        /// </summary>
        public float? Likelihood;

        public byte[] Data;

        #endregion

        public IEnumerable<string> ExportableExtensions => new List<string> { FileType };

        public USound()
        {
            ShouldDeserializeOnDemand = true;
        }

        public bool CanExport()
        {
            return Package.Version >= 61 && Package.Version <= 129;
        }

        public void SerializeExport(string desiredExportExtension, System.IO.Stream exportStream)
        {
            exportStream.Write(Data, 0, Data.Length);
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            FileType = _Buffer.ReadNameReference();
            Record(nameof(FileType), FileType);
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
            _Buffer.ReadLazyArray(out Data);
            Record(nameof(Data), Data);
        }
    }
}