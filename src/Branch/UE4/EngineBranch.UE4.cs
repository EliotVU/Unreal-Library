using System;

namespace UELib.Branch.UE4
{
    /// <summary>
    /// An EngineBranch to assist with the parsing of UE4 packages, this way we can easily separate UE4 specific changes away from the default UE3 implementation.
    /// The branch is selected based on whether a UE4Version is set to a value greater than 0.
    /// </summary>
    public class EngineBranchUE4 : DefaultEngineBranch
    {
        [Flags]
        public enum PackageFlagsUE4 : uint
        {
            /// <summary>
            /// Runtime-only;not-serialized
            /// </summary>
            [Obsolete]
            NewlyCreated = 0x00000001,

            EditorOnly = 0x00000040U,

            /// <summary>
            /// Whether the package has been cooked.
            /// </summary>
            Cooked = 0x00000200U,

            UnversionedProperties = 0x00002000U,
            ReloadingForCooker = 0x40000000U,
            FilterEditorOnly = 0x80000000U,
        }
        
        public EngineBranchUE4(UnrealPackage package) : base(package)
        {
        }

        protected override void SetupSerializer(UnrealPackage package)
        {
            Serializer = new PackageSerializerUE4();
        }

        /// <summary>
        /// We re-map all PackageFlags because they are no longer a match with those of UE3 or older.
        /// </summary>
        protected override void SetupEnumPackageFlags(UnrealPackage package)
        {
            PackageFlags[(int)Flags.PackageFlags.ClientOptional] = (uint)PackageFlagsDefault.ClientOptional;
            PackageFlags[(int)Flags.PackageFlags.ServerSideOnly] = (uint)PackageFlagsDefault.ServerSideOnly;
            PackageFlags[(int)Flags.PackageFlags.EditorOnly] = (uint)PackageFlagsUE4.EditorOnly;
            PackageFlags[(int)Flags.PackageFlags.Cooked] = (uint)PackageFlagsUE4.Cooked;
            PackageFlags[(int)Flags.PackageFlags.UnversionedProperties] = (uint)PackageFlagsUE4.UnversionedProperties;
            PackageFlags[(int)Flags.PackageFlags.ReloadingForCooker] = (uint)PackageFlagsUE4.ReloadingForCooker;
            PackageFlags[(int)Flags.PackageFlags.FilterEditorOnly] = (uint)PackageFlagsUE4.FilterEditorOnly;
        }
    }
}