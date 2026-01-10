using UELib.Flags;

namespace UELib.Branch.UE2.VG;

public class VengeanceEngineBranch(BuildGeneration generation) : DefaultEngineBranch(generation)
{
    protected override void SetupEnumClassFlags(UnrealPackage linker)
    {
        base.SetupEnumClassFlags(linker);

        // The class is marked with the modifier 'Interface'
        ClassFlags[(int)ClassFlag.Interface] = 0x01000000UL;
    }
}
