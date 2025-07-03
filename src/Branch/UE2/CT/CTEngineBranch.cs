using System;
using UELib.Flags;

namespace UELib.Branch.UE2.CT;

public class CTEngineBranch(BuildGeneration generation) : DefaultEngineBranch(generation)
{
    protected override void SetupEnumPropertyFlags(UnrealPackage linker)
    {
        base.SetupEnumPropertyFlags(linker);

        PropertyFlags[(int)PropertyFlag.DuplicateTransient] = 0x00; // Disable, collides with Static
    }

    protected override void SetupSerializer(UnrealPackage linker) => SetupSerializer<CTPackageSerializer>();

    [Flags]
    public enum CTPropertyFlags : uint
    {
        Static = 0x_008_0000,

        // AutoLoad (Auto loads an object from a string property)
        AutoLoad = 0x_0010_0000,
    }
}

