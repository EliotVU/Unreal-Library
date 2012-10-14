using System.Text;
using System.Collections.Generic;
using UELib.Core;

namespace UELib
{
	/// <summary>
	/// Represents a basic file table.
	/// </summary>
	public class Table
	{
		/// <summary>
		/// Index of this Table in a TableList
		/// </summary>
		public int TableIndex = 0;

		/// <summary>
		/// Offset to this Table in a Package
		/// </summary>
		public long TableOffset = 0;

		public int TableSize = 0;
	}

	/// <summary>
	/// Represents a unreal name table with serialized data from a unreal package header.
	/// </summary>
	public class UnrealNameTable : Table, IUnrealDeserializableClass
	{
		#region Serialized Members
		/// <summary>
		/// Object Name
		/// </summary>
		public string Name = "";

		/// <summary>
		/// Object Flags, such as LoadForEdit, LoadForServer, LoadForClient
		/// </summary>
		/// <value>
		/// 32bit in UE2
		/// 64bit in UE3
		/// </value>
		public ulong Flags = 0;
		#endregion

		public void Deserialize( IUnrealStream stream )
		{
			Name = stream.ReadName();
			if( Name.IndexOf( "\\", System.StringComparison.Ordinal ) != -1 )
			{
				Name = Name.Replace( "\0", "N_" + TableIndex );
			}
			Flags = stream.UR.ReadQWORDFlags();
						
			// De-obfuscate names that contain unprintable characters!
			/*foreach( char c in Name )
			{
				if( !char.IsLetterOrDigit( c ) )
				{
					Name = "N" + TableIndex + "_OBF";
					break;
				}
			}*/
		}

		#region Writing Methods
		/// <summary>
		/// Updates the Name inside the Stream to the current set Name of this Table
		/// </summary>
		/// <param name="stream">Stream to Update</param>
		public void WriteName( UPackageStream stream )
		{
			stream.Seek( TableOffset, System.IO.SeekOrigin.Begin );

			int Size = stream.ReadIndex();
			byte[] rawName = Encoding.ASCII.GetBytes( Name );
			stream.UW.Write( rawName, 0, Size - 1 );
		}

		/// <summary>
		/// Updates the Flags inside the Stream to the current set Flags of this Table
		/// </summary>
		/// <param name="stream">Stream to Update</param>
		public void WriteFlags( UPackageStream stream )
		{
			stream.Seek( TableOffset, System.IO.SeekOrigin.Begin );

			stream.ReadName();
			if( stream.Version <= 200 )
			{
				// Writing UINT
				stream.UW.Write( (uint)Flags );
			}
			else
			{
				// Writing ULONG
				stream.UW.Write( Flags );
			}
		}
		#endregion
	}

	/// <summary>
	/// Represents a unreal table with general deserialized data from a unreal package header.
	/// </summary>
	public abstract class UnrealTable : Table
	{
		#region PreInitialized Members
		/// <summary>
		/// Reference to the UnrealPackage this object resists in
		/// </summary>
		public UnrealPackage Owner = null;

		/// <summary>
		/// Reference to the serialized object based on this table.
		/// 
		/// Only valid if Owner != null and Owner is fully serialized or on demand.
		/// </summary>
		public UObject Object = null;
		#endregion

		#region Serialized Members
		/// <summary>
		/// Name index to the name of this object
		/// -- Fixed
		/// </summary>
		public int ObjectIndex = 0;
		public string ObjectName{ get{ return ObjectNumber > 0 ? Owner.GetIndexName( ObjectIndex ) + "_" + ObjectNumber : Owner.GetIndexName( ObjectIndex ); } }
		public int ObjectNumber;

		/// <summary>
		/// Import:Name index to the class of this object
		/// Export:Object index to the class of this object
		/// -- Not Fixed
		/// </summary>
		public int ClassIndex = 0;
		public UnrealTable ClassTable{ get{ return Owner.GetIndexTable( ClassIndex ); } }
		public string ClassName
		{ 
			get
			{ 
				if( this is UnrealImportTable )
				{
					return Owner.GetIndexName( ClassIndex );
				}
				else
				{
					if( ClassIndex != 0 )
					{
						return Owner.GetIndexTable( ClassIndex ).ObjectName;
					}
					else return "class";
				}
			} 
		}

		/// <summary>
		/// Object index to the outer of this object
		/// -- Not Fixed
		/// </summary>
		public int OuterIndex = 0;
		public UnrealTable OuterTable{ get{ return Owner.GetIndexTable( OuterIndex ); } }
		public string OuterName{ get{ UnrealTable table = OuterTable; return table != null ? table.ObjectName : ""; } }
		#endregion
	}

	/// <summary>
	/// Represents a unreal export table with deserialized data from a unreal package header.
	/// </summary>
	public class UnrealExportTable : UnrealTable, IUnrealDeserializableClass
	{
		#region Serialized Members
		/// <summary>
		/// Object index to the Super(parent) object of structs.
		/// -- Not Fixed
		/// </summary>
		public int SuperIndex = 0;
		public UnrealTable SuperTable{ get{ return Owner.GetIndexTable( SuperIndex ); } }
		public string SuperName{ get{ UnrealTable table = SuperTable; return table != null ? table.ObjectName : ""; } }

		/// <summary>
		/// Object index.
		/// -- Not Fixed
		/// </summary>
		public int ArchetypeIndex = 0;
		public UnrealTable ArchetypeTable{ get{ return Owner.GetIndexTable( ArchetypeIndex ); } }
		public string ArchetypeName{ get{ UnrealTable table = ArchetypeTable; return table != null ? table.ObjectName : ""; } }

		public int UnknownIndex;

		/// <summary>
		/// Object flags, such as Public, Protected and Private.
		/// 32bit aligned.
		/// </summary>
		public ulong ObjectFlags = 0;

		/// <summary>
		/// Object size in bytes.
		/// </summary>
		public int SerialSize = 0;

		/// <summary>
		/// Object offset in bytes. Starting from the beginning of a file.
		/// </summary>
		public int SerialOffset = 0;

		/// <summary>
		/// ???
		/// 
		/// UE3 Only
		/// </summary>
		public uint ExportFlags = 0;

		public Dictionary<int, int> ComponentMap;
		public List<int> NetObjects;
		public string Guid;

		#endregion

		#region Writing Related
		private long _ObjectFlagsOffset = 0;
		#endregion

		public void Deserialize( IUnrealStream stream )
		{
			ClassIndex 		= stream.ReadObjectIndex();
			SuperIndex 		= stream.ReadObjectIndex();
			OuterIndex 		= stream.ReadInt32();
			ObjectIndex 	= stream.ReadNameIndex( out ObjectNumber );	
			
			// GoW Version
			if( stream.Version >= 220 )
			{
				ArchetypeIndex = stream.ReadInt32();
			}

			_ObjectFlagsOffset = stream.Position;	
			//if( stream.Version >= 195 )
			//{
			//    var flags = stream.ReadUInt64();
			//    System.Console.WriteLine( "flags:" + flags );
			//    var hoFlags = ((ulong)(((uint)flags)) << 32);
			//    var loFlags = ((ulong)(flags & 0xFFFFFFFF00000000U) >> 32);
			//    System.Console.WriteLine( "hoFlags:" + hoFlags );
			//    System.Console.WriteLine( "loFlags:" + loFlags );

			//    ObjectFlags =  hoFlags | loFlags;
			//    System.Console.WriteLine( "ObjectFlags:" + ObjectFlags );
			//}
			//else
			//{
			//    ObjectFlags = stream.ReadUInt32();
			//}

			ObjectFlags = stream.ReadUInt32();
			if( stream.Version >= 195 )
			{
			    ObjectFlags = (ObjectFlags << 32) | (ulong)stream.ReadUInt32();
			}

			SerialSize = stream.ReadIndex();
			if( SerialSize > 0 || stream.Version >= 249 )
			{
				SerialOffset = stream.ReadIndex();
			}

			if( stream.Version >= 220 )
			{
				if( stream.Version < 543 )
				{
					// ComponentMap
					int componentMapCount = stream.ReadInt32();
					if( componentMapCount > 0 )
					{
						ComponentMap = new Dictionary<int,int>( componentMapCount );
						for( int i = 0; i < componentMapCount; ++ i )
						{
							ComponentMap.Add( stream.ReadNameIndex(), stream.ReadObjectIndex() );
						}
					}
				}

				if( stream.Version >= 247 )
				{
					ExportFlags = stream.ReadUInt32();
					if( stream.Version >= 322 )
					{
						// NetObjectCount
						int netObjectCount = stream.ReadInt32();
						if( netObjectCount > 0 )
						{
							NetObjects = new List<int>( netObjectCount );
							for( int i = 0; i < netObjectCount; ++ i )
							{
								NetObjects.Add( stream.ReadObjectIndex() );
							}
						}

						// 000* Guid...
						Guid = stream.ReadGuid();
						if( stream.Version > 486 )	// 475?	 486(> Stargate Worlds)
						{
							// Depends?
							stream.ReadInt32();
						}
					}
				}
			}
		}

		#region Writing Methods
		/// <summary>
		/// Updates the ObjectFlags inside the Stream to the current set ObjectFlags of this Table
		/// </summary>
		public void WriteObjectFlags()
		{
			Owner.Stream.Seek( _ObjectFlagsOffset, System.IO.SeekOrigin.Begin );
			Owner.Stream.UW.Write( (uint)ObjectFlags );
		}
		#endregion
	}

	/// <summary>
	/// Represents a unreal import table with deserialized data from a unreal package header.
	/// </summary>
	public class UnrealImportTable : UnrealTable, IUnrealDeserializableClass
	{
		#region Serialized Members
		/// <summary>
		/// Name index to the package that contains this object class
		/// -- Fixed
		/// </summary>
		public int PackageIndex = 0;
		public string PackageName{ get{ return PackageNumber > 0 ? Owner.GetIndexName( PackageIndex ) + "_" + PackageNumber : Owner.GetIndexName( PackageIndex ); } }
		public int PackageNumber;
		#endregion

		public void Deserialize( IUnrealStream stream )
		{
			PackageIndex 		= stream.ReadNameIndex( out PackageNumber );
			ClassIndex 			= stream.ReadNameIndex();
			OuterIndex 			= stream.ReadInt32();
			ObjectIndex 		= stream.ReadNameIndex( out ObjectNumber );
		}
	}

	public class UnrealDependsTable : UnrealTable, IUnrealDeserializableClass
	{
		#region Serialized Members
		public List<int> Dependencies;
		#endregion

		public void Deserialize( IUnrealStream stream )
		{
			Dependencies.Deserialize( stream );
		}
	}
}