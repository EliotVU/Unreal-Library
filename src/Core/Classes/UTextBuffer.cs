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
#if UNDYING
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                _Buffer.Version >= 85)
            {
                int uncompressedDataSize = _Buffer.ReadIndex();
                Record(nameof(uncompressedDataSize), uncompressedDataSize);
                
                int dataLength = _Buffer.ReadIndex();
                Record(nameof(dataLength), dataLength);
                if (dataLength > 0)
                {
                    var data = new byte[uncompressedDataSize];
                    _Buffer.Read(data, 0, dataLength);
                    Record(nameof(data), data);
                }
                
                ScriptText = "Text data is compressed";
                return;
            }
#endif
            ScriptText = _Buffer.ReadText();
            Record(nameof(ScriptText), "...");
        }

        #endregion
    }
}