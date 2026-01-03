namespace UELib.Branch.UE2.SCX
{
    public class EngineBranchSCX : DefaultEngineBranch
    {
        public EngineBranchSCX(BuildGeneration generation) : base(BuildGeneration.SCX)
        {
        }

        protected override void SetupSerializer(UnrealPackage package)
        {
            if (package.LicenseeVersion < 85)
            {
                base.SetupSerializer(package);
                return;
            }

            SetupSerializer<PackageSerializerSCX>();
        }
    }
}
