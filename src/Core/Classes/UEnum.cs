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
        public UArray<UName> Names
        {
            get => _Names;
            set => _Names = value;
        }

        private UArray<UName> _Names;

        #endregion

        #region Constructors

        protected override void Deserialize()
        {
            base.Deserialize();

            _Buffer.ReadArray(out _Names);
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
