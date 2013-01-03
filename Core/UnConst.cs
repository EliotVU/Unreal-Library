namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal const. 
    /// </summary>
    [UnrealRegisterClass]
    public partial class UConst : UField
    {
        #region Serialized Members
        /// <summary>
        /// Constant Value
        /// </summary>
        public string Value
        {
            get;
            private set;
        }
        #endregion

        #region Constructors
        protected override void Deserialize()
        {
            base.Deserialize();

            // Size:BYTES:\0
            Value = _Buffer.ReadString().Replace( "\"", "\\\"" )
                .Replace( "\\", "\\\\" )
                .Replace( "\n", "\\n" )
                .Replace( "\r", "\\r" );
        }
        #endregion
    }
}
