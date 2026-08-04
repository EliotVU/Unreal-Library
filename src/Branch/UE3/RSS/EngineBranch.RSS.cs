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

            // Starting with bm2 flags have been split into two segments:
            // var private int ObjectFlags;
            // var private int EditorObjectFlags;

            // EditorObjectFlags may possible be represented by the unknown int (v18) in the UExportTableItem

            // Bm1 flags Object.uc:
            // Standalone [0x80000] | Public [0x4] | LoadForClient [0x10000] | LoadForServer [0x20000] | LoadForEditor [0x40000] | Native [0x4000000]
            // Bm2 flags Object.uc:
            // [0x80000] | Public [0x10000000000000] | [0x4000] | [0x1000] | [0x200000000] | [0x1000000000000000]
            // Bm1 HelpCommandlet:
            // [0x80000] | Public [0x4] | LoadForClient [0x10000] | LoadForServer [0x20000] | LoadForEditor [0x40000] | Native [0x4000000]
            // Bm2 HelpCommandlet:
            // Public [0x10000000000000] | Transient [0x4000] | Native [0x1000] | [0x80000] | [0x200000000] | [0x1000000000000000]

            // Bm1 VfTableObject : LoadForClient [0x10000] | LoadForServer [0x20000] | LoadForEditor [0x40000]
            // Bm2 VfTableObject : Standalone [0x80000] | Native [0x1000] | [0x1000000000000000]

            // Bm1 public name:
            // Public [0x4] | LoadForClient [0x10000] | LoadForServer [0x20000] | LoadForEditor [0x40000]
            // Bm2 public name:
            // Standalone [0x80000] | Public [0x10000000000000] | Native [0x1000] | [0x1000000000000000]

            // Bm1 private hashnext :
            // LoadForClient [0x10000] | LoadForServer [0x20000] | LoadForEditor [0x40000]
            // Bm2 Non native struct prop:
            // Standalone [0x80000] | Public [0x10000000000000] | Native [0x1000] | [0x1000000000000000]
            // Bm2 Native:
            // Standalone [0x80000] | Public [0x10000000000000] | Native [0x1000] | [0x1000000000000000]
            // Bm2 native and private prop:
            // Standalone [0x80000] | Native [0x1000] | [0x1000000000000000]

            // ObjectFlags shifted
            if (linker.LicenseeVersion >= 98)
            {
                // left flag < 98; right flag >= 98
                // Shift flags around to accommodate for the shifting we're performing in the package resource.

                // param_2 mappings
                //[0x00100000] = 0x000000000020000000UL;
                ObjectFlags[(int)ObjectFlag.NotForClient] = 0x000000000020000000UL << 32;

                //[0x00200000] = 0x000000001000000000UL;
                ObjectFlags[(int)ObjectFlag.NotForServer] = 0x000000001000000000UL >> 32;

                //[0x00400000] = 0x000000002000000000UL;
                ObjectFlags[(int)ObjectFlag.NotForEditor] = 0x000000002000000000UL >> 32;

                //[0x00010000] = 0x000000000010000000UL;
                ObjectFlags[(int)ObjectFlag.LoadForClient] = 0x000000000010000000UL << 32;

                //[0x00020000] = 0x000000000800000000UL;
                ObjectFlags[(int)ObjectFlag.LoadForServer] = 0x000000000800000000UL >> 32;

                //[0x00040000] = 0x000000001000000000UL;
                ObjectFlags[(int)ObjectFlag.LoadForEditor] = 0x000000001000000000UL >> 32;

                //[0x00000004] = 0x000000000000100000UL;
                ObjectFlags[(int)ObjectFlag.Public] = 0x000000000000100000UL << 32;

                //[0x00080000] = 0x000000004000000000UL;
                ObjectFlags[(int)ObjectFlag.Standalone] = 0x000000004000000000UL >> 32;

                //[0x04000000] = 0x000000000000000002UL;
                ObjectFlags[(int)ObjectFlag.Native] = 0x000000000000000002UL << 32;

                //[0x00000020] = 0x000000000100000000UL;
                ObjectFlags[(int)ObjectFlag.PerObjectLocalized] = 0x000000001000000000UL >> 32;

                //[0x00000001] = 0x000000002000000000UL;
                ObjectFlags[(int)ObjectFlag.Transactional] = 0x000000002000000000UL >> 32;

                //[0x02000000] = 0x000000000000000040UL;
                ObjectFlags[(int)ObjectFlag.HasStack] = 0x000000000000000040UL << 32;

                //[0x00000100] = 0x000000400000000000UL;

                // param_1 mappings
                //[0x00000100] = 0x000000001000000000UL;
                ObjectFlags[(int)ObjectFlag.Protected] = 0x1000000000UL;

                //[0x00000200] = 0x000000000000000080UL;
                ObjectFlags[(int)ObjectFlag.ClassDefaultObject] = 0x8000000000UL; // << 1 0x80, (same bit as BulletStorm)

                //[0x00000400] = 0x000000000000000100UL;
                ObjectFlags[(int)ObjectFlag.ArchetypeObject] = 0x10000000UL; // ?? assumed

                //[0x00080000] = 0x000000000000004000UL;

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
                PropertyFlags[(int)PropertyFlag.CtorLink] = 0x10000UL; // displaced Travel
                PropertyFlags[(int)PropertyFlag.NoExport] = 0x20000UL;
                PropertyFlags[(int)PropertyFlag.NoImport] = 0x40000UL;
                PropertyFlags[(int)PropertyFlag.Deprecated] = 0x80000UL;
                PropertyFlags[(int)PropertyFlag.DataBinding] = 0x100000UL;
                PropertyFlags[(int)PropertyFlag.NonTransactional] = 0x400000UL;
                PropertyFlags[(int)PropertyFlag.Archetype] = 0x800000UL;
                PropertyFlags[(int)PropertyFlag.Net] = 0x4000000UL; 
                PropertyFlags[(int)PropertyFlag.RepRetry] = 0x8000000UL;
                PropertyFlags[(int)PropertyFlag.RepNotify] = 0x10000000UL;

                // << 32
                PropertyFlags[(int)PropertyFlag.Editable] = 0x1UL << 32;
                PropertyFlags[(int)PropertyFlag.EditFixedSize] = 0x2UL << 32;
                PropertyFlags[(int)PropertyFlag.EditConst] = 0x4UL << 32;
                PropertyFlags[(int)PropertyFlag.NoClear] = 0x08UL << 32;
                PropertyFlags[(int)PropertyFlag.EditInline] = 0x40UL << 32;

                // Additions by @etkramer
                PropertyFlags[(int)PropertyFlag.EditInlineUse] = 0x8000000000UL;
                PropertyFlags[(int)PropertyFlag.Interp] = 0x10000000000UL;
                PropertyFlags[(int)PropertyFlag.AlwaysInit] = 0x20000000000UL;
                PropertyFlags[(int)PropertyFlag.EditorOnly] = 0x80000000000UL;
                PropertyFlags[(int)PropertyFlag.NotForConsole] = 0x100000000000UL;

                // Unknown, possibly deprecated
                PropertyFlags[(int)PropertyFlag.Travel] = 0;
                PropertyFlags[(int)PropertyFlag.EdFindable] = 0;
                PropertyFlags[(int)PropertyFlag.EditHide] = 0;
                PropertyFlags[(int)PropertyFlag.EditTextBox] = 0;
                PropertyFlags[(int)PropertyFlag.SerializeText] = 0;
                PropertyFlags[(int)PropertyFlag.PrivateWrite] = 0;
                PropertyFlags[(int)PropertyFlag.ProtectedWrite] = 0;
                PropertyFlags[(int)PropertyFlag.Travel] = 0;
                PropertyFlags[(int)PropertyFlag.PrivateWrite] = 0;
                PropertyFlags[(int)PropertyFlag.EditHide] = 0;

                // Not added (these were introduced in a later UDK build)
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
