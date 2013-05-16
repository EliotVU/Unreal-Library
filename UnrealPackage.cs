using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UELib
{
    using Core;

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
    public delegate void NotifyUpdateEvent();

    /// <summary>
    /// Registers an unreal class. Any class with this attribute will automatically be registered.
    /// </summary>
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
        public string FullPackageName
        {
            get{ return _FullPackageName; }
        }

        public string PackageName
        {
            get{ return Path.GetFileNameWithoutExtension( _FullPackageName ); }
        }

        public string PackageDirectory
        {
            get{ return Path.GetDirectoryName( _FullPackageName ); }
        }
        #endregion

        #region Serialized Members
        private uint _Version;
        public uint Version
        {
            get{ return OverrideVersion > 0 ? OverrideVersion : _Version; }
            private set{ _Version = value; }
        }

        /// <summary>
        /// For debugging purposes. Change this to override the present Version deserialized from the package.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible" )]
        public static ushort OverrideVersion;

        #region Version history
        /// <summary>
        /// 64
        /// </summary>
        public const ushort VSIZEPREFIXDEPRECATED	= 64;

        /// <summary>
        /// 178
        /// </summary>
        public const ushort VINDEXDEPRECATED		= 178;

        /// <summary>
        /// 277
        /// </summary>
        public const ushort VCOOKEDPACKAGES			= 277;

        /// <summary>
        /// DLLBind(Name)
        /// 655
        /// </summary>
        public const ushort VDLLBIND				= 655;

        /// <summary>
        /// New class modifier "ClassGroup(Name[,Name])"
        /// 789
        /// </summary>
        public const ushort VCLASSGROUP				= 789;
        #endregion

        private ushort _LicenseeVersion;
        public ushort LicenseeVersion
        {
            get{ return OverrideLicenseeVersion > 0 ? OverrideLicenseeVersion : _LicenseeVersion; }
            private set{ _LicenseeVersion = value; }
        }

        /// <summary>
        /// For debugging purposes. Change this to override the present Version deserialized from the package.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible" )]
        public static ushort OverrideLicenseeVersion;

        public class GameBuild
        {
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
                        ? package.Version == _MinVersion && package.LicenseeVersion == _MinLicensee
                        : package.Version >= _MinVersion && package.Version <= _MaxVersion
                            && package.LicenseeVersion >= _MinLicensee && package.LicenseeVersion <= _MaxLicensee )
                    {
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
                    return false;
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
                /// 860/004
                /// </summary>
                [Build( 860, 4 )]
                Hawken,

                /// <summary>
                /// 904/009
                /// </summary>
                [Build( 904, 904, 9u, 9u, 0, 0 )]
                SpecialForce2
            }

            public BuildName Name
            {
                get;
                private set;
            }

            /// <summary>
            /// Is cooked for consoles.
            /// </summary>
            public bool IsConsoleCompressed;

            /// <summary>
            /// Is cooked for Xenon(Xbox 360). Could be true on PC games.
            /// </summary>
            public bool IsXenonCompressed;

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
                    if( attribs.Length == 0 )
                        continue;

                    var myAttrib = attribs[0] as BuildAttribute;
                    if( !myAttrib.Verify( this, package ) )
                        continue;

                    Name = (BuildName)Enum.Parse( typeof(BuildName), Enum.GetName( typeof(BuildName), gameBuild ) );
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

        public GameBuild Build;

        /// <summary>
        /// Whether the package was serialized in BigEndian encoding.
        /// </summary>
        public bool IsBigEndianEncoded{ get; private set; }

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

        private struct PackageSummary : IUnrealDeserializableClass
        {
            public uint NamesCount, 	NamesOffset;

            public uint ExportsCount, 	ExportsOffset;

            public uint ImportsCount, 	ImportsOffset;

            public uint DependsOffset;
            public uint DependsCount{ get{ return ExportsCount; } }

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
                if( stream.Version < 415 )
                    return;

                DependsOffset = stream.ReadUInt32();
                if( stream.Version >= 584 )
                {
                    // Additional tables, like thumbnail, and guid data.
                    if( stream.Version >= 623 )
                    {
                        stream.Skip( 12 );
                    }
                    stream.Skip( 4 );
                }
            }
        }

        private PackageSummary _Data;

        /// <summary>
        /// The guid of this package. Used to test if the package on a client is equal to the one on a server.
        /// </summary>
        public string GUID;

        /// <summary>
        /// List of heritages. UE1 way of defining generations.
        /// </summary>
        private IList<ushort> _Heritages;

        /// <summary>
        /// List of package generations.
        /// </summary>
        public UArray<UGenerationTableItem> Generations{ get; private set; }

        /// <summary>
        /// The Engine version the package was created with.
        /// </summary>
        public int EngineVersion = -1;

        /// <summary>
        /// The Cooker version the package was cooked with.
        /// </summary>
        public int CookerVersion;

        /// <summary>
        /// The type of compression the package is compressed with.
        /// </summary>
        public uint CompressionFlags;

        /// <summary>
        /// List of compressed chunks throughout the package.
        /// </summary>
        public UArray<CompressedChunk> CompressedChunks;

        /// <summary>
        /// List of unique unreal names.
        /// </summary>
        public List<UNameTableItem> Names{ get; private set; }

        /// <summary>
        /// List of info about exported objects.
        /// </summary>
        public List<UExportTableItem> Exports{ get; private set; }

        /// <summary>
        /// List of info about imported objects.
        /// </summary>
        public List<UImportTableItem> Imports{ get; private set; }

        /// <summary>
        /// List of info about dependency objects.
        /// </summary>
        //public List<UDependencyTableItem> Dependencies{ get; private set; }
        #endregion

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
        public List<UObject> Objects{ get; private set; }

        public NativesTablePackage NTLPackage;
        #endregion

        #region Constructors
        /// <summary>
        /// A Collection of flags describing how a package should be initialized.
        /// </summary>
        [Flags]
        [ObfuscationAttribute(Exclude = true)]
        public enum InitFlags : ushort
        {
            Construct		=	0x0001,
            Deserialize		=	0x0002,// 			| Construct,
            Import			=	0x0004,// 			| Serialize,
            Link			=	0x0008,// 			| Serialize,
            All				=	RegisterClasses		| Construct 	| Deserialize 	| Import 	| Link,
            RegisterClasses	=	0x0010
        }

        /// <summary>
        /// Creates a new instance of the UELib.UnrealPackage class with a PackageStream and name. 
        /// </summary>
        /// <param name="stream">A loaded UELib.PackageStream.</param>
        private UnrealPackage( UPackageStream stream )
        {
            _FullPackageName = stream.Name;
            Stream = stream;
        }

        /// <summary>
        /// Load a package and return it with all the basic data that can be found in every unreal package.
        /// </summary>
        /// <param name="packagePath"> </param>
        /// <param name="fileAccess"> </param>
        /// <returns>Deserialized UELib.UnrealPackage.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Reliability", "CA2000:Dispose objects before losing scope" )]
        public static UnrealPackage DeserializePackage( string packagePath, FileAccess fileAccess = FileAccess.Read )
        {
            var stream = new UPackageStream( packagePath, FileMode.Open, fileAccess );
            var pkg = new UnrealPackage( stream );
            stream.Package = pkg;	 // Very important so the stream Version will not throw a lot exceptions :P
            Console.WriteLine( "Package:" + pkg.PackageName );

            // File Type
            // Signature is tested in UPackageStream
            pkg.IsBigEndianEncoded = stream.BigEndianCode;

            // Read as one variable due Big Endian Encoding.
            pkg.Version = stream.ReadUInt32();
            pkg.LicenseeVersion = (ushort)(pkg.Version >> 16);
            pkg.Version = (pkg.Version & 0xFFFFU);
            Console.WriteLine( "\tPackage Version:" + pkg.Version + "/" + pkg.LicenseeVersion );

            pkg.Build = new GameBuild( pkg );
            Console.WriteLine( "\tBuild:" + pkg.Build.Name );

            if( pkg.Version >= 249 )
            {
                // Offset to the first class(not object) in the package.
                pkg.HeaderSize = stream.ReadUInt32();
                if( pkg.Version >= 269 )
                {
                    // UPK content category e.g. Weapons, Sounds or Meshes.
                    pkg.Group = stream.ReadText();
                }
            }

            // Bitflags such as AllowDownload.
            pkg.PackageFlags = stream.ReadUInt32();
            Console.WriteLine( "\tPackage Flags:" + pkg.PackageFlags );

            // Summary data such as ObjectCount.
            pkg._Data = new PackageSummary();
            pkg._Data.Deserialize( stream );
            Console.WriteLine( "\tNames Count:" + pkg._Data.NamesCount + "\tNames Offset:" + pkg._Data.NamesOffset 
                + "\r\n\tExports Count:" + pkg._Data.ExportsCount + "\tExports Offset:" + pkg._Data.ExportsOffset 
                + "\r\n\tImports Count:" + pkg._Data.ImportsCount + "\tImports Offset:" + pkg._Data.ImportsOffset 
            );
    
            if( pkg.Version < 68 )
            {
                int heritageCount = stream.ReadInt32();
                int heritageOffset = stream.ReadInt32();

                stream.Seek( heritageOffset, SeekOrigin.Begin );
                pkg._Heritages = new List<ushort>( heritageCount );
                for( var i = 0; i < heritageCount; ++ i )
                {
                    pkg._Heritages.Add( stream.ReadUShort() );
                }
            }
            else
            {
#if THIEFDEADLYSHADOWS
                if( pkg.Build == GameBuild.BuildName.Thief_DS )
                {
                    stream.Skip( 4 );
                }
#endif
#if BORDERLANDS
                if( pkg.Build == GameBuild.BuildName.Borderlands )
                {
                    stream.Skip( 4 );
                }
#endif
                pkg.GUID = stream.ReadGuid();
                Console.Write( "\r\n\tGUID:" + pkg.GUID + "\r\n" );

                int generationCount = stream.ReadInt32();
                pkg.Generations = new UArray<UGenerationTableItem>( stream, generationCount );	

                if( pkg.Version >= 245 )
                {
                    // The Engine Version this package was created with
                    pkg.EngineVersion = stream.ReadInt32();
                    Console.WriteLine( "\tEngineVersion:" + pkg.EngineVersion );
                    if( pkg.Version >= VCOOKEDPACKAGES )
                    {
                        // The Cooker Version this package was cooked with
                        pkg.CookerVersion = stream.ReadInt32();
                        Console.WriteLine( "\tCookerVersion:" + pkg.CookerVersion );

                        // Read compressed info?
                        if( pkg.Version >= 334 )
                        {	
                            if( pkg.IsCooked() )
                            {
                                pkg.CompressionFlags = stream.ReadUInt32();
                                Console.WriteLine( "\tCompressionFlags:" + pkg.CompressionFlags );
                                pkg.CompressedChunks = new UArray<CompressedChunk>{Capacity = stream.ReadInt32()};
                                //long uncookedSize = stream.Position;
                                if( pkg.CompressedChunks.Capacity > 0 )
                                {
                                    pkg.CompressedChunks.Deserialize( stream, pkg.CompressedChunks.Capacity );
                                    return pkg;

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
                        }
                    }
                }
            }
            
            
            // Read the name table
            if( pkg._Data.NamesCount > 0 )
            {
                stream.Seek( pkg._Data.NamesOffset, SeekOrigin.Begin );
                pkg.Names = new List<UNameTableItem>( (int)pkg._Data.NamesCount );
                for( var i = 0; i < pkg._Data.NamesCount; ++ i )
                {
                    var nameEntry = new UNameTableItem{Offset = (int)stream.Position, Index = i};
                    nameEntry.Deserialize( stream );
                    nameEntry.Size = (int)(stream.Position - nameEntry.Offset);
                    pkg.Names.Add( nameEntry );
                }
            }

            // Read Import Table
            if( pkg._Data.ImportsCount > 0 )
            {
                stream.Seek( pkg._Data.ImportsOffset, SeekOrigin.Begin );
                pkg.Imports = new List<UImportTableItem>( (int)pkg._Data.ImportsCount );
                for( var i = 0; i < pkg._Data.ImportsCount; ++ i )
                {
                    var imp = new UImportTableItem{Offset = (int)stream.Position, Index = i, Owner = pkg};
                    imp.Deserialize( stream );		
                    imp.Size = (int)(stream.Position - imp.Offset);
                    pkg.Imports.Add( imp );
                }
            }

            // Read Export Table
            if( pkg._Data.ExportsCount > 0 )
            {
                stream.Seek( pkg._Data.ExportsOffset, SeekOrigin.Begin );
                pkg.Exports = new List<UExportTableItem>( (int)pkg._Data.ExportsCount );
                for( var i = 0; i < pkg._Data.ExportsCount; ++ i )
                {
                    var exp = new UExportTableItem{Offset = (int)stream.Position, Index = i, Owner = pkg};
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
                        pkg.Exports.Add( exp );
                    }
                }
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
            }*/

            pkg.HeaderSize = stream.Position;

            return pkg;
        }

        /// <summary>
        /// Constructs all export objects.
        /// </summary>
        /// <param name="initFlags">Initializing rules such as deserializing and/or linking.</param>
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
                CreateObjectForTable( exp );
            }

            foreach( var imp in Imports )
            {
                CreateObjectForTable( imp );
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
        [Pure]private Type GetClassTypeByClassName( string className )
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
        [Pure]public bool IsRegisteredClass( string className )
        {
            return _RegisteredClasses.Exists( o => o.Name.ToLower() == className.ToLower() );
        }

        /// <summary>
        /// Returns a Object that resides at the specified ObjectIndex.
        /// 
        /// if index is positive a exported Object will be returned.
        /// if index is negative a imported Object will be returned.
        /// if index is zero null will be returned.
        /// </summary>
        /// <param name="objectIndex">The index of the Object in a tablelist.</param>
        /// <returns>The found UELib.Core.UObject if any.</returns>
        [Pure]public UObject GetIndexObject( int objectIndex )
        {
            return (objectIndex < 0 ? Imports[-objectIndex - 1].Object 
                        : (objectIndex > 0 ? Exports[objectIndex - 1].Object 
                        : null));
        }

        /// <summary>
        /// Returns a Object name that resides at the specified ObjectIndex.
        /// </summary>
        /// <param name="objectIndex">The index of the object in a tablelist.</param>
        /// <returns>The found UELib.Core.UObject name if any.</returns>
        [Pure]public string GetIndexObjectName( int objectIndex )
        {
            return GetIndexTable( objectIndex ).ObjectName;
        }

        /// <summary>
        /// Returns a name that resides at the specified NameIndex.
        /// </summary>
        /// <param name="nameIndex">A NameIndex into the NameTableList.</param>
        /// <returns>The name at specified NameIndex.</returns>
        [Pure]public string GetIndexName( int nameIndex )
        {
            return Names[nameIndex].Name;
        }

        /// <summary>
        /// Returns a UnrealTable that resides at the specified TableIndex.
        /// 
        /// if index is positive a ExportTable will be returned.
        /// if index is negative a ImportTable will be returned.
        /// if index is zero null will be returned.
        /// </summary>
        /// <param name="tableIndex">The index of the Table.</param>
        /// <returns>The found UELib.Core.UnrealTable if any.</returns>
        [Pure]public UObjectTableItem GetIndexTable( int tableIndex )
        {
            return 	(tableIndex < 0 ? Imports[-tableIndex - 1] 
                    : (tableIndex > 0 ? (UObjectTableItem)Exports[tableIndex - 1] 
                    : null));
        }

        /// <summary>
        /// Tries to find a UELib.Core.UObject with a specified name and type.
        /// </summary>
        /// <param name="objectName">The name of the object to find.</param>
        /// <param name="type">The type of the object to find.</param>
        /// <param name="checkForSubclass">Whether to test for subclasses of type as well.</param>
        /// <returns>The found UELib.Core.UObject if any.</returns>
        [Pure]public UObject FindObject( string objectName, Type type, bool checkForSubclass = false )
        { 
            if( Objects == null )
            {
                return null;
            }

            var obj = Objects.Find( o => String.Compare(o.Name, objectName, StringComparison.OrdinalIgnoreCase) == 0 &&
                (checkForSubclass ? o.GetType().IsSubclassOf( type ) : o.GetType() == type) );
            return obj;
        }

        /// <summary>
        /// Checks whether this package is marked with @flag.
        /// </summary>
        /// <param name="flag">The enum @flag to test.</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [Pure]public bool HasPackageFlag( Flags.PackageFlags flag )
        {
            return (PackageFlags & (uint)flag) != 0;
        }

        /// <summary>
        /// Checks whether this package is marked with @flag. 
        /// </summary>
        /// <param name="flag">The uint @flag to test</param>
        /// <returns>Whether this package is marked with @flag.</returns>
        [Pure]public bool HasPackageFlag( uint flag )
        {
            return (PackageFlags & flag) != 0;
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is cooked. 
        /// </summary>
        /// <returns>True if cooked or False if not.</returns>
        [Pure]public bool IsCooked()
        {
            return HasPackageFlag( Flags.PackageFlags.Cooked ) && Version >= VCOOKEDPACKAGES;
        }

        /// <summary>
        /// Tests the package for console build indications.
        /// </summary>
        /// <returns>Whether package is cooked for consoles.</returns>
        [Pure]public bool IsConsoleCooked()
        {
            return IsCooked() && (IsBigEndianEncoded || Build.IsConsoleCompressed) && !Build.IsXenonCompressed;
        }

        /// <summary>
        /// Checks for the Map flag in PackageFlags. 
        /// </summary>
        /// <returns>Whether if this package is a map.</returns>
        [Pure]public bool IsMap()
        {
            return HasPackageFlag( Flags.PackageFlags.Map );
        }

        /// <summary>
        /// Checks if this package contains code classes.
        /// </summary>
        /// <returns>Whether if this package contains code classes.</returns>
        [Pure]public bool IsScript()
        {
            return HasPackageFlag( Flags.PackageFlags.Script );
        }

        /// <summary>
        /// Checks if this package was built using the debug configuration.
        /// </summary>
        /// <returns>Whether if this package was built in debug configuration.</returns>
        [Pure]public bool IsDebug()
        {
            return HasPackageFlag( Flags.PackageFlags.Debug );
        }

        /// <summary>
        /// Checks for the Stripped flag in PackageFlags.
        /// </summary>
        /// <returns>Whether if this package is stripped.</returns>
        [Pure]public bool IsStripped()
        {
            return HasPackageFlag( Flags.PackageFlags.Stripped );
        }

        /// <summary>
        /// Tests the packageflags of this UELib.UnrealPackage instance whether it is encrypted. 
        /// </summary>
        /// <returns>True if encrypted or False if not.</returns>
        [Pure]public bool IsEncrypted()
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

        [Pure]
        public IUnrealStream GetBuffer()
        {
            return Stream;
        }

        [Pure]
        public int GetBufferPosition()
        {
            return 0;
        }

        [Pure]
        public int GetBufferSize()
        {
            return (int)HeaderSize;
        }

        [Pure]
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

    [UnrealRegisterClass]
    public class UPackage : UObject
    {
        public UPackage()
        {
            ShouldDeserializeOnDemand = true;
        }
    }
}