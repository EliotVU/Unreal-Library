using UELib.Flags;

namespace UELib.Core
{
    [UnrealRegisterClass]
    public class UScriptStruct : UStruct
    {
        #region Constructors

        protected override void Deserialize()
        {
            base.Deserialize();
            StructFlags = _Buffer.ReadUInt32();
            Record(nameof(StructFlags), (StructFlags)StructFlags);
            DeserializeProperties();
        }

        #endregion
    }
}