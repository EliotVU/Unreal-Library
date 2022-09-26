using UELib.Branch.UE3.RSS.Tokens;
using UELib.Core.Tokens;

namespace UELib.Branch.UE3.RSS
{
    public class EngineBranchRSS : DefaultEngineBranch
    {
        public EngineBranchRSS(BuildGeneration generation) : base(BuildGeneration.RSS)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);
            if (linker.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                tokenMap[0x2B] = typeof(NameConstNoNumberToken); // FIXME: NameConst but without the Int32 number at the end
            }
            return tokenMap;
        }
    }
}