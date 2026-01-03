namespace UELib.Branch.UE3.DD2
{
    [Build(UnrealPackage.GameBuild.BuildName.DD2)]
    public class EngineBranchDD2 : DefaultEngineBranch
    {
        public EngineBranchDD2(BuildGeneration generation) : base(generation)
        {
        }

        public override void PostDeserializePackage(UnrealPackage package, IUnrealStream stream)
        {
            base.PostDeserializePackage(package, stream);

            int position = package.Summary.HeaderSize;
            var exports = package.Exports;
            foreach (var exp in exports)
            {
                // Just in-case.
                if (exp.SerialOffset != 0)
                {
                    position += exp.SerialOffset;
                    continue;
                }

                exp.SerialOffset = position;
                position += exp.SerialSize;
            }
        }
    }
}
