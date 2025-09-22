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

            if (linker.Build == UnrealPackage.GameBuild.BuildName.Batman2)
            {
                ObjectFlags[(int)ObjectFlag.Public] = 0x10000000000000UL; // Fixes false 'Private' modifier on properties.
            }
            else if (linker.Build == UnrealPackage.GameBuild.BuildName.Batman4)
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

            if (linker.Build == UnrealPackage.GameBuild.BuildName.Batman2)
            {

                PropertyFlags[(int)PropertyFlag.EditConst] = PropertyFlags[(int)PropertyFlag.DuplicateTransient];
                PropertyFlags[(int)PropertyFlag.DuplicateTransient] = 0; // ??

                PropertyFlags[(int)PropertyFlag.EditorOnly] = 0x1UL << 32;
            }

            if (linker.LicenseeVersion >= 101)
            {
                PropertyFlags[(int)PropertyFlag.RepNotify] = PropertyFlags[(int)PropertyFlag.EditInline];
                PropertyFlags[(int)PropertyFlag.EditInline] = 0; // ??

                PropertyFlags[(int)PropertyFlag.Editable] = PropertyFlags[(int)PropertyFlag.EditFixedSize];
                PropertyFlags[(int)PropertyFlag.EditFixedSize] = 0; // ??

                PropertyFlags[(int)PropertyFlag.Net] = PropertyFlags[(int)PropertyFlag.Interp];
                PropertyFlags[(int)PropertyFlag.Interp] = 0; // ??
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
