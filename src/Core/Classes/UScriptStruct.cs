using UELib.JsonDecompiler.Flags;

namespace UELib.JsonDecompiler.Core
{
    [UnrealRegisterClass]
    public class UScriptStruct : UStruct
    {
        #region Constructors
        protected override void Deserialize()
        {
            base.Deserialize();
            StructFlags = _Buffer.ReadUInt32();
            Record( "StructFlags", (StructFlags)StructFlags );
            DeserializeProperties();
        }
        #endregion
    }
}