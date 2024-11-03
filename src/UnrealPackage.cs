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
using UELib.Branch.UE2.DNF;
using UELib.Branch.UE3.APB;
using UELib.Branch.UE3.DD2;
using UELib.Branch.UE3.GIGANTIC;
using UELib.Branch.UE3.MOH;
using UELib.Branch.UE3.RSS;
using UELib.Branch.UE4;
using UELib.Flags;

namespace UELib
{
    using Core;
    using Decoding;
    using Branch.UE2.DVS;
    using Branch.UE3.RL;
    using Branch.UE3.SFX;
    using Branch.UE2.ShadowStrike;

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
        public UPackageStream Stream;

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
                [Build(61, 0, BuildGeneration.UE1)] Unreal1,

                /// <summary>
                /// Standard, Unreal Tournament & Deus Ex
                /// 
                /// 68:69/000
                /// </summary>
                [Build(68, 69, 0u, 0u, BuildGeneration.UE1)]
                UT,

                //[Build(80, 0, BuildGeneration.UE1)]
                BrotherBear,

                /// <summary>
                /// Clive Barker's Undying
                ///
                /// 72:85/000 (Only 84 to 85 is auto detected, other versions are overlapping with older unrelated games)
                /// </summary>
                [Build(84, 85, 0u, 0u, BuildGeneration.UE1)]
                Undying,

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

                [Build(100, 17)] SC1,

                /// <summary>
                /// 100/058
                /// </summary>
                [Build(100, 58)] XIII,

                /// <summary>
                /// 110/2609
                /// </summary>
                [Build(110, 110, 2481u, 2609u)] Unreal2,

                /// <summary>
                /// 118:120/004:008
                /// </summary>
                [BuildEngineBranch(typeof(EngineBranchDVS))] [Build(118, 120, 4u, 8u)]
                Devastation,

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
                /// 
                /// For now we have three AA2 versions defined here to help us distinguish the byte-code token map.
                /// </summary>
                [Build(128, 32u, BuildGeneration.AGP)] [BuildEngineBranch(typeof(EngineBranchAA2))]
                AA2_2_5,

                [Build(128, 32u, BuildGeneration.AGP)] [BuildEngineBranch(typeof(EngineBranchAA2))]
                AA2_2_6,

                [Build(128, 33u, BuildGeneration.AGP)] [BuildEngineBranch(typeof(EngineBranchAA2))]
                AA2_2_8,

                /// <summary>
                /// Vanguard: Saga of Heroes
                /// 
                /// 129/035
                /// Some packages have 128/025 but those are in conflict with UT2004.
                /// </summary>
                [Build(128, 129, 34u, 35u, BuildGeneration.UE2_5)]
                Vanguard_SOH,

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
                /// Lemony Snicket's A Series of Unfortunate Events
                /// 
                /// 129/003
                /// </summary>
                [Build(129, 3, BuildGeneration.UE2)] LSGame,

                /// <summary>
                /// Stargate SG-1: The Alliance
                /// 
                /// 130/004
                /// </summary>
                [Build(130, 4, BuildGeneration.UE2_5)] SG1_TA,

                /// <summary>
                /// BioShock 1 & 2
                /// 
                /// 130:143/056:059
                /// </summary>
                [Build(130, 143, 56u, 59u, BuildGeneration.Vengeance)]
                BioShock,

                /// <summary>
                /// Men of Valor
                /// 
                /// 137/000
                /// </summary>
                [Build(137, 0u, BuildGeneration.UE2_5)]
                MOV,

                /// <summary>
                /// Duke Nukem Forever
                ///
                /// 156/036
                /// </summary>
                [Build(156, 36u, BuildGeneration.UE2)] [BuildEngineBranch(typeof(EngineBranchDNF))]
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

                [Build(100, 167, BuildGeneration.SCX)]
                SC_DA_Offline,

                /// <summary>
                /// Tom Clancy's Splinter Cell: Double Agent
                ///
                /// 275/000
                /// Overriden to version 120, so we can pickup the CppText property in UStruct (although this might be a ProcessedText reference)
                /// </summary>
                [Build(275, 0, BuildGeneration.ShadowStrike)]
                [BuildEngineBranch(typeof(EngineBranchShadowStrike))]
                [OverridePackageVersion(120)]
                SC_DA_Online,

                /// <summary>
                /// EndWar
                /// 
                /// 369/006
                /// </summary>
                [Build(329, 0)] [OverridePackageVersion((uint)PackageObjectLegacyVersion.AddedInterfacesFeature)]
                EndWar,

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
                [Build(421, 11)] MoHA,

                /// <summary>
                /// Frontlines: Fuel of War
                ///
                /// 433/052
                /// </summary>
                [Build(433, 52)] FFoW,

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
                [Build(547, 547, 28u, 32u)] [BuildEngineBranch(typeof(EngineBranchAPB))]
                APB,

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
                [Build(581, 58, BuildFlags.ConsoleCooked)] [BuildEngineBranch(typeof(EngineBranchMOH))]
                MoH,

                /// <summary>
                /// Borderlands
                /// 
                /// 584/057-058
                ///
                /// Includes back-ported features from UDK
                /// </summary>
                [Build(584, 584, 57, 58, BuildGeneration.GB)]
                Borderlands,

                /// <summary>
                /// Borderlands Game of the Year Enhanced
                /// 
                /// 594/058
                /// 
                /// Includes back-ported features from UDK of at least v813 (NativeClassGroup)
                /// Appears to be missing (v623:ExportGuids, v767:TextureAllocations, and v673:FPropertyTag's BoolValue change).
                /// Presume at least v832 from Borderlands 2 
                /// </summary>
                [Build(594, 58, BuildGeneration.GB)] [OverridePackageVersion(832)]
                Borderlands_GOTYE,

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
                /// Mass Effect: Legendary Edition
                ///
                /// 684/171
                /// Engine: 6383
                /// Cooker: 65643
                /// </summary>
                [Build(391, 0092, BuildGeneration.SFX)] // Xenon
                [Build(491, 1008, BuildGeneration.SFX)] // PC
                [Build(684, 0153, BuildFlags.ConsoleCooked, BuildGeneration.SFX)] // PS3
                [Build(684, 0171, BuildFlags.ConsoleCooked, BuildGeneration.SFX)] // LE
                [BuildEngineBranch(typeof(EngineBranchSFX))]
                ME1,

                [Build(512, 0130, BuildGeneration.SFX)] // Demo
                [Build(513, 0130, BuildGeneration.SFX)] // PC
                [Build(684, 0150, BuildFlags.ConsoleCooked, BuildGeneration.SFX)] // PS3
                [Build(684, 0168, BuildGeneration.SFX)] // LE
                [BuildEngineBranch(typeof(EngineBranchSFX))]
                ME2,

                [Build(684, 0185, BuildGeneration.SFX)] // Demo
                [Build(684, 0194, BuildFlags.ConsoleCooked, BuildGeneration.SFX)] // PC
                [Build(845, 0194, BuildFlags.ConsoleCooked, BuildGeneration.SFX)] // Wii
                [Build(685, 0205, BuildGeneration.SFX)] // LE
                [BuildEngineBranch(typeof(EngineBranchSFX))]
                ME3,

                /// <summary>
                /// Dungeon Defenders 2
                ///
                /// 687-688/111-117
                /// </summary>
                [Build(687, 688, 111, 117)] [BuildEngineBranch(typeof(EngineBranchDD2))]
                DD2,

                /// <summary>
                /// BioShock Infinite
                /// 727/075 (partially upgraded to 756 or higher)
                /// </summary>
                [Build(727, 75)] [OverridePackageVersion((uint)PackageObjectLegacyVersion.SuperReferenceMovedToUStruct)]
                Bioshock_Infinite,

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
                /// 788/001,828/000
                /// </summary>
                [Build(788, 1, BuildFlags.ConsoleCooked)]
                // Conflict with GoW3
                //[Build(828, 0, BuildFlags.ConsoleCooked)]
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
                /// 842-864/001
                /// </summary>
                [Build(842, 1, BuildFlags.ConsoleCooked)] [Build(864, 1, BuildFlags.ConsoleCooked)]
                InfinityBlade2,

                // Cannot auto-detect, ambiguous with UDK-2015-01-29
                //[Build(868, 0, BuildFlags.ConsoleCooked)]
                InfinityBlade3,

                /// <summary>
                /// 868/008
                /// </summary>
                [Build(868, 8, BuildFlags.ConsoleCooked)]
                InjusticeMobile,

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
                /// 860/002-004
                /// </summary>
                [Build(860, 860, 2, 4)] Hawken,

                /// <summary>
                /// Batman: Arkham City
                /// 
                /// 805/101
                /// </summary>
                [Build(805, 101, BuildGeneration.RSS)] [BuildEngineBranch(typeof(EngineBranchRSS))]
                Batman2,

                /// <summary>
                /// Batman: Arkham Origins
                ///
                /// 806/103
                /// 807/137-138
                /// </summary>
                [Build(806, 103, BuildGeneration.RSS)]
                [Build(807, 807, 137, 138, BuildGeneration.RSS)]
                [BuildEngineBranch(typeof(EngineBranchRSS))]
                Batman3,

                /// <summary>
                /// 807/104
                /// </summary>
                [Build(807, 104, BuildGeneration.RSS)] [BuildEngineBranch(typeof(EngineBranchRSS))]
                Batman3MP,

                /// <summary>
                /// Batman: Arkham Knight
                ///
                /// 863/32995(227 & ~8000)
                /// </summary>
                [Build(863, 32995, BuildGeneration.RSS)]
                [OverridePackageVersion(863, 227)]
                [BuildEngineBranch(typeof(EngineBranchRSS))]
                Batman4,

                /// <summary>
                /// Gigantic: Rampage Edition
                /// 
                /// 867/008:010
                /// </summary>
                [Build(867, 867, 8u, 10u)] [BuildEngineBranch(typeof(EngineBranchGigantic))]
                Gigantic,

                /// <summary>
                /// Rocket League
                /// 
                /// 867/009:032
                /// Requires third-party decompression and decryption
                /// </summary>
                [Build(867, 868, 9u, 32u)] [BuildEngineBranch(typeof(EngineBranchRL))]
                RocketLeague,

                /// <summary>
                /// Battleborn
                ///
                /// 874/078
                ///
                /// EngineVersion and CookerVersion are packed with the respective Licensee version.
                /// </summary>
                [Build(874, 78u)] Battleborn,

                /// <summary>
                /// A Hat in Time
                /// 
                /// 877:893/005
                /// 
                /// The earliest available version with any custom specifiers is 1.0 (877) - Un-Drew.
                /// </summary>
                [Build(877, 893, 5, 5)] AHIT,

                /// <summary>
                /// Special Force 2
                /// 
                /// 904/009 (Non-standard version, actual Epic version might be 692 or higher)
                /// </summary>
                [Build(904, 904, 09u, 014u)]
                [OverridePackageVersion((uint)PackageObjectLegacyVersion.ProbeMaskReducedAndIgnoreMaskRemoved)]
                SpecialForce2,
            }

            public BuildName Name { get; }

            [Obsolete] public uint Version { get; }

            [Obsolete] public uint LicenseeVersion { get; }

            public uint? OverrideVersion { get; }
            public ushort? OverrideLicenseeVersion { get; }

            public BuildGeneration Generation { get; }
            [CanBeNull] public readonly Type EngineBranchType;

            [Obsolete("To be deprecated")] public readonly BuildFlags Flags;

            public GameBuild(uint overrideVersion, ushort overrideLicenseeVersion, BuildGeneration generation,
                Type engineBranchType,
                BuildFlags flags)
            {
                OverrideVersion = overrideVersion;
                OverrideLicenseeVersion = overrideLicenseeVersion;
                Generation = generation;
                EngineBranchType = engineBranchType;
                Flags = flags;
            }

            public GameBuild(UnrealPackage package)
            {
                // If UE Explorer's PlatformMenuItem is equal to "Console", set ConsoleCooked flag.
                // This is required for correct serialization of unrecognized Console packages
                if (UnrealConfig.Platform == UnrealConfig.CookedPlatform.Console)
                {
                    Flags |= BuildFlags.ConsoleCooked;
                }

                var buildInfo = FindBuildInfo(package, out var buildAttribute);
                if (buildInfo == null)
                {
                    Name = package.Summary.LicenseeVersion == 0
                        ? BuildName.Default
                        : BuildName.Unknown;
                    return;
                }

                Name = (BuildName)Enum.Parse(typeof(BuildName), buildInfo.Name);
                Version = package.Summary.Version;
                LicenseeVersion = package.Summary.LicenseeVersion;

                if (buildAttribute != null)
                {
                    Generation = buildAttribute.Generation;
                    Flags = buildAttribute.Flags;
                }

                var overrideAttribute = buildInfo.GetCustomAttribute<OverridePackageVersionAttribute>(false);
                if (overrideAttribute != null)
                {
                    OverrideVersion = overrideAttribute.FixedVersion;
                    OverrideLicenseeVersion = overrideAttribute.FixedLicenseeVersion;
                }

                var engineBranchAttribute = buildInfo.GetCustomAttribute<BuildEngineBranchAttribute>(false);
                if (engineBranchAttribute != null)
                {
                    // We cannot create the instance here, because the instance itself may be dependent on GameBuild.
                    EngineBranchType = engineBranchAttribute.EngineBranchType;
                }
            }

            [CanBeNull]
            private FieldInfo FindBuildInfo(UnrealPackage linker, [CanBeNull] out BuildAttribute buildAttribute)
            {
                buildAttribute = null;

                // Auto-detect
                if (linker.BuildTarget == BuildName.Unset)
                {
                    var builds = typeof(BuildName).GetFields();
                    foreach (var build in builds)
                    {
                        var buildAttributes = build.GetCustomAttributes<BuildAttribute>(false);
                        buildAttribute = buildAttributes.FirstOrDefault(attr => attr.Verify(this, linker.Summary));
                        if (buildAttribute == null)
                            continue;

                        return build;
                    }

                    return null;
                }

                if (linker.BuildTarget != BuildName.Unknown)
                {
                    string buildName = Enum.GetName(typeof(BuildName), linker.BuildTarget);
                    var build = typeof(BuildName).GetField(buildName);
                    return build;
                }

                return null;
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

            public override string ToString()
            {
                return Name.ToString();
            }
        }

        public GameBuild.BuildName BuildTarget = GameBuild.BuildName.Unset;

        /// <summary>
        /// The auto-detected (can be set before deserialization to override auto-detection).
        /// This needs to be set to the correct build in order to load some game-specific data from packages.
        /// </summary>
        public GameBuild Build;

        /// <summary>
        /// The branch that we are using to load the data contained within this package.
        /// </summary>
        public EngineBranch Branch;

        /// <summary>
        /// The platform that the cooker was cooking this package for.
        /// Needs to be set to Console for decompressed .xxx packages etc.
        /// </summary>
        public BuildPlatform CookerPlatform;

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
                Branch = stream.ReadString();
            }

            public override string ToString()
            {
                return $"{Major}.{Minor}.{Patch}";
            }
        }

        public struct PackageFileSummary : IUnrealSerializableClass
        {
            public uint Version;
            public ushort LicenseeVersion;

            public uint UE4Version;
            public uint UE4LicenseeVersion;

            public UnrealFlags<PackageFlag> PackageFlags;

            [Obsolete] private const int VHeaderSize = 249;
            public int HeaderSize;

            [Obsolete] private const int VFolderName = 269;

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
            public UArray<UGuid> Heritages;

            [Obsolete] private const int VDependsOffset = 415;
            public int DependsOffset;

            public UGuid Guid;
            public UArray<UGenerationTableItem> Generations;

            private PackageFileEngineVersion PackageEngineVersion;
            private PackageFileEngineVersion PackageCompatibleEngineVersion;

            [Obsolete] private const int VEngineVersion = 245;

            [Obsolete] public const int VCookerVersion = 277;

            public int EngineVersion;
            public int CookerVersion;

            public uint CompressionFlags;

            /// <summary>
            /// A list of compressed chunks in the package.
            /// The package should be considered compressed if any.
            ///
            /// If <see cref="CompressionFlags"/> equals 0 then the list will be cleared on <see cref="UnrealPackage.Deserialize"/>
            /// 
            /// Will be null if not deserialized (<see cref="Version"/> &lt; <see cref="PackageObjectLegacyVersion.CompressionAdded"/>)
            /// </summary>
            public UArray<CompressedChunk> CompressedChunks;

            [Obsolete] private const int VPackageSource = 482;
            public uint PackageSource;

            [Obsolete] private const int VAdditionalPackagesToCook = 516;
            public UArray<string> AdditionalPackagesToCook;

            [Obsolete] private const int VImportExportGuidsOffset = 623;
            public int ImportExportGuidsOffset;
            public int ImportGuidsCount;
            public int ExportGuidsCount;

            [Obsolete] private const int VThumbnailTableOffset = 584;
            public int ThumbnailTableOffset;

            [Obsolete] private const int VTextureAllocations = 767;

            public int GatherableTextDataCount;
            public int GatherableTextDataOffset;

            public int StringAssetReferencesCount;
            public int StringAssetReferencesOffset;

            public int SearchableNamesOffset;

            public UGuid PersistentGuid;
            public UGuid OwnerPersistentGuid;

            private void SetupBuild(UnrealPackage package)
            {
                // Auto-detect
                if (package.Build == null)
                {
                    package.Build = new GameBuild(package);

                    if (package.Build.Flags.HasFlag(BuildFlags.ConsoleCooked))
                    {
                        package.CookerPlatform = BuildPlatform.Console;
                    }
                }

                if (package.CookerPlatform == BuildPlatform.Undetermined)
                {
                    if (string.Compare(
                            package.PackageDirectory,
                            "CookedPC",
                            StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        package.CookerPlatform = BuildPlatform.PC;
                    }
                    // file may also end in .pcc
                    else if (string.Compare(
                                 package.PackageDirectory,
                                 "CookedPCConsole",
                                 StringComparison.OrdinalIgnoreCase
                             ) == 0)
                    {
                        package.CookerPlatform = BuildPlatform.Console;
                    }
                    else if (string.Compare(
                                 package.PackageDirectory,
                                 "CookedPCServer",
                                 StringComparison.OrdinalIgnoreCase
                             ) == 0)
                    {
                        package.CookerPlatform = BuildPlatform.Console;
                    }
                    else if (string.Compare(
                                 package.PackageDirectory,
                                 "CookedXenon",
                                 StringComparison.OrdinalIgnoreCase
                             ) == 0)
                    {
                        package.CookerPlatform = BuildPlatform.Console;
                    }
                    else if (Path.GetExtension(package.FullPackageName) == ".xxx")
                    {
                        // ... fully compressed
                    }
                }

                if (package.Build.OverrideVersion.HasValue) Version = package.Build.OverrideVersion.Value;
                if (package.Build.OverrideLicenseeVersion.HasValue)
                    LicenseeVersion = package.Build.OverrideLicenseeVersion.Value;

                if (OverrideVersion != 0) Version = OverrideVersion;
                if (OverrideLicenseeVersion != 0) LicenseeVersion = OverrideLicenseeVersion;
            }

            // TODO: Re-use an instantiated branch if available and if the package's version and licensee are an identical match.
            private void SetupBranch(UnrealPackage package)
            {
                if (package.Build.EngineBranchType != null)
                {
                    package.Branch = (EngineBranch)Activator.CreateInstance(package.Build.EngineBranchType,
                        package.Build.Generation);
                }
                else if (package.Summary.UE4Version > 0)
                {
                    package.Branch = new EngineBranchUE4();
                }
                else
                {
                    package.Branch = new DefaultEngineBranch(package.Build.Generation);
                }

                package.Branch.Setup(package);
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
                                stream.ReadStruct(out UGuid key);
                                stream.Read(out int version);
                                stream.Read(out string friendlyName);
                            }
                        }
                        else
                        {
                            int count = stream.ReadInt32();
                            for (var i = 0; i < count; ++i)
                            {
                                stream.ReadStruct(out UGuid key);
                                stream.Read(out int version);
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
                Debug.Assert(stream.Package.Build != null);
                Console.WriteLine("Build:" + stream.Package.Build);

                SetupBranch(stream.Package);
                Debug.Assert(stream.Package.Branch != null);
                Console.WriteLine("Branch:" + stream.Package.Branch);
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
                    FolderName = stream.ReadString();
                }
#if SHADOW_STRIKE
                if (stream.Package.Build == BuildGeneration.SCX &&
                    stream.LicenseeVersion >= 83)
                {
                    // reads 0
                    int scInt32 = stream.ReadInt32();
                }
#endif
                PackageFlags = stream.ReadFlags32<PackageFlag>();
                Console.WriteLine("Package Flags:" + PackageFlags);
#if HAWKEN || GIGANTIC
                if ((stream.Package.Build == GameBuild.BuildName.Hawken ||
                     stream.Package.Build == GameBuild.BuildName.Gigantic) &&
                    stream.LicenseeVersion >= 2)
                {
                    stream.Read(out int vUnknown);
                }
#endif
#if MASS_EFFECT
                if (stream.Package.Build == BuildGeneration.SFX)
                {
                    // Untested, but seen in the reverse-engineered assembly...
                    if ((int)PackageFlags < 0)
                    {
                        // ... virtual call (didn't reverse)
                    }

                    if (PackageFlags.HasFlag(PackageFlag.Cooked) &&
                        stream.LicenseeVersion >= 194 &&
                        stream.LicenseeVersion != 1008)
                    {
                        // SFXPatch Version (according to a localized string that references the same global constant)
                        int v94 = stream.ReadInt32();
                    }
                }
#endif
                NameCount = stream.ReadInt32();
                NameOffset = stream.ReadInt32();
#if UE4
                if (stream.UE4Version >= 516 && stream.Package.ContainsEditorData())
                {
                    LocalizationId = stream.ReadString();
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
#if SHADOW_STRIKE
                // No version check, not serialized for DA_Online.
                if (stream.Package.Build == BuildGeneration.SCX)
                {
                    int scInt32_2 = stream.ReadInt32();
                    Debug.Assert(scInt32_2 == 0xff0adde);
                    
                    string scSaveInfo = stream.ReadText();
                }
#endif
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
#if SPLINTERCELL
                if (stream.Package.Build == GameBuild.BuildName.SC1 &&
                    stream.LicenseeVersion >= 12)
                {
                    // compiled-constant: 0xff0adde
                    stream.Read(out int uStack_10c);

                    // An FString converted to an FArray? Concatenating appUserName, appComputerName, appBaseDir, and appTimestamp.
                    stream.ReadArray(out UArray<byte> iStack_fc);
                }
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
#if BORDERLANDS
                    && stream.Package.Build != GameBuild.BuildName.Borderlands_GOTYE
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
                if (stream.Package.Build == GameBuild.BuildName.DD2 && PackageFlags.HasFlag(PackageFlag.Cooked))
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
                {
                    goto skipGuid;
                }
#endif
                stream.ReadStruct(out Guid);
                Console.WriteLine("GUID:" + Guid);
            skipGuid:
#if TERA
                if (stream.Package.Build == GameBuild.BuildName.Tera) stream.Position -= 4;
#endif
#if MKKE
                if (stream.Package.Build == GameBuild.BuildName.MKKE)
                {
                    goto skipGenerations;
                }
#endif
                int generationCount = stream.ReadInt32();
                Contract.Assert(generationCount >= 0);
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
            skipGenerations:
#if DNF
                if (stream.Package.Build == GameBuild.BuildName.DNF &&
                    stream.Version >= 151)
                {
                    if (PackageFlags.HasFlags(0x20U))
                    {
                        int buildMonth = stream.ReadInt32();
                        int buildYear = stream.ReadInt32();
                        int buildDay = stream.ReadInt32();
                        int buildSeconds = stream.ReadInt32();
                    }

                    string dnfString = stream.ReadString();

                    // DLC package
                    if (PackageFlags.HasFlags(0x80U))
                    {
                        // No additional data, just DLC authentication.
                    }
                }
#endif
                if (stream.Version >= VEngineVersion &&
                    stream.UE4Version == 0)
                {
                    // The Engine Version this package was created with
                    EngineVersion = stream.ReadInt32();
                    Console.WriteLine("EngineVersion:" + EngineVersion);
                }
#if UE4
                if (stream.Package.ContainsEditorData())
                {
                    if (stream.UE4Version >= 518)
                    {
                        stream.ReadStruct(out PersistentGuid);
                        if (stream.UE4Version < 520)
                        {
                            stream.ReadStruct(out OwnerPersistentGuid);
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
#if MASS_EFFECT
                if (stream.Package.Build == BuildGeneration.SFX)
                {
                    // Appears to be similar to a PackageFileEngineVersion

                    if (stream.LicenseeVersion >= 16 && stream.LicenseeVersion < 136)
                    {
                        stream.Read(out int _);
                    }

                    if (stream.LicenseeVersion >= 32 && stream.LicenseeVersion < 136)
                    {
                        stream.Read(out int _);
                    }

                    if (stream.LicenseeVersion >= 35 && stream.LicenseeVersion < 113)
                    {
                        stream.ReadMap(out UMap<string, UArray<string>> branch);
                        Console.WriteLine("Branch:" + branch);
                    }

                    if (stream.LicenseeVersion >= 37)
                    {
                        // Compiler-Constant ? 1
                        stream.Read(out int _);

                        // Compiler-Constant changelist? 1376256 (Mass Effect 1: LE)
                        stream.Read(out int _);
                    }

                    if (stream.LicenseeVersion >= 39 && stream.LicenseeVersion < 136)
                    {
                        stream.Read(out int _);
                    }
                }
#endif
                // Read compressed info?
                if (stream.Version >= (uint)PackageObjectLegacyVersion.CompressionAdded)
                {
                    CompressionFlags = stream.ReadUInt32();
                    Console.WriteLine("CompressionFlags:" + CompressionFlags);

                    stream.ReadArray(out CompressedChunks);
                }

                // SFX reads 392?
                if (stream.Version >= VPackageSource)
                {
                    PackageSource = stream.ReadUInt32();
                    Console.WriteLine("PackageSource:" + PackageSource);
                }
#if MASS_EFFECT
                if (stream.Package.Build == BuildGeneration.SFX)
                {
                    if (stream.LicenseeVersion >= 44 && stream.LicenseeVersion < 136)
                    {
                        stream.Read(out int _);
                    }
                }
#endif
#if UE4
                if (stream.UE4Version > 0)
                {
                    return;
                }
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
#if BORDERLANDS
                if (stream.Package.Build == GameBuild.BuildName.Borderlands_GOTYE)
                {
                    return;
                }
#endif
#if BATTLEBORN
                if (stream.Package.Build == GameBuild.BuildName.Battleborn)
                {
                    // FIXME: Package format is being deserialized incorrectly and fails here.
                    stream.ReadUInt32();

                    return;
                }
#endif
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
                    && PackageFlags.HasFlag(PackageFlag.Cooked))
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

        [Obsolete] public const int VSIZEPREFIXDEPRECATED = 64;

        [Obsolete] public const int VINDEXDEPRECATED = 178;

        [Obsolete] public const int VDLLBIND = 655;

        [Obsolete] public const int VCLASSGROUP = 789;

        [Obsolete] public const int VCOOKEDPACKAGES = 277;

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
        public List<UNameTableItem> Names { get; private set; } = new List<UNameTableItem>();

        /// <summary>
        /// List of info about exported objects.
        /// </summary>
        [PublicAPI]
        public List<UExportTableItem> Exports { get; private set; } = new List<UExportTableItem>();

        /// <summary>
        /// List of info about imported objects.
        /// </summary>
        [PublicAPI]
        public List<UImportTableItem> Imports { get; private set; } = new List<UImportTableItem>();

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
        public List<UObject> Objects { get; private set; } = new List<UObject>();

        [PublicAPI] public NativesTablePackage NTLPackage;

        [Obsolete("See UPackageStream.Decoder", true)]
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
            Branch.PostDeserializeSummary(this, stream, ref Summary);
            Debug.Assert(Branch.Serializer != null,
                "Branch.Serializer cannot be null. Did you forget to initialize the Serializer in PostDeserializeSummary?");

            // We can't continue without decompressing.
            if (Summary.CompressedChunks != null &&
                Summary.CompressedChunks.Any())
            {
                if (Summary.CompressionFlags != 0)
                {
                    return;
                }

                // Flags 0? Let's pretend that we no longer possess any chunks.
                Summary.CompressedChunks.Clear();
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
                    Branch.Serializer.Deserialize(stream, nameEntry);
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
                stream.ReadArray(out Summary.Heritages, Summary.HeritageCount);

                BinaryMetaData.AddField(nameof(Summary.Heritages), Summary.Heritages, Summary.HeritageOffset,
                    stream.Position - Summary.HeritageOffset);
            }

            // Read Import Table
            if (Summary.ImportCount > 0)
            {
                stream.Seek(Summary.ImportOffset, SeekOrigin.Begin);
                Imports = new List<UImportTableItem>(Summary.ImportCount);
                for (var i = 0; i < Summary.ImportCount; ++i)
                {
                    var imp = new UImportTableItem { Offset = (int)stream.Position, Index = i, Owner = this };
                    Branch.Serializer.Deserialize(stream, imp);
                    imp.Size = (int)(stream.Position - imp.Offset);
                    Imports.Add(imp);
                }

                BinaryMetaData.AddField(nameof(Imports), Imports, Summary.ImportOffset,
                    stream.Position - Summary.ImportOffset);
            }

            // Read Export Table
            if (Summary.ExportCount > 0)
            {
                stream.Seek(Summary.ExportOffset, SeekOrigin.Begin);
                Exports = new List<UExportTableItem>(Summary.ExportCount);
                for (var i = 0; i < Summary.ExportCount; ++i)
                {
                    var exp = new UExportTableItem { Offset = (int)stream.Position, Index = i, Owner = this };
                    Branch.Serializer.Deserialize(stream, exp);
                    exp.Size = (int)(stream.Position - exp.Offset);
                    Exports.Add(exp);
                }

                BinaryMetaData.AddField(nameof(Exports), Exports, Summary.ExportOffset,
                    stream.Position - Summary.ExportOffset);

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

                        BinaryMetaData.AddField(nameof(dependsMap), dependsMap, Summary.DependsOffset,
                            stream.Position - Summary.DependsOffset);
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
                        string levelName = stream.ReadString();
                        int guidCount = stream.ReadInt32();
                        stream.Skip(guidCount * 16);
                    }

                    for (var i = 0; i < Summary.ExportGuidsCount; ++i)
                    {
                        stream.ReadStruct(out UGuid objectGuid);
                        int exportIndex = stream.ReadInt32();
                    }

                    if (stream.Position != Summary.ImportExportGuidsOffset)
                    {
                        BinaryMetaData.AddField("ImportExportGuids", null, Summary.ImportExportGuidsOffset,
                            stream.Position - Summary.ImportExportGuidsOffset);
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
                    BinaryMetaData.AddField("Thumbnails", null, Summary.ThumbnailTableOffset,
                        stream.Position - Summary.ThumbnailTableOffset);
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

            Branch.PostDeserializePackage(stream.Package, stream);
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
        [Obsolete("Pending deprecation")]
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

            if ((initFlags & InitFlags.Construct) == 0)
            {
                return;
            }

            ConstructObjects();
            if ((initFlags & InitFlags.Deserialize) == 0)
                return;

            try
            {
                DeserializeObjects();
            }
            catch (Exception ex)
            {
                throw new UnrealException("Deserialization", ex);
            }

            try
            {
                if ((initFlags & InitFlags.Link) != 0) LinkObjects();
            }
            catch (Exception ex)
            {
                throw new UnrealException("Linking", ex);
            }
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
                }
                catch (InvalidCastException)
                {
                    Console.WriteLine("InvalidCastException occurred on object: " + exp.Object);
                }
                finally
                {
                    OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Object));
                }
        }

        private void RegisterExportedClassTypes()
        {
            var exportedTypes = Assembly.GetExecutingAssembly().GetExportedTypes();
            foreach (var exportedType in exportedTypes)
            {
                object[] attributes = exportedType.GetCustomAttributes(typeof(UnrealRegisterClassAttribute), false);
                if (attributes.Length == 1) TryAddClassType(exportedType.Name.Substring(1), exportedType);
            }
        }

        #endregion

        #region Methods

        // Create pseudo objects for imports so that we have non-null references to imports.
        private void CreateObject(UImportTableItem item)
        {
            var classType = GetClassType(item.ClassName);
            item.Object = (UObject)Activator.CreateInstance(classType);
            AddObject(item.Object, item);
            OnNotifyPackageEvent(new PackageEventArgs(PackageEventArgs.Id.Object));
        }

        private void CreateObject(UExportTableItem item)
        {
            var objectClass = item.Class;
            var classType = GetClassType(objectClass != null ? objectClass.ObjectName : "Class");
            // Try one of the "super" classes for unregistered classes.
        loop:
            if (objectClass != null && classType == typeof(UnknownObject))
            {
                switch (objectClass)
                {
                    case UExportTableItem classExp:
                        var super = classExp.Super;
                        switch (super)
                        {
                            //case UImportTableItem superImport:
                            //    CreateObject(superImport);
                            //    return;

                            case UExportTableItem superExport:
                                objectClass = superExport;
                                classType = GetClassType(objectClass.ObjectName);
                                goto loop;
                        }

                        break;
                }
            }

            item.Object = (UObject)Activator.CreateInstance(classType);
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
            Stream.Writer.Write(PackageFlags);
        }

        [PublicAPI]
        [Obsolete]
        public void RegisterClass(string className, Type classObject)
        {
            TryAddClassType(className, classObject);
        }

        [PublicAPI]
        public bool TryAddClassType(string className, Type classObject)
        {
            return _ClassTypes.TryAdd(className.ToLower(), classObject);
        }

        [PublicAPI]
        [NotNull]
        public Type GetClassType(string className)
        {
            _ClassTypes.TryGetValue(className.ToLower(), out var classType);
            return classType ?? typeof(UnknownObject);
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
            return Summary.PackageFlags.HasFlag(PackageFlag.Cooked)
                   && CookerPlatform == BuildPlatform.Console;
        }

        /// <summary>
        /// Checks whether this package is marked with @flags.
        /// </summary>
        /// <param name="flags">The enum @flag to test.</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [Obsolete("See Summary.PackageFlags.HasFlag")]
        public bool HasPackageFlag(PackageFlags flags)
        {
            return Summary.PackageFlags.HasFlags((uint)flags);
        }

        /// <summary>
        /// Checks whether this package is marked with @flags.
        /// </summary>
        /// <param name="flags">The uint @flag to test</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [Obsolete("See Summary.PackageFlags.HasFlag")]
        public bool HasPackageFlag(uint flags)
        {
            return (PackageFlags & flags) != 0;
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is cooked.
        /// </summary>
        /// <returns>True if cooked or False if not.</returns>
        [Obsolete]
        public bool IsCooked()
        {
            return Summary.PackageFlags.HasFlag(PackageFlag.Cooked);
        }

        /// <summary>
        /// Checks for the Map flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is a map.</returns>
        [Obsolete]
        public bool IsMap()
        {
            return Summary.PackageFlags.HasFlag(PackageFlag.ContainsMap);
        }

        /// <summary>
        /// Checks if this package contains code classes.
        /// </summary>
        /// <returns>Whether if this package contains code classes.</returns>
        [Obsolete]
        public bool IsScript()
        {
            return Summary.PackageFlags.HasFlag(PackageFlag.ContainsScript);
        }

        /// <summary>
        /// Checks if this package was built using the debug configuration.
        /// </summary>
        /// <returns>Whether if this package was built in debug configuration.</returns>
        [Obsolete]
        public bool IsDebug()
        {
            return Summary.PackageFlags.HasFlag(PackageFlag.ContainsDebugData);
        }

        /// <summary>
        /// Checks for the Stripped flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is stripped.</returns>
        [Obsolete]
        public bool IsStripped()
        {
            return Summary.PackageFlags.HasFlag(PackageFlag.StrippedSource);
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is encrypted.
        /// </summary>
        /// <returns>True if encrypted or False if not.</returns>
        [Obsolete]
        public bool IsEncrypted()
        {
            return Summary.PackageFlags.HasFlag(PackageFlag.Encrypted);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsEditorData()
        {
            return Summary.UE4Version > 0 && !Summary.PackageFlags.HasFlag(PackageFlag.FilterEditorOnly);
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
            if (Objects != null && Objects.Any())
            {
                foreach (var obj in Objects) obj.Dispose();

                Objects.Clear();
                Objects = null;
            }

            if (Stream == null)
                return;

            Stream.Close();
            Stream = null;
        }

        #endregion
    }
}
