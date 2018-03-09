using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
    public delegate void NotifyObjectAddedEventHandler( object sender, ObjectEventArgs e );

    /// <summary>
    /// Represents the method that will handle the UELib.UnrealPackage.NotifyPackageEvent
    /// event of a triggered event within the UELib.UnrealPackage.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A UELib.UnrealPackage.PackageEventArgs that contains the event data.</param>
    public delegate void PackageEventHandler( object sender, UnrealPackage.PackageEventArgs e );

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
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class UnrealRegisterClassAttribute : Attribute
    {
    }

    /// <summary>
    /// Represents data of a loaded unreal package.
    /// </summary>
    public sealed class UnrealPackage : IDisposable, IBuffered
    {
        // Reference to the stream used when reading this package
        public readonly UPackageStream Stream;

        /// <summary>
        /// The signature of a 'Unreal Package'.
        /// </summary>
        public const uint Signature = 0x9E2A83C1;
        public const uint Signature_BigEndian = 0xC1832A9E;

        [PublicAPI]
        public string FullPackageName { get; }

        [PublicAPI]
        public string PackageName => Path.GetFileNameWithoutExtension( FullPackageName );

        [PublicAPI]
        public string PackageDirectory => Path.GetDirectoryName( FullPackageName );

        private uint _Version;
        public uint Version
        {
            get => OverrideVersion > 0 ? OverrideVersion : _Version;
            private set => _Version = value;
        }

        /// <summary>
        /// For debugging purposes. Change this to override the present Version deserialized from the package.
        /// </summary>
        [SuppressMessage( "Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible" )]
        public static ushort OverrideVersion;

        #region Version history
        /// <summary>
        /// 64
        /// </summary>
        public const ushort VSIZEPREFIXDEPRECATED   = 64;

        /// <summary>
        /// 178
        /// </summary>
        public const ushort VINDEXDEPRECATED        = 178;

        /// <summary>
        /// 277
        /// </summary>
        public const ushort VCOOKEDPACKAGES         = 277;

        /// <summary>
        /// DLLBind(Name)
        /// 655
        /// </summary>
        public const ushort VDLLBIND                = 655;

        /// <summary>
        /// New class modifier "ClassGroup(Name[,Name])"
        /// 789
        /// </summary>
        public const ushort VCLASSGROUP             = 789;

        private const int VCompression = 334;
        private const int VEngineVersion = 245;
        private const int VGroup = 269;
        private const int VHeaderSize = 249;
        #endregion

        private ushort _LicenseeVersion;
        public ushort LicenseeVersion
        {
            get => OverrideLicenseeVersion > 0 ? OverrideLicenseeVersion : _LicenseeVersion;
            private set => _LicenseeVersion = value;
        }

        /// <summary>
        /// For debugging purposes. Change this to override the present Version deserialized from the package.
        /// </summary>
        [SuppressMessage( "Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible" )]
        public static ushort OverrideLicenseeVersion;

        public uint UE4Version{ get; private set; }
        public uint UE4LicenseeVersion{ get; private set; }

        public sealed class GameBuild
        {
            [UsedImplicitly]
            [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
            private sealed class BuildDecoderAttribute : Attribute
            {
                private readonly Type _BuildDecoder;

                public BuildDecoderAttribute( Type buildDecoder )
                {
                    _BuildDecoder = buildDecoder;
                }

                public IBufferDecoder CreateDecoder()
                {
                    return (IBufferDecoder)Activator.CreateInstance( _BuildDecoder );
                }
            }

            [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
            private sealed class BuildAttribute : Attribute
            {
                private readonly int _MinVersion;
                private readonly int _MaxVersion;
                private readonly uint _MinLicensee;
                private readonly byte _IsConsoleCompressed;
                private readonly byte _IsXenonCompressed;
                private readonly uint _MaxLicensee;

                private readonly bool _VerifyEqual;

                public BuildAttribute( int minVersion, uint minLicensee,
                    byte isConsoleCompressed = 2, byte isXenonCompressed = 2 )
                {
                    _MinVersion = minVersion;
                    _MinLicensee = minLicensee;
                    _IsConsoleCompressed = isConsoleCompressed;
                    _IsXenonCompressed = isXenonCompressed;
                    _VerifyEqual = true;
                }

                public BuildAttribute( int minVersion, int maxVersion, uint minLicensee, uint maxLicensee,
                    byte isConsoleCompressed = 2, byte isXenonCompressed = 2 )
                {
                    _MinVersion = minVersion;
                    _MaxVersion = maxVersion;
                    _MinLicensee = minLicensee;
                    _MaxLicensee = maxLicensee;
                    _IsConsoleCompressed = isConsoleCompressed;
                    _IsXenonCompressed = isXenonCompressed;
                }

                public bool Verify( GameBuild gb, UnrealPackage package )
                {
                    if( _VerifyEqual
                        ? package.Version != _MinVersion || package.LicenseeVersion != _MinLicensee
                        : package.Version < _MinVersion || package.Version > _MaxVersion ||
                          package.LicenseeVersion < _MinLicensee || package.LicenseeVersion > _MaxLicensee )
                        return false;

                    gb.Version = package.Version;
                    gb.LicenseeVersion = package.LicenseeVersion;

                    if( _IsConsoleCompressed < 2 )
                    {
                        gb.IsConsoleCompressed = _IsConsoleCompressed == 1;
                    }

                    if( _IsXenonCompressed < 2 )
                    {
                        gb.IsXenonCompressed = _IsXenonCompressed == 1;
                    }
                    return true;
                }
            }

            public enum BuildName
            {
                Unset,
                Default,
                Unknown,

                /// <summary>
                /// 61/000
                /// </summary>
                [Build( 61, 0 )]
                Unreal1,

                /// <summary>
                /// 68:69/000
                /// </summary>
                [Build( 68, 69, 0u, 0u )]
                UT,

                /// <summary>
                /// 95/69
                /// </summary>
                [Build( 95, 69 )]
                DeusEx_IW,

                /// <summary>
                /// 95/133
                /// </summary>
                [Build( 95, 133 )]
                Thief_DS,

                /// <summary>
                /// 99:117/005:008
                /// </summary>
                [Build( 99, 117, 5u, 8u )]
                UT2003,

                /// <summary>
                /// 100/058
                /// </summary>
                [Build( 100, 58 )]
                XIII,

                /// <summary>
                /// 110/2609
                /// </summary>
                [Build( 110, 2609 )]
                Unreal2,

                /// <summary>
                /// 118/025:029
                /// </summary>
                [Build( 118, 128, 25u, 29u )]
                UT2004,

                /// <summary>
                /// 129/027
                /// </summary>
                [Build( 129, 27 )]
                Swat4,

                /// <summary>
                /// 129/035
                /// </summary>
                [Build( 129, 35 )]
                Vanguard,

                /// <summary>
                /// 130:143/056:059
                /// </summary>
                [Build( 130, 143, 56u, 59u, 0, 0 )]
                Bioshock,

                // IrrationalGames - 129:143/027:059

                /// <summary>
                /// 369/006
                /// </summary>
                [Build( 369, 6 )]
                RoboBlitz,

                /// <summary>
                /// 421/011
                /// </summary>
                [Build( 421, 11 )]
                MOHA,

                /// <summary>
                /// 472/046
                /// </summary>
                [Build( 472, 46, 1 )]
                MKKE,

                /// <summary>
                /// 490/009
                /// </summary>
                [Build( 490, 9 )]
                GoW1,

                /// <summary>
                /// 512/000
                /// </summary>
                [Build( 512, 0 )]
                UT3,

                /// <summary>
                /// 536/043
                /// </summary>
                [Build( 536, 43 )]
                MirrorsEdge,

                /// <summary>
                /// 539/091
                /// </summary>
                [Build( 539, 91 )]
                AlphaProtcol,

                /// <summary>
                /// 547/028:032
                /// </summary>
                [Build( 547, 547, 28u, 32u )]
                APB,

                /// <summary>
                /// 575/000
                /// </summary>
                [Build( 575, 0, 0, 1 )]
                GoW2,

                /// <summary>
                /// 576/005
                /// </summary>
                [Build( 576, 5 )]
                CrimeCraft,

                /// <summary>
                /// 576/100
                /// </summary>
                [Build( 576, 100 )]
                Homefront,

                /// <summary>
                /// 584/058
                /// </summary>
                [Build( 584, 58 )]
                Borderlands,

                /// <summary>
                /// 584/126
                /// </summary>
                [Build( 584, 126 )]
                Singularity,

                /// <summary>
                /// 590/001
                /// </summary>
                [Build( 590, 1, 0, 1 )]
                ShadowComplex,

                /// <summary>
                /// 610/014
                /// </summary>
                [Build( 610, 14 )]
                Tera,

                /// <summary>
                /// 727/075
                /// </summary>
                [Build( 727, 75 )]
                Bioshock_Infinite,

                /// <summary>
                /// 742/029
                /// </summary>
                [Build( 742, 29 )]
                BulletStorm,

                /// <summary>
                /// 801/030
                /// </summary>
                [Build( 801, 30 )]
                Dishonored,

                /// <summary>
                /// 828/000
                /// </summary>
                [Build( 828, 0 )]
                InfinityBlade,

                /// <summary>
                /// 828/000
                /// </summary>
                [Build( 828, 0 )]
                GoW3,

                /// <summary>
                /// 832/021
                /// </summary>
                [Build( 832, 21 )]
                RememberMe,

                /// <summary>
                /// 832/046
                /// </summary>
                [Build( 832, 46 )]
                Borderlands2,

                /// <summary>
                /// 842/001
                /// </summary>
                [Build( 842, 1, 1 )]
                InfinityBlade2,

                /// <summary>
                /// 845/059
                /// </summary>
                [Build( 845, 59 )]
                XCOM_EU,

                /// <summary>
                /// 846/181
                /// </summary>
                [Build( 511, 039 )] // The Bourne Conspiracy
                [Build( 511, 145 )] // Transformers: War for Cybertron (PC version)
                [Build( 511, 144 )] // Transformers: War for Cybertron (PS3 and XBox 360 version)
                [Build( 537, 174 )] // Transformers: Dark of the Moon
                [Build( 846, 181, 2, 1 )] // Transformers: Fall of Cybertron
                Transformers,

                /// <summary>
                /// 860/004
                /// </summary>
                [Build( 860, 4 )]
                Hawken,

                /// <summary>
                /// 904/009
                /// </summary>
                [Build( 904, 904, 09u, 014u, 0, 0 )]
                SpecialForce2,

                /// <summary>
                /// 845/120
                /// </summary>
                [Build(845, 120)]
                XCOM2WotC
            }

            public BuildName Name
            {
                get;
                private set;
            }

            public uint Version{ get; private set; }
            public uint LicenseeVersion{ get; private set; }

            /// <summary>
            /// Is cooked for consoles.
            /// </summary>
            public bool IsConsoleCompressed{ get; private set; }

            /// <summary>
            /// Is cooked for Xenon(Xbox 360). Could be true on PC games.
            /// </summary>
            public bool IsXenonCompressed{ get; private set; }

            public GameBuild( UnrealPackage package )
            {
                if( UnrealConfig.Platform == UnrealConfig.CookedPlatform.Console )
                {
                    IsConsoleCompressed = true;
                }

                var gameBuilds = Enum.GetValues( typeof(BuildName) ) as BuildName[];
                foreach( var gameBuild in gameBuilds )
                {
                    var gameBuildMember = typeof(BuildName).GetMember( gameBuild.ToString() );
                    if( gameBuildMember.Length == 0 )
                        continue;

                    var attribs = gameBuildMember[0].GetCustomAttributes( false );
                    var game = attribs.OfType<BuildAttribute>().SingleOrDefault(attr => attr.Verify( this, package ));
                    if( game == null ) 
                        continue;

                    Name = (BuildName)Enum.Parse( typeof(BuildName), Enum.GetName( typeof(BuildName), gameBuild ) );
                    if( package.Decoder != null )
                    {
                        break;
                    }

                    var buildDecoderAttr = attribs.SingleOrDefault( attr => attr is BuildDecoderAttribute ) as BuildDecoderAttribute;
                    if( buildDecoderAttr == null )
                        break;

                    package.Decoder = buildDecoderAttr.CreateDecoder();
                    break;
                }

                if( Name == BuildName.Unset )
                {
                    Name = package.LicenseeVersion == 0 ? BuildName.Default : BuildName.Unknown;
                }
            }

            public static bool operator ==( GameBuild b, BuildName i )
            {
                return b != null && b.Name == i;
            }

            public static bool operator !=( GameBuild b, BuildName i )
            {
                return b != null && b.Name != i;
            }

            /// <inheritdoc/>
            public override bool Equals( object obj )
            {
                return Name == (BuildName)obj;
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return (int)Name;
            }
        }

        public GameBuild Build{ get; private set; }

        /// <summary>
        /// Whether the package was serialized in BigEndian encoding.
        /// </summary>
        public bool IsBigEndianEncoded{ get; }

        /// <summary>
        /// The bitflags of this package.
        /// </summary>
        public uint PackageFlags;

        /// <summary>
        /// Size of the Header. Basically points to the first Object in the package.
        /// </summary>
        public long HeaderSize{ get; private set; }

        /// <summary>
        /// The group the package is associated with in the Content Browser.
        /// </summary>
        public string Group;

        private struct UEngineVersion : IUnrealDeserializableClass
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

        private struct UPackageSummary : IUnrealSerializableClass
        {
            public uint NamesCount{ get; internal set; }
            public uint NamesOffset{ get; internal set; }

            public uint ExportsCount{ get; internal set; }
            public uint ExportsOffset{ get; private set; }

            public uint ImportsCount{ get; internal set; }
            public uint ImportsOffset{ get; internal set; }

            public uint DependsOffset{ get; internal set; }
            public uint DependsCount => ExportsCount;

            public string Guid { get; private set; }
            public UArray<UGenerationTableItem> Generations { get; private set; }

            [DefaultValue(-1)]
            public int EngineVersion { get; private set; }
            public int CookerVersion { get; private set; }

            public uint CompressionFlags { get; private set; }
            public UArray<CompressedChunk> CompressedChunks { get; private set; }

            public void Serialize( IUnrealStream stream )
            {
                stream.Write( NamesCount );
                stream.Write( NamesOffset );

                stream.Write( ExportsCount );
                stream.Write( ExportsOffset );

                stream.Write( ImportsCount );
                stream.Write( ImportsOffset );

                if( stream.Version < 415 )
                    return;

                // DependsOffset
                stream.Write( DependsOffset );
            }

            public void Deserialize( IUnrealStream stream )
            {
#if HAWKEN
                if( stream.Package.Build == GameBuild.BuildName.Hawken )
                {
                    stream.Skip( 4 );
                }
#endif
                NamesCount = stream.ReadUInt32();
                NamesOffset = stream.ReadUInt32();

#if UE4
                if (stream.Package.UE4Version >= 459)
                {
                    uint gatherableTextDataCount = stream.ReadUInt32();
                    uint gatherableTextDataOffset = stream.ReadUInt32();
                }
#endif

                ExportsCount = stream.ReadUInt32();
                ExportsOffset = stream.ReadUInt32();
#if APB
                if( stream.Package.Build == GameBuild.BuildName.APB )
                {
                    stream.Skip( 24 );
                }
#endif
                ImportsCount = stream.ReadUInt32();
                ImportsOffset = stream.ReadUInt32();

                Console.WriteLine( "\tNames Count:" + NamesCount + "\tNames Offset:" + NamesOffset
                    + "\r\n\tExports Count:" + ExportsCount + "\tExports Offset:" + ExportsOffset
                    + "\r\n\tImports Count:" + ImportsCount + "\tImports Offset:" + ImportsOffset
                );

                if (stream.Version >= 415)
                {
                    DependsOffset = stream.ReadUInt32();
                    if (stream.Version >= 584 ||
                        (stream.Package.Build == GameBuild.BuildName.Transformers && stream.Version >= 535))
                    {
                        if (stream.Package.UE4Version > 0)
                        {
                            if (stream.Package.UE4Version >= 384)
                            {
                                stream.Skip(4 + 4); // StringAssetReferencesCount, StringAssetReferencesOffset
                                if (stream.Package.UE4Version >= 510)
                                {
                                    uint searchableNamesOffset = stream.ReadUInt32();
                                }
                            }
                        }
                        // Additional tables, like thumbnail, and guid data.
                        else if (stream.Version >= 623
#if BIOSHOCK
                            && stream.Package.Build != GameBuild.BuildName.Bioshock_Infinite
#endif
#if TRANSFORMERS
                            && stream.Package.Build != GameBuild.BuildName.Transformers
#endif
                        )
                        {
                            stream.Skip(12);
                        }

                        stream.Skip(4);
                    }
                    else
                    {
                        return;
                    }
                }

                if (stream.Version < 68)
                {
                    int heritageCount = stream.ReadInt32();
                    int heritageOffset = stream.ReadInt32();

                    // Uncommented because this is not in use and should in theory only be read after PackageSummary's deserialization.
//                    stream.Seek(heritageOffset, SeekOrigin.Begin);
//                    var heritages = new List<ushort>(heritageCount);
//                    for (var i = 0; i < heritageCount; ++i)
//                    {
//                        heritages.Add(stream.ReadUInt16());
//                    }
                }
                else
                {
#if DEUSEXINVISIBLEWAR
                if( stream.Package.Build == GameBuild.BuildName.DeusEx_IW )
                {
                    //stream.Skip( 4 );
                    int unknown = stream.ReadInt32();
                    Console.WriteLine( "\tUnknown:" + unknown );
                }
#endif
#if THIEFDEADLYSHADOWS
                    if (stream.Package.Build == GameBuild.BuildName.Thief_DS)
                    {
                        //stream.Skip( 4 );
                        int unknown = stream.ReadInt32();
                        Console.WriteLine("\tUnknown:" + unknown);
                    }
#endif
#if BORDERLANDS
                    if (stream.Package.Build == GameBuild.BuildName.Borderlands)
                    {
                        stream.Skip(4);
                    }
#endif
#if MKKE
                    if (stream.Package.Build == GameBuild.BuildName.MKKE)
                    {
                        stream.Skip(4);
                    }
#endif
                    Guid = stream.ReadGuid();
                    Console.Write("\r\n\tGUID:" + Guid + "\r\n");
#if TERA
                    if (stream.Package.Build == GameBuild.BuildName.Tera)
                    {
                        stream.Position -= 4;
                    }
#endif
#if MKKE
                    if (stream.Package.Build != GameBuild.BuildName.MKKE)
                    {
#endif
                        int generationCount = stream.ReadInt32();
                        Generations = new UArray<UGenerationTableItem>(stream, generationCount);
                        Console.WriteLine("Deserialized {0} generations", Generations.Count);
#if MKKE
                    }
#endif
#if TERA
                    if (stream.Package.Build == GameBuild.BuildName.Tera)
                    {
                        NamesCount = (uint)Generations.Last().NamesCount;
                    }
#endif
                    if (stream.Version < VEngineVersion)
                        return;
#if UE4
                    if (stream.Package.UE4Version >= 336)
                    {
                        // EngineVersion
                        var engineVersion = new UEngineVersion();
                        engineVersion.Deserialize(stream);
                    }
                    else
                    {
#endif
                        // The Engine Version this package was created with
                        EngineVersion = stream.ReadInt32();
                        Console.WriteLine("\tEngineVersion:" + EngineVersion);
#if UE4
                    }

                    if (stream.Package.UE4Version >= 444)
                    {
                        // Compatible EngineVersion
                        var engineVersion = new UEngineVersion();
                        engineVersion.Deserialize(stream);
                    }
#endif
                    if (stream.Version < VCOOKEDPACKAGES)
                        return;
#if UE4
                    if (stream.Package.UE4Version == 0)
                    {
#endif
                        // The Cooker Version this package was cooked with
                        CookerVersion = stream.ReadInt32();
                        Console.WriteLine("\tCookerVersion:" + CookerVersion);
#if UE4
                    }
#endif
                    // Read compressed info?
                    if (stream.Version < VCompression)
                        return;

                    if (stream.Package.IsCooked())
                    {
                        CompressionFlags = stream.ReadUInt32();
                        Console.WriteLine("\tCompressionFlags:" + CompressionFlags);
                        CompressedChunks = new UArray<CompressedChunk> { Capacity = stream.ReadInt32() };
                        //long uncookedSize = stream.Position;
                        if (CompressedChunks.Capacity > 0)
                        {
                            CompressedChunks.Deserialize(stream, CompressedChunks.Capacity);
                            return;

                            //try
                            //{
                            //    UPackageStream outStream = new UPackageStream( packagePath + ".dec", System.IO.FileMode.Create, FileAccess.ReadWrite );
                            //    //File.SetAttributes( packagePath + ".dec", FileAttributes.Temporary );
                            //    outStream.Package = pkg;
                            //    outStream._BigEndianCode = stream._BigEndianCode;

                            //    var headerBytes = new byte[uncookedSize];
                            //    stream.Seek( 0, SeekOrigin.Begin );
                            //    stream.Read( headerBytes, 0, (int)uncookedSize );
                            //    outStream.Write( headerBytes, 0, (int)uncookedSize );
                            //    foreach( var chunk in pkg.CompressedChunks )
                            //    {
                            //        chunk.Decompress( stream, outStream );
                            //    }
                            //    outStream.Flush();
                            //    pkg.Stream = outStream;
                            //    stream = outStream;
                            //    return pkg;
                            //}
                            //catch( Exception e )
                            //{
                            //    throw new DecompressPackageException();
                            //}
                        }
                    }
#if UE4
                    if (stream.Package.UE4Version <= 0)
                        return;

                    uint source = stream.ReadUInt32();
                    Console.WriteLine("\tSource:{0}", source);

                    // AdditionalPackagesToCook
                    // NumTextureAllocations
#endif
                }

            }
        }

        private UPackageSummary _Summary;

        /// <summary>
        /// The guid of this package. Used to test if the package on a client is equal to the one on a server.
        /// </summary>
        [PublicAPI]
        public string GUID => _Summary.Guid;

        /// <summary>
        /// List of package generations.
        /// </summary>
        [PublicAPI]
        public UArray<UGenerationTableItem> Generations => _Summary.Generations;

        /// <summary>
        /// The Engine version the package was created with.
        /// </summary>
        [DefaultValue(-1)]
        [PublicAPI]
        public int EngineVersion => _Summary.EngineVersion;

        /// <summary>
        /// The Cooker version the package was cooked with.
        /// </summary>
        [PublicAPI]
        public int CookerVersion => _Summary.CookerVersion;

        /// <summary>
        /// The type of compression the package is compressed with.
        /// </summary>
        [PublicAPI]
        public uint CompressionFlags => _Summary.CompressionFlags;

        /// <summary>
        /// List of compressed chunks throughout the package.
        /// </summary>
        [PublicAPI]
        public UArray<CompressedChunk> CompressedChunks => _Summary.CompressedChunks;

        /// <summary>
        /// List of unique unreal names.
        /// </summary>
        [PublicAPI]
        public List<UNameTableItem> Names{ get; private set; }

        /// <summary>
        /// List of info about exported objects.
        /// </summary>
        [PublicAPI]
        public List<UExportTableItem> Exports{ get; private set; }

        /// <summary>
        /// List of info about imported objects.
        /// </summary>
        [PublicAPI]
        public List<UImportTableItem> Imports{ get; private set; }

        /// <summary>
        /// List of info about dependency objects.
        /// </summary>
        //public List<UDependencyTableItem> Dependencies{ get; private set; }

        #region Initialized Members
        private struct ClassType
        {
            public string Name;
            public Type Class;
        }

        /// <summary>
        /// Class types that should get added to the ObjectsList.
        /// </summary>
        private readonly List<ClassType> _RegisteredClasses = new List<ClassType>();

        /// <summary>
        /// List of UObjects that were constructed by function ConstructObjects, later deserialized and linked.
        ///
        /// Includes Exports and Imports!.
        /// </summary>
        [PublicAPI]
        public List<UObject> Objects{ get; private set; }

        [PublicAPI]
        public NativesTablePackage NTLPackage;

        [PublicAPI]
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
            Construct       =   0x0001,
            Deserialize     =   0x0002,//           | Construct,
            Import          =   0x0004,//           | Serialize,
            Link            =   0x0008,//           | Serialize,
            All             =   RegisterClasses     | Construct     | Deserialize   | Import    | Link,
            RegisterClasses =   0x0010
        }

        [Obsolete]
        [PublicAPI]
        public static UnrealPackage DeserializePackage( string packagePath, FileAccess fileAccess = FileAccess.Read )
        {
            var stream = new UPackageStream( packagePath, FileMode.Open, fileAccess );
            var pkg = new UnrealPackage( stream );
            pkg.Deserialize( stream );
            return pkg;
        }

        /// <summary>
        /// Creates a new instance of the UELib.UnrealPackage class with a PackageStream and name.
        /// </summary>
        /// <param name="stream">A loaded UELib.PackageStream.</param>
        public UnrealPackage( UPackageStream stream )
        {
            FullPackageName = stream.Name;
            Stream = stream;
            Stream.PostInit( this );

            // File Type
            // Signature is tested in UPackageStream
            IsBigEndianEncoded = stream.BigEndianCode;
        }

        public void Serialize( IUnrealStream stream )
        {
            // Serialize tables
            var namesBuffer = new UObjectStream( stream );
            foreach( var name in Names )
            {
                name.Serialize( namesBuffer );
            }

            var importsBuffer = new UObjectStream( stream );
            foreach( var import in Imports )
            {
                import.Serialize( importsBuffer );
            }

            var exportsBuffer = new UObjectStream( stream );
            foreach( var export in Exports )
            {
                //export.Serialize( exportsBuffer );
            }

            stream.Seek( 0, SeekOrigin.Begin );

            stream.Write( Signature );

            // Serialize header
            var version = (int)(Version & 0x0000FFFFU) | (LicenseeVersion << 16);
            stream.Write( version );

            var headerSizePosition = stream.Position;
            if( Version >= VHeaderSize )
            {
                stream.Write( (int)HeaderSize );
                if( Version >= VGroup )
                {
                    stream.WriteString( Group );
                }
            }

            stream.Write( PackageFlags );

            _Summary.NamesCount = (uint)Names.Count;
            _Summary.ExportsCount = (uint)Exports.Count;
            _Summary.ImportsCount = (uint)Imports.Count;

            var tablesDataPosition = stream.Position;
            _Summary.Serialize( stream );

            // TODO: Serialize Heritages

            stream.Write( Guid.NewGuid().ToByteArray(), 0, 16 );
            Generations.Serialize( stream );

            if( Version >= VEngineVersion )
            {
                stream.Write( EngineVersion );
                if( Version >= VCOOKEDPACKAGES )
                {
                    stream.Write( CookerVersion );

                    if( Version >= VCompression )
                    {
                        if( IsCooked() )
                        {
                            stream.Write( CompressionFlags );
                            CompressedChunks.Serialize( stream );
                        }
                    }
                }
            }

            // TODO: Unknown data
            stream.Write( (uint)0 );

            // Serialize objects

            // Write tables

            // names
            Console.WriteLine( "Writing names at position " + stream.Position );
            _Summary.NamesOffset = (uint)stream.Position;
            var namesBytes = namesBuffer.GetBuffer();
            stream.Write( namesBytes, 0, (int)namesBuffer.Length );

            // imports
            Console.WriteLine( "Writing imports at position " + stream.Position );
            _Summary.ImportsOffset = (uint)stream.Position;
            var importsBytes = importsBuffer.GetBuffer();
            stream.Write( importsBytes, 0, (int)importsBuffer.Length );

            // exports
            Console.WriteLine( "Writing exports at position " + stream.Position );

            // Serialize tables data again now that offsets are known.
            var currentPosition = stream.Position;
            stream.Seek( tablesDataPosition, SeekOrigin.Begin );
            _Summary.Serialize( stream );
            stream.Seek( currentPosition, SeekOrigin.Begin );
        }

        private const short MaxLegacyVersion = -7;
        public void Deserialize( UPackageStream stream )
        {
            // Read as one variable due Big Endian Encoding.       
            int legacyVersion = stream.ReadInt32();
            if( legacyVersion < 0 )
            {
#if UE4
                if( legacyVersion < MaxLegacyVersion)
                {
                    throw new Exception( "This version of the Unreal Engine 4 is not supported!" );
                }

                uint ue3Version = 0;
                if (legacyVersion != -4)
                {
                    ue3Version = stream.ReadUInt32();
                }
                UE4Version = stream.ReadUInt32();
                UE4LicenseeVersion = stream.ReadUInt32();

                Version = ue3Version;

                // Really old, probably no longer in production files? Other than some UE4 assets found in the first public release
                if ( UE4Version >= 138 && UE4Version < 142 )
                {
                    stream.Skip( 8 ); // CookedVersion, CookedLicenseeVersion   
                }

                if( legacyVersion <= -2 )
                {
                    // Read enum based version
                    if( legacyVersion == -2 )
                    {
                        int count = stream.ReadInt32(); // Versions
                        stream.Skip( count*(4 + 4) ); // Tag, Version
                    }
                    else if( legacyVersion < -2 && legacyVersion >= -5)
                    {
                        int count = stream.ReadInt32();
                        for( var i = 0; i < count; ++ i )
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
                        for( var i = 0; i < count; ++ i )
                        {
                            stream.ReadGuid(); // Key
                            stream.ReadInt32(); // Version
                        }
                    }
                }
#endif
            }
            else
            {
                Version = (uint)legacyVersion;
            }
            LicenseeVersion = (ushort)(Version >> 16);
            Version = (Version & 0xFFFFU);
            Console.WriteLine( "\tPackage Version:" + Version + "/" + LicenseeVersion );

            Build = new GameBuild( this );
            Console.WriteLine( "\tBuild:" + Build.Name );

            stream.BuildDetected( Build );

            if( Version >= VHeaderSize )
            {
#if BIOSHOCK
                if( Build == GameBuild.BuildName.Bioshock_Infinite )
                {
                    int unk = stream.ReadInt32();
                }
#endif
#if MKKE
                if( Build == GameBuild.BuildName.MKKE )
                {
                    stream.Skip( 8 );
                }
#endif
#if TRANSFORMERS
                if( Build == GameBuild.BuildName.Transformers && LicenseeVersion >= 55 )
                {
                    if( LicenseeVersion >= 181 )
                    {
                        stream.Skip( 16 );
                    }
                    stream.Skip( 4 );
                }
#endif
                // Offset to the first class(not object) in the package.
                HeaderSize = stream.ReadUInt32();
                Console.WriteLine( "\tHeader Size: " + HeaderSize );
                if( Version >= VGroup )
                {
                    // UPK content category e.g. Weapons, Sounds or Meshes.
                    Group = stream.ReadText();
                }
            }

            // Bitflags such as AllowDownload.
            PackageFlags = stream.ReadUInt32();
            Console.WriteLine( "\tPackage Flags:" + PackageFlags );

            // Summary data such as ObjectCount.
            _Summary = new UPackageSummary();
            _Summary.Deserialize( stream );

            // Read the name table
            if( _Summary.NamesCount > 0 )
            {
                Console.WriteLine( "P: " + stream.Position + " NP: " + _Summary.NamesOffset );

                stream.Seek( _Summary.NamesOffset, SeekOrigin.Begin );
                Names = new List<UNameTableItem>( (int)_Summary.NamesCount );
                for( var i = 0; i < _Summary.NamesCount; ++ i )
                {
                    var nameEntry = new UNameTableItem{Offset = (int)stream.Position, Index = i};
                    nameEntry.Deserialize( stream );
                    nameEntry.Size = (int)(stream.Position - nameEntry.Offset);
                    Names.Add( nameEntry );
                }
                Console.WriteLine( "Deserialized {0} names", Names.Count );
            }

            // Read Import Table
            if( _Summary.ImportsCount > 0 )
            {
                Console.WriteLine( "P: " + stream.Position + " IP: " + _Summary.ImportsOffset );

                stream.Seek( _Summary.ImportsOffset, SeekOrigin.Begin );
                Imports = new List<UImportTableItem>( (int)_Summary.ImportsCount );
                for( var i = 0; i < _Summary.ImportsCount; ++ i )
                {
                    var imp = new UImportTableItem{Offset = (int)stream.Position, Index = i, Owner = this};
                    imp.Deserialize( stream );
                    imp.Size = (int)(stream.Position - imp.Offset);
                    Imports.Add( imp );
                }
                Console.WriteLine( "Deserialized {0} imports", Imports.Count );
            }

            // Read Export Table
            if( _Summary.ExportsCount > 0 )
            {
                Console.WriteLine( "P: " + stream.Position + " EP: " + _Summary.ExportsOffset );

                stream.Seek( _Summary.ExportsOffset, SeekOrigin.Begin );
                Exports = new List<UExportTableItem>( (int)_Summary.ExportsCount );
                for( var i = 0; i < _Summary.ExportsCount; ++ i )
                {
                    var exp = new UExportTableItem{Offset = (int)stream.Position, Index = i, Owner = this};
                    // For the GetObjectName like functions
                    try
                    {
                        exp.Deserialize( stream );
                    }
                    catch
                    {
                        Console.WriteLine( "Failed to deserialize export object at index:" + i );
                        break;
                    }
                    finally
                    {
                        exp.Size = (int)(stream.Position - exp.Offset);
                        Exports.Add( exp );
                    }
                }
                Console.WriteLine( "Deserialized {0} exports", Exports.Count );
            }

            /*if( pkg.Data.DependsOffset > 0 )
            {
                stream.Seek( pkg.Data.DependsOffset, SeekOrigin.Begin );
                pkg._DependsTableList = new List<UnrealDependsTable>( (int)pkg.Data.DependsCount );
                for( var i = 0; i < pkg.Data.DependsCount; ++ i )
                {
                    var dep = new UnrealDependsTable{TableOffset = stream.Position, TableIndex = i, Owner = pkg};
                    dep.Deserialize( stream );
                    dep.TableSize = (int)(stream.Position - dep.TableOffset);
                    pkg.DependsTableList.Add( dep );
                }
                Console.WriteLine( "Deserialized {0} dependencies", pkg.DependsTableList.Count );
            }*/

            HeaderSize = stream.Position;
        }

        /// <summary>
        /// Constructs all export objects.
        /// </summary>
        /// <param name="initFlags">Initializing rules such as deserializing and/or linking.</param>
        [PublicAPI]
        public void InitializeExportObjects( InitFlags initFlags = InitFlags.All )
        {
            Objects = new List<UObject>( Exports.Count );
            foreach( var exp in Exports )
            {
                CreateObjectForTable( exp );
            }

            if( (initFlags & InitFlags.Deserialize) == 0 )
                return;

            DeserializeObjects();
            if( (initFlags & InitFlags.Link) != 0 )
            {
                LinkObjects();
            }
        }

        /// <summary>
        /// Constructs all import objects.
        /// </summary>
        /// <param name="initialize">If TRUE initialize all constructed objects.</param>
        [PublicAPI]
        public void InitializeImportObjects( bool initialize = true )
        {
            Objects = new List<UObject>( Imports.Count );
            foreach( var imp in Imports )
            {
                CreateObjectForTable( imp );
            }

            if( !initialize )
            {
                return;
            }

            foreach( var obj in Objects )
            {
                obj.PostInitialize();
            }
        }

        /// <summary>
        /// Initializes all the objects that resist in this package as well tries to import deserialized data from imported objects.
        /// </summary>
        /// <param name="initFlags">A collection of initializing flags to notify what should be initialized.</param>
        /// <example>InitializePackage( UnrealPackage.InitFlags.All )</example>
        [PublicAPI]
        public void InitializePackage( InitFlags initFlags = InitFlags.All )
        {
            if( (initFlags & InitFlags.RegisterClasses) != 0 )
            {
                RegisterAllClasses();
            }

            if( (initFlags & InitFlags.Construct) == 0 )
            {
                return;
            }

            ConstructObjects();
            if( (initFlags & InitFlags.Deserialize) == 0 )
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
                if( (initFlags & InitFlags.Import) != 0 )
                {
                    ImportObjects();
                }
            }
            catch( Exception e )
            {
                //can be treat with as a warning!
                throw new Exception( "An exception occurred while importing objects", e );
            }

            try
            {
                if( (initFlags & InitFlags.Link) != 0 )
                {
                    LinkObjects();
                }
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
                Import = 2,

                /// <summary>
                /// Connecting deserialized object indexes.
                /// </summary>
                Link = 3,

                /// <summary>
                /// Deserialized a Export/Import object.
                /// </summary>
                Object = 0xFF,
            }

            /// <summary>
            /// The event identification.
            /// </summary>
            [PublicAPI]
            public readonly Id EventId;

            /// <summary>
            /// Constructs a new event with @eventId.
            /// </summary>
            /// <param name="eventId">Event identification.</param>
            public PackageEventArgs( Id eventId )
            {
                EventId = eventId;
            }
        }

        /// <summary>
        ///
        /// </summary>
        [PublicAPI]
        public event PackageEventHandler NotifyPackageEvent = null;
        private void OnNotifyPackageEvent( PackageEventArgs e )
        {
            if( NotifyPackageEvent != null )
            {
                NotifyPackageEvent.Invoke( this, e );
            }
        }

        /// <summary>
        /// Called when an object is added to the ObjectsList via the AddObject function.
        /// </summary>
        [PublicAPI]
        public event NotifyObjectAddedEventHandler NotifyObjectAdded = null;

        /// <summary>
        /// Constructs all the objects based on data from _ExportTableList and _ImportTableList, and
        /// all constructed objects are added to the _ObjectsList.
        /// </summary>
        private void ConstructObjects()
        {
            Objects = new List<UObject>();
            OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Construct ) );
            foreach( var exp in Exports )
            {
                try
                {
                    CreateObjectForTable( exp );
                }
                catch( Exception exc )
                {
                    throw new UnrealException( "couldn't create export object for " + exp, exc );
                }
            }

            foreach( var imp in Imports )
            {
                try
                {
                    CreateObjectForTable( imp );
                }
                catch( Exception exc )
                {
                    throw new UnrealException( "couldn't create import object for " + imp, exc );
                }
            }
        }

        /// <summary>
        /// Deserializes all exported objects.
        /// </summary>
        private void DeserializeObjects()
        {
            // Only exports should be deserialized and PostInitialized!
            OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Deserialize ) );
            foreach( var exp in Exports )
            {
                if( !(exp.Object is UnknownObject || exp.Object.ShouldDeserializeOnDemand) )
                {
                    //Console.WriteLine( "Deserializing object:" + exp.ObjectName );
                    exp.Object.BeginDeserializing();
                }
                OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Object ) );
            }
        }

        /// <summary>
        /// Tries to import necessary deserialized data from imported objects.
        /// </summary>
        private void ImportObjects()
        {
            // TODO:Figure out why this freezes.
            /*OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Import ) );
            foreach( UnrealImportTable Imp in _ImportTableList )
            {
                if( !(Imp.Object.GetType() == typeof(UnknownObject)) )
                {
                    Imp.Object.InitializeImports();
                }
                OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Object ) );
            }
            UnrealLoader.CachedPackages.Clear();*/
        }

        /// <summary>
        /// Initializes all exported objects.
        /// </summary>
        private void LinkObjects()
        {
            // Notify that deserializing is done on all objects, now objects can read properties that were dependent on deserializing
            OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Link ) );
            foreach( var exp in Exports )
            {
                try
                {
                    if( !(exp.Object is UnknownObject) )
                    {
                        exp.Object.PostInitialize();
                    }
                    OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Object ) );
                }
                catch( InvalidCastException )
                {
                    Console.WriteLine( "InvalidCastException occurred on object: " + exp.Object );
                }
            }
        }

        private void RegisterAllClasses()
        {
            var exportedTypes = Assembly.GetExecutingAssembly().GetExportedTypes();
            foreach( var exportedType in exportedTypes )
            {
                var attributes = exportedType.GetCustomAttributes( typeof(UnrealRegisterClassAttribute), false );
                if( attributes.Length == 1 )
                {
                    RegisterClass( exportedType.Name.Substring( 1 ), exportedType );
                }
            }
        }
        #endregion

        #region Methods
        [System.Diagnostics.Contracts.Pure]private Type GetClassTypeByClassName( string className )
        {
            return _RegisteredClasses.FirstOrDefault
            (
                registered => String.Compare( registered.Name, className, StringComparison.OrdinalIgnoreCase ) == 0
            ).Class;
        }

        private void CreateObjectForTable( UObjectTableItem table )
        {
            var objectType = GetClassTypeByClassName( table.ClassName );
            table.Object = objectType == null ? new UnknownObject() : (UObject)Activator.CreateInstance( objectType );
            AddObject( table.Object, table );
            OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Object ) );
        }

        private void AddObject( UObject obj, UObjectTableItem T )
        {
            T.Object = obj;
            obj.Package = this;
            obj.Table = T;

            Objects.Add( obj );
            if( NotifyObjectAdded != null )
            {
                NotifyObjectAdded.Invoke( this, new ObjectEventArgs( obj ) );
            }
        }

        /// <summary>
        /// Writes the present PackageFlags to disk. HardCoded!
        /// Only supports UT2004.
        /// </summary>
        public void WritePackageFlags()
        {
            Stream.Position = 8;
            Stream.UW.Write( PackageFlags );
        }

        [PublicAPI]
        public void RegisterClass( string className, Type classObject )
        {
            var obj = new ClassType{ Name = className, Class = classObject };
            _RegisteredClasses.Add( obj );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool IsRegisteredClass( string className )
        {
            return _RegisteredClasses.Exists( o => o.Name.ToLower() == className.ToLower() );
        }

        /// <summary>
        /// Returns an Object that resides at the specified ObjectIndex.
        ///
        /// if index is positive an exported Object will be returned.
        /// if index is negative an imported Object will be returned.
        /// if index is zero null will be returned.
        /// </summary>
        /// <param name="objectIndex">The index of the Object in a tablelist.</param>
        /// <returns>The found UELib.Core.UObject if any.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public UObject GetIndexObject( int objectIndex )
        {
            return (objectIndex < 0 ? Imports[-objectIndex - 1].Object
                        : (objectIndex > 0 ? Exports[objectIndex - 1].Object
                        : null));
        }

        /// <summary>
        /// Returns an Object name that resides at the specified ObjectIndex.
        /// </summary>
        /// <param name="objectIndex">The index of the object in a tablelist.</param>
        /// <returns>The found UELib.Core.UObject name if any.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public string GetIndexObjectName( int objectIndex )
        {
            return GetIndexTable( objectIndex ).ObjectName;
        }

        /// <summary>
        /// Returns a name that resides at the specified NameIndex.
        /// </summary>
        /// <param name="nameIndex">A NameIndex into the NameTableList.</param>
        /// <returns>The name at specified NameIndex.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public string GetIndexName( int nameIndex )
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
        /// <param name="tableIndex">The index of the Table.</param>
        /// <returns>The found UELib.Core.UnrealTable if any.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public UObjectTableItem GetIndexTable( int tableIndex )
        {
            return  (tableIndex < 0 ? Imports[-tableIndex - 1]
                    : (tableIndex > 0 ? (UObjectTableItem)Exports[tableIndex - 1]
                    : null));
        }

        /// <summary>
        /// Tries to find an UELib.Core.UObject with a specified name and type.
        /// </summary>
        /// <param name="objectName">The name of the object to find.</param>
        /// <param name="type">The type of the object to find.</param>
        /// <param name="checkForSubclass">Whether to test for subclasses of type as well.</param>
        /// <returns>The found UELib.Core.UObject if any.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public UObject FindObject( string objectName, Type type, bool checkForSubclass = false )
        {
            if( Objects == null )
            {
                return null;
            }

            var obj = Objects.Find( o => String.Compare( o.Name, objectName, StringComparison.OrdinalIgnoreCase ) == 0 &&
                (checkForSubclass ? o.GetType().IsSubclassOf( type ) : o.GetType() == type) );
            return obj;
        }

        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public UObject FindObjectByGroup( string objectGroup )
        {
            var groups = objectGroup.Split( '.' );
            UObject lastObj = null;
            for( var i = 0; i < groups.Length; ++ i )
            {
                var obj = Objects.Find( o => String.Compare( o.Name, groups[i], StringComparison.OrdinalIgnoreCase ) == 0 && o.Outer == lastObj );
                if( obj != null )
                {
                    lastObj = obj;
                }
                else
                {
                    lastObj = Objects.Find( o => String.Compare( o.Name, groups[i], StringComparison.OrdinalIgnoreCase ) == 0 );
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
        [System.Diagnostics.Contracts.Pure]public bool HasPackageFlag( PackageFlags flag )
        {
            return (PackageFlags & (uint)flag) != 0;
        }

        /// <summary>
        /// Checks whether this package is marked with @flag.
        /// </summary>
        /// <param name="flag">The uint @flag to test</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool HasPackageFlag( uint flag )
        {
            return (PackageFlags & flag) != 0;
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is cooked.
        /// </summary>
        /// <returns>True if cooked or False if not.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool IsCooked()
        {
            return HasPackageFlag( Flags.PackageFlags.Cooked ) && Version >= VCOOKEDPACKAGES;
        }

        /// <summary>
        /// Tests the package for console build indications.
        /// </summary>
        /// <returns>Whether package is cooked for consoles.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool IsConsoleCooked()
        {
            return IsCooked() && (IsBigEndianEncoded || Build.IsConsoleCompressed) && !Build.IsXenonCompressed;
        }

        /// <summary>
        /// Checks for the Map flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is a map.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool IsMap()
        {
            return HasPackageFlag( Flags.PackageFlags.Map );
        }

        /// <summary>
        /// Checks if this package contains code classes.
        /// </summary>
        /// <returns>Whether if this package contains code classes.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool IsScript()
        {
            return HasPackageFlag( Flags.PackageFlags.Script );
        }

        /// <summary>
        /// Checks if this package was built using the debug configuration.
        /// </summary>
        /// <returns>Whether if this package was built in debug configuration.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool IsDebug()
        {
            return HasPackageFlag( Flags.PackageFlags.Debug );
        }

        /// <summary>
        /// Checks for the Stripped flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is stripped.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool IsStripped()
        {
            return HasPackageFlag( Flags.PackageFlags.Stripped );
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is encrypted.
        /// </summary>
        /// <returns>True if encrypted or False if not.</returns>
        [PublicAPI]
        [System.Diagnostics.Contracts.Pure]public bool IsEncrypted()
        {
            return HasPackageFlag( Flags.PackageFlags.Encrypted );
        }

        #region IBuffered
        public byte[] CopyBuffer()
        {
            var buff = new byte[HeaderSize];
            Stream.Seek( 0, SeekOrigin.Begin );
            Stream.Read( buff, 0, (int)HeaderSize );
            if( Stream.BigEndianCode )
            {
                Array.Reverse( buff );
            }
            return buff;
        }

        [System.Diagnostics.Contracts.Pure]
        public IUnrealStream GetBuffer()
        {
            return Stream;
        }

        [System.Diagnostics.Contracts.Pure]
        public int GetBufferPosition()
        {
            return 0;
        }

        [System.Diagnostics.Contracts.Pure]
        public int GetBufferSize()
        {
            return (int)HeaderSize;
        }

        [System.Diagnostics.Contracts.Pure]
        public string GetBufferId( bool fullName = false )
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
            Console.WriteLine( "Disposing {0}", PackageName );

            DisposeStream();
            if( Objects != null && Objects.Any() )
            {
                foreach( var obj in Objects )
                {
                    obj.Dispose();
                }
                Objects.Clear();
                Objects = null;
            }
        }

        private void DisposeStream()
        {
            if( Stream == null )
                return;

            Console.WriteLine( "Disposing package stream" );
            Stream.Dispose();
        }
        #endregion
    }
}