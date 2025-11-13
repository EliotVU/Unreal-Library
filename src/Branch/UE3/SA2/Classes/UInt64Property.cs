using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Branch.UE3.SA2.Classes
{
    [UnrealRegisterClass]
    public class UInt64Property : UProperty
    {
        public override string GetFriendlyType()
        {
            return "int64";
        }
    }
}