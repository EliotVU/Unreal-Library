using UELib.Core;

namespace UELib.Branch.UE3.SFX.Classes
{
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.SFX)]
    public class UStringRefProperty : UProperty
    {
        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "strref";
        }
    }
}
