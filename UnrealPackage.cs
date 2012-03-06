#define UNREAL2

using System;
using System.Collections.Generic;
using System.IO;

namespace UELib
{
	using UELib.Core;
	using UELib.Engine;

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
		public UPackageStream Stream = null;

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
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue" )]
		public enum GameVersions : ushort
		{
			/// <summary>
			/// 1999
			/// </summary>
			Unreal1 = (ushort)69U,

			/// <summary>
			/// 2004
			/// </summary>
			ThiefDeadlyShadows = (ushort)95U,

			/// <summary>
			/// 2002
			/// </summary>
			Unreal2 = (ushort)110U,

			/// <summary>
			/// 2004
			/// </summary>
			UT2K4 = (ushort)128U,

			/// <summary>
			/// 2006
			/// </summary>
			Roboblitz = (ushort)369U,

			/// <summary>
			/// 2006
			/// </summary>
			GoW = (ushort)490U,

			/// <summary>
			/// 2007
			/// </summary>
			UT3 = (ushort)512U,

			/// <summary>
			/// 2008
			/// </summary>
			MirrorsEdge = (ushort)536U,

			/// <summary>
			/// 2009
			/// </summary>
			Borderlands = (ushort)584U,

			// UDK 2009 - 2011
			UDK_11_2009 = (ushort)648U,
			UDK_12_2009 = (ushort)678U,
			UDK_01_2010 = (ushort)600U,
			UDK_02_2010 = (ushort)600U,
			UDK_03_2010 = (ushort)600U,
			UDK_04_2010 = (ushort)600U,
			UDK_05_2010 = (ushort)706U,
			UDK_06_2010 = (ushort)727U,
			UDK_07_2010 = (ushort)737U,
			UDK_08_2010 = (ushort)756U,
			UDK_09_2010 = (ushort)765U,
			UDK_10_2010 = (ushort)776U,
			UDK_11_2010 = (ushort)799U,
			UDK_12_2010 = (ushort)803U,
			UDK_01_2011 = (ushort)805U,
			UDK_02_2011 = (ushort)810U,
			UDK_04_2011 = (ushort)813U,
			UDK_06_2011 = (ushort)832U,
		}

		private uint _Version = 0;
		public uint Version
		{
			get
			{
				return OverrideVersion > 0 ? OverrideVersion : _Version;
			}

			private set
			{
				_Version = value;
			}
		}

		/// <summary>
		/// For debugging purposes. Change this to override the present Version deserialized from the package.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible" )]
		public static ushort OverrideVersion = 0;

		public const ushort VSizePrefixDeprecated	= 64;
		public const ushort VIndexDeprecated		= 178;
		public const ushort VCookedPackages			= 277;

		/// <summary>
		/// All NameIndex diverse from the standard ReadIndex due an additional 32bit after each index targeted for names.
		/// </summary>
		public const ushort VNameIndex				= 500;	// Guessed
		public const ushort VDLLBind				= 664;

		/// <summary>
		/// New class modifier "ClassGroup(Name[,Name])"
		/// </summary>
		public const ushort VClassGroup				= 789;//799;

		private ushort _LicenseeVersion = 0;
		public ushort LicenseeVersion
		{
			get
			{
				return OverrideLicenseeVersion > 0 ? OverrideLicenseeVersion : _LicenseeVersion;
			}

			private set
			{
				_LicenseeVersion = value;
			}
		}

		/// <summary>
		/// For debugging purposes. Change this to override the present Version deserialized from the package.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible" )]
		public static ushort OverrideLicenseeVersion;

		public enum LicenseeVersions : ushort
		{
			UnrealEngine = 0,
			UT2k4 = 29,
			Unreal2 = 2609,	// 3
			Borderlands = 58,
			MirrorsEdge = 43,
			ThiefDeadlyShadows = 133,
			Roboblitz = 6,	  
			GearsOfWar = 9,
			CrimeCraft = 5,
			DungeonDefenders = 2,
			Swat4 = 27,
		}

		/// <summary>
		/// The bitflags of this package.
		/// </summary>
		public uint PackageFlags;

		/// <summary>
		/// Size of the Header. Basically points to the first Object in the package.
		/// </summary>
		public uint HeaderSize;

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

				ImportCount = stream.ReadUInt32();
				ImportOffset = stream.ReadUInt32();

				if( stream.Version >= 415 )
				{
					DependsOffset = stream.ReadUInt32();
					if( stream.Version >= 584 )	// 623?
					{
						stream.ReadUInt32();	// ImportExportGuidsOffset
						stream.ReadUInt32();	// ImportGuidsCount
						stream.ReadUInt32();	// ExportGuidsCount
						stream.ReadUInt32();	// ThumbnailTableOffset
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
		private List<int> _HeritageTableList = null;

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
		/// > UE1 Only!
		/// </summary>
		private UArray<GenerationInfo> _GenerationInfoList = null;

		/// <summary>
		/// List of package generations.
		/// </summary>
		public UArray<GenerationInfo> GenerationsList
		{
			get{ return _GenerationInfoList; }
		}

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
		public UArray<CompressedChunk> CompressedChunks = null;

		/// <summary>
		/// List of unique unreal names.
		/// </summary>
		private List<UnrealNameTable> _NameTableList = null;

		/// <summary>
		/// List of unique unreal names.
		/// </summary>
		public List<UnrealNameTable> NameTableList
		{
			get{ return _NameTableList; }
		}

		/// <summary>
		/// List of info about exported objects.
		/// </summary>
		private List<UnrealExportTable> _ExportTableList = null;

		/// <summary>
		/// List of info about exported objects.
		/// </summary>
		public List<UnrealExportTable> ExportTableList
		{
			get{ return _ExportTableList; }
		}

		/// <summary>
		/// List of info about imported objects.
		/// </summary>
		private List<UnrealImportTable> _ImportTableList = null;

		/// <summary>
		/// List of info about imported objects.
		/// </summary>
		public List<UnrealImportTable> ImportTableList
		{
			get{ return _ImportTableList; }
		}

		/// <summary>
		/// List of info about dependency objects.
		/// </summary>
		private List<UnrealDependsTable> _DependsTableList = null;

		/// <summary>
		/// List of info about dependency objects.
		/// </summary>
		public List<UnrealDependsTable> DependsTableList
		{
			get{ return _DependsTableList; }
		}
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
		private List<ObjectClass> _RegisteredClasses = new List<ObjectClass>();

		/// <summary>
		/// List of UObjects that were constructed by function ConstructObjects, later deserialized and linked.
		/// 
		/// Includes Exports and Imports!.
		/// </summary>
		public List<UObject> ObjectsList { get; private set; }

		public NativesTablePackage NTLPackage = null;
		#endregion

		/// <summary>
		/// A Collection of flags describing how a package should be initialized.
		/// </summary>
		[Flags]
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
			ObjectsList = null;
			_FullPackageName = stream.Name;
			Stream = stream;
		}

		/// <summary>
		/// Load a package and return it with all the basic data that can be found in every unreal package.
		/// </summary>
		/// <param name="stream">A loaded UELib.PackageStream.</param>
		/// <returns>Deserialized UELib.UnrealPackage.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Reliability", "CA2000:Dispose objects before losing scope" )]
		public static UnrealPackage DeserializePackage( string packagePath, System.IO.FileAccess fileAccess = System.IO.FileAccess.Read )
		{
			var stream = new UPackageStream( packagePath, System.IO.FileMode.Open, fileAccess );
			var pkg = new UnrealPackage( stream );
			stream.Package = pkg;	 // Very important so the stream Version will not throw a lot exceptions :P

			// File Type
			// Signature is tested in UPackageStream

			// Read as one variable due Big Endian Encoding.
			pkg.Version = stream.ReadUInt32();
			pkg.LicenseeVersion = (ushort)(pkg.Version >> 16);
			pkg.Version = (pkg.Version & 0xFFFFU);

			if( pkg.Version >= 249 )
			{
				// Offset to the first class(not object) in the package.
				pkg.HeaderSize = stream.ReadUInt32();
				if( pkg.Version >= 269 )
				{
					// UPK content category e.g. Weapons, Sounds or Meshes.
					pkg.Group = stream.ReadName();
				}
			}

			// Bitflags such as AllowDownload.
			pkg.PackageFlags = stream.ReadUInt32();

			// Summary data such as ObjectCount.
			pkg.Data = new PackageSummary();
			pkg.Data.Deserialize( stream );

			Console.Write( "Package:" + pkg.PackageName 
				+ "\r\n\t" + "PackageVersion:" + pkg.Version + "/" + pkg.LicenseeVersion 
				+ "\r\n\tPackageFlags:" + pkg.PackageFlags
				+ "\r\n\tNameCount:" + pkg.Data.NameCount + " NameOffset:" + pkg.Data.NameOffset 
				+ "\r\n\tExportCount:" + pkg.Data.ExportCount + " ExportOffset:" + pkg.Data.ExportOffset 
				+ "\r\n\tImportCount:" + pkg.Data.ImportCount + " ImportOffset:" + pkg.Data.ImportOffset 
				+ "\r\n\r\n"
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
				if( pkg.LicenseeVersion == (ushort)LicenseeVersions.ThiefDeadlyShadows )
				{
					// Unknown
					stream.Skip( 4 );
				}
				else if( pkg.LicenseeVersion == (ushort)LicenseeVersions.Borderlands )
				{
					stream.Skip( 4 );
				}

				pkg.GUID = stream.ReadGuid();
				Console.WriteLine( "\tGUID:" + pkg.GUID );
				pkg._GenerationInfoList = new UArray<GenerationInfo>( stream, stream.ReadInt32() );	

				if( pkg.Version >= 245 )
				{
					// The Engine Version this package was created with
					pkg.EngineVersion = stream.ReadInt32();
					Console.WriteLine( "\tEngineVersion:" + pkg.EngineVersion );
					if( pkg.Version >= VCookedPackages )
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

									try
									{
										UPackageStream outStream = new UPackageStream( packagePath + ".dec", System.IO.FileMode.Create, FileAccess.ReadWrite );
										//File.SetAttributes( packagePath + ".dec", FileAttributes.Temporary );
										outStream.Package = pkg;
										outStream._BigEndianCode = stream._BigEndianCode;
									
										var headerBytes = new byte[uncookedSize];
										stream.Seek( 0, SeekOrigin.Begin );
										stream.Read( headerBytes, 0, (int)uncookedSize );
										outStream.Write( headerBytes, 0, (int)uncookedSize );   
										foreach( var chunk in pkg.CompressedChunks )
										{
											chunk.Decompress( stream, outStream );
										}
										outStream.Flush();
										pkg.Stream = outStream;
										stream = outStream;
										return pkg;
									}
									catch( Exception e )
									{
										throw new DecompressPackageException();
									}
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
				pkg._NameTableList = new List<UnrealNameTable>( (int)pkg.Data.NameCount );
				for( var i = 0; i < pkg.Data.NameCount; ++ i )
				{
					var nameEntry = new UnrealNameTable {TableOffset = stream.Position, TableIndex = i};
					nameEntry.Deserialize( stream );
					nameEntry.TableSize = (int)(stream.Position - nameEntry.TableOffset);
					pkg.NameTableList.Add( nameEntry );
				}
			}

			// Read Export Table
			if( pkg.Data.ExportCount > 0 )
			{
				stream.Seek( pkg.Data.ExportOffset, SeekOrigin.Begin );
				pkg._ExportTableList = new List<UnrealExportTable>( (int)pkg.Data.ExportCount );
				for( var i = 0; i < pkg.Data.ExportCount; ++ i )
				{
					var exp = new UnrealExportTable{TableOffset = stream.Position, TableIndex = i, Owner = pkg};
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
						exp.TableSize = (int)(stream.Position - exp.TableOffset);
						pkg.ExportTableList.Add( exp );
					}
				}
			}

			// Read Import Table
			if( pkg.Data.ImportCount > 0 )
			{
				stream.Seek( pkg.Data.ImportOffset, SeekOrigin.Begin );
				pkg._ImportTableList = new List<UnrealImportTable>( (int)pkg.Data.ImportCount );
				for( var i = 0; i < pkg.Data.ImportCount; ++ i )
				{
					var imp = new UnrealImportTable{TableOffset = stream.Position, TableIndex = i, Owner = pkg};
					imp.Deserialize( stream );		
					imp.TableSize = (int)(stream.Position - imp.TableOffset);
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

			// AdditionalPackagesToCook
			// TextureAllocations

			return pkg;
		}

		private void CreateObjectForTable( UnrealTable table )
		{
			var objectType = GetClassTypeByClassName( table.ClassName );
			table.Object = objectType == null ? new UnknownObject() : (UObject)Activator.CreateInstance( objectType );
			AddObject( table.Object, table );
			OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Object ) );
		}

		// Used for importing purposes.
		public void InitializeExportObjects( InitFlags initFlags = InitFlags.All )
		{
			ObjectsList = new List<UObject>( _ExportTableList.Count );
			foreach( var exp in _ExportTableList )
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
			ObjectsList = new List<UObject>( _ImportTableList.Count );
			foreach( var imp in _ImportTableList )
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
				RegisterClasses();
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
				catch
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
				catch
				{
					throw new LinkingObjectsException();
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

			public Id EventId;

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
			foreach( var exp in _ExportTableList )
			{
				CreateObjectForTable( exp );
			}

			foreach( var imp in _ImportTableList )
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
			foreach( var exp in _ExportTableList )
			{
				if( !(exp.Object.GetType() == typeof(UnknownObject) || exp.Object.bDeserializeOnDemand) )
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic" )]
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
			foreach( var exp in _ExportTableList )
			{
				try
				{
					if( !(exp.Object.GetType() == typeof(UnknownObject)) )
					{
						exp.Object.PostInitialize();
					}
					OnNotifyPackageEvent( new PackageEventArgs( PackageEventArgs.Id.Object ) );
				}
				catch( InvalidCastException )
				{
					//Exp.Object.
					continue;
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
			return _RegisteredClasses.FindIndex( (o) => o.Name.ToLower() == className.ToLower() ) != -1;
		}

		public void RegisterClasses()					
		{
			// Object...
			RegisterClass( "MetaData", typeof(UMetaData) );
			RegisterClass( "DrawLightConeComponent", typeof(UComponent) );
			RegisterClass( "Field", typeof(UField) );
				RegisterClass( "Const", typeof(UConst) );
				RegisterClass( "Enum", typeof(UEnum) );
				RegisterClass( "Property", typeof(UProperty) );
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
				RegisterClass( "Struct", typeof(UStruct) );	 
					RegisterClass( "ScriptStruct", typeof(UStruct) );
						RegisterClass( "Function", typeof(UFunction) );
						RegisterClass( "State", typeof(UState) );
							RegisterClass( "Class", typeof(UClass) );
			RegisterClass( "TextBuffer", typeof(UTextBuffer) );
			
			RegisterClass( "Package", typeof(UPackage) );

			RegisterClass( "Texture", typeof(UTexture) );
			RegisterClass( "Palette", typeof(UPalette) );

			RegisterClass( "Model", typeof(UModel) );
			RegisterClass( "Sound", typeof(USound) );
		}

		private Type GetClassTypeByClassName( string className )
		{		
			var c = _RegisteredClasses.Find( rclass => String.Compare( rclass.Name, className, true ) == 0 );
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
		private void AddObject( UObject obj, UnrealTable T )
		{
			T.Object = obj;
			obj.Package = this;
			obj.NameTable = _NameTableList[T.ObjectIndex];
			obj.Table = T;

			if( T is UnrealExportTable )
			{
				obj.ObjectIndex = T.TableIndex + 1;
	 		}
			else if( T is UnrealImportTable )
			{
				obj.ObjectIndex = -(T.TableIndex + 1);
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
			// Performance tests

			//return _ObjectsList[index < 0 ? (_ObjectsList.Count - 1) + (index - 1) : index > 0 ? (index - 1) : 0];
			//UnrealTable UT = GetIndexTable( index );
			//return (UT != null ? UT.Object : null);

			/*if( index > 0 )
			{
				return _ObjectsList[index - 1];
			}
			else if( index < 0 )
			{
				return _ImportTableList[-index - 1].Object;
			}
			else return null;*/

			return (objectIndex < 0 ? _ImportTableList[-objectIndex - 1].Object 
						: (objectIndex > 0 ? _ExportTableList[objectIndex - 1].Object 
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
			return _NameTableList[nameIndex].Name;
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
		public UnrealTable GetIndexTable( int tableIndex )
		{
			try
			{
				return 	(tableIndex < 0 ? _ImportTableList[-tableIndex - 1] 
						: (tableIndex > 0 ? (UnrealTable)_ExportTableList[tableIndex - 1] 
						: null));
			}
			catch( ArgumentOutOfRangeException )
			{
				return _ExportTableList[0];
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

			var obj = ObjectsList.Find( o => String.Compare( o.Name, objectName, true ) == 0 &&
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
			return HasPackageFlag( Flags.PackageFlags.Cooked ) && Version >= VCookedPackages;
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