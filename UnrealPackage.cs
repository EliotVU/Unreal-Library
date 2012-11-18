#define UNREAL2

using System;
using System.Collections.Generic;
using System.IO;

namespace UELib
{
	using Core;
	using Engine;

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
	/// Represents data of a loaded unreal package. 
	/// </summary>
	public sealed class UnrealPackage : IDisposable
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
			public const ushort VSIZEPREFIXDEPRECATED	= 64;
			public const ushort VINDEXDEPRECATED		= 178;
			public const ushort VCOOKEDPACKAGES			= 277;

			/// <summary>
			/// All NameIndex diverse from the standard ReadIndex due an additional 32bit after each index targeted for names.
			/// </summary>
			public const ushort VNAMEINDEX				= 500;
			public const ushort VDLLBIND				= 655;

			/// <summary>
			/// New class modifier "ClassGroup(Name[,Name])"
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

		public enum LicenseeVersions : ushort
		{
			UT2K4 = 29,
			Borderlands = 58,
			MirrorsEdge = 43,
			ThiefDeadlyShadows = 133,
			Swat4 = 27,
		}

		public class GameBuild
		{
			public sealed class GameIDAttribute : Attribute
			{
				private readonly int _MinVersion;
				private readonly int _MaxVersion;
				private readonly uint _MinLicensee;
				private readonly byte _IsConsoleCompressed;
				private readonly byte _IsXenonCompressed;
				private readonly uint _MaxLicensee;

				private readonly bool _VerifyEqual;

				public GameIDAttribute( int minVersion, uint minLicensee, 
					byte isConsoleCompressed = 2, byte isXenonCompressed = 2 )
				{
					_MinVersion = minVersion;
					_MinLicensee = minLicensee;
					_IsConsoleCompressed = isConsoleCompressed;
					_IsXenonCompressed = isXenonCompressed;
					_VerifyEqual = true;
				}

				public GameIDAttribute( int minVersion, int maxVersion, uint minLicensee, uint maxLicensee, 
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

			public enum ID
			{
				Unset,
				Default,
				Unknown,

				[GameIDAttribute( 61, 0 )]
				Unreal1,

				[GameIDAttribute( 68, 69, 0u, 0u )]
				UT,

				[GameIDAttribute( 99, 117, 5u, 8u )]
				UT2003,

				[GameIDAttribute( 100, 58 )]
				XIII,

				[GameIDAttribute( 110, 2609 )]
				Unreal2,			// Has custom support!

				[GameIDAttribute( 118, 128, 25u, 29u )]
				UT2004,

				[GameIDAttribute( 129, 27 )]
				Swat4,				// Has custom support!

				[GameIDAttribute( 369, 6 )]
				RoboBlitz,

				[GameIDAttribute( 421, 11 )]
				MOHA,

				[GameIDAttribute( 490, 9 )]
				GoW1,

				[GameIDAttribute( 512, 0 )]
				UT3,

				[GameIDAttribute( 536, 43 )]
				MirrorsEdge,		// Has custom support!

				[GameIDAttribute( 547, 547, 28u, 32u )]
				APB,				// Has custom support!

				[GameIDAttribute( 575, 0, 0, 1 )]
				GoW2,				// Has custom support!

				[GameIDAttribute( 576, 5 )]
				CrimeCraft,			// Has custom support!

				[GameIDAttribute( 576, 100 )]
				Homefront,

				[GameIDAttribute( 584, 126 )]
				Singularity,

				[GameIDAttribute( 590, 1, 0, 1 )]
				ShadowComplex,

				[GameIDAttribute( 742, 29 )]
				BulletStorm,

				[GameIDAttribute( 828, 0 )]
				InfinityBlade,

				[GameIDAttribute( 828, 0 )]
				GoW3,

				[GameIDAttribute( 832, 46 )]
				Borderlands2,		// Has custom support!

				[GameIDAttribute( 842, 1, 1 )]
				InfinityBlade2,

				[GameIDAttribute( 904, 904, 9u, 9u, 0, 0 )]
				SpecialForce2,		// Has custom support!
			}

			public ID GameID
			{
				get;
				private set;
			}

			public bool IsConsoleCompressed;
			public bool IsXenonCompressed;

			public GameBuild( UnrealPackage package )
			{
				if( UnrealConfig.Platform == UnrealConfig.CookedPlatform.Console )
				{
					IsConsoleCompressed = true;
				}

				var gameBuilds = Enum.GetValues( typeof(ID) ) as ID[];
				foreach( var gameBuild in gameBuilds )
				{
					var gameBuildMember = typeof(ID).GetMember( gameBuild.ToString() );
					if( gameBuildMember.Length == 0 )
						continue;

					var attribs = gameBuildMember[0].GetCustomAttributes( false );
					if( attribs.Length == 0 )
						continue;

					var myAttrib = attribs[0] as GameIDAttribute;
 					if( myAttrib.Verify( this, package ) )
 					{
						GameID = (ID)Enum.Parse( typeof(ID), Enum.GetName( typeof(ID), gameBuild ) );
						break;
 					}
				}

				if( GameID == ID.Unset )
				{
					GameID = package.LicenseeVersion == 0 ? ID.Default : ID.Unknown;	
				}
				
			}

			public static bool operator ==( GameBuild b, ID i )
			{
				return b.GameID == i;
			}

			public static bool operator !=( GameBuild b, ID i )
			{
				return b.GameID != i;
			}

			public override bool Equals( object obj )
			{
				return GameID == (ID)obj;
			}

			public override int GetHashCode()
			{
				return (int)GameID;
			}
		}

		public GameBuild Build;

		/// <summary>
		/// The bitflags of this package.
		/// </summary>
		public uint PackageFlags;

		/// <summary>
		/// Size of the Header. Basically points to the first Object in the package.
		/// </summary>
		private uint _HeaderSize;

		public string Group;

		/// <summary>
		/// Writes the present PackageFlags to disk. HardCoded!
		/// 
		/// Only supports UT2004.
		/// </summary>
		public void WritePackageFlags()
		{
			Stream.Position = 8;
			Stream.UW.Write( PackageFlags );
		}

		public struct PackageSummary : IUnrealDeserializableClass
		{
			public uint NameCount, 		NameOffset;
			public uint ExportCount, 	ExportOffset;
			public uint ImportCount, 	ImportOffset;
			public uint DependsOffset;

			public uint DependsCount{ get{ return ExportCount; } }

			public void Deserialize( IUnrealStream stream )
			{
				NameCount = stream.ReadUInt32();
				NameOffset = stream.ReadUInt32();

				ExportCount = stream.ReadUInt32();
				ExportOffset = stream.ReadUInt32();

#if APB
				if( stream.Package.Build == GameBuild.ID.APB )
				{
					stream.Skip( 24 );
				}
#endif

				ImportCount = stream.ReadUInt32();
				ImportOffset = stream.ReadUInt32();

				if( stream.Version >= 415 )
				{
					DependsOffset = stream.ReadUInt32();
					if( stream.Version >= 584 )
					{
						if( stream.Version >= 623 )
						{
							stream.ReadUInt32();	// ImportExportGuidsOffset
							stream.ReadUInt32();	// ImportGuidsCount
							stream.ReadUInt32();	// ExportGuidsCount
						}
						stream.ReadUInt32();		// ThumbnailTableOffset
					}
				}	
			}
		}

		public PackageSummary Data;

		/// <summary>
		/// The guid of this package. Used to test if the package on a client is equal to the one on a server.
		/// </summary>
		public string GUID;

		/// <summary>
		/// UE1- Only!
		/// </summary>
		private List<int> _HeritageTableList;

		public struct GenerationInfo : IUnrealDeserializableClass
		{
			/// <summary>
			/// Amount of exported objects that resist within a package.
			/// </summary>
			public int ExportCount;

			/// <summary>
			/// Amount of unique names that resist within a package.
			/// </summary>
			public int NameCount;
			public int NetObjectCount;

			public void Deserialize( IUnrealStream stream )
			{
				ExportCount = stream.ReadInt32();
				NameCount = stream.ReadInt32();
				if( stream.Version >= 322 )
				{
					NetObjectCount = stream.ReadInt32();
				}		
			}
		}

		/// <summary>
		/// List of package generations.
		/// </summary>
		public UArray<GenerationInfo> GenerationsList{ get; private set; }

		/// <summary>
		/// The Engine version this package was created with
		/// UE3+ Only!
		/// </summary>
		public int EngineVersion = -1;

		/// <summary>
		/// The Cooker version this package was cooked with
		/// UE3+ Only!
		/// </summary>
		public int CookerVersion;

		public uint CompressionFlags;
		public UArray<CompressedChunk> CompressedChunks;

		/// <summary>
		/// List of unique unreal names.
		/// </summary>
		public List<UNameTableItem> NameTableList{ get; private set; }

		/// <summary>
		/// List of info about exported objects.
		/// </summary>
		public List<UExportTableItem> ExportTableList{ get; private set; }

		/// <summary>
		/// List of info about imported objects.
		/// </summary>
		public List<UImportTableItem> ImportTableList{ get; private set; }

		/// <summary>
		/// List of info about dependency objects.
		/// </summary>
		public List<UDependencyTableItem> DependsTableList{ get; private set; }
		#endregion

		#region Initialized Members
		private struct ObjectClass
		{
			public string Name;
			public Type Class;
		}

		/// <summary>
		/// Class types that should get added to the ObjectsList.
		/// </summary>
		private readonly List<ObjectClass> _RegisteredClasses = new List<ObjectClass>();

		/// <summary>
		/// List of UObjects that were constructed by function ConstructObjects, later deserialized and linked.
		/// 
		/// Includes Exports and Imports!.
		/// </summary>
		public List<UObject> ObjectsList { get; private set; }

		public NativesTablePackage NTLPackage;
		#endregion

		/// <summary>
		/// A Collection of flags describing how a package should be initialized.
		/// </summary>
		[Flags]
		[System.Reflection.ObfuscationAttribute(Exclude = true)]
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

		public bool IsBigEndian;

		/// <summary>
		/// Load a package and return it with all the basic data that can be found in every unreal package.
		/// </summary>
		/// <param name="stream">A loaded UELib.PackageStream.</param>
		/// <returns>Deserialized UELib.UnrealPackage.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Reliability", "CA2000:Dispose objects before losing scope" )]
		public static UnrealPackage DeserializePackage( string packagePath, FileAccess fileAccess = FileAccess.Read )
		{
			var stream = new UPackageStream( packagePath, FileMode.Open, fileAccess );
			var pkg = new UnrealPackage( stream );
			stream.Package = pkg;	 // Very important so the stream Version will not throw a lot exceptions :P
			Console.Write( "Package:" + pkg.PackageName );

			// File Type
			// Signature is tested in UPackageStream
			pkg.IsBigEndian = stream.BigEndianCode;

			// Read as one variable due Big Endian Encoding.
			pkg.Version = stream.ReadUInt32();
			pkg.LicenseeVersion = (ushort)(pkg.Version >> 16);
			pkg.Version = (pkg.Version & 0xFFFFU);
			Console.Write( "\r\n\t" + "PackageVersion:" + pkg.Version + "/" + pkg.LicenseeVersion );

			pkg.Build = new GameBuild( pkg );
			Console.Write( "\r\n\t" + "Build:" + pkg.Build.GameID );

			if( pkg.Version >= 249 )
			{
				// Offset to the first class(not object) in the package.
				pkg._HeaderSize = stream.ReadUInt32();
				if( pkg.Version >= 269 )
				{
					// UPK content category e.g. Weapons, Sounds or Meshes.
					pkg.Group = stream.ReadName();
				}
			}

			// Bitflags such as AllowDownload.
			pkg.PackageFlags = stream.ReadUInt32();
			Console.Write( "\r\n\tPackageFlags:" + pkg.PackageFlags );

			// Summary data such as ObjectCount.
			pkg.Data = new PackageSummary();
			pkg.Data.Deserialize( stream );
			Console.Write( "\r\n\tNameCount:" + pkg.Data.NameCount + " NameOffset:" + pkg.Data.NameOffset 
				+ "\r\n\tExportCount:" + pkg.Data.ExportCount + " ExportOffset:" + pkg.Data.ExportOffset 
				+ "\r\n\tImportCount:" + pkg.Data.ImportCount + " ImportOffset:" + pkg.Data.ImportOffset 
			);
	
			if( pkg.Version < 68 )
			{
				int heritageCount = stream.ReadInt32();
				int heritageOffset = stream.ReadInt32();

				stream.Seek( heritageOffset, SeekOrigin.Begin );
				pkg._HeritageTableList = new List<int>( heritageCount );
				for( var i = 0; i < heritageCount; ++ i )
				{
					pkg._HeritageTableList.Add( stream.ReadUShort() );
				}
			}
			else
			{
				// thief-deadly shadows.
				if( pkg.LicenseeVersion == (ushort)LicenseeVersions.ThiefDeadlyShadows 
					|| pkg.LicenseeVersion == (ushort)LicenseeVersions.Borderlands

					)
				{
					// Unknown
					stream.Skip( 4 );
				}


				pkg.GUID = stream.ReadGuid();
				Console.Write( "\r\n\tGUID:" + pkg.GUID + "\r\n" );

				int generationCount = stream.ReadInt32();
				#if APB
				if( pkg.Build == GameBuild.ID.APB && pkg.LicenseeVersion >= 32 )
				{
					stream.Skip( 16 );
				}
				#endif
				pkg.GenerationsList = new UArray<GenerationInfo>( stream, generationCount );	

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
						if( pkg.LicenseeVersion == (ushort)LicenseeVersions.MirrorsEdge )
						{
							//stream.Skip( 28 );
						}

						// Read compressed info?
						if( pkg.Version >= 334 )
						{	
							if( pkg.IsCooked() )
							{
								pkg.CompressionFlags = stream.ReadUInt32();
								Console.WriteLine( "\tCompressionFlags:" + pkg.CompressionFlags );
								pkg.CompressedChunks = new UArray<CompressedChunk>();
								pkg.CompressedChunks.Capacity = stream.ReadInt32();
								long uncookedSize = stream.Position;
								if( pkg.CompressedChunks.Capacity > 0 )
								{
									pkg.CompressedChunks.Deserialize( stream, pkg.CompressedChunks.Capacity );
									stream.Chunked = true;

									/*if( pkg.Version >= 482 )
									{
										stream.Skip( 4 );
									}*/

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
			if( pkg.Data.NameCount > 0 )
			{
				stream.Seek( pkg.Data.NameOffset, SeekOrigin.Begin );
				pkg.NameTableList = new List<UNameTableItem>( (int)pkg.Data.NameCount );
				for( var i = 0; i < pkg.Data.NameCount; ++ i )
				{
					var nameEntry = new UNameTableItem {Offset = (int)stream.Position, Index = i};
					nameEntry.Deserialize( stream );
					nameEntry.Size = (int)(stream.Position - nameEntry.Offset);
					pkg.NameTableList.Add( nameEntry );
				}
			}

			// Read Export Table
			if( pkg.Data.ExportCount > 0 )
			{
				stream.Seek( pkg.Data.ExportOffset, SeekOrigin.Begin );
				pkg.ExportTableList = new List<UExportTableItem>( (int)pkg.Data.ExportCount );
				for( var i = 0; i < pkg.Data.ExportCount; ++ i )
				{
					var exp = new UExportTableItem{Offset = (int)stream.Position, Index = i, Owner = pkg};
					// For the GetObjectName like functions
					try
					{
						exp.Deserialize( stream );
					}
					catch
					{
						Console.WriteLine( "Failed to deserialize export table index:" + i );
						break;
					}
					finally
					{
						exp.Size = (int)(stream.Position - exp.Offset);
						pkg.ExportTableList.Add( exp );
					}
				}
			}

			// Read Import Table
			if( pkg.Data.ImportCount > 0 )
			{
				stream.Seek( pkg.Data.ImportOffset, SeekOrigin.Begin );
				pkg.ImportTableList = new List<UImportTableItem>( (int)pkg.Data.ImportCount );
				for( var i = 0; i < pkg.Data.ImportCount; ++ i )
				{
					var imp = new UImportTableItem{Offset = (int)stream.Position, Index = i, Owner = pkg};
					imp.Deserialize( stream );		
					imp.Size = (int)(stream.Position - imp.Offset);
					pkg.ImportTableList.Add( imp );
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

			return pkg;
		}

		private void CreateObjectForTable( UObjectTableItem table )
		{
			var objectType = GetClassTypeByClassName( table.ClassName );
			table.Object = objectType == null ? new UnknownObject() : (UObject)Activator.CreateInstance( objectType );
			AddObject( table.Object, table );
			OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Object ) );
		}

		// Used for importing purposes.
		public void InitializeExportObjects( InitFlags initFlags = InitFlags.All )
		{
			ObjectsList = new List<UObject>( ExportTableList.Count );
			foreach( var exp in ExportTableList )
			{
				CreateObjectForTable( exp );
			}

			if( (initFlags & InitFlags.Deserialize) != 0 )
			{
				DeserializeObjects();

				if( (initFlags & InitFlags.Link) != 0 )
				{
					LinkObjects();
				}
			}
			
		}

		// Used for importing purposes.
		public void InitializeImportObjects( bool bInitialize = true )
		{
			ObjectsList = new List<UObject>( ImportTableList.Count );
			foreach( var imp in ImportTableList )
			{
				CreateObjectForTable( imp );
			}

			if( !bInitialize )
			{
				return;
			}

			foreach( var obj in ObjectsList )
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
			if( (initFlags & InitFlags.Deserialize) != 0 )
			{
				try
				{
					DeserializeObjects();
				}
				catch
				{
					throw new SerializingObjectsException();
				}

				try
				{
					if( (initFlags & InitFlags.Import) != 0 )
					{
						ImportObjects();
					}
				}
				catch( Exception )
				{
					//can be treat with as a warning!
					//throw new Exception( "An exception occurred while importing objects" );
				}

				try
				{
					if( (initFlags & InitFlags.Link) != 0 )
					{
						LinkObjects();
					}
				}
				catch( Exception )
				{
					//throw new LinkingObjectsException();
				}
			}
		}

		public class PackageEventArgs : EventArgs
		{
			public enum Id : byte
			{
				Construct = 0,
				Deserialize = 1,
				Import = 2,
				Link = 3,
				Object = 0xFF,
			}

			public readonly Id EventId;

			public PackageEventArgs( Id eventId )
			{
				EventId = eventId;
			}
		}

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
		public void ConstructObjects()
		{		
			ObjectsList = new List<UObject>();
			OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Construct ) );
			foreach( var exp in ExportTableList )
			{
				CreateObjectForTable( exp );
			}

			foreach( var imp in ImportTableList )
			{
				CreateObjectForTable( imp );
			}
		}

		/// <summary>
		/// Deserializes all exported objects. 
		/// </summary>
		public void DeserializeObjects()
		{
			// Only exports should be deserialized and PostInitialized!
			OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Deserialize ) );
			foreach( var exp in ExportTableList )
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
		public void ImportObjects()
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
		public void LinkObjects()
		{
			// Notify that deserializing is done on all objects, now objects can read properties that were dependent on deserializing
			OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Link ) );
			foreach( var exp in ExportTableList )		
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

		// RegisterClass method is based upon UTPT
		#region RegisterClasses
		public void RegisterClass( string className, Type classObject )
		{
			var obj = new ObjectClass{ Name = className, Class = classObject };
			_RegisteredClasses.Add( obj );
		}

		public bool IsRegisteredClass( string className )
		{
			return _RegisteredClasses.FindIndex( o => o.Name.ToLower() == className.ToLower() ) != -1;
		}

		public void RegisterAllCodeClasses()
		{
			//RegisterClass( "Field", typeof(UField) );
				RegisterClass( "Const", typeof(UConst) );
				RegisterClass( "Enum", typeof(UEnum) );
				
				RegisterClass( "Struct", typeof(UStruct) );	 
					RegisterClass( "ScriptStruct", typeof(UStruct) );
					RegisterClass( "Function", typeof(UFunction) );
					RegisterClass( "State", typeof(UState) );
						RegisterClass( "Class", typeof(UClass) );
				//RegisterClass( "Property", typeof(UProperty) );
					RegisterClass( "ArrayProperty", typeof(UArrayProperty) );
					RegisterClass( "BoolProperty", typeof(UBoolProperty) );
					RegisterClass( "ByteProperty", typeof(UByteProperty) );
					RegisterClass( "DelegateProperty", typeof(UDelegateProperty) );			// UE3?
					RegisterClass( "FixedArrayProperty", typeof(UFixedArrayProperty) );		// UE1
					RegisterClass( "FloatProperty", typeof(UFloatProperty) );
					RegisterClass( "InterfaceProperty", typeof(UInterfaceProperty) );		// UE3+
					RegisterClass( "IntProperty", typeof(UIntProperty) );
					RegisterClass( "MapProperty", typeof(UMapProperty) );					// Obsolete
					RegisterClass( "NameProperty", typeof(UNameProperty) );
					RegisterClass( "ObjectProperty", typeof(UObjectProperty) );
						RegisterClass( "ClassProperty", typeof(UClassProperty) );
						RegisterClass( "ComponentProperty", typeof(UComponentProperty) );	// UE3
					RegisterClass( "PointerProperty", typeof(UPointerProperty) );			// UE2 Only?
					RegisterClass( "StringProperty", typeof(UStringProperty) ); 			// UE1
					RegisterClass( "StrProperty", typeof(UStrProperty) );
					RegisterClass( "StructProperty", typeof(UStructProperty) );			
		}

		public void RegisterAllContentClasses()
		{
			RegisterClass( "Package", typeof(UPackage) );
			RegisterClass( "Texture", typeof(UTexture) );
			RegisterClass( "Palette", typeof(UPalette) );
			RegisterClass( "Model", typeof(UModel) );
			RegisterClass( "Sound", typeof(USound) );

			RegisterClass( "TextBuffer", typeof(UTextBuffer) );
			RegisterClass( "MetaData", typeof(UMetaData) );
		}

		public void RegisterAllClasses()					
		{
			RegisterAllCodeClasses();
			RegisterAllContentClasses();
		}

		private Type GetClassTypeByClassName( string className )
		{		
			var c = _RegisteredClasses.Find( 
				rclass => String.Compare( rclass.Name, className, StringComparison.OrdinalIgnoreCase ) == 0 );
			try
			{
				return c.Class;
			}
			catch( NullReferenceException )
			{
				return null;
			}	         
		}
		#endregion

		// Assumes that ObjectsList was constructed by the programmer using this library.
		private void AddObject( UObject obj, UObjectTableItem T )
		{
			T.Object = obj;
			obj.Package = this;
			obj.NameTable = NameTableList[T.ObjectIndex];
			obj.Table = T;

			if( T is UExportTableItem )
			{
				obj.ObjectIndex = T.Index + 1;
	 		}
			else if( T is UImportTableItem )
			{
				obj.ObjectIndex = -(T.Index + 1);
			}

			ObjectsList.Add( obj );
			if( NotifyObjectAdded != null )
			{
				NotifyObjectAdded.Invoke( this, new ObjectEventArgs( obj ) );
			}
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
		public UObject GetIndexObject( int objectIndex )
		{
			return (objectIndex < 0 ? ImportTableList[-objectIndex - 1].Object 
						: (objectIndex > 0 ? ExportTableList[objectIndex - 1].Object 
						: null));
		}

		/// <summary>
		/// Returns a Object name that resides at the specified ObjectIndex.
		/// </summary>
		/// <param name="objectIndex">The index of the object in a tablelist.</param>
		/// <returns>The found UELib.Core.UObject name if any.</returns>
		public string GetIndexObjectName( int objectIndex )
		{
			return GetIndexTable( objectIndex ).ObjectName;
		}

		/// <summary>
		/// Returns a name that resides at the specified NameIndex.
		/// </summary>
		/// <param name="nameIndex">A NameIndex into the NameTableList.</param>
		/// <returns>The name at specified NameIndex.</returns>
		public string GetIndexName( int nameIndex )
		{
			return NameTableList[nameIndex].Name;
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
		public UObjectTableItem GetIndexTable( int tableIndex )
		{
			try
			{
				return 	(tableIndex < 0 ? ImportTableList[-tableIndex - 1] 
						: (tableIndex > 0 ? (UObjectTableItem)ExportTableList[tableIndex - 1] 
						: null));
			}
			catch( ArgumentOutOfRangeException )
			{
				return ExportTableList[0];
			}
		}

		/// <summary>
		/// Tries to find a UELib.Core.UObject with a specified name and type.
		/// </summary>
		/// <param name="objectName">The name of the object to find.</param>
		/// <param name="type">The type of the object to find.</param>
		/// <returns>The found UELib.Core.UObject if any.</returns>
		public UObject FindObject( string objectName, Type type, bool checkForSubclass = false )
		{ 
			if( ObjectsList == null )
			{
				return null;
			}

			var obj = ObjectsList.Find( o => String.Compare(o.Name, objectName, StringComparison.OrdinalIgnoreCase) == 0 &&
				(checkForSubclass ? o.GetType().IsSubclassOf( type ) : o.GetType() == type) );
			return obj;
		}

		public bool HasPackageFlag( Flags.PackageFlags flag )
		{
			//return ((Flags.PackageFlags)PackageFlags).HasFlag( flag );
			return (PackageFlags & (uint)flag) != 0;
		}

		public bool HasPackageFlag( uint flag )
		{
			return (PackageFlags & flag) != 0;
		}

		/// <summary>
		/// Tests the packageflags of this UELib.UnrealPackage instance whether it is cooked. 
		/// </summary>
		/// <returns>True if cooked or False if not.</returns>
		public bool IsCooked()
		{
			return HasPackageFlag( Flags.PackageFlags.Cooked ) && Version >= VCOOKEDPACKAGES;
		}

		public bool IsConsoleCooked()
		{
			return IsCooked() && (IsBigEndian || Build.IsConsoleCompressed) && !Build.IsXenonCompressed;
		}

		public bool IsMap()
		{
			return HasPackageFlag( Flags.PackageFlags.Map );
		}

		public bool IsScript()
		{
			return HasPackageFlag( Flags.PackageFlags.Script );
		}

		public bool IsDebug()
		{
			return HasPackageFlag( Flags.PackageFlags.Debug );
		}

		public bool IsStripped()
		{
			return HasPackageFlag( Flags.PackageFlags.Stripped );
		}

		/// <summary>
		/// Tests the packageflags of this UELib.UnrealPackage instance whether it is encrypted. 
		/// </summary>
		/// <returns>True if encrypted or False if not.</returns>
		public bool IsEncrypted()
		{
			return HasPackageFlag( Flags.PackageFlags.Encrypted );
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return PackageName;
		}

		public void Dispose()
		{
			if( Stream != null )
			{
				Stream.Close();
			}

			GC.SuppressFinalize( this );
		}

		~UnrealPackage()
		{
			Dispose();
		}
	}
}