using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UMultiFont/Engine.MultiFont
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UMultiFont : UFont
    {
        #region Serialized Members

        /// <summary>
        ///     A list of resolutions that map to a given set of font pages.
        /// </summary>
        [StreamRecord, UnrealProperty]
        public UArray<float>? ResolutionTestTable { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (stream.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                ResolutionTestTable = stream.ReadFloatArray();
                stream.Record(nameof(ResolutionTestTable), ResolutionTestTable);
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (stream.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                stream.WriteArray(ResolutionTestTable);
            }
        }
    }
}
