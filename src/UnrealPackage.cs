using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UELib.Annotations;
using UELib.Branch;
using UELib.Branch.UE2.AA2;
using UELib.Branch.UE3.DD2;
using UELib.Branch.UE4;
using UELib.Flags;

namespace UELib
{
    using Core;
    using Decoding;

    /// <summary>
    /// Represents the method that will handle the UELib.UnrealPackage.NotifyObjectAdded
    /// event of a new added UELib.Core.UObject.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A UELib.UnrealPackage.ObjectEventArgs that contains the event data.</param>
    public delegate void NotifyObjectAddedEventHandler(object sender, ObjectEventArgs e);

    /// <summary>
    /// Represents the method that will handle the UELib.UnrealPackage.NotifyPackageEvent
    /// event of a triggered event within the UELib.UnrealPackage.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A UELib.UnrealPackage.PackageEventArgs that contains the event data.</param>
    public delegate void PackageEventHandler(object sender, UnrealPackage.PackageEventArgs e);

    /// <summary>
    /// Represents the method that will handle the UELib.UnrealPackage.NotifyInitializeUpdate
    /// event of a UELib.Core.UObject update.
    /// </summary>
    [PublicAPI]
    public delegate void NotifyUpdateEvent();

    /// <summary>
    /// Registers the class as an Unreal class. The class's name is required to begin with the letter "U".
    /// When an Unreal Package is initializing, all described objects will be initialized as the registered class if its name matches as described by its export item.
    /// 
    /// Note: Usage restricted to the executing assembly(UELib) only!
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrealRegisterClassAttribute : Attribute
    {
    }

    /// <summary>
    /// Represents data of a loaded unreal package.
    /// </summary>
    public sealed class UnrealPackage : IDisposable, IBinaryData
    {
        #region General Members

        public BinaryMetaData BinaryMetaData { get; } = new BinaryMetaData();

        // Reference to the stream used when reading this package
        public readonly UPackageStream Stream;

        /// <summary>
        /// The signature of a 'Unreal Package'.
        /// </summary>
        public const uint Signature = 0x9E2A83C1;

        public const uint Signature_BigEndian = 0xC1832A9E;

        /// <summary>
        /// The full name of this package including directory.
        /// </summary>
        private readonly string _FullPackageName = "UnrealPackage";

        [PublicAPI] public string FullPackageName => _FullPackageName;

        [PublicAPI] public string PackageName => Path.GetFileNameWithoutExtension(_FullPackageName);

        [PublicAPI] public string PackageDirectory => Path.GetDirectoryName(_FullPackageName);

        #endregion

        // TODO: Move to UnrealBuild.cs
        public sealed class GameBuild : object
        {
            [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
            private sealed class BuildAttribute : Attribute
            {
                private readonly int _MinVersion;
                private readonly int _MaxVersion;
                private readonly uint _MinLicensee;
                private readonly uint _MaxLicensee;

                public readonly BuildGeneration Generation;
                public readonly BuildFlags Flags;

                private readonly bool _VerifyEqual;

                public BuildAttribute(int minVersion, uint minLicensee,
                    BuildGeneration gen = BuildGeneration.Undefined)
                {
                    _MinVersion = minVersion;
                    _MinLicensee = minLicensee;
                    Generation = gen;
                    _VerifyEqual = true;
                }


                public BuildAttribute(int minVersion, uint minLicensee,
                    BuildFlags flags,
                    BuildGeneration gen = BuildGeneration.Undefined)
                {
                    _MinVersion = minVersion;
                    _MinLicensee = minLicensee;
                    Flags = flags;
                    Generation = gen;
                    _VerifyEqual = true;
                }

                public BuildAttribute(int minVersion, int maxVersion, uint minLicensee, uint maxLicensee,
                    BuildGeneration gen = BuildGeneration.Undefined)
                {
                    _MinVersion = minVersion;
                    _MaxVersion = maxVersion;
                    _MinLicensee = minLicensee;
                    _MaxLicensee = maxLicensee;
                    Generation = gen;
                }

                public BuildAttribute(int minVersion, int maxVersion, uint minLicensee, uint maxLicensee,
                    BuildFlags flags,
                    BuildGeneration gen = BuildGeneration.Undefined)
                {
                    _MinVersion = minVersion;
                    _MaxVersion = maxVersion;
                    _MinLicensee = minLicensee;
                    _MaxLicensee = maxLicensee;
                    Flags = flags;
                    Generation = gen;
                }

                public bool Verify(GameBuild gb, PackageFileSummary summary)
                {
                    return _VerifyEqual
                        ? summary.Version == _MinVersion && summary.LicenseeVersion == _MinLicensee
                        : summary.Version >= _MinVersion && summary.Version <= _MaxVersion
                                                         && summary.LicenseeVersion >= _MinLicensee
                                                         && summary.LicenseeVersion <= _MaxLicensee;
                }
            }

            // Note: Some builds use the EngineVersion to represent as the LicenseeVersion, e.g Unreal2 and DCUO.
            public enum BuildName
            {
                Unset,
                Default,
                Unknown,

                /// <summary>
                /// Standard
                /// 
                /// 61/000
                /// </summary>
                [Build(61, 0)] Unreal1,

                /// <summary>
                /// Standard, Unreal Tournament & Deus Ex
                /// 
                /// 68:69/000
                /// </summary>
                [Build(68, 69, 0u, 0u)] UT,

                /// <summary>
                /// Deus Ex: Invisible War
                /// 
                /// Missing support for custom classes such as BitfieldProperty and BitfieldEnum among others.
                /// 95/69
                /// </summary>
                [Build(95, 69, BuildGeneration.Flesh)] DeusEx_IW,

                /// <summary>
                /// Thief: Deadly Shadows
                /// 
                /// 95/133
                /// </summary>
                [Build(95, 133, BuildGeneration.Flesh)]
                Thief_DS,

                /// <summary>
                /// 99:117/005:008
                /// Latest patch? Same structure as UT2004's UE2.5
                /// 121/029 (Overlapped with UT2004)
                /// </summary>
                [Build(99, 117, 5u, 8u)] UT2003,

                /// <summary>
                /// 100/058
                /// </summary>
                [Build(100, 58)] XIII,

                /// <summary>
                /// 110/2609
                /// </summary>
                [Build(110, 110, 2481u, 2609u)] Unreal2,

                /// <summary>
                /// Tom Clancy's Rainbow Six 3: Raven Shield
                /// 
                /// 118/011:014
                /// </summary>
                [Build(118, 118, 11u, 14u)] R6RS,

                /// <summary>
                /// Unreal II: eXpanded MultiPlayer
                /// 
                /// 126/000
                /// </summary>
                [Build(123, 126, 0u, 0u)] Unreal2XMP,

                /// <summary>
                /// 118:128/025:029
                /// (Overlaps latest UT2003)
                /// </summary>
                [Build(118, 128, 25u, 29u, BuildGeneration.UE2_5)]
                UT2004,

                /// <summary>
                /// America's Army 2.X
                /// Represents both AAO and AAA
                /// 
                /// Built on UT2004
                /// 128/032:033
                /// </summary>
                [Build(128, 128, 32u, 33u, BuildGeneration.UE2_5)] [BuildEngineBranch(typeof(EngineBranchAA2))]
                AA2,

                /// <summary>
                /// Vanguard: Saga of Heroes
                /// 
                /// 129/035
                /// Some packages have 128/025 but those are in conflict with UT2004.
                /// </summary>
                [Build(128, 129, 34u, 35u, BuildGeneration.UE2_5)] Vanguard_SOH,

                // IrrationalGames/Vengeance - 129:143/027:059

                /// <summary>
                /// Tribes: Vengeance
                ///
                /// 130/027
                /// </summary>
                [Build(130, 27, BuildGeneration.Vengeance)]
                Tribes_VG,

                /// <summary>
                /// 129/027
                /// </summary>
                [Build(129, 27, BuildGeneration.Vengeance)]
                Swat4,

                /// <summary>
                /// BioShock 1 & 2
                /// 
                /// 130:143/056:059
                /// </summary>
                [Build(130, 143, 56u, 59u, BuildGeneration.Vengeance)]
                BioShock,

                /// <summary>
                /// Duke Nukem Forever
                ///
                /// 156/036
                /// </summary>
                [Build(156, 36u, BuildGeneration.UE2)]
                DNF,

                /// <summary>
                /// The Chronicles of Spellborn
                /// 
                /// Built on UT2004
                /// 159/029
                /// Comes with several new non-standard UnrealScript features, these are however not supported.
                /// </summary>
                [Build(159, 29u, BuildGeneration.UE2_5)]
                Spellborn,

                /// <summary>
                /// Standard
                /// 
                /// 369/006
                /// </summary>
                [Build(369, 6)] RoboBlitz,

                /// <summary>
                /// Medal of Honor: Airborne
                /// 
                /// 421/011
                /// </summary>
                [Build(421, 11)] MOHA,

                /// <summary>
                /// 472/046
                /// </summary>
                [Build(472, 46, BuildFlags.ConsoleCooked)]
                MKKE,

                /// <summary>
                /// Gears of War
                /// 
                /// 490/009
                /// </summary>
                [Build(490, 9)] GoW1,

                [Build(511, 039, BuildGeneration.HMS)] // The Bourne Conspiracy
                [Build(511, 145, BuildGeneration.HMS)] // Transformers: War for Cybertron (PC version)
                [Build(511, 144, BuildGeneration.HMS)]
                // Transformers: War for Cybertron (PS3 and XBox 360 version)
                Transformers,

                /// <summary>
                /// 512/000
                /// </summary>
                [Build(512, 0)] UT3,

                /// <summary>
                /// 536/043
                /// </summary>
                [Build(536, 43)] MirrorsEdge,

                /// <summary>
                /// Transformers: Dark of the Moon
                /// </summary>
                [Build(537, 174, BuildGeneration.HMS)] Transformers2,

                /// <summary>
                /// 539/091
                /// </summary>
                [Build(539, 91)] AlphaProtocol,

                /// <summary>
                /// APB: All Points Bulletin & APB: Reloaded
                /// 
                /// 547/028:032
                /// </summary>
                [Build(547, 547, 28u, 32u)] APB,

                /// <summary>
                /// Standard, Gears of War 2
                /// 
                /// 575/000
                /// Xenon is enabled here, because the package is missing editor data, the editor data of UStruct is however still serialized.
                /// </summary>
                [Build(575, 0, BuildFlags.XenonCooked)]
                GoW2,

                /// <summary>
                /// 576/005
                /// </summary>
                [Build(576, 5)] CrimeCraft,

                /// <summary>
                /// Batman: Arkham Asylum
                /// 
                /// 576/021
                /// No Special support, but there's no harm in recognizing this build.
                /// </summary>
                [Build(576, 21)] Batman1,

                /// <summary>
                /// 576/100
                /// </summary>
                [Build(576, 100)] Homefront,

                /// <summary>
                /// Medal of Honor (2010)
                /// Windows, PS3, Xbox 360
                /// Defaulting to ConsoleCooked.
                /// XenonCooked is required to read the Xbox 360 packages.
                /// 581/058
                /// </summary>
                [Build(581, 58, BuildFlags.ConsoleCooked)]
                MOH,

                /// <summary>
                /// 584/058
                /// </summary>
                [Build(584, 58)] Borderlands,

                /// <summary>
                /// 584/126
                /// </summary>
                [Build(584, 126)] Singularity,

                /// <summary>
                /// 590/001
                /// </summary>
                [Build(590, 1, BuildFlags.XenonCooked)]
                ShadowComplex,

                /// <summary>
                /// 610/014
                /// </summary>
                [Build(610, 14)] Tera,

                /// <summary>
                /// DC Universe Online
                /// 
                /// 648/6405
                /// </summary>
                [Build(648, 6405)] DCUO,

                /// <summary>
                /// Dungeon Defenders 2
                ///
                /// 687-688/111-117
                /// </summary>
                [Build(687, 688, 111, 117)] [BuildEngineBranch(typeof(EngineBranchDD2))]
                DD2,

                /// <summary>
                /// BioShock Infinite
                /// 727/075
                /// </summary>
                [Build(727, 75)] Bioshock_Infinite,

                /// <summary>
                /// 742/029
                /// </summary>
                [Build(742, 29, BuildFlags.ConsoleCooked)]
                BulletStorm,

                /// <summary>
                /// 801/030
                /// </summary>
                [Build(801, 30)] Dishonored,

                /// <summary>
                /// 828/000
                /// </summary>
                [Build(788, 1, BuildFlags.ConsoleCooked)] [Build(828, 0, BuildFlags.ConsoleCooked)]
                InfinityBlade,

                /// <summary>
                /// Standard, Gears of War 3
                /// 
                /// 828/000
                /// </summary>
                [Build(828, 0, BuildFlags.ConsoleCooked)]
                GoW3,

                /// <summary>
                /// 832/021
                /// </summary>
                [Build(832, 21)] RememberMe,

                /// <summary>
                /// 832/046
                /// </summary>
                [Build(832, 46)] Borderlands2,

                /// <summary>
                /// 842/001
                /// </summary>
                [Build(842, 1, BuildFlags.ConsoleCooked)]
                InfinityBlade2,

                /// <summary>
                /// XCom
                /// 
                /// 845/059
                /// </summary>
                [Build(845, 59)] XCOM_EU,

                /// <summary>
                /// XCom 2: War of The Chosen
                /// 
                /// 845/120
                /// </summary>
                [Build(845, 120)] XCOM2WotC,

                /// <summary>
                /// Transformers: Fall of Cybertron
                /// 846/181
                /// </summary>
                [Build(846, 181, BuildGeneration.HMS)] [OverridePackageVersion(587)]
                Transformers3,

                /// <summary>
                /// 860/004
                /// </summary>
                [Build(860, 4)] Hawken,

                /// <summary>
                /// Batman: Arkham City
                /// 
                /// 805/101
                /// </summary>
                [Build(805, 101, BuildGeneration.RSS)]
                Batman2,

                /// <summary>
                /// Batman: Arkham Origins
                ///
                /// 806/103
                /// 807/807
                /// </summary>
                [Build(806, 103, BuildGeneration.RSS)]
                [Build(807, 807, 137, 138, BuildGeneration.RSS)]
                Batman3,
                
                /// <summary>
                /// 807/104
                /// </summary>
                [Build(807, 104, BuildGeneration.RSS)]
                Batman3MP,

                /// <summary>
                /// Batman: Arkham Knight
                ///
                /// 863/32995
                /// </summary>
                [Build(863, 32995, BuildGeneration.RSS)]
                Batman4,

                /// <summary>
                /// 867/009:032
                /// Requires third-party decompression and decryption
                /// </summary>
                [Build(867, 868, 9u, 32u)] RocketLeague,

                /// <summary>
                /// 904/009
                /// </summary>
                [Build(904, 904, 09u, 014u)] SpecialForce2,
            }

            public BuildName Name { get; }

            public uint Version { get; }
            public uint? OverrideVersion { get; }
            public uint LicenseeVersion { get; }
            public ushort? OverrideLicenseeVersion { get; }

            /// <summary>
            /// Is cooked for consoles.
            /// </summary>
            [Obsolete("See BuildFlags", true)]
            public bool IsConsoleCompressed { get; }

            /// <summary>
            /// Is cooked for Xenon(Xbox 360). Could be true on PC games.
            /// </summary>
            [Obsolete("See BuildFlags", true)]
            public bool IsXenonCompressed { get; }

            public BuildGeneration Generation { get; }
            [CanBeNull] public readonly Type EngineBranchType;

            public readonly BuildFlags Flags;

            public GameBuild(UnrealPackage package)
            {
                if (UnrealConfig.Platform == UnrealConfig.CookedPlatform.Console) Flags |= BuildFlags.ConsoleCooked;

                var builds = typeof(BuildName).GetFields();
                foreach (var build in builds)
                {
                    var buildAttributes = build.GetCustomAttributes<BuildAttribute>(false);
                    var buildAttribute = buildAttributes.FirstOrDefault(attr => attr.Verify(this, package.Summary));
                    if (buildAttribute == null)
                        continue;

                    Name = (BuildName)Enum.Parse(typeof(BuildName), build.Name);

                    Version = package.Summary.Version;
                    LicenseeVersion = package.Summary.LicenseeVersion;
                    Flags = buildAttribute.Flags;
                    Generation = buildAttribute.Generation;

                    var overrideAttribute = build.GetCustomAttribute<OverridePackageVersionAttribute>(false);
                    if (overrideAttribute != null)
                    {
                        OverrideVersion = overrideAttribute.FixedVersion;
                        OverrideLicenseeVersion = overrideAttribute.FixedLicenseeVersion;
                    }

                    var engineBranchAttribute = build.GetCustomAttribute<BuildEngineBranchAttribute>(false);
                    if (engineBranchAttribute != null)
                    {
                        // We cannot create the instance here, because the instance itself may be dependent on GameBuild.
                        EngineBranchType = engineBranchAttribute.EngineBranchType;
                    }

                    break;
                }

                if (Name == BuildName.Unset)
                    Name = package.Summary.LicenseeVersion == 0 ? BuildName.Default : BuildName.Unknown;
            }

            public static bool operator ==(GameBuild b, BuildGeneration gen)
            {
                return b.Generation == gen;
            }

            public static bool operator !=(GameBuild b, BuildGeneration gen)
            {
                return b.Generation != gen;
            }

            public static bool operator ==(GameBuild b, BuildName name)
            {
                return b.Name == name;
            }

            public static bool operator !=(GameBuild b, BuildName name)
            {
                return b.Name != name;
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                return Name == (BuildName)obj;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return (int)Name;
            }

            public bool HasFlags(BuildFlags flags)
            {
                return (Flags & flags) == flags;
            }

            public override string ToString()
            {
                return Name.ToString();
            }
        }

        public GameBuild Build => Summary.Build;
        public EngineBranch Branch => Summary.Branch;

        public struct PackageFileEngineVersion : IUnrealDeserializableClass
        {
            public uint Major, Minor, Patch;
            public uint Changelist;
            public string Branch;

            public void Deserialize(IUnrealStream stream)
            {
                Major = stream.ReadUInt16();
                Minor = stream.ReadUInt16();
                Patch = stream.ReadUInt16();
                Changelist = stream.ReadUInt32();
                Branch = stream.ReadText();
            }

            public override string ToString()
            {
                return $"{Major}.{Minor}.{Patch}";
            }
        }

        public struct PackageFileSummary : IUnrealSerializableClass
        {
            public GameBuild Build;
            public EngineBranch Branch;

            public uint Version;
            public ushort LicenseeVersion;

            public uint UE4Version;
            public uint UE4LicenseeVersion;

            public UnrealFlags<PackageFlags> PackageFlags;

            private const int VHeaderSize = 249;
            public int HeaderSize;

            private const int VFolderName = 269;

            /// <summary>
            /// UPK content category e.g. Weapons, Sounds or Meshes.
            /// </summary>
            public string FolderName;

            public string LocalizationId;

            public int NameCount, NameOffset;
            public int ExportCount, ExportOffset;
            public int ImportCount, ImportOffset;
            public int HeritageCount, HeritageOffset;

            /// <summary>
            /// List of heritages. UE1 way of defining generations.
            /// </summary>
            public UArray<Guid> Heritages;

            private const int VDependsOffset = 415;
            public int DependsOffset;

            public Guid Guid;
            public UArray<UGenerationTableItem> Generations;

            private PackageFileEngineVersion PackageEngineVersion;
            private PackageFileEngineVersion PackageCompatibleEngineVersion;

            private const int VEngineVersion = 245;
            public int EngineVersion;
            public const int VCookerVersion = 277;
            public int CookerVersion;

            private const int VCompression = 334;
            public uint CompressionFlags;
            public UArray<CompressedChunk> CompressedChunks;

            private const int VPackageSource = 482;
            public uint PackageSource;

            private const int VAdditionalPackagesToCook = 516;
            public UArray<string> AdditionalPackagesToCook;

            private const int VImportExportGuidsOffset = 623;
            public int ImportExportGuidsOffset;
            public int ImportGuidsCount;
            public int ExportGuidsCount;

            private const int VThumbnailTableOffset = 584;
            public int ThumbnailTableOffset;

            private const int VTextureAllocations = 767;

            public int GatherableTextDataCount;
            public int GatherableTextDataOffset;

            public int StringAssetReferencesCount;
            public int StringAssetReferencesOffset;

            public int SearchableNamesOffset;

            public Guid PersistentGuid;
            public Guid OwnerPersistentGuid;

            // In UELib 2.0 we pass the version to the Archives instead.
            private void SetupBuild(UnrealPackage package)
            {
                Build = new GameBuild(package);

                if (Build.OverrideVersion.HasValue) Version = Build.OverrideVersion.Value;
                if (Build.OverrideLicenseeVersion.HasValue) LicenseeVersion = Build.OverrideLicenseeVersion.Value;

                if (OverrideVersion != 0) Version = OverrideVersion;
                if (OverrideLicenseeVersion != 0) LicenseeVersion = OverrideLicenseeVersion;
            }

            private void SetupBranch(UnrealPackage package)
            {
                if (package.Build.EngineBranchType != null)
                {
                    Branch = (EngineBranch)Activator.CreateInstance(package.Build.EngineBranchType, package);
                }
                else if (package.Summary.UE4Version > 0)
                {
                    Branch = new EngineBranchUE4(package);
                }
                else
                {
                    Branch = new DefaultEngineBranch(package);
                }

                Debug.Assert(Branch.Serializer != null, "Branch.Serializer cannot be null");
            }

            public void Serialize(IUnrealStream stream)
            {
                throw new NotImplementedException();
            }

            public void Deserialize(IUnrealStream stream)
            {
                const short maxLegacyVersion = -7;

                // Read as one variable due Big Endian Encoding.       
                int legacyVersion = stream.ReadInt32();
                // FIXME: >= -7 is true for the game Quantum
                if (legacyVersion < 0 && legacyVersion >= maxLegacyVersion)
                {
#if UE4
                    uint ue3Version = 0;
                    if (legacyVersion != -4)
                    {
                        ue3Version = stream.ReadUInt32();
                    }

                    UE4Version = stream.ReadUInt32();
                    UE4LicenseeVersion = stream.ReadUInt32();

                    Version = ue3Version;

                    // Really old, probably no longer in production files? Other than some UE4 assets found in the first public release
                    if (UE4Version >= 138 && UE4Version < 142)
                    {
                        stream.Skip(8); // CookedVersion, CookedLicenseeVersion   
                    }

                    if (legacyVersion <= -2)
                    {
                        // Read enum based version
                        if (legacyVersion == -2)
                        {
                            int count = stream.ReadInt32(); // Versions
                            stream.Skip(count * (4 + 4)); // Tag, Version
                        }
                        else if (legacyVersion >= -5)
                        {
                            int count = stream.ReadInt32();
                            for (var i = 0; i < count; ++i)
                            {
                                // Key
                                stream.ReadGuid();
                                // Version
                                stream.ReadInt32();
                                // FriendlyName
                                stream.ReadText();
                            }
                        }
                        else
                        {
                            int count = stream.ReadInt32();
                            for (var i = 0; i < count; ++i)
                            {
                                stream.ReadGuid(); // Key
                                stream.ReadInt32(); // Version
                            }
                        }
                    }
#else
                    throw new NotSupportedException("This version of the Unreal Engine 4 is not supported!");
#endif
                }
                else
                {
                    Version = (uint)legacyVersion;
                }

                LicenseeVersion = (ushort)(Version >> 16);
                Version &= 0xFFFFU;
                Console.WriteLine("Package Version:" + Version + "/" + LicenseeVersion);

                SetupBuild(stream.Package);
                Debug.Assert(Build != null);
                Console.WriteLine("Build:" + Build);
                SetupBranch(stream.Package);
                Debug.Assert(Branch != null);
                Console.WriteLine("Branch:" + Branch);
                stream.SetBranch(Branch);
#if BIOSHOCK
                if (stream.Package.Build == GameBuild.BuildName.Bioshock_Infinite)
                {
                    int unk = stream.ReadInt32();
                }
#endif
#if MKKE
                if (stream.Package.Build == GameBuild.BuildName.MKKE) stream.Skip(8);
#endif
#if TRANSFORMERS
                if (stream.Package.Build == BuildGeneration.HMS &&
                    stream.LicenseeVersion >= 55)
                {
                    if (stream.LicenseeVersion >= 181) stream.Skip(16);

                    stream.Skip(4);
                }
#endif
                if (stream.Version >= VHeaderSize)
                {
                    // Offset to the first class(not object) in the package.
                    HeaderSize = stream.ReadInt32();
                    Console.WriteLine("Header Size: " + HeaderSize);
                }

                if (stream.Version >= VFolderName)
                {
                    FolderName = stream.ReadText();
                }

                PackageFlags = stream.ReadFlags32<PackageFlags>();
                Console.WriteLine("Package Flags:" + PackageFlags);
#if HAWKEN
                if (stream.Package.Build == GameBuild.BuildName.Hawken &&
                    stream.LicenseeVersion >= 2)
                    stream.Skip(4);
#endif
                NameCount = stream.ReadInt32();
                NameOffset = stream.ReadInt32();
#if UE4
                if (stream.UE4Version >= 516 && stream.Package.ContainsEditorData())
                {
                    LocalizationId = stream.ReadText();
                }

                if (stream.UE4Version >= 459)
                {
                    GatherableTextDataCount = stream.ReadInt32();
                    GatherableTextDataOffset = stream.ReadInt32();
                }
#endif
                ExportCount = stream.ReadInt32();
                ExportOffset = stream.ReadInt32();
#if APB
                if (stream.Package.Build == GameBuild.BuildName.APB &&
                    stream.LicenseeVersion >= 28)
                {
                    if (stream.LicenseeVersion >= 29)
                    {
                        stream.Skip(4);
                    }

                    stream.Skip(20);
                }
#endif
                ImportCount = stream.ReadInt32();
                ImportOffset = stream.ReadInt32();

                Console.WriteLine("Names Count:" + NameCount + " Names Offset:" + NameOffset
                                  + " Exports Count:" + ExportCount + " Exports Offset:" + ExportOffset
                                  + " Imports Count:" + ImportCount + " Imports Offset:" + ImportOffset
                );

                if (stream.Version < 68)
                {
                    HeritageCount = stream.ReadInt32();
                    Contract.Assert(HeritageCount > 0);
                    HeritageOffset = stream.ReadInt32();
                    return;
                }

                if (stream.Version >= VDependsOffset)
                {
                    DependsOffset = stream.ReadInt32();
                }
#if THIEF_DS || DEUSEX_IW
                if (stream.Package.Build == GameBuild.BuildName.Thief_DS ||
                    stream.Package.Build == GameBuild.BuildName.DeusEx_IW)
                {
                    //stream.Skip( 4 );
                    int unknown = stream.ReadInt32();
                    Console.WriteLine("Unknown:" + unknown);
                }
#endif
#if BORDERLANDS
                if (stream.Package.Build == GameBuild.BuildName.Borderlands) stream.Skip(4);
#endif
                if (stream.UE4Version >= 384)
                {
                    StringAssetReferencesCount = stream.ReadInt32();
                    StringAssetReferencesOffset = stream.ReadInt32();
                }

                if (stream.UE4Version >= 510)
                {
                    SearchableNamesOffset = stream.ReadInt32();
                }

                if (stream.Version >= VImportExportGuidsOffset &&
                    stream.UE4Version == 0
                    // FIXME: Correct the output version of these games instead.
#if BIOSHOCK
                    && stream.Package.Build != GameBuild.BuildName.Bioshock_Infinite
#endif
                   )
                {
                    ImportExportGuidsOffset = stream.ReadInt32();
                    ImportGuidsCount = stream.ReadInt32();
                    ExportGuidsCount = stream.ReadInt32();
                }
#if TRANSFORMERS
                if (stream.Package.Build == BuildGeneration.HMS &&
                    stream.Version >= 535)
                {
                    // ThumbnailTableOffset? But if so, the partial-upgrade must have skipped @AdditionalPackagesToCook
                    stream.Skip(4);
                    return;
                }
#endif
#if DD2
                // No version check found in the .exe
                if (stream.Package.Build == GameBuild.BuildName.DD2 && PackageFlags.HasFlag(Flags.PackageFlags.Cooked))
                    stream.Skip(4);
#endif
                if (stream.Version >= VThumbnailTableOffset)
                {
                    ThumbnailTableOffset = stream.ReadInt32();
                }
#if MKKE
                if (stream.Package.Build == GameBuild.BuildName.MKKE) stream.Skip(4);
#endif
#if SPELLBORN
                if (stream.Package.Build == GameBuild.BuildName.Spellborn
                    && stream.Version >= 148)
                    goto skipGuid;
#endif
                Guid = stream.ReadGuid();
                Console.WriteLine("GUID:" + Guid);
            skipGuid:
#if TERA
                if (stream.Package.Build == GameBuild.BuildName.Tera) stream.Position -= 4;
#endif
#if MKKE
                if (stream.Package.Build != GameBuild.BuildName.MKKE)
                {
#endif
                    int generationCount = stream.ReadInt32();
                    Contract.Assert(generationCount > 0);
                    Console.WriteLine("Generations Count:" + generationCount);
#if APB
                    // Guid, however only serialized for the first generation item.
                    if (stream.Package.Build == GameBuild.BuildName.APB &&
                        stream.LicenseeVersion >= 32)
                    {
                        stream.Skip(16);
                    }
#endif
                    stream.ReadArray(out Generations, generationCount);
#if MKKE
                }
#endif
                if (stream.Version >= VEngineVersion &&
                    stream.UE4Version == 0)
                {
                    // The Engine Version this package was created with
                    EngineVersion = stream.ReadInt32();
                    Console.WriteLine("\tEngineVersion:" + EngineVersion);
                }
#if UE4
                if (stream.Package.ContainsEditorData())
                {
                    if (stream.UE4Version >= 518)
                    {
                        PersistentGuid = stream.ReadGuid();
                        if (stream.UE4Version < 520)
                        {
                            OwnerPersistentGuid = stream.ReadGuid();
                        }
                    }
                }

                if (stream.UE4Version >= 336)
                {
                    // EngineVersion
                    PackageEngineVersion = new PackageFileEngineVersion();
                    PackageEngineVersion.Deserialize(stream);
                }

                if (stream.UE4Version >= 444)
                {
                    // Compatible EngineVersion
                    PackageCompatibleEngineVersion = new PackageFileEngineVersion();
                    PackageCompatibleEngineVersion.Deserialize(stream);
                }
#endif
                if (stream.Version >= VCookerVersion &&
                    stream.UE4Version == 0)
                {
                    // The Cooker Version this package was cooked with
                    CookerVersion = stream.ReadInt32();
                    Console.WriteLine("CookerVersion:" + CookerVersion);
                }

                // Read compressed info?
                if (stream.Version >= VCompression)
                {
                    CompressionFlags = stream.ReadUInt32();
                    Console.WriteLine("CompressionFlags:" + CompressionFlags);
                    stream.ReadArray(out CompressedChunks);
                }

                if (stream.Version >= VPackageSource)
                {
                    PackageSource = stream.ReadUInt32();
                    Console.WriteLine("PackageSource:" + PackageSource);
                }
#if UE4
                if (stream.UE4Version > 0)
                    return;
#endif
                if (stream.Version >= VAdditionalPackagesToCook)
                {
#if TRANSFORMERS
                    if (stream.Package.Build == BuildGeneration.HMS)
                    {
                        return;
                    }
#endif
                    stream.ReadArray(out AdditionalPackagesToCook);
#if DCUO
                    if (stream.Package.Build == GameBuild.BuildName.DCUO)
                    {
                        var realNameOffset = (int)stream.Position;
                        Debug.Assert(
                            realNameOffset <= NameOffset,
                            "realNameOffset is > the parsed name offset for a DCUO package, we don't know where to go now!"
                        );

                        int offsetDif = NameOffset - realNameOffset;
                        NameOffset -= offsetDif;
                        ImportOffset -= offsetDif;
                        ExportOffset -= offsetDif;
                        DependsOffset = 0; // not working
                        ImportExportGuidsOffset -= offsetDif;
                        ThumbnailTableOffset -= offsetDif;
                    }
#endif
                }

                if (stream.Version >= VTextureAllocations)
                {
                    // TextureAllocations, TextureTypes
                    int count = stream.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        stream.ReadInt32();
                        stream.ReadInt32();
                        stream.ReadInt32();
                        stream.ReadUInt32();
                        stream.ReadUInt32();
                        int count2 = stream.ReadInt32();
                        stream.Skip(count2 * 4);
                    }
                }
#if ROCKETLEAGUE
                if (stream.Package.Build == GameBuild.BuildName.RocketLeague
                    && PackageFlags.HasFlag(Flags.PackageFlags.Cooked))
                {
                    int garbageSize = stream.ReadInt32();
                    Debug.WriteLine(garbageSize, "GarbageSize");
                    int compressedChunkInfoOffset = stream.ReadInt32();
                    Debug.WriteLine(compressedChunkInfoOffset, "CompressedChunkInfoOffset");
                    int lastBlockSize = stream.ReadInt32();
                    Debug.WriteLine(lastBlockSize, "LastBlockSize");
                    Debug.Assert(stream.Position == NameOffset, "There is more data before the NameTable");
                    // Data after this is encrypted
                }
#endif
            }
        }

        public PackageFileSummary Summary;

        /// <summary>
        /// Whether the package was serialized in BigEndian encoding.
        /// </summary>
        public bool IsBigEndianEncoded { get; }

        public const int VSIZEPREFIXDEPRECATED = 64;
        public const int VINDEXDEPRECATED = 178;

        /// <summary>
        /// DLLBind(Name)
        /// </summary>
        public const int VDLLBIND = 655;

        /// <summary>
        /// New class modifier "ClassGroup(Name[,Name])"
        /// </summary>
        public const int VCLASSGROUP = 789;

        public uint Version => Summary.Version;

        /// <summary>
        /// For debugging purposes. Change this to override the present Version deserialized from the package.
        /// </summary>
        public static ushort OverrideVersion;

        public ushort LicenseeVersion => Summary.LicenseeVersion;

        /// <summary>
        /// For debugging purposes. Change this to override the present Version deserialized from the package.
        /// </summary>
        public static ushort OverrideLicenseeVersion;

        /// <summary>
        /// The bitflags of this package.
        /// </summary>
        [Obsolete("See Summary.PackageFlags")] public uint PackageFlags;

        /// <summary>
        /// Size of the Header. Basically points to the first Object in the package.
        /// </summary>
        [Obsolete("See Summary.HeaderSize")]
        public int HeaderSize => Summary.HeaderSize;

        /// <summary>
        /// The group the package is associated with in the Content Browser.
        /// </summary>
        [Obsolete("See Summary.FolderName")] public string Group;

        /// <summary>
        /// The guid of this package. Used to test if the package on a client is equal to the one on a server.
        /// </summary>
        [Obsolete("See Summary.Guid")]
        public string GUID => Summary.Guid.ToString();

        /// <summary>
        /// List of package generations.
        /// </summary>
        [Obsolete("See Summary.Generations")]
        public UArray<UGenerationTableItem> Generations => Summary.Generations;

        /// <summary>
        /// The Engine version the package was created with.
        /// </summary>
        [Obsolete("See Summary.EngineVersion")]
        public int EngineVersion => Summary.EngineVersion;

        /// <summary>
        /// The Cooker version the package was cooked with.
        /// </summary>
        [Obsolete("See Summary.CookerVersion")]
        public int CookerVersion => Summary.CookerVersion;

        /// <summary>
        /// The type of compression the package is compressed with.
        /// </summary>
        [Obsolete("See Summary.CompressionFlags")]
        public uint CompressionFlags => Summary.CompressionFlags;

        /// <summary>
        /// List of compressed chunks throughout the package.
        /// Null if package version less is than <see cref="VCompression" />
        /// </summary>
        [Obsolete("See Summary.CompressedChunks")]
        public UArray<CompressedChunk> CompressedChunks => Summary.CompressedChunks;

        /// <summary>
        /// List of unique unreal names.
        /// </summary>
        [PublicAPI]
        public List<UNameTableItem> Names { get; private set; }

        /// <summary>
        /// List of info about exported objects.
        /// </summary>
        [PublicAPI]
        public List<UExportTableItem> Exports { get; private set; }

        /// <summary>
        /// List of info about imported objects.
        /// </summary>
        [PublicAPI]
        public List<UImportTableItem> Imports { get; private set; }

        /// <summary>
        /// List of info about dependency objects.
        /// </summary>
        //public List<UDependencyTableItem> Dependencies{ get; private set; }

        #region Initialized Members

        /// <summary>
        /// Class types that should get added to the ObjectsList.
        /// </summary>
        private readonly Dictionary<string, Type> _ClassTypes = new Dictionary<string, Type>();

        /// <summary>
        /// List of UObjects that were constructed by function ConstructObjects, later deserialized and linked.
        ///
        /// Includes Exports and Imports!.
        /// </summary>
        [PublicAPI]
        public List<UObject> Objects { get; private set; }

        [PublicAPI] public NativesTablePackage NTLPackage;

        [Obsolete("See UPackageStream.Decoder")]
        public IBufferDecoder Decoder;

        #endregion

        #region Constructors

        /// <summary>
        /// A Collection of flags describing how a package should be initialized.
        /// </summary>
        [Flags]
        [Obfuscation(Exclude = true)]
        public enum InitFlags : ushort
        {
            Construct = 0x0001,
            Deserialize = 0x0002,
            [Obsolete] Import = 0x0004,
            Link = 0x0008,
            All = RegisterClasses | Construct | Deserialize | Link,
            RegisterClasses = 0x0010
        }

        [PublicAPI]
        [Obsolete]
        public static UnrealPackage DeserializePackage(string packagePath, FileAccess fileAccess = FileAccess.Read)
        {
            var stream = new UPackageStream(packagePath, FileMode.Open, fileAccess);
            var pkg = new UnrealPackage(stream);
            pkg.Deserialize(stream);
            return pkg;
        }

        /// <summary>
        /// Creates a new instance of the UELib.UnrealPackage class with a PackageStream and name.
        /// </summary>
        /// <param name="stream">A loaded UELib.PackageStream.</param>
        public UnrealPackage(UPackageStream stream)
        {
            _FullPackageName = stream.Name;
            Stream = stream;
            Stream.PostInit(this);

            // File Type
            // Signature is tested in UPackageStream
            IsBigEndianEncoded = stream.BigEndianCode;
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(UPackageStream stream)
        {
            Summary = new PackageFileSummary();
            Summary.Deserialize(stream);
            BinaryMetaData.AddField(nameof(Summary), Summary, 0, stream.Position);
            
            // FIXME: For backwards compatibility.
            PackageFlags = (uint)Summary.PackageFlags;
            Group = Summary.FolderName;
            Branch.PostDeserializeSummary(stream, ref Summary);

            // We can't continue without decompressing.
            if (CompressedChunks != null && CompressedChunks.Any())
            {
                return;
            }
#if TERA
            if (Build == GameBuild.BuildName.Tera) Summary.NameCount = Generations.Last().NameCount;
#endif
            // Read the name table
            if (Summary.NameCount > 0)
            {
                stream.Seek(Summary.NameOffset, SeekOrigin.Begin);
                Names = new List<UNameTableItem>(Summary.NameCount);
                for (var i = 0; i < Summary.NameCount; ++i)
                {
                    var nameEntry = new UNameTableItem { Offset = (int)stream.Position, Index = i };
                    stream.Serializer.Deserialize(stream, nameEntry);
                    nameEntry.Size = (int)(stream.Position - nameEntry.Offset);
                    Names.Add(nameEntry);
                }
                BinaryMetaData.AddField(nameof(Names), Names, Summary.NameOffset, stream.Position - Summary.NameOffset);

#if SPELLBORN
                // WTF were they thinking? Change DRFORTHEWIN to None
                if (Build == GameBuild.BuildName.Spellborn
                    && Names[0].Name == "DRFORTHEWIN")
                    Names[0].Name = "None";
                // False??
                //Debug.Assert(stream.Position == Summary.ImportsOffset);
#endif
            }

            // Read Heritages
            if (Summary.HeritageCount > 0)
            {
                stream.Seek(Summary.HeritageOffset, SeekOrigin.Begin);
                Summary.Heritages = new UArray<Guid>(Summary.HeritageCount);
                for (var i = 0; i < Summary.HeritageCount; ++i)
                    Summary.Heritages.Add(stream.ReadGuid());
                BinaryMetaData.AddField(nameof(Summary.Heritages), Summary.Heritages, Summary.HeritageOffset, stream.Position - Summary.HeritageOffset);
            }

            // Read Import Table
            if (Summary.ImportCount > 0)
            {
                stream.Seek(Summary.ImportOffset, SeekOrigin.Begin);
                Imports = new List<UImportTableItem>(Summary.ImportCount);
                for (var i = 0; i < Summary.ImportCount; ++i)
                {
                    var imp = new UImportTableItem { Offset = (int)stream.Position, Index = i, Owner = this };
                    stream.Serializer.Deserialize(stream, imp);
                    imp.Size = (int)(stream.Position - imp.Offset);
                    Imports.Add(imp);
                }
                BinaryMetaData.AddField(nameof(Imports), Imports, Summary.ImportOffset, stream.Position - Summary.ImportOffset);
            }

            // Read Export Table
            if (Summary.ExportCount > 0)
            {
                stream.Seek(Summary.ExportOffset, SeekOrigin.Begin);
                Exports = new List<UExportTableItem>(Summary.ExportCount);
                for (var i = 0; i < Summary.ExportCount; ++i)
                {
                    var exp = new UExportTableItem { Offset = (int)stream.Position, Index = i, Owner = this };
                    stream.Serializer.Deserialize(stream, exp);
                    exp.Size = (int)(stream.Position - exp.Offset);
                    Exports.Add(exp);
                }
                BinaryMetaData.AddField(nameof(Exports), Exports, Summary.ExportOffset, stream.Position - Summary.ExportOffset);

                if (Summary.DependsOffset > 0)
                {
                    try
                    {
                        stream.Seek(Summary.DependsOffset, SeekOrigin.Begin);
                        int dependsCount = Summary.ExportCount;
#if BIOSHOCK
                        // FIXME: Version?
                        if (Build == GameBuild.BuildName.Bioshock_Infinite)
                        {
                            dependsCount = stream.ReadInt32();
                        }
#endif
                        var dependsMap = new List<int[]>(dependsCount);
                        for (var i = 0; i < dependsCount; ++i)
                        {
                            // DependencyList, index to import table
                            int count = stream.ReadInt32(); // -1 in DCUO?
                            var imports = new int[count];
                            for (var j = 0; j < count; ++j)
                            {
                                imports[j] = stream.ReadInt32();
                            }

                            dependsMap.Add(imports);
                        }
                        BinaryMetaData.AddField(nameof(dependsMap), dependsMap, Summary.DependsOffset, stream.Position - Summary.DependsOffset);
                    }
                    catch (Exception ex)
                    {
                        // Errors shouldn't be fatal here because this feature is not necessary for our purposes.
                        Console.Error.WriteLine("Couldn't parse DependenciesTable");
                        Console.Error.WriteLine(ex.ToString());
#if STRICT
                        throw new UnrealException("Couldn't parse DependenciesTable", ex);
#endif
                    }
                }
            }

            if (Summary.ImportExportGuidsOffset > 0)
            {
                try
                {
                    for (var i = 0; i < Summary.ImportGuidsCount; ++i)
                    {
                        string levelName = stream.ReadText();
                        int guidCount = stream.ReadInt32();
                        stream.Skip(guidCount * 16);
                    }

                    for (var i = 0; i < Summary.ExportGuidsCount; ++i)
                    {
                        var objectGuid = stream.ReadGuid();
                        int exportIndex = stream.ReadInt32();
                    }

                    if (stream.Position != Summary.ImportExportGuidsOffset)
                    {
                        BinaryMetaData.AddField("ImportExportGuids", null, Summary.ImportExportGuidsOffset, stream.Position - Summary.ImportExportGuidsOffset);
                    }
                }
                catch (Exception ex)
                {
                    // Errors shouldn't be fatal here because this feature is not necessary for our purposes.
                    Console.Error.WriteLine("Couldn't parse ImportExportGuidsTable");
                    Console.Error.WriteLine(ex.ToString());
#if STRICT
                    throw new UnrealException("Couldn't parse ImportExportGuidsTable", ex);
#endif
                }
            }

            if (Summary.ThumbnailTableOffset != 0)
            {
                try
                {
                    int thumbnailCount = stream.ReadInt32();
                    // TODO: Serialize
                    BinaryMetaData.AddField("Thumbnails", null, Summary.ThumbnailTableOffset, stream.Position - Summary.ThumbnailTableOffset);

                }
                catch (Exception ex)
                {
                    // Errors shouldn't be fatal here because this feature is not necessary for our purposes.
                    Console.Error.WriteLine("Couldn't parse ThumbnailTable");
                    Console.Error.WriteLine(ex.ToString());
#if STRICT
                    throw new UnrealException("Couldn't parse ThumbnailTable", ex);
#endif
                }
            }

            Debug.Assert(stream.Position <= int.MaxValue);
            if (Summary.HeaderSize == 0) Summary.HeaderSize = (int)stream.Position;

            Branch.PostDeserializePackage(stream, stream.Package);
        }

        /// <summary>
        /// Constructs all export objects.
        /// </summary>
        /// <param name="initFlags">Initializing rules such as deserializing and/or linking.</param>
        [PublicAPI]
        public void InitializeExportObjects(InitFlags initFlags = InitFlags.All)
        {
            Objects = new List<UObject>(Exports.Count);
            foreach (var exp in Exports) CreateObject(exp);

            if ((initFlags & InitFlags.Deserialize) == 0)
                return;

            DeserializeObjects();
            if ((initFlags & InitFlags.Link) != 0) LinkObjects();
        }

        /// <summary>
        /// Constructs all import objects.
        /// </summary>
        /// <param name="initialize">If TRUE initialize all constructed objects.</param>
        [PublicAPI]
        public void InitializeImportObjects(bool initialize = true)
        {
            Objects = new List<UObject>(Imports.Count);
            foreach (var imp in Imports) CreateObject(imp);

            if (!initialize) return;

            foreach (var obj in Objects) obj.PostInitialize();
        }

        /// <summary>
        /// Initializes all the objects that resist in this package as well tries to import deserialized data from imported objects.
        /// </summary>
        /// <param name="initFlags">A collection of initializing flags to notify what should be initialized.</param>
        /// <example>InitializePackage( UnrealPackage.InitFlags.All )</example>
        [PublicAPI]
        public void InitializePackage(InitFlags initFlags = InitFlags.All)
        {
            if ((initFlags & InitFlags.RegisterClasses) != 0) RegisterExportedClassTypes();

            if ((initFlags & InitFlags.Construct) == 0) return;

            ConstructObjects();
            if ((initFlags & InitFlags.Deserialize) == 0)
                return;

            try
            {
                DeserializeObjects();
            }
            catch
            {
                throw new DeserializingObjectsException();
            }

            try
            {
                if ((initFlags & InitFlags.Link) != 0) LinkObjects();
            }
            catch
            {
                throw new LinkingObjectsException();
            }

            DisposeStream();
        }

        /// <summary>
        ///
        /// </summary>
        public class PackageEventArgs : EventArgs
        {
            /// <summary>
            /// Event identification.
            /// </summary>
            public enum Id : byte
            {
                /// <summary>
                /// Constructing Export/Import objects.
                /// </summary>
                Construct = 0,

                /// <summary>
                /// Deserializing objects.
                /// </summary>
                Deserialize = 1,

                /// <summary>
                /// Importing objects from linked packages.
                /// </summary>
                [Obsolete] Import = 2,

                /// <summary>
                /// Connecting deserialized object indexes.
                /// </summary>
                Link = 3,

                /// <summary>
                /// Deserialized a Export/Import object.
                /// </summary>
                Object = 0xFF
            }

            /// <summary>
            /// The event identification.
            /// </summary>
            [PublicAPI] public readonly Id EventId;

            /// <summary>
            /// Constructs a new event with @eventId.
            /// </summary>
            /// <param name="eventId">Event identification.</param>
            public PackageEventArgs(Id eventId)
            {
                EventId = eventId;
            }
        }

        /// <summary>
        ///
        /// </summary>
        [PublicAPI]
        public event PackageEventHandler NotifyPackageEvent;

        private void OnNotifyPackageEvent(PackageEventArgs e)
        {
            NotifyPackageEvent?.Invoke(this, e);
        }

        /// <summary>
        /// Called when an object is added to the ObjectsList via the AddObject function.
        /// </summary>
        [PublicAPI]
        public event NotifyObjectAddedEventHandler NotifyObjectAdded;

        /// <summary>
        /// Constructs all the objects based on data from _ExportTableList and _ImportTableList, and
        /// all constructed objects are added to the _ObjectsList.
        /// </summary>
        private void ConstructObjects()
        {
            Objects = new List<UObject>();
            OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Construct));
            foreach (var exp in Exports)
                try
                {
                    CreateObject(exp);
                }
                catch (Exception exc)
                {
                    throw new UnrealException("couldn't create export object for " + exp, exc);
                }

            foreach (var imp in Imports)
                try
                {
                    CreateObject(imp);
                }
                catch (Exception exc)
                {
                    throw new UnrealException("couldn't create import object for " + imp, exc);
                }
        }

        /// <summary>
        /// Deserializes all exported objects.
        /// </summary>
        private void DeserializeObjects()
        {
            // Only exports should be deserialized and PostInitialized!
            OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Deserialize));
            foreach (var exp in Exports)
            {
                if (!(exp.Object is UnknownObject || exp.Object.ShouldDeserializeOnDemand))
                    //Console.WriteLine( "Deserializing object:" + exp.ObjectName );
                    exp.Object.BeginDeserializing();

                OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Object));
            }
        }

        /// <summary>
        /// Initializes all exported objects.
        /// </summary>
        private void LinkObjects()
        {
            // Notify that deserializing is done on all objects, now objects can read properties that were dependent on deserializing
            OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Link));
            foreach (var exp in Exports)
                try
                {
                    if (!(exp.Object is UnknownObject)) exp.Object.PostInitialize();

                    OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Object));
                }
                catch (InvalidCastException)
                {
                    Console.WriteLine("InvalidCastException occurred on object: " + exp.Object);
                }
        }

        private void RegisterExportedClassTypes()
        {
            var exportedTypes = Assembly.GetExecutingAssembly().GetExportedTypes();
            foreach (var exportedType in exportedTypes)
            {
                object[] attributes = exportedType.GetCustomAttributes(typeof(UnrealRegisterClassAttribute), false);
                if (attributes.Length == 1) AddClassType(exportedType.Name.Substring(1), exportedType);
            }
        }

        #endregion

        #region Methods
        
        // Create pseudo objects for imports so that we have non-null references to imports.
        private void CreateObject(UImportTableItem item)
        {
            var classType = GetClassType(item.ClassName);
            item.Object = classType == null
                ? new UnknownObject()
                : (UObject)Activator.CreateInstance(classType);
            AddObject(item.Object, item);
            OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Object));
        }
        
        private void CreateObject(UExportTableItem item)
        {
            var @class = GetIndexTable(item.ClassIndex);
            var classType = GetClassType(@class != null ? @class.ObjectName : "Class");
            item.Object = classType == null
                ? new UnknownObject()
                : (UObject)Activator.CreateInstance(classType);
            AddObject(item.Object, item);
            OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Object));
        }

        private void AddObject(UObject obj, UObjectTableItem table)
        {
            table.Object = obj;
            obj.Package = this;
            obj.Table = table;

            Objects.Add(obj);
            NotifyObjectAdded?.Invoke(this, new ObjectEventArgs(obj));
        }

        /// <summary>
        /// Writes the present PackageFlags to disk. HardCoded!
        /// Only supports UT2004.
        /// </summary>
        [PublicAPI]
        [Obsolete]
        public void WritePackageFlags()
        {
            Stream.Position = 8;
            Stream.UW.Write(PackageFlags);
        }

        [PublicAPI]
        [Obsolete]
        public void RegisterClass(string className, Type classObject)
        {
            AddClassType(className, classObject);
        }

        [PublicAPI]
        public void AddClassType(string className, Type classObject)
        {
            _ClassTypes.Add(className.ToLower(), classObject);
        }

        [PublicAPI]
        public Type GetClassType(string className)
        {
            _ClassTypes.TryGetValue(className.ToLower(), out var classType);
            return classType;
        }

        [PublicAPI]
        public bool HasClassType(string className)
        {
            return _ClassTypes.ContainsKey(className.ToLower());
        }

        [PublicAPI]
        [Obsolete]
        public bool IsRegisteredClass(string className)
        {
            return HasClassType(className);
        }

        /// <summary>
        /// Returns an Object that resides at the specified ObjectIndex.
        ///
        /// if index is positive an exported Object will be returned.
        /// if index is negative an imported Object will be returned.
        /// if index is zero null will be returned.
        /// </summary>
        [PublicAPI]
        public UObject GetIndexObject(int objectIndex)
        {
            return objectIndex < 0
                ? Imports[-objectIndex - 1].Object
                : objectIndex > 0
                    ? Exports[objectIndex - 1].Object
                    : null;
        }

        [PublicAPI]
        public string GetIndexObjectName(int objectIndex)
        {
            return GetIndexTable(objectIndex).ObjectName;
        }

        /// <summary>
        /// Returns a name that resides at the specified NameIndex.
        /// </summary>
        [PublicAPI]
        public string GetIndexName(int nameIndex)
        {
            return Names[nameIndex].Name;
        }

        /// <summary>
        /// Returns an UnrealTable that resides at the specified TableIndex.
        ///
        /// if index is positive an ExportTable will be returned.
        /// if index is negative an ImportTable will be returned.
        /// if index is zero null will be returned.
        /// </summary>
        [PublicAPI]
        public UObjectTableItem GetIndexTable(int tableIndex)
        {
            return tableIndex < 0
                ? Imports[-tableIndex - 1]
                : tableIndex > 0
                    ? (UObjectTableItem)Exports[tableIndex - 1]
                    : null;
        }

        [PublicAPI]
        [Obsolete("See below")]
        public UObject FindObject(string objectName, Type classType, bool checkForSubclass = false)
        {
            var obj = Objects?.Find(o => string.Compare(o.Name, objectName, StringComparison.OrdinalIgnoreCase) == 0 &&
                                         (checkForSubclass
                                             ? o.GetType().IsSubclassOf(classType)
                                             : o.GetType() == classType));
            return obj;
        }

        [PublicAPI]
        public T FindObject<T>(string objectName, bool checkForSubclass = false) where T : UObject
        {
            var obj = Objects?.Find(o => string.Compare(o.Name, objectName, StringComparison.OrdinalIgnoreCase) == 0 &&
                                         (checkForSubclass
                                             ? o.GetType().IsSubclassOf(typeof(T))
                                             : o.GetType() == typeof(T)));
            return obj as T;
        }

        [PublicAPI]
        public UObject FindObjectByGroup(string objectGroup)
        {
            if (Objects == null)
            {
                return null;
            }
            
            string[] groups = objectGroup.Split('.');
            UObject lastObj = null;
            for (var i = 0; i < groups.Length; ++i)
            {
                var obj = Objects.Find(o =>
                    string.Compare(o.Name, groups[i], StringComparison.OrdinalIgnoreCase) == 0 && o.Outer == lastObj);
                if (obj != null)
                {
                    lastObj = obj;
                }
                else
                {
                    lastObj = Objects.Find(o =>
                        string.Compare(o.Name, groups[i], StringComparison.OrdinalIgnoreCase) == 0);
                    break;
                }
            }

            return lastObj;
        }

        /// <summary>
        /// If true, the package won't have any editor data such as HideCategories, ScriptText etc.
        /// 
        /// However this condition is not only determined by the package flags property.
        /// Thus it is necessary to explicitly indicate this state.
        /// </summary>
        /// <returns>Whether package is cooked for consoles.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsConsoleCooked()
        {
            return Summary.PackageFlags.HasFlag(Flags.PackageFlags.Cooked) &&
                   Build.Flags.HasFlag(BuildFlags.ConsoleCooked);
        }

        /// <summary>
        /// Checks whether this package is marked with @flag.
        /// </summary>
        /// <param name="flag">The enum @flag to test.</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [Obsolete]
        public bool HasPackageFlag(PackageFlags flag)
        {
            return (PackageFlags & (uint)flag) != 0;
        }

        /// <summary>
        /// Checks whether this package is marked with @flag.
        /// </summary>
        /// <param name="flag">The uint @flag to test</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [Obsolete]
        public bool HasPackageFlag(uint flag)
        {
            return (PackageFlags & flag) != 0;
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is cooked.
        /// </summary>
        /// <returns>True if cooked or False if not.</returns>
        [Obsolete]
        public bool IsCooked()
        {
            return Summary.PackageFlags.HasFlag(Flags.PackageFlags.Cooked);
        }

        /// <summary>
        /// Checks for the Map flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is a map.</returns>
        [Obsolete]
        public bool IsMap()
        {
            return Summary.PackageFlags.HasFlag(Flags.PackageFlags.ContainsMap);
        }

        /// <summary>
        /// Checks if this package contains code classes.
        /// </summary>
        /// <returns>Whether if this package contains code classes.</returns>
        [Obsolete]
        public bool IsScript()
        {
            return Summary.PackageFlags.HasFlag(Flags.PackageFlags.ContainsScript);
        }

        /// <summary>
        /// Checks if this package was built using the debug configuration.
        /// </summary>
        /// <returns>Whether if this package was built in debug configuration.</returns>
        [Obsolete]
        public bool IsDebug()
        {
            return Summary.PackageFlags.HasFlag(Flags.PackageFlags.ContainsDebugData);
        }

        /// <summary>
        /// Checks for the Stripped flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is stripped.</returns>
        [Obsolete]
        public bool IsStripped()
        {
            return Summary.PackageFlags.HasFlag(Flags.PackageFlags.StrippedSource);
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is encrypted.
        /// </summary>
        /// <returns>True if encrypted or False if not.</returns>
        [Obsolete]
        public bool IsEncrypted()
        {
            return Summary.PackageFlags.HasFlag(Flags.PackageFlags.Encrypted);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsEditorData()
        {
            return Summary.UE4Version > 0 && !Summary.PackageFlags.HasFlag(Flags.PackageFlags.FilterEditorOnly);
        }

        #region IBuffered

        public byte[] CopyBuffer()
        {
            var buff = new byte[HeaderSize];
            Stream.Seek(0, SeekOrigin.Begin);
            Stream.Read(buff, 0, HeaderSize);
            if (Stream.BigEndianCode) Array.Reverse(buff);

            return buff;
        }

        public IUnrealStream GetBuffer()
        {
            return Stream;
        }

        public int GetBufferPosition()
        {
            return 0;
        }

        public int GetBufferSize()
        {
            return HeaderSize;
        }

        public string GetBufferId(bool fullName = false)
        {
            return fullName ? FullPackageName : PackageName;
        }

        #endregion

        /// <inheritdoc/>
        public override string ToString()
        {
            return PackageName;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Console.WriteLine("Disposing {0}", PackageName);

            DisposeStream();
            if (Objects != null && Objects.Any())
            {
                foreach (var obj in Objects) obj.Dispose();

                Objects.Clear();
                Objects = null;
            }
        }

        private void DisposeStream()
        {
            if (Stream == null)
                return;

            Console.WriteLine("Disposing package stream");
            Stream.Dispose();
        }

#endregion

    }
}