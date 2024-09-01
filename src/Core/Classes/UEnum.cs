namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal enum.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UEnum : UField
    {
        #region Serialized Members

        /// <summary>
        /// Names of each element in the UEnum.
        /// </summary>
        public UArray<UName> Names;

        #endregion

        #region Constructors

        protected override void Deserialize()
        {
            base.Deserialize();

            _Buffer.ReadArray(out Names);
            Record(nameof(Names), Names);
#if SPELLBORN
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn
                && 145 < _Buffer.Version)
            {
                uint unknownEnumFlags = _Buffer.ReadUInt32();
                Record(nameof(unknownEnumFlags), unknownEnumFlags);
            }
#endif
        }

        #endregion
    }
}
