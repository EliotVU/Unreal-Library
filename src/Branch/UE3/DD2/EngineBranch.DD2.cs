namespace UELib.Branch.UE3.DD2
{
    [Build(UnrealPackage.GameBuild.BuildName.DD2)]
    public class EngineBranchDD2 : DefaultEngineBranch
    {
        public EngineBranchDD2(BuildGeneration generation) : base(generation)
        {
        }

        public override void PostDeserializePackage(UnrealPackage linker, IUnrealStream stream)
        {
            base.PostDeserializePackage(linker, stream);
            int position = stream.Package.Summary.HeaderSize;
            var exports = stream.Package.Exports;
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