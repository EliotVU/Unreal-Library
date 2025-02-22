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

            // FIXME: Version 241 because it does not exist in R6 Vegas, nor EndWar 222
            if (_Buffer.Version > 241)
            {
                StructFlags = _Buffer.ReadUInt32();
                Record(nameof(StructFlags), (StructFlags)StructFlags);
            }

            DeserializeProperties();
        }

        #endregion
    }
}
