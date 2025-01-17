namespace UELib.Branch.UE2.SC
{
    public class EngineBranchSC : DefaultEngineBranch
    {
        public EngineBranchSC(BuildGeneration generation) : base(BuildGeneration.UE2)
        {
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            if (linker.LicenseeVersion < 85)
            {
                base.SetupSerializer(linker);
                return;
            }

            SetupSerializer<PackageSerializerSC>();
        }
    }
}
