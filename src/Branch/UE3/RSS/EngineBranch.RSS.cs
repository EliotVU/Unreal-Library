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

            // Identical to ContextToken and ClassContextToken. Spotted in BM 1, 2, and 4
            tokenMap[0x50] = typeof(RSSContextToken);

            if (linker.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                // FIXME: NameConst but without the Int32 number at the end
                tokenMap[0x2B] = typeof(NameConstNoNumberToken);
            }

            return tokenMap;
        }
    }
}
