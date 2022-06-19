namespace UELib.Branch.UE2.AA2
{
    [Build(UnrealPackage.GameBuild.BuildName.AA2)]
    public class EngineBranchAA2 : DefaultEngineBranch
    {
        /// Decoder initialization is handled in <see cref="UnrealPackage.Deserialize"/>
        public EngineBranchAA2(UnrealPackage package) : base(package)
        {
        }

        protected override void SetupSerializer(UnrealPackage package)
        {
            if (package.LicenseeVersion >= 33)
            {
                Serializer = new PackageSerializerAA2();
                return;
            }
            
            base.SetupSerializer(package);
        }
    }
}