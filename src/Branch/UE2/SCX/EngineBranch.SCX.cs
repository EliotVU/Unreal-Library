namespace UELib.Branch.UE2.SCX
{
    public class EngineBranchSCX : DefaultEngineBranch
    {
        public EngineBranchSCX(BuildGeneration generation) : base(BuildGeneration.SCX)
        {
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            if (linker.LicenseeVersion < 85)
            {
                base.SetupSerializer(linker);
                return;
            }

            SetupSerializer<PackageSerializerSCX>();
        }
    }
}
