using UELib.Branch.UE3.RSS.Tokens;
using UELib.Core.Tokens;
using UELib.Flags;

namespace UELib.Branch.UE3.RSS
{
    public class EngineBranchRSS : DefaultEngineBranch
    {
        public EngineBranchRSS(BuildGeneration generation) : base(BuildGeneration.RSS)
        {
        }

        protected override void SetupEnumObjectFlags(UnrealPackage linker)
        {
            base.SetupEnumObjectFlags(linker);

            if (linker.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                ObjectFlags[(int)ObjectFlag.Public] = 0x1000U;

                ObjectFlags[(int)ObjectFlag.ClassDefaultObject] = 0x80UL << 32; // << 1 0x80, (same bit as BulletStorm)
                ObjectFlags[(int)ObjectFlag.ArchetypeObject] = 0x10000000UL;
                ObjectFlags[(int)ObjectFlag.TemplateObject] = ObjectFlags[(int)ObjectFlag.ClassDefaultObject] | ObjectFlags[(int)ObjectFlag.ArchetypeObject];
            }
        }

        protected override void SetupEnumPropertyFlags(UnrealPackage linker)
        {
            base.SetupEnumPropertyFlags(linker);

            if (linker.LicenseeVersion >= 101)
            {
                //PropertyFlags[(int)PropertyFlag.Net] = 0x4000000;
            }
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
