using UELib.Core;
using UELib.Core.Tokens;

namespace UELib.Branch.UE2.ShadowStrike
{
    public class EngineBranchShadowStrike : DefaultEngineBranch
    {
        public EngineBranchShadowStrike(BuildGeneration generation) : base(BuildGeneration.ShadowStrike)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage package)
        {
            var tokenMap = base.BuildTokenMap(package);

            if (package.Build == UnrealPackage.GameBuild.BuildName.SCDA_Online)
            {
                // TODO: All tokens
                tokenMap[0x28] = typeof(UStruct.UByteCodeDecompiler.NativeParameterToken);
            }

            return tokenMap;
        }

        protected override void SetupSerializer(UnrealPackage package)
        {
            SetupSerializer<PackageSerializerShadowStrike>();
        }
    }
}
