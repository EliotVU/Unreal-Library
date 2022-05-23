namespace UELib.Core
{
    [UnrealRegisterClass]
    public partial class UTextBuffer : UObject
    {
        #region Serialized Members

        protected uint _Top;
        protected uint _Pos;
        public string ScriptText = string.Empty;

        #endregion

        #region Constructors

        public UTextBuffer()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();
            _Top = _Buffer.ReadUInt32();
            _Pos = _Buffer.ReadUInt32();
            ScriptText = _Buffer.ReadText();
        }

        #endregion
    }
}