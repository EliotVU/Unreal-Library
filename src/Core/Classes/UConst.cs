namespace UELib.Core
{
    /// <summary>
    ///     Implements UEnum/Core.Enum
    /// </summary>
    [UnrealRegisterClass]
    public partial class UConst : UField
    {
        #region Serialized Members

        /// <summary>
        /// The literal value of this const.
        /// </summary>
        public string Value { get; set; }

        #endregion

        #region Constructors

        protected override void Deserialize()
        {
            base.Deserialize();
            
            Value = _Buffer.ReadString();
            Record(nameof(Value), Value);
        }

        #endregion
    }
}
