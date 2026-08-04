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

            if (linker.LicenseeVersion >= 101)
            {
                ObjectFlags[(int)ObjectFlag.Public] = 0x10000000000000UL; // Fixes false 'Private' modifier on properties.
                ObjectFlags[(int)ObjectFlag.ClassDefaultObject] = 0x80UL << 32; // << 1 0x80, (same bit as BulletStorm)
                ObjectFlags[(int)ObjectFlag.ArchetypeObject] = 0x100UL << 32;

                ObjectFlags[(int)ObjectFlag.TemplateObject] = ObjectFlags[(int)ObjectFlag.ClassDefaultObject] | ObjectFlags[(int)ObjectFlag.ArchetypeObject];
            }
        }

        protected override void SetupEnumPropertyFlags(UnrealPackage linker)
        {
            base.SetupEnumPropertyFlags(linker);

            if (linker.LicenseeVersion >= 101)
            {
                // Most property flags reordered in RSS
                PropertyFlags[(int)PropertyFlag.Const] = 0x1UL;
                PropertyFlags[(int)PropertyFlag.Input] = 0x2UL;
                PropertyFlags[(int)PropertyFlag.ExportObject] = 0x4UL;
                PropertyFlags[(int)PropertyFlag.Parm] = 0x8UL;
                PropertyFlags[(int)PropertyFlag.OptionalParm] = 0x10UL;
                PropertyFlags[(int)PropertyFlag.OutParm] = 0x20UL;
                PropertyFlags[(int)PropertyFlag.SkipParm] = 0x40UL;
                PropertyFlags[(int)PropertyFlag.ReturnParm] = 0x80UL;
                PropertyFlags[(int)PropertyFlag.CoerceParm] = 0x100UL;
                PropertyFlags[(int)PropertyFlag.Native] = 0x200UL;
                PropertyFlags[(int)PropertyFlag.Transient] = 0x400UL;
                PropertyFlags[(int)PropertyFlag.Config] = 0x800UL;
                PropertyFlags[(int)PropertyFlag.Localized] = 0x1000UL;
                PropertyFlags[(int)PropertyFlag.GlobalConfig] = 0x2000UL;
                PropertyFlags[(int)PropertyFlag.Component] = 0x4000UL;
                PropertyFlags[(int)PropertyFlag.DuplicateTransient] = 0x8000UL;
                PropertyFlags[(int)PropertyFlag.NoExport] = 0x20000UL;
                PropertyFlags[(int)PropertyFlag.NoImport] = 0x40000UL;
                PropertyFlags[(int)PropertyFlag.Deprecated] = 0x80000UL;
                PropertyFlags[(int)PropertyFlag.DataBinding] = 0x100000UL;
                PropertyFlags[(int)PropertyFlag.NonTransactional] = 0x400000UL;
                PropertyFlags[(int)PropertyFlag.Archetype] = 0x800000UL;
                PropertyFlags[(int)PropertyFlag.Net] = 0x4000000UL;
                PropertyFlags[(int)PropertyFlag.RepRetry] = 0x8000000UL;
                PropertyFlags[(int)PropertyFlag.RepNotify] = 0x10000000UL;
                PropertyFlags[(int)PropertyFlag.Editable] = 0x100000000UL;
                PropertyFlags[(int)PropertyFlag.EditFixedSize] = 0x200000000UL;
                PropertyFlags[(int)PropertyFlag.EditConst] = 0x400000000UL;
                PropertyFlags[(int)PropertyFlag.NoClear] = 0x800000000UL;
                PropertyFlags[(int)PropertyFlag.EditInline] = 0x4000000000UL;
                PropertyFlags[(int)PropertyFlag.EditInlineUse] = 0x8000000000UL;
                PropertyFlags[(int)PropertyFlag.Interp] = 0x10000000000UL;
                PropertyFlags[(int)PropertyFlag.AlwaysInit] = 0x20000000000UL;
                PropertyFlags[(int)PropertyFlag.EditorOnly] = 0x80000000000UL;
                PropertyFlags[(int)PropertyFlag.NotForConsole] = 0x100000000000UL;

                // Unknown
                PropertyFlags[(int)PropertyFlag.CtorLink] = 0;
                PropertyFlags[(int)PropertyFlag.Travel] = 0;
                PropertyFlags[(int)PropertyFlag.EdFindable] = 0;
                PropertyFlags[(int)PropertyFlag.EditHide] = 0;
                PropertyFlags[(int)PropertyFlag.EditTextBox] = 0;
                PropertyFlags[(int)PropertyFlag.SerializeText] = 0;
                PropertyFlags[(int)PropertyFlag.PrivateWrite] = 0;
                PropertyFlags[(int)PropertyFlag.ProtectedWrite] = 0;
                PropertyFlags[(int)PropertyFlag.CrossLevelPassive] = 0;
                PropertyFlags[(int)PropertyFlag.CrossLevelActive] = 0;
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
