using UELib.Branch.UE3.SFX.Tokens;
using UELib.Core.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.SFX
{
    public class EngineBranchSFX : DefaultEngineBranch
    {
        public EngineBranchSFX(BuildGeneration generation) : base(generation)
        {
        }

        public override void Setup(UnrealPackage linker)
        {
            base.Setup(linker);

            // FIXME: Temporary workaround
            if (linker.LicenseeVersion == 1008)
            {
                linker.Summary.LicenseeVersion = 112;
            }
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            SetupSerializer<PackageSerializerSFX>();
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);
            tokenMap[0x4F] = typeof(StringRefConstToken);
            // IfLocal
            tokenMap[0x63] = typeof(UnresolvedToken);
            // IfInstance
            tokenMap[0x64] = typeof(UnresolvedToken);
            // NamedFunction
            tokenMap[0x65] = typeof(UnresolvedToken);
            return tokenMap;
        }
    }
}
