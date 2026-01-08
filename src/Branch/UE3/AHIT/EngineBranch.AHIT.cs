using UELib.Flags;

namespace UELib.Branch.UE3.AHIT;

public class EngineBranchAHIT(BuildGeneration generation) : DefaultEngineBranch(generation)
{
    protected override void SetupEnumPropertyFlags(UnrealPackage package)
    {
        base.SetupEnumPropertyFlags(package);

        PropertyFlags[(int)PropertyFlag.EdFindable] = 0;
    }

    protected override void SetupEnumFunctionFlags(UnrealPackage package)
    {
        base.SetupEnumFunctionFlags(package);

        // Remove K2, because they overlap with custom flags.
        FunctionFlags[(int)FunctionFlag.K2Call] = 0;
        FunctionFlags[(int)FunctionFlag.K2Override] = 0;
        FunctionFlags[(int)FunctionFlag.K2Pure] = 0;
    }
}
