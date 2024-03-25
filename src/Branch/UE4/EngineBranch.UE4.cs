using System;
using UELib.Core.Tokens;
using UELib.Flags;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE4
{
    /// <summary>
    /// An EngineBranch to assist with the parsing of UE4 packages, this way we can easily separate UE4 specific changes away from the default UE3 implementation.
    /// The branch is selected based on whether a UE4Version is set to a value greater than 0.
    /// </summary>
    public class EngineBranchUE4 : EngineBranch
    {
        [Flags]
        public enum PackageFlagsUE4 : uint
        {
            /// <summary>
            /// Runtime-only;not-serialized
            /// </summary>
            [Obsolete] NewlyCreated = 0x00000001,

            EditorOnly = 0x00000040U,

            /// <summary>
            /// Whether the package has been cooked.
            /// </summary>
            Cooked = 0x00000200U,

            UnversionedProperties = 0x00002000U,
            ReloadingForCooker = 0x40000000U,
            FilterEditorOnly = 0x80000000U,
        }

        public EngineBranchUE4() : base(BuildGeneration.UE4)
        {
        }

        /// <summary>
        /// We re-map all PackageFlags because they are no longer a match with those of UE3 or older.
        /// </summary>
        public override void Setup(UnrealPackage linker)
        {
            SetupEnumPackageFlags(linker);
            EnumFlagsMap.Add(typeof(PackageFlag), PackageFlags);
        }

        protected virtual void SetupEnumPackageFlags(UnrealPackage linker)
        {
            PackageFlags[(int)Flags.PackageFlag.ClientOptional] =
                (uint)DefaultEngineBranch.PackageFlagsDefault.ClientOptional;
            PackageFlags[(int)Flags.PackageFlag.ServerSideOnly] =
                (uint)DefaultEngineBranch.PackageFlagsDefault.ServerSideOnly;
            PackageFlags[(int)Flags.PackageFlag.EditorOnly] = (uint)PackageFlagsUE4.EditorOnly;
            PackageFlags[(int)Flags.PackageFlag.Cooked] = (uint)PackageFlagsUE4.Cooked;
            PackageFlags[(int)Flags.PackageFlag.UnversionedProperties] = (uint)PackageFlagsUE4.UnversionedProperties;
            PackageFlags[(int)Flags.PackageFlag.ReloadingForCooker] = (uint)PackageFlagsUE4.ReloadingForCooker;
            PackageFlags[(int)Flags.PackageFlag.FilterEditorOnly] = (uint)PackageFlagsUE4.FilterEditorOnly;
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            SetupSerializer<PackageSerializerUE4>();
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = new TokenMap
            {
                { 0x00, typeof(LocalVariableToken) }
            };
            return tokenMap;
        }
    }
}