namespace UELib.Core
{
    [UnrealRegisterClass]
    public partial class UTextBuffer : UObject
    {
        #region Serialized Members

        public uint Top;
        public uint Pos;
        
        public string ScriptText;

        #endregion

        #region Constructors

        public UTextBuffer()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();
            
            Top = _Buffer.ReadUInt32();
            Record(nameof(Top), Top);
            Pos = _Buffer.ReadUInt32();
            Record(nameof(Pos), Pos);
            ScriptText = _Buffer.ReadText();
            Record(nameof(ScriptText), "...");
        }

        #endregion
    }
}