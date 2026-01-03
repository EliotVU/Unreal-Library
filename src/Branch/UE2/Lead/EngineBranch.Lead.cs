using UELib.Flags;

namespace UELib.Branch.UE2.Lead
{
    public class EngineBranchLead : DefaultEngineBranch
    {
        public EngineBranchLead(BuildGeneration generation) : base(BuildGeneration.Lead)
        {
        }

        protected override void SetupEnumPropertyFlags(UnrealPackage linker)
        {
            base.SetupEnumPropertyFlags(linker);

            PropertyFlags[(int)PropertyFlag.OptionalParm] = 0x10;
            PropertyFlags[(int)PropertyFlag.Parm] = 0x20;
            PropertyFlags[(int)PropertyFlag.OutParm] = 0x040;
            PropertyFlags[(int)PropertyFlag.SkipParm] = 0x080;
            PropertyFlags[(int)PropertyFlag.ReturnParm] = 0x100;
            PropertyFlags[(int)PropertyFlag.CoerceParm] = 0x200;
            PropertyFlags[(int)PropertyFlag.Native] = 0x400; // ??
            PropertyFlags[(int)PropertyFlag.Transient] = 0x800; // ??
            //PropertyFlags[(int)PropertyFlag.] = 0x20000; // ??
        }
        
        protected override void SetupSerializer(UnrealPackage package)
        {
            SetupSerializer<PackageSerializerLead>();
        }

        public override void PostDeserializeSummary(UnrealPackage package, IUnrealStream stream,
            ref UnrealPackage.PackageFileSummary summary)
        {
            base.PostDeserializeSummary(package, stream, ref summary);

            summary.NameOffset += 4;
        }
    }
}
