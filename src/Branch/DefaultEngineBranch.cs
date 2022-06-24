using System;
using UELib.Flags;

namespace UELib.Branch
{
    /// <summary>
    /// The default EngineBranch handles UE1, 2, and 3 packages, this helps us separate the UE3 or less specific code away from the <see cref="UE4.EngineBranchUE4"/> implementation.
    ///
    /// Note: The current implementation is incomplete, i.e. it does not map any other enum flags other than PackageFlags yet.
    ///
    /// Its main focus is to split UE3 and UE4 logic.
    /// </summary>
    public class DefaultEngineBranch : EngineBranch
    {
        [Flags]
        public enum PackageFlagsDefault : uint
        {
            /// <summary>
            /// UEX: Whether clients are allowed to download the package from the server.
            /// UE4: Displaced by "NewlyCreated"
            /// </summary>
            AllowDownload = 0x00000001U,

            /// <summary>
            /// Whether clients can skip downloading the package but still able to join the server.
            /// </summary>
            ClientOptional = 0x00000002U,

            /// <summary>
            /// Only necessary to load on the server.
            /// </summary>
            ServerSideOnly = 0x00000004U,

            /// <summary>
            /// UE4: Displaced by "CompiledIn"
            /// </summary>
            Unsecure = 0x00000010U,

            Protected = 0x80000000U,
        }
#if TRANSFORMERS
        [Flags]
        public enum PackageFlagsHMS : uint
        {
            XmlFormat = 0x80000000U,
        }
#endif
        [Flags]
        public enum PackageFlagsUE1 : uint
        {
            /// <summary>
            /// Whether the package has broken links.
            /// 
            /// Runtime-only;not-serialized
            /// </summary>
            [Obsolete]
            BrokenLinks = 0x00000008U,

            /// <summary>
            /// Whether the client needs to download the package.
            /// 
            /// Runtime-only;not-serialized
            /// </summary>
            [Obsolete]
            Need = 0x00008000U,

            /// <summary>
            /// The package is encrypted.
            /// <= UT
            /// Also attested in file UT2004/Packages.MD5 but it is not encrypted.
            /// </summary>
            Encrypted = 0x00000020U,
        }

        [Flags]
        public enum PackageFlagsUE2 : uint
        {
            
        }
       
        [Flags]
        public enum PackageFlagsUE3 : uint
        {
            /// <summary>
            /// Whether the package has been cooked.
            /// </summary>
            Cooked = 0x00000008U,

            /// <summary>
            /// Whether the package contains a ULevel or UWorld object.
            /// </summary>
            ContainsMap = 0x00020000U,

            [Obsolete]
            Trash = 0x00040000,

            /// <summary>
            /// Whether the package contains a UClass or any UnrealScript types.
            /// </summary>
            ContainsScript = 0x00200000U,
            
            /// <summary>
            /// Whether the package contains debug info i.e. it was built with -Debug.
            /// </summary>
            ContainsDebugData = 0x00400000U,
            
            Imports = 0x00800000U,

            StoreCompressed = 0x02000000U,
            StoreFullyCompressed = 0x04000000U,

            /// <summary>
            /// Whether package has metadata exported(anything related to the editor).
            /// </summary>
            NoExportsData = 0x20000000U,

            /// <summary>
            /// Whether the package TextBuffers' have been stripped of its content.
            /// </summary>
            StrippedSource = 0x40000000U,
        }
      
        protected readonly ulong[] PackageFlags = new ulong[(int)Flags.PackageFlags.Max];

        public DefaultEngineBranch(UnrealPackage package) : base(package)
        {
            SetupSerializer(package);
            SetupEnumFlagsMap(package);
        }

        protected virtual void SetupSerializer(UnrealPackage package)
        {
            Serializer = new DefaultPackageSerializer();
        }

        private void SetupEnumFlagsMap(UnrealPackage package)
        {
            SetupEnumPackageFlags(package);
            EnumFlagsMap.Add(typeof(PackageFlags), PackageFlags);
        }

        protected virtual void SetupEnumPackageFlags(UnrealPackage package)
        {
            PackageFlags[(int)Flags.PackageFlags.AllowDownload] = (uint)PackageFlagsDefault.AllowDownload;
            PackageFlags[(int)Flags.PackageFlags.ClientOptional] = (uint)PackageFlagsDefault.ClientOptional;
            PackageFlags[(int)Flags.PackageFlags.ServerSideOnly] = (uint)PackageFlagsDefault.ServerSideOnly;
#if UE1
            // FIXME: Version
            if (package.Version > 61 && package.Version <= 69) // <= UT99
                PackageFlags[(int)Flags.PackageFlags.Encrypted] = (uint)PackageFlagsUE1.Encrypted;
#endif

#if UE3
            // Map the new PackageFlags, but the version is nothing but a guess!
            if (package.Version >= 180)
            {
                if (package.Version >= UnrealPackage.PackageFileSummary.VCookerVersion)
                {
                    PackageFlags[(int)Flags.PackageFlags.Cooked] = (uint)PackageFlagsUE3.Cooked;
                }

                PackageFlags[(int)Flags.PackageFlags.ContainsMap] = (uint)PackageFlagsUE3.ContainsMap;
                PackageFlags[(int)Flags.PackageFlags.ContainsDebugData] = (uint)PackageFlagsUE3.ContainsDebugData;
                PackageFlags[(int)Flags.PackageFlags.ContainsScript] = (uint)PackageFlagsUE3.ContainsScript;
                PackageFlags[(int)Flags.PackageFlags.StrippedSource] = (uint)PackageFlagsUE3.StrippedSource;
            }
#endif
        }

        public override void PostDeserializeSummary(IUnrealStream stream, ref UnrealPackage.PackageFileSummary summary)
        {
        }

        public override void PostDeserializePackage(IUnrealStream stream, UnrealPackage package)
        {
        }
    }
}