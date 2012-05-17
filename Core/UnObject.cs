using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UELib;

namespace UELib.Core
{
	public class ObjectEventArgs : EventArgs, IRefUObject
	{
		protected UObject _Object;
		public UObject ObjectRef
		{
			get{ return _Object; }
		}

		public ObjectEventArgs( UObject objectRef )
		{
			_Object = objectRef;
		}
	}

	/// <summary>
	/// Represents a unreal object. 
	/// </summary>
	public partial class UObject : Object, ISupportsBuffer, IUnrealDeserializableObject, IDisposable, IComparable
	{
		#region PostConstruct Members
		/// <summary>
		/// The package this object resists in.
		/// </summary>
		public UnrealPackage Package
		{
			get;
			internal set;
		}

		/// <summary>
		/// Index of this Object in the ExportTableList.
		/// 0 if not exported.
		/// </summary>
		public int ExportIndex
		{
			get{ return ObjectIndex; }
		}

		/// <summary>
		/// Index of this Object in the ImportTableList.
		/// 0 if not imported.
		/// </summary>
		public int ImportIndex
		{
			get{ return -ObjectIndex; }
		}
		#endregion

		#region PreInitialized Members
		#region Table
		public UnrealTable Table
		{
			get;
			internal set;
		}

		#region ExportTable
		public UnrealExportTable ExportTable
		{
			get{ return Table as UnrealExportTable; }
		}

		/// <summary>
		/// Object Flags
		/// Warning: Only call after PreInitialize!
		/// </summary>
		/// <value>
		/// 32bit in UE2
		/// 32bit aligned in UE3
		/// </value>
		private ulong ObjectFlags
		{
			get
			{
				// Imports have no flags!
				if( ImportTable != null )
				{
					return 0;
				}
				return ExportTable.ObjectFlags;
			}
		}
		#endregion

		#region ImportTable
		public UnrealImportTable ImportTable
		{
			get{ return Table as UnrealImportTable; }
		}
		#endregion

		/// <summary>
		/// UClass, UStruct etc
		/// </summary>
		public UObject Class
		{
			get{ return Package.GetIndexObject( Table.ClassIndex ); }
		}

		/// <summary>
		/// [Package.Group:Outer].Object
		/// </summary>
		public UObject Outer
		{
			get{ return Package.GetIndexObject( Table.OuterIndex ); }
		}

		public int ObjectIndex
		{
			get;
			internal set;
		}
		#endregion

		#region NameTable
		public UnrealNameTable NameTable
		{
			get;
			internal set;
		}

		private string _CustomName = String.Empty;
		public string Name
		{
			get
			{ 
				if( _CustomName.Length != 0 )
				{
					return _CustomName;
				}
				return Table.ObjectName;
				//return NameTable.Name; 
			}
			set{ _CustomName = value; }
		}

		public int NameIndex
		{
			get{ return NameTable.TableIndex; }
		}

		/// <value>
		/// 32bit in UE2
		/// 64bit in UE3
		/// </value>
		public ulong NameFlags
		{
			get{ return NameTable.Flags; }
			set
			{
				NameTable.Flags = value;
				NameTable.WriteFlags( Package.Stream );
			}
		}
		#endregion
		#endregion

		#region Serialized Members
		/// <summary>
		/// Copy of the Object bytes
		/// </summary>
		protected UObjectStream _Buffer;

		/// <summary>
		/// Copy of the Object bytes
		/// </summary>
		public UObjectStream Buffer 	// Public needed for passing a buffer see(UnDefaultProperties.cs subobjects deserializer).
		{
			get{ return _Buffer; }
		}

		/// <summary>
		/// Object Properties e.g. SubObjects or/and DefaultProperties
		/// </summary>
		protected DefaultPropertiesCollection _Properties = null;

		/// <summary>
		/// Object Properties e.g. SubObjects or/and DefaultProperties
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly" )]
		public DefaultPropertiesCollection Properties
		{
			get{ return _Properties; }
			set{ _Properties = value; }
		}

		public int NetIndex;
		#endregion

		#region General Members
		[Flags]
		public enum ObjectState : byte
		{
			Deserialied = 0x01,
			Errorlized = 0x02,
		}

		public ObjectState SerializationState;
		public Exception ThrownException;
		public long ExceptionPosition;

		/// <summary>
		/// Whether to release the buffer from memory when this Object is done deserializing.
		/// </summary>
		protected bool _bReleaseBuffer = true;

		/// <summary>
		/// Object will not be deserialized by UnrealPackage, Can only be deserialized by calling the methods yourself.
		/// </summary>
		protected bool _bDeserializeOnDemand;

		/// <summary>
		/// Object will not be deserialized by UnrealPackage, Can only be deserialized by calling the methods yourself.
		/// </summary>
		public bool bDeserializeOnDemand
		{
			get{ return _bDeserializeOnDemand; }
		}
		#endregion

		/// <summary>
		///	Creates a new instance of the UELib.Core.UObject class. 
		///	
		/// Everything must be initialized programmatic.
		/// </summary>
		public UObject(){}

		#region Serializiation Methods
		/// <summary>
		/// Notifies this object instance to make a copy of this object's data from the Owner.Stream and then start deserializing this instance.
		/// </summary>
		public void BeginDeserializing()
		{
			// FIXME: Objects deserialization is not supported for Thief's Deadly Shadows!
			if( Package.Version == 95 && Package.LicenseeVersion == (ushort)UnrealPackage.LicenseeVersions.ThiefDeadlyShadows )
			{
				return;
			}

			if( SerializationState.HasFlag( ObjectState.Deserialied ) )
			{
				return;
			}

			// Imported objects cannot be deserialized!
			if( ImportTable != null )
			{
				return;
			}

			// e.g. None.
			if( ExportTable.SerialSize == 0 )
			{
				SerializationState |= ObjectState.Deserialied;
				return;
			}

			try
			{
				var buff = new byte[ExportTable.SerialSize];
				Package.Stream.Seek( ExportTable.SerialOffset, SeekOrigin.Begin ); 
				Package.Stream.Read( buff, 0, ExportTable.SerialSize ); 
				if( Package.Stream._BigEndianCode )
				{
					Array.Reverse( buff );
				}
				_Buffer = new UObjectStream( Package.Stream, ref buff );

				Deserialize();
				SerializationState |= ObjectState.Deserialied;
			}
			catch( Exception e )
			{
				ThrownException = e;
				ExceptionPosition = _Buffer.Position;
				SerializationState |= ObjectState.Errorlized;

				Console.WriteLine( e.Source + ":" + Name + ":" + e.GetType().Name + " occurred while deserializing;"  
					+ "\r\n" + e.StackTrace 
					+ "\r\n" + e.Message
				);
			}
			finally
			{
				if( _bReleaseBuffer )
				{
					_Buffer.Dispose();
					_Buffer.Close();
					_Buffer = null;
				}
			}
		}

		/// <summary>
		/// Deserialize this object's structure from the _Buffer stream.
		/// </summary>
		protected virtual void Deserialize()
		{
#if DEBUG
			Console.WriteLine( "" );
#endif
			NoteRead( Name, this );
			NoteRead( "ExportSize", ExportTable.SerialSize );
			if( HasObjectFlag( Flags.ObjectFlagsLO.HasStack ) )
			{
				int node = _Buffer.ReadIndex();
				/*int StateNode =*/ _Buffer.ReadIndex();
				/*ulong ProbeMask =*/ _Buffer.ReadUInt64();
				/*uint LatentAction =*/ _Buffer.ReadUInt32();
				if( node != 0 )
				{
					/*int NodeOffset =*/ _Buffer.ReadIndex();
				}
			}

#if SWAT4
			if( Package.Build == UnrealPackage.GameBuild.ID.Swat4 )
			{
				// 8 bytes: Value: 3
				// 4 bytes: Value: 1
				_Buffer.Skip( 12 );
			}
#endif

			if( _Buffer.Version > 400 && GetClassName() != "Component" && GetClassName().EndsWith( "Component" ) )
			{
				var componentClass = _Buffer.ReadObjectIndex();
				var componentName = _Buffer.ReadNameIndex();
			}

			// TODO: Corrigate Version
			if( _Buffer.Version >= 322 )
			{
				NetIndex = _Buffer.ReadObjectIndex();
				NoteRead( "NetIndex", NetIndex );
			}

			if( !IsClassType( "Class" ) )
			{	
				/*if( Class.Name.EndsWith( "Component" ) ) 
				{
					// ComponentClass
					_Buffer.ReadNameIndex();
					// ComponentInstance
					_Buffer.ReadInt32();
				}*/

				// REMINDER:Ends with a NameIndex referencing to "None"; 1/4/8 bytes
#if SWAT4
				if( Package.Build != UnrealPackage.GameBuild.ID.Swat4 )
				{
					DeserializeProperties();
				}
				else
				{
					_Buffer.Skip( 1 );
				}
#endif
			}
#if UNREAL2
			else if( Package.Build == UnrealPackage.GameBuild.ID.Unreal2 )
			{
				int count = _Buffer.ReadIndex();
				for( int i = 0; i < count; ++ i )
				{
					/*UObject obj = GetIndexObject(*/ _Buffer.ReadObjectIndex();// );
					//obj.ToString();		// breakpoint
				}
			}
#endif
		}

		/// <summary>
		/// Tries to read all properties that resides in this object instance.
		/// </summary>
		/// <param name="properties">The read properties.</param>
		protected void DeserializeProperties()
		{
			_Properties = new DefaultPropertiesCollection();
			while( true )
			{
				var tag = new UPropertyTag( this );
				if( !tag.Deserialize() )
				{
					break;
				}

				var property = new UDefaultProperty{Tag = tag};
				_Properties.Add( property );
			}

			// We need to keep the MemoryStream alive,
			// because we first deserialize the defaultproperties but we skip the values, which we'll deserialize later on by demand.
			if( _Properties.Count > 0 )
			{
				_bReleaseBuffer = false;
			}
		}

		// Write this instance to the Owner.Stream at the present position.
		[Obsolete("TODO")]
		public virtual void Serialize()
		{
		}

		/// <summary>
		/// Save this buffer to the package stream. NOTE:The size must be equal as the original buffer.
		/// </summary>
		public virtual void CopyToPackageStream()
		{
			Package.Stream.Seek( ExportTable.SerialOffset, SeekOrigin.Begin );
			Package.Stream.Write( _Buffer.GetBuffer(), 0, (int)_Buffer.Length );
		}

		/// <summary>
		/// Initializes this object instance important members.
		/// </summary>
		public virtual void PostInitialize(){}
		public virtual void InitializeImports(){}
		#endregion

		#region Methods
		/// <summary>
		/// Checks if the object contains the specified @flag or one of the specified flags.
		/// 
		/// Checks the lower bits of ObjectFlags.
		/// </summary>
		/// <param name="flag">The flag(s) to compare to.</param>
		/// <returns>Whether it contained one of the specified flags.</returns>
		public bool HasObjectFlag( Flags.ObjectFlagsLO flag )
		{
			return ((uint)ObjectFlags & (uint)flag) != 0;
		}

		/// <summary>
		/// Checks if the object contains the specified @flag or one of the specified flags.
		/// 
		/// Checks the higher bits of ObjectFlags.
		/// </summary>
		/// <param name="flag">The flag(s) to compare to.</param>
		/// <returns>Whether it contained one of the specified flags.</returns>
		public bool HasObjectFlag( Flags.ObjectFlagsHO flag )
		{
			return ((ObjectFlags >> 32) & (uint)flag) != 0;
		}

		// 32bit aligned.
		/// <summary>
		/// Returns a copy of the ObjectFlags.
		/// </summary>
		/// <returns>A copy of @ObjectFlags.</returns>
		public ulong GetObjectFlags()
		{
			return ObjectFlags;
		}

		// OBJECTFLAG:PUBLIC
		/// <summary>
		/// Whether object is publically accessable.
		/// </summary>
		/// <returns>Whether it is publically accessable.</returns>
		public bool IsPublic()
		{
			return HasObjectFlag( Flags.ObjectFlagsLO.Public );
		}

		// The rules of protected and private changed since:
		private const uint AccessFlagChangeVersion = 180;

		// OBJECTFLAG:!PUBLIC
		public bool IsProtected()
		{
			//return !HasObjectFlag( Flags.ObjectFlagsLO.Public );
			return Package.Version < AccessFlagChangeVersion ? !IsPublic() && !IsPrivate() : HasObjectFlag( Flags.ObjectFlagsHO.PerObjectLocalized );
		}

		// OBJECTFLAG:!PUBLIC, FINAL
		public bool IsPrivate()
		{
			//return !HasObjectFlag( Flags.ObjectFlagsLO.Public ) && HasObjectFlag( Flags.ObjectFlagsLO.Private );
			return Package.Version < AccessFlagChangeVersion ? HasObjectFlag( Flags.ObjectFlagsLO.Private ) : !IsPublic();
		}

		/// <summary>
		/// Gets a human-readable name of this object instance.
		/// </summary>
		/// <returns>The human-readable name of this object instance.</returns>
		public virtual string GetFriendlyType()
		{
			return Name;
		}

		/// <summary>
		/// Gets the highest outer relative from the specified @offset.
		/// </summary>
		/// <param name="offset">Optional relative offset.</param>
		/// <returns>The highest outer.</returns>
		public UObject GetHighestOuter( byte offset = (byte)0 )
		{
			var parents = new List<UObject>();
			for( UObject outer = Outer; outer != null; outer = outer.Outer )
			{
				parents.Add( outer );
			}
			return parents.Count > 0 ? parents[parents.Count - offset] : Outer; 
		}

		/// <summary>
		/// Gets a full name of this object instance i.e. including outers.
		/// 
		/// e.g. var Core.Object.Vector Location;
		/// </summary>
		/// <returns>The full name.</returns>
		public string GetOuterGroup()
		{
			string group = String.Empty;
			// TODO:Should support importtable loop
			for( UObject outer = Outer; outer != null; outer = outer.Outer )
			{
				group = outer.Name + "." + group;
			}
			return group + Name; 
		}

		/// <summary>
		/// Gets the name of this object instance outer.
		/// </summary>
		/// <returns>The outer name of this object instance.</returns>
		public string GetOuterName()
		{
			return Table.OuterName;
		}

		/// <summary>
		/// Gets the name of this object instance class.
		/// </summary>
		/// <returns>The class name of this object instance.</returns>
		public string GetClassName()
		{
			return Table.ClassName;
		}

		/// <summary>
		/// Use this over 'typeof' to support UELib modifications such as replacing classes with the 'RegisterClass' function.
		/// </summary>
		/// <param name="className">The class name to compare to.</param>
		/// <returns>TRUE if this object instance class name is equal className, FALSE otherwise.</returns>
		public bool IsClassType( string className )
		{
			return String.Compare( GetClassName(), className, true ) == 0; 
		}

		/// <summary>
		/// Checks if this object's class equals @className, parents included.
		/// </summary>
		/// <param name="className">The name of the class to compare to.</param>
		/// <returns>Whether it extends class @className.</returns>
		public bool IsClass( string className )
		{
			for( var c = Table.ClassTable; c != null; c = c.ClassTable )
			{
				if( String.Compare( c.ObjectName, className, true ) == 0 )
					return true;
			} 
			return false;
		}

		/// <summary>
		/// Macro for getting a object instance by index.
		/// </summary>
		protected UObject GetIndexObject( int index )
		{
			return Package.GetIndexObject( index );
		}

		/// <summary>
		/// Try to get the object located @index.
		/// </summary>
		/// <param name="index">The object's index.</param>
		/// <returns>The reference of the specified object's index. NULL if none.</returns>
		protected UObject TryGetIndexObject( int index )
		{
			try
			{
				return GetIndexObject( index );
			}
			catch
			{
				return null;	
			}
		}

		/// <summary>
		/// Gets the specified object's name along with the instance number.
		/// </summary>
		/// <param name="index">The object's nameIndex.</param>
		/// <param name="number">The instance number.</param>
		/// <returns></returns>
		public string GetIndexName( int index, int number = -1 )
		{
			return number > 0 ? Package.GetIndexName( index ) + "_" + number : Package.GetIndexName( index ); 
		}

		/// <summary>
		/// Loads the package that this object instance resides in.
		/// 
		/// Note: The package closes when the Owner is done with importing objects data.
		/// </summary>
		public UnrealPackage LoadImportPackage()
		{
			UnrealPackage pkg = null;
			try
			{
				UObject outer = Outer;
				while( outer != null )
				{
					if( outer.Outer == null )
					{
						pkg = UnrealLoader.LoadCachedPackage( Path.GetDirectoryName( Package.FullPackageName ) + "\\" + outer.Name + ".u" );
						break;
					}
					outer = outer.Outer;
				}
			}
			catch( System.IO.IOException )
			{
				if( pkg != null )
				{
					pkg.Dispose();
				}
				return null;
			}
			return pkg;
		}

		/// <summary>
		/// Return a copy of this object's serialized bytes.
		/// </summary>
		/// <returns>This object's serialized bytes.</returns>
		public virtual byte[] GetBuffer()
		{
			var buff = new byte[ExportTable.SerialSize];
			Package.Stream.Seek( ExportTable.SerialOffset, System.IO.SeekOrigin.Begin );
			Package.Stream.Read( buff, 0, ExportTable.SerialSize );
			if( Package.Stream._BigEndianCode )
			{
				Array.Reverse( buff );
			}
			return buff;
		}

		public int CompareTo( object obj )
		{
			return NameIndex - ((UObject)obj).NameIndex;
		}
		#endregion

		/// <summary>
		/// Outputs the present position and the value of the parsed object.
		/// 
		/// Only called in the DEBUGBUILD!
		/// </summary>
		/// <param name="varName">The struct that was read from the previous buffer position.</param>
		/// <param name="varObject">The struct's value that was read.</param>
		[System.Diagnostics.DebuggerHidden()]
		[System.Diagnostics.Conditional( "DEBUG" )]
		internal void NoteRead( string varName, object varObject )
		{
			Console.WriteLine( _Buffer.Position 
				+ ":".PadLeft( 4, ' ' ) 
				+ varName.PadRight( 32, ' ' ) 
				+ " => " + (varObject != null ? varObject.ToString() : "null") );
		}

		[System.Diagnostics.Conditional( "DEBUG_TEST" )]
		internal void TestNoteRead( string varName, object varObject )
		{
			NoteRead( varName, varObject );
		}

		public void SwitchPackage( UnrealPackage newPackage )
		{
			Package = newPackage;
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool managed )
		{
			if( managed )
			{		
				if( _Buffer != null )
				{
					_Buffer.Close();
					_Buffer = null;
				}
			}
		}

		~UObject()
		{
		   Dispose( false );
		}
	}

	/// <summary>
	/// Unknown Object
	/// 
	/// Notes:
	/// 	Instances of this class are created because of a class type that was not found within the 'RegisteredClasses' list.
	/// 	Instances of this class will only be deserialized on demand.
	/// </summary>
	public sealed class UnknownObject : UObject
	{
		public bool bReleaseBuffer
		{
			get{ return _bReleaseBuffer; }
			set{ _bReleaseBuffer = value; }
		}

		/// <summary>
		///	Creates a new instance of the UELib.Core.UnknownObject class. 
		/// </summary>
		public UnknownObject()
		{
			bReleaseBuffer = false;
			_bDeserializeOnDemand = true;
		}
	}
}