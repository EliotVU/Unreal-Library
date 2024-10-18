using UELib.Core;

namespace UELib.Branch.UE3.SFX.Classes
{
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.SFX)]
    public class UBioMask4Property : UProperty
    {
        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "mask4";
        }
    }
}
