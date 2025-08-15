using System;
using UELib.Core.Tokens;
using UELib.Flags;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE4;

/// <summary>
///     An EngineBranch to assist with the parsing of UE4 packages, this way we can easily separate UE4 specific changes
///     away from the default UE3 implementation.
///     The branch is selected based on whether a UE4Version is set to a value greater than 0.
/// </summary>
public class EngineBranchUE4 : EngineBranch
{
    [Flags]
    public enum PackageFlagsUE4 : uint
    {
        /// <summary>
        ///     Runtime-only;not-serialized
        /// </summary>
        [Obsolete] NewlyCreated = 0x00000001,

        EditorOnly = 0x00000040U,

        /// <summary>
        ///     Whether the package has been cooked.
        /// </summary>
        Cooked = 0x00000200U,

        UnversionedProperties = 0x00002000U,
        ReloadingForCooker = 0x40000000U,
        FilterEditorOnly = 0x80000000U
    }

    public EngineBranchUE4() : base(BuildGeneration.UE4)
    {
    }

    /// <summary>
    ///     We re-map all PackageFlags because they are no longer a match with those of UE3 or older.
    /// </summary>
    public override void Setup(UnrealPackage linker)
    {
        SetupEnumPackageFlags(linker);
        SetupEnumObjectFlags(linker);
        //SetupEnumPropertyFlags(linker);
        //SetupEnumStructFlags(linker);
        //SetupEnumFunctionFlags(linker);
        //SetupEnumStateFlags(linker);
        //SetupEnumClassFlags(linker);

        EnumFlagsMap.Add(typeof(PackageFlag), PackageFlags);
        EnumFlagsMap.Add(typeof(ObjectFlag), ObjectFlags);
        EnumFlagsMap.Add(typeof(PropertyFlag), PropertyFlags);
        EnumFlagsMap.Add(typeof(StructFlag), StructFlags);
        EnumFlagsMap.Add(typeof(FunctionFlag), FunctionFlags);
        EnumFlagsMap.Add(typeof(StateFlag), StateFlags);
        EnumFlagsMap.Add(typeof(ClassFlag), ClassFlags);
    }

    protected virtual void SetupEnumObjectFlags(UnrealPackage linker)
    {
        ObjectFlags[(int)ObjectFlag.Public] = 0x01;
        ObjectFlags[(int)ObjectFlag.Standalone] = 0x02;
        ObjectFlags[(int)ObjectFlag.Transactional] = 0x08;
        ObjectFlags[(int)ObjectFlag.ClassDefaultObject] = 0x10;
        ObjectFlags[(int)ObjectFlag.ArchetypeObject] = 0x20;
        ObjectFlags[(int)ObjectFlag.Transient] = 0x40;
    }

    protected virtual void SetupEnumPackageFlags(UnrealPackage linker)
    {
        PackageFlags[(int)PackageFlag.ClientOptional] =
            (uint)DefaultEngineBranch.PackageFlagsDefault.ClientOptional;
        PackageFlags[(int)PackageFlag.ServerSideOnly] =
            (uint)DefaultEngineBranch.PackageFlagsDefault.ServerSideOnly;
#if UE4
        PackageFlags[(int)PackageFlag.EditorOnly] = (uint)PackageFlagsUE4.EditorOnly;
        PackageFlags[(int)PackageFlag.Cooked] = (uint)PackageFlagsUE4.Cooked;
        PackageFlags[(int)PackageFlag.UnversionedProperties] = (uint)PackageFlagsUE4.UnversionedProperties;
        PackageFlags[(int)PackageFlag.ReloadingForCooker] = (uint)PackageFlagsUE4.ReloadingForCooker;
        PackageFlags[(int)PackageFlag.FilterEditorOnly] = (uint)PackageFlagsUE4.FilterEditorOnly;
#endif
    }

    protected override void SetupSerializer(UnrealPackage linker) => SetupSerializer<PackageSerializerUE4>();

    protected override TokenMap BuildTokenMap(UnrealPackage linker)
    {
        var tokenMap = new TokenMap { { 0x00, typeof(LocalVariableToken) } };

        return tokenMap;
    }
}
