using System;
using UELib.Core.Tokens;

namespace UELib.Branch.UE3.R6
{
    public class EngineBranchKeller : DefaultEngineBranch
    {
        public EngineBranchKeller(BuildGeneration generation) : base(generation)
        {
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            SetupSerializer<PackageSerializerKeller>();
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);


            return tokenMap;
        }

        [Flags]
        public enum PropertyFlagsHO : uint
        {
            // Applied keyword to any properties related to sound, usually a SoundCue, but also to a bool or component.
            SoundData     = 0x04000000,

            // Applied keyword to name properties.
            PickBone      = 0x08000000,

            // Applied keyword to name properties.
            PickAnim      = 0x10000000,
            
            // Applied keyword to int properties.
            Degree        = 0x20000000,

            // Applied keyword to object properties.
            ListInstance  = 0x40000000,

            // Applied keyword to object properties.
            NoNew         = 0x80000000,
        }
    }
}
