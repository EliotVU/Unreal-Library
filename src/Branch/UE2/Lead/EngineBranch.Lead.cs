namespace UELib.Branch.UE2.Lead
{
    public class EngineBranchLead : DefaultEngineBranch
    {
        public EngineBranchLead(BuildGeneration generation) : base(BuildGeneration.UE2)
        {
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            SetupSerializer<PackageSerializerLead>();
        }

        public override void PostDeserializeSummary(UnrealPackage linker, IUnrealStream stream, ref UnrealPackage.PackageFileSummary summary)
        {
            base.PostDeserializeSummary(linker, stream, ref summary);

            summary.NameOffset += 4;
        }
    }
}
