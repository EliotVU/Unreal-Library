using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UELib.Annotations;
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
    public sealed class UnrealPackage : IDisposable, IBuffered
    {
        #region General Members

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

        #region Serialized Members

        public uint Version { get; set; }

        /// <summary>
        /// For debugging purposes. Change this to override the present Version deserialized from the package.
        /// </summary>
        public static ushort OverrideVersion;

        #region Version history

        public const int VSIZEPREFIXDEPRECATED = 64;
        public const int VINDEXDEPRECATED = 178;
        public const int VCOOKEDPACKAGES = 277;

        /// <summary>
        /// DLLBind(Name)
        /// </summary>
        public const int VDLLBIND = 655;

        /// <summary>
        /// New class modifier "ClassGroup(Name[,Name])"
        /// </summary>
        public const int VCLASSGROUP = 789;

        private const int VCompression = 334;
        private const int VEngineVersion = 245;
        private const int VGroup = 269;
        private const int VHeaderSize = 249;
        private const int VPackageSource = 482;
        private const int VAdditionalPackagesToCook = 516;
        private const int VTextureAllocations = 767;

        #endregion

        public ushort LicenseeVersion { get; set; }

        /// <summary>
        /// For debugging purposes. Change this to override the present Version deserialized from the package.
        /// </summary>
        public static ushort OverrideLicenseeVersion;

        // TODO: Move to UnrealBuild.cs
        public sealed class GameBuild : object
        {
            [UsedImplicitly]
            [AttributeUsage(AttributeTargets.Field)]
            private sealed class BuildDecoderAttribute : Attribute
            {
                private readonly Type _BuildDecoder;

                public BuildDecoderAttribute(Type buildDecoder)
                {
                    _BuildDecoder = buildDecoder;
                }

                public IBufferDecoder CreateDecoder()
                {
                    return (IBufferDecoder)Activator.CreateInstance(_BuildDecoder);
                }
            }

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

                public bool Verify(GameBuild gb, UnrealPackage package)
                {
                    return _VerifyEqual
                        ? package.Version == _MinVersion && package.LicenseeVersion == _MinLicensee
                        : package.Version >= _MinVersion && package.Version <= _MaxVersion
                                                         && package.LicenseeVersion >= _MinLicensee
                                                         && package.LicenseeVersion <= _MaxLicensee;
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
                /// 61/000
                /// </summary>
                [Build(61, 0)] Unreal1,

                /// <summary>
                /// Standard, Unreal Tournament & Deus Ex
                /// 68:69/000
                /// </summary>
                [Build(68, 69, 0u, 0u)] UT,

                /// <summary>
                /// Deus Ex: Invisible War
                /// Missing support for custom classes such as BitfieldProperty and BitfieldEnum among others.
                /// 95/69
                /// </summary>
                [Build(95, 69, BuildGeneration.Thief)] DeusEx_IW,

                /// <summary>
                /// Thief: Deadly Shadows
                /// 95/133
                /// </summary>
                [Build(95, 133, BuildGeneration.Thief)]
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
                [Build(110, 2609)] Unreal2,

                /// <summary>
                /// Tom Clancy's Rainbow Six 3: Raven Shield
                /// 118/011:014
                /// </summary>
                [Build(118, 118, 11u, 14u)] R6RS,

                /// <summary>
                /// Unreal II: eXpanded MultiPlayer
                /// 126/000
                /// </summary>
                [Build(126, 0)] Unreal2XMP,

                /// <summary>
                /// 118:128/025:029
                /// (Overlaps latest UT2003)
                /// </summary>
                [Build(118, 128, 25u, 29u, BuildGeneration.UE2_5)]
                UT2004,

                /// <summary>
                /// Built on UT2004
                /// Represents both AAO and AAA
                /// 128/032:033
                /// </summary>
                [Build(128, 128, 32u, 33u, BuildGeneration.UE2_5)]
                AA2,

                // IrrationalGames/Vengeance - 129:143/027:059

                /// <summary>
                /// Tribes: Vengeance
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
                /// 130:143/056:059
                /// </summary>
                [Build(130, 143, 56u, 59u, BuildGeneration.Vengeance)]
                BioShock,

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
                /// 369/006
                /// </summary>
                [Build(369, 6)] RoboBlitz,

                /// <summary>
                /// 421/011
                /// </summary>
                [Build(421, 11)] MOHA,

                /// <summary>
                /// 472/046
                /// </summary>
                [Build(472, 46, BuildFlags.ConsoleCooked)]
                MKKE,

                /// <summary>
                /// 490/009
                /// </summary>
                [Build(490, 9)] GoW1,
                
                [Build(511, 039, BuildGeneration.HMS)] // The Bourne Conspiracy
                [Build(511, 145, BuildGeneration.HMS)] // Transformers: War for Cybertron (PC version)
                [Build(511, 144, BuildGeneration.HMS)] // Transformers: War for Cybertron (PS3 and XBox 360 version)
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
                [Build(537, 174, BuildGeneration.HMS)]
                Transformers2,
                
                /// <summary>
                /// 539/091
                /// </summary>
                [Build(539, 91)] AlphaProtocol,

                /// <summary>
                /// 547/028:032
                /// </summary>
                [Build(547, 547, 28u, 32u)] APB,

                /// <summary>
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
                /// 576/021
                /// 
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
                /// 648/6405
                /// </summary>
                [Build(648, 6405)] DCUO,

                [Build(687, 111)] DungeonDefenders2,

                /// <summary>
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
                /// 845/059
                /// </summary>
                [Build(845, 59)] XCOM_EU,

                /// <summary>
                /// 845/120
                /// </summary>
                [Build(845, 120)] XCOM2WotC,

                /// <summary>
                /// Transformers: Fall of Cybertron
                /// 846/181
                /// </summary>
                [Build(846, 181, BuildGeneration.HMS)]
                [OverridePackageVersion(587)]
                Transformers3,

                /// <summary>
                /// 860/004
                /// </summary>
                [Build(860, 4)] Hawken,

                /// <summary>
                /// 805-6/101-3
                /// 807/137-8
                /// 807/104
                /// 863/32995
                /// </summary>
                [Build(805, 101, BuildGeneration.Batman2)]
                [Build(806, 103, BuildGeneration.Batman3)]
                [Build(807, 807, 137, 138, BuildGeneration.Batman3)]
                [Build(807, 104, BuildGeneration.Batman3MP)]
                [Build(863, 32995, BuildGeneration.Batman4)]
                BatmanUDK,

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

            public readonly BuildFlags Flags;

            public GameBuild(UnrealPackage package)
            {
                if (UnrealConfig.Platform == UnrealConfig.CookedPlatform.Console) Flags |= BuildFlags.ConsoleCooked;

                var builds = typeof(BuildName).GetFields();
                foreach (var build in builds)
                {
                    var buildAttributes = build.GetCustomAttributes<BuildAttribute>(false);
                    var buildAttribute = buildAttributes.FirstOrDefault(attr => attr.Verify(this, package));
                    if (buildAttribute == null)
                        continue;

                    Version = package.Version;
                    LicenseeVersion = package.LicenseeVersion;
                    Flags = buildAttribute.Flags;
                    Generation = buildAttribute.Generation;
                    
                    var overrideAttribute = build.GetCustomAttribute<OverridePackageVersionAttribute>(false);
                    if (overrideAttribute != null)
                    {
                        OverrideVersion = overrideAttribute.FixedVersion;
                        OverrideLicenseeVersion = overrideAttribute.FixedLicenseeVersion;
                    }

                    Name = (BuildName)Enum.Parse(typeof(BuildName), build.Name);
                    if (package.Decoder != null) break;

                    var buildDecoderAttribute = build.GetCustomAttribute<BuildDecoderAttribute>(false);
                    if (buildDecoderAttribute == null)
                        break;

                    package.Decoder = buildDecoderAttribute.CreateDecoder();
                    break;
                }

                if (Name == BuildName.Unset)
                    Name = package.LicenseeVersion == 0 ? BuildName.Default : BuildName.Unknown;
            }

            public static bool operator ==(GameBuild b, BuildGeneration gen)
            {
                return b?.Generation == gen;
            }

            public static bool operator !=(GameBuild b, BuildGeneration gen)
            {
                return b?.Generation != gen;
            }

            public static bool operator ==(GameBuild b, BuildName name)
            {
                return b?.Name == name;
            }

            public static bool operator !=(GameBuild b, BuildName name)
            {
                return b?.Name != name;
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
        }

        public GameBuild Build { get; private set; }

        /// <summary>
        /// Whether the package was serialized in BigEndian encoding.
        /// </summary>
        public bool IsBigEndianEncoded { get; }

        /// <summary>
        /// The bitflags of this package.
        /// </summary>
        public uint PackageFlags;

        /// <summary>
        /// Size of the Header. Basically points to the first Object in the package.
        /// </summary>
        public int HeaderSize { get; private set; }

        /// <summary>
        /// The group the package is associated with in the Content Browser.
        /// </summary>
        public string Group;

        public struct TablesData : IUnrealSerializableClass
        {
            public int NamesCount;
            public int NamesOffset { get; internal set; }

            public int ExportsCount { get; internal set; }
            public int ExportsOffset { get; internal set; }

            public int ImportsCount { get; internal set; }
            public int ImportsOffset { get; internal set; }

            private const int VDependsOffset = 415;
            public int DependsOffset;

            private const int VImportExportGuidsOffset = 623;
            public int ImportExportGuidsOffset;
            public int ImportGuidsCount;
            public int ExportGuidsCount;

            private const int VThumbnailTableOffset = 584;
            public int ThumbnailTableOffset;

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(NamesCount);
                stream.Write(NamesOffset);

                stream.Write(ExportsCount);
                stream.Write(ExportsOffset);

                stream.Write(ImportsCount);
                stream.Write(ImportsOffset);

                if (stream.Version >= 414)
                    stream.Write(DependsOffset);
            }

            public void Deserialize(IUnrealStream stream)
            {
#if HAWKEN
                if (stream.Package.Build == GameBuild.BuildName.Hawken &&
                    stream.Package.LicenseeVersion >= 2)
                    stream.Skip(4);
#endif
                NamesCount = stream.ReadInt32();
                NamesOffset = stream.ReadInt32();
                ExportsCount = stream.ReadInt32();
                ExportsOffset = stream.ReadInt32();
#if APB
                if (stream.Package.Build == GameBuild.BuildName.APB &&
                    stream.Package.LicenseeVersion >= 28)
                {
                    if (stream.Package.LicenseeVersion >= 29)
                    {
                        stream.Skip(4);
                    }

                    stream.Skip(20);
                }
#endif
                ImportsCount = stream.ReadInt32();
                ImportsOffset = stream.ReadInt32();

                Console.WriteLine("Names Count:" + NamesCount + " Names Offset:" + NamesOffset
                                  + " Exports Count:" + ExportsCount + " Exports Offset:" + ExportsOffset
                                  + " Imports Count:" + ImportsCount + " Imports Offset:" + ImportsOffset
                );

                if (stream.Version >= VDependsOffset)
                {
                    DependsOffset = stream.ReadInt32();
                }

                if (stream.Version >= VImportExportGuidsOffset
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
                if (stream.Version >= VThumbnailTableOffset)
                {
#if APB
                    if (stream.Package.Build == GameBuild.BuildName.DungeonDefenders2) stream.Skip(4);
#endif
                    ThumbnailTableOffset = stream.ReadInt32();
                }

                // Generations
                // ... etc, see Deserialize() below
            }
        }

        private TablesData _TablesData;
        public TablesData Summary => _TablesData;

        /// <summary>
        /// The guid of this package. Used to test if the package on a client is equal to the one on a server.
        /// </summary>
        [PublicAPI]
        public string GUID { get; private set; }

        /// <summary>
        /// List of heritages. UE1 way of defining generations.
        /// </summary>
        private IList<ushort> _Heritages;

        /// <summary>
        /// List of package generations.
        /// </summary>
        [PublicAPI]
        public UArray<UGenerationTableItem> Generations => _Generations;

        private UArray<UGenerationTableItem> _Generations;

        /// <summary>
        /// The Engine version the package was created with.
        /// </summary>
        [DefaultValue(-1)]
        [PublicAPI]
        public int EngineVersion { get; private set; }

        /// <summary>
        /// The Cooker version the package was cooked with.
        /// </summary>
        [PublicAPI]
        public int CookerVersion { get; private set; }

        /// <summary>
        /// The type of compression the package is compressed with.
        /// </summary>
        [PublicAPI]
        public uint CompressionFlags { get; private set; }

        /// <summary>
        /// List of compressed chunks throughout the package.
        /// Null if package version less is than <see cref="VCompression" />
        /// </summary>
        [PublicAPI("UE Explorer requires 'get'")]
        [CanBeNull]
        public UArray<CompressedChunk> CompressedChunks => _CompressedChunks;

        [CanBeNull] private UArray<CompressedChunk> _CompressedChunks;

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

        #endregion

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

        [PublicAPI] public IBufferDecoder Decoder;

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
            // Serialize tables
            var namesBuffer = new UObjectStream(stream);
            foreach (var name in Names) name.Serialize(namesBuffer);

            var importsBuffer = new UObjectStream(stream);
            foreach (var import in Imports) import.Serialize(importsBuffer);

            var exportsBuffer = new UObjectStream(stream);
            foreach (var export in Exports) export.Serialize(exportsBuffer);

            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(Signature);

            // Serialize header
            int version = (int)(Version & 0x0000FFFFU) | (LicenseeVersion << 16);
            stream.Write(version);

            long headerSizePosition = stream.Position;
            if (Version >= VHeaderSize)
            {
                stream.Write(HeaderSize);
                if (Version >= VGroup) stream.Write(Group);
            }

            stream.Write(PackageFlags);

            _TablesData.NamesCount = Names.Count;
            _TablesData.ExportsCount = Exports.Count;
            _TablesData.ImportsCount = Imports.Count;

            long tablesDataPosition = stream.Position;
            _TablesData.Serialize(stream);

            // TODO: Serialize Heritages

            stream.Write(Guid.NewGuid().ToByteArray(), 0, 16);
            //Generations.Serialize(stream);

            if (Version >= VEngineVersion)
            {
                stream.Write(EngineVersion);
                if (Version >= VCOOKEDPACKAGES)
                {
                    stream.Write(CookerVersion);

                    if (Version >= VCompression)
                        if (IsCooked())
                            stream.Write(CompressionFlags);
                    //CompressedChunks.Serialize(stream);
                }
            }

            // TODO: Unknown data
            stream.Write((uint)0);

            // Serialize objects

            // Write tables

            // names
            Console.WriteLine("Writing names at position " + stream.Position);
            _TablesData.NamesOffset = (int)stream.Position;
            byte[] namesBytes = namesBuffer.GetBuffer();
            stream.Write(namesBytes, 0, (int)namesBuffer.Length);

            // imports
            Console.WriteLine("Writing imports at position " + stream.Position);
            _TablesData.ImportsOffset = (int)stream.Position;
            byte[] importsBytes = importsBuffer.GetBuffer();
            stream.Write(importsBytes, 0, (int)importsBuffer.Length);

            // exports
            Console.WriteLine("Writing exports at position " + stream.Position);

            // Serialize tables data again now that offsets are known.
            long currentPosition = stream.Position;
            stream.Seek(tablesDataPosition, SeekOrigin.Begin);
            _TablesData.Serialize(stream);
            stream.Seek(currentPosition, SeekOrigin.Begin);
        }

        // TODO: Move to FilePackageSummary, but first we want to merge the support-* branches
        public void Deserialize(UPackageStream stream)
        {
            // Read as one variable due Big Endian Encoding.
            Version = stream.ReadUInt32();
            LicenseeVersion = (ushort)(Version >> 16);
            Version &= 0xFFFFU;
            Console.WriteLine("Package Version:" + Version + "/" + LicenseeVersion);
            SetupBuild(stream);
            stream.BuildDetected(Build);
            Console.WriteLine("Build:" + Build.Name);

            if (Version >= VHeaderSize)
            {
#if BIOSHOCK
                if (Build == GameBuild.BuildName.Bioshock_Infinite)
                {
                    int unk = stream.ReadInt32();
                }
#endif
#if MKKE
                if (Build == GameBuild.BuildName.MKKE) stream.Skip(8);
#endif
#if TRANSFORMERS
                if (Build == BuildGeneration.HMS && 
                    LicenseeVersion >= 55)
                {
                    if (LicenseeVersion >= 181) stream.Skip(16);

                    stream.Skip(4);
                }
#endif
                // Offset to the first class(not object) in the package.
                HeaderSize = stream.ReadInt32();
                Console.WriteLine("Header Size: " + HeaderSize);
            }

            if (Version >= VGroup)
            {
                // UPK content category e.g. Weapons, Sounds or Meshes.
                Group = stream.ReadText();
            }

            // Bitflags such as AllowDownload.
            PackageFlags = stream.ReadUInt32();
            Console.WriteLine("Package Flags:" + PackageFlags);

            // Summary data such as ObjectCount.
            _TablesData = new TablesData();
            _TablesData.Deserialize(stream);
            if (Version < 68)
            {
                int heritageCount = stream.ReadInt32();
                int heritageOffset = stream.ReadInt32();

                stream.Seek(heritageOffset, SeekOrigin.Begin);
                _Heritages = new List<ushort>(heritageCount);
                for (var i = 0; i < heritageCount; ++i) _Heritages.Add(stream.ReadUShort());
            }
            else
            {
#if THIEF_DS || DEUSEX_IW
                if (Build == GameBuild.BuildName.Thief_DS ||
                    Build == GameBuild.BuildName.DeusEx_IW)
                {
                    //stream.Skip( 4 );
                    int unknown = stream.ReadInt32();
                    Console.WriteLine("Unknown:" + unknown);
                }
#endif
#if BORDERLANDS
                if (Build == GameBuild.BuildName.Borderlands) stream.Skip(4);
#endif
#if MKKE
                if (Build == GameBuild.BuildName.MKKE) stream.Skip(4);
#endif
                if (Build == GameBuild.BuildName.Spellborn
                    && stream.Version >= 148)
                    goto skipGuid;
                GUID = stream.ReadGuid().ToString();
                Console.WriteLine("GUID:" + GUID);
                skipGuid:
#if TERA
                if (Build == GameBuild.BuildName.Tera) stream.Position -= 4;
#endif
#if MKKE
                if (Build != GameBuild.BuildName.MKKE)
                {
#endif
                    int generationCount = stream.ReadInt32();
                    Console.WriteLine("Generations Count:" + generationCount);
#if APB
                    // Guid, however only serialized for the first generation item.
                    if (stream.Package.Build == GameBuild.BuildName.APB &&
                        stream.Package.LicenseeVersion >= 32)
                    {
                        stream.Skip(16);
                    }
#endif
                    stream.ReadArray(out _Generations, generationCount);
#if MKKE
                }
#endif
                if (Version >= VEngineVersion)
                {
                    // The Engine Version this package was created with
                    EngineVersion = stream.ReadInt32();
                    Console.WriteLine("EngineVersion:" + EngineVersion);
                }

                if (Version >= VCOOKEDPACKAGES)
                {
                    // The Cooker Version this package was cooked with
                    CookerVersion = stream.ReadInt32();
                    Console.WriteLine("CookerVersion:" + CookerVersion);
                }

                // Read compressed info?
                if (Version >= VCompression)
                {
                    CompressionFlags = stream.ReadUInt32();
                    Console.WriteLine("CompressionFlags:" + CompressionFlags);
                    stream.ReadArray(out _CompressedChunks);
                }

                if (Version >= VPackageSource)
                {
                    uint packageSource = stream.ReadUInt32();
                    Console.WriteLine("PackageSource:" + packageSource);
                }

                if (Version >= VAdditionalPackagesToCook)
                {
#if TRANSFORMERS
                    if (Build == BuildGeneration.HMS)
                    {
                        goto endOfSummary;
                    }
#endif
                    UArray<string> additionalPackagesToCook;
                    stream.ReadArray(out additionalPackagesToCook);
#if DCUO
                    if (Build == GameBuild.BuildName.DCUO)
                    {
                        var realNameOffset = (int)stream.Position;
                        Debug.Assert(
                            realNameOffset <= _TablesData.NamesOffset,
                            "realNameOffset is > the parsed name offset for a DCUO package, we don't know where to go now!"
                        );

                        int offsetDif = _TablesData.NamesOffset - realNameOffset;
                        _TablesData.NamesOffset -= offsetDif;
                        _TablesData.ImportsOffset -= offsetDif;
                        _TablesData.ExportsOffset -= offsetDif;
                        _TablesData.DependsOffset = 0; // not working
                        _TablesData.ImportExportGuidsOffset -= offsetDif;
                        _TablesData.ThumbnailTableOffset -= offsetDif;
                    }
#endif
                }

                if (Version >= VTextureAllocations)
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
            }
#if ROCKETLEAGUE
            if (Build == GameBuild.BuildName.RocketLeague
                && IsCooked())
            {
                int garbageSize = stream.ReadInt32();
                Debug.WriteLine(garbageSize, "GarbageSize");
                int compressedChunkInfoOffset = stream.ReadInt32();
                Debug.WriteLine(compressedChunkInfoOffset, "CompressedChunkInfoOffset");
                int lastBlockSize = stream.ReadInt32();
                Debug.WriteLine(lastBlockSize, "LastBlockSize");
                Debug.Assert(stream.Position == _TablesData.NamesOffset, "There is more data before the NameTable");
                // Data after this is encrypted
            }
#endif
            endOfSummary:
            // We can't continue without decompressing.
            if (CompressionFlags != 0 || (_CompressedChunks != null && _CompressedChunks.Any()))
            {
                // HACK: To fool UE Explorer
                if (_CompressedChunks.Capacity == 0) _CompressedChunks.Capacity = 1;
                return;
            }
#if AA2
            if (Build == GameBuild.BuildName.AA2
                // Note: Never true, AA2 is not a detected build for packages with LicenseeVersion 27 or less
                // But we'll preserve this nonetheless
                && LicenseeVersion >= 19)
            {
                bool isEncrypted = stream.ReadInt32() > 0;
                if (isEncrypted)
                {
                    // TODO: Use a stream wrapper instead; but this is blocked by an overly intertwined use of PackageStream.
                    if (LicenseeVersion >= 33)
                    {
                        var decoder = new CryptoDecoderAA2();
                        Decoder = decoder;
                    }
                    else
                    {
                        var decoder = new CryptoDecoderWithKeyAA2();
                        Decoder = decoder;

                        long nonePosition = _TablesData.NamesOffset;
                        stream.Seek(nonePosition, SeekOrigin.Begin);
                        byte scrambledNoneLength = stream.ReadByte();
                        decoder.Key = scrambledNoneLength;
                        stream.Seek(nonePosition, SeekOrigin.Begin);
                        byte unscrambledNoneLength = stream.ReadByte();
                        Debug.Assert((unscrambledNoneLength & 0x3F) == 5);
                    }
                }

                // Always one
                //int unkCount = stream.ReadInt32();
                //for (var i = 0; i < unkCount; i++)
                //{
                //    // All zero
                //    stream.Skip(24);
                //    // Always identical to the package's GUID
                //    var guid = stream.ReadGuid();
                //}

                //// Always one
                //int unk2Count = stream.ReadInt32();
                //for (var i = 0; i < unk2Count; i++)
                //{
                //    // All zero
                //    stream.Skip(12);
                //}
            }
#endif
            // Read the name table
#if TERA
            if (Build == GameBuild.BuildName.Tera) _TablesData.NamesCount = Generations.Last().NamesCount;
#endif
            if (_TablesData.NamesCount > 0)
            {
                stream.Seek(_TablesData.NamesOffset, SeekOrigin.Begin);
                Names = new List<UNameTableItem>(_TablesData.NamesCount);
                for (var i = 0; i < _TablesData.NamesCount; ++i)
                {
                    var nameEntry = new UNameTableItem { Offset = (int)stream.Position, Index = i };
                    nameEntry.Deserialize(stream);
                    nameEntry.Size = (int)(stream.Position - nameEntry.Offset);
                    Names.Add(nameEntry);
                }
#if SPELLBORN
                // WTF were they thinking? Change DRFORTHEWIN to None
                if (Build == GameBuild.BuildName.Spellborn
                    && Names[0].Name == "DRFORTHEWIN")
                    Names[0].Name = "None";
                // False??
                //Debug.Assert(stream.Position == _TablesData.ImportsOffset);
#endif
            }

            // Read Import Table
            if (_TablesData.ImportsCount > 0)
            {
                stream.Seek(_TablesData.ImportsOffset, SeekOrigin.Begin);
                Imports = new List<UImportTableItem>(_TablesData.ImportsCount);
                for (var i = 0; i < _TablesData.ImportsCount; ++i)
                {
                    var imp = new UImportTableItem { Offset = (int)stream.Position, Index = i, Owner = this };
                    imp.Deserialize(stream);
                    imp.Size = (int)(stream.Position - imp.Offset);
                    Imports.Add(imp);
                }
            }

            // Read Export Table
            if (_TablesData.ExportsCount > 0)
            {
                stream.Seek(_TablesData.ExportsOffset, SeekOrigin.Begin);
                Exports = new List<UExportTableItem>(_TablesData.ExportsCount);
                for (var i = 0; i < _TablesData.ExportsCount; ++i)
                {
                    var exp = new UExportTableItem { Offset = (int)stream.Position, Index = i, Owner = this };
                    exp.Deserialize(stream);
                    exp.Size = (int)(stream.Position - exp.Offset);
                    Exports.Add(exp);
                }

                if (_TablesData.DependsOffset > 0)
                {
                    stream.Seek(_TablesData.DependsOffset, SeekOrigin.Begin);
                    int dependsCount = _TablesData.ExportsCount;
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
                }
            }

            if (_TablesData.ImportExportGuidsOffset > 0)
            {
                for (var i = 0; i < _TablesData.ImportGuidsCount; ++i)
                {
                    string levelName = stream.ReadText();
                    int guidCount = stream.ReadInt32();
                    stream.Skip(guidCount * 16);
                }

                for (var i = 0; i < _TablesData.ExportGuidsCount; ++i)
                {
                    var objectGuid = stream.ReadGuid();
                    int exportIndex = stream.ReadInt32();
                }
            }

            if (_TablesData.ThumbnailTableOffset != 0)
            {
                int thumbnailCount = stream.ReadInt32();
                // TODO: Serialize
            }

            Debug.Assert(stream.Position <= int.MaxValue);
            HeaderSize = (int)stream.Position;
        }

        private void SetupBuild(UPackageStream stream)
        {
            Build = new GameBuild(this);
            
            if (Build.OverrideVersion.HasValue) Version = Build.OverrideVersion.Value;
            if (Build.OverrideLicenseeVersion.HasValue) LicenseeVersion = Build.OverrideLicenseeVersion.Value;

            if (OverrideVersion != 0) Version = OverrideVersion;
            if (OverrideLicenseeVersion != 0) LicenseeVersion = OverrideLicenseeVersion;
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

        private void CreateObject(UObjectTableItem table)
        {
            var classType = GetClassType(table.ClassName);
            table.Object = classType == null
                ? new UnknownObject()
                : (UObject)Activator.CreateInstance(classType);
            AddObject(table.Object, table);
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
        /// Checks whether this package is marked with @flag.
        /// </summary>
        /// <param name="flag">The enum @flag to test.</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [PublicAPI]
        public bool HasPackageFlag(PackageFlags flag)
        {
            return (PackageFlags & (uint)flag) != 0;
        }

        /// <summary>
        /// Checks whether this package is marked with @flag.
        /// </summary>
        /// <param name="flag">The uint @flag to test</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [PublicAPI]
        public bool HasPackageFlag(uint flag)
        {
            return (PackageFlags & flag) != 0;
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is cooked.
        /// </summary>
        /// <returns>True if cooked or False if not.</returns>
        [PublicAPI]
        public bool IsCooked()
        {
            return HasPackageFlag(Flags.PackageFlags.Cooked) && Version >= VCOOKEDPACKAGES;
        }

        /// <summary>
        /// If true, the package won't have any editor data such as HideCategories, ScriptText etc.
        /// 
        /// However this condition is not only determined by the package flags property.
        /// Thus it is necessary to explicitly indicate this state.
        /// </summary>
        /// <returns>Whether package is cooked for consoles.</returns>
        [PublicAPI]
        public bool IsConsoleCooked()
        {
            return IsCooked() && Build.Flags.HasFlag(BuildFlags.ConsoleCooked);
        }

        /// <summary>
        /// Checks for the Map flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is a map.</returns>
        [PublicAPI]
        public bool IsMap()
        {
            return HasPackageFlag(Flags.PackageFlags.Map);
        }

        /// <summary>
        /// Checks if this package contains code classes.
        /// </summary>
        /// <returns>Whether if this package contains code classes.</returns>
        [PublicAPI]
        public bool IsScript()
        {
            return HasPackageFlag(Flags.PackageFlags.Script);
        }

        /// <summary>
        /// Checks if this package was built using the debug configuration.
        /// </summary>
        /// <returns>Whether if this package was built in debug configuration.</returns>
        [PublicAPI]
        public bool IsDebug()
        {
            return HasPackageFlag(Flags.PackageFlags.Debug);
        }

        /// <summary>
        /// Checks for the Stripped flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is stripped.</returns>
        [PublicAPI]
        public bool IsStripped()
        {
            return HasPackageFlag(Flags.PackageFlags.Stripped);
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is encrypted.
        /// </summary>
        /// <returns>True if encrypted or False if not.</returns>
        [PublicAPI]
        public bool IsEncrypted()
        {
            return HasPackageFlag(Flags.PackageFlags.Encrypted);
        }

        #region IBuffered

        public byte[] CopyBuffer()
        {
            var buff = new byte[HeaderSize];
            Stream.Seek(0, SeekOrigin.Begin);
            Stream.Read(buff, 0, (int)HeaderSize);
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
            return (int)HeaderSize;
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