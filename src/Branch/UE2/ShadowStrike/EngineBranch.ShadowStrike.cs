using UELib.Core;
using UELib.Core.Tokens;

namespace UELib.Branch.UE2.ShadowStrike
{
    public class EngineBranchShadowStrike : DefaultEngineBranch
    {
        public EngineBranchShadowStrike(BuildGeneration generation) : base(BuildGeneration.UE2)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);

            if (linker.Build == UnrealPackage.GameBuild.BuildName.SCDA_Online)
            {
                // TODO: All tokens
                tokenMap[0x28] = typeof(UStruct.UByteCodeDecompiler.NativeParameterToken);
            }

            return tokenMap;
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            SetupSerializer<PackageSerializerShadowStrike>();
        }
    }
}
