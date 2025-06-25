using UELib.Branch;
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

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedStructFlagsToScriptStruct)
            {
                StructFlags = _Buffer.ReadFlags32<StructFlag>();
                Record(nameof(StructFlags), StructFlags);
            }

            DeserializeProperties(_Buffer);
        }

        #endregion
    }
}
