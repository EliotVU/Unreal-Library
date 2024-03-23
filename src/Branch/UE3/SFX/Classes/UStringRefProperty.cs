using UELib.Core;

namespace UELib.Branch.UE3.SFX.Classes
{
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.SFX)]
    public class UStringRefProperty : UProperty
    {
        protected override void Deserialize()
        {
            base.Deserialize();
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "stringref";
        }
    }
}