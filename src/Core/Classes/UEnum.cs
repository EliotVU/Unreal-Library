using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UEnum/Core.Enum
    /// </summary>
    [UnrealRegisterClass]
    public partial class UEnum : UField
    {
        #region Serialized Members

        /// <summary>
        ///     Enum tags (or members) of this enum.
        /// </summary>
        [StreamRecord]
        public UArray<UName> Names { get; set; } = [];

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Names = stream.ReadNameArray();
            stream.Record(nameof(Names), Names);
#if SPELLBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn
                && 145 < stream.Version)
            {
                uint unknownEnumFlags = stream.ReadUInt32();
                stream.Record(nameof(unknownEnumFlags), unknownEnumFlags);
            }
#endif
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.WriteArray(Names);
#if SPELLBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn
                && 145 < stream.Version)
            {
                uint unknownEnumFlags = 0;
                stream.Write(unknownEnumFlags);
            }
#endif
        }
    }
}
