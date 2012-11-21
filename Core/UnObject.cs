using System;
using System.Collections.Generic;
using System.IO;

namespace UELib.Core
{
	public class ObjectEventArgs : EventArgs, IRefUObject
	{
		public UObject ObjectRef{ get; protected set; }

		public ObjectEventArgs( UObject objectRef )
		{
			ObjectRef = objectRef;
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
		public UObjectTableItem Table
		{
			get;
			internal set;
		}

		#region ExportTable
		public UExportTableItem ExportTable
		{
			get{ return Table as UExportTableItem; }
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
		public UImportTableItem ImportTable
		{
			get{ return Table as UImportTableItem; }
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
		public UNameTableItem NameTable
		{
			get;
			internal set;
		}

		private string _CustomName;
		public string Name
		{
			get{ return _CustomName ?? Table.ObjectName; }
			set{ _CustomName = value; }
		}

		public int NameIndex
		{
			get{ return NameTable.Index; }
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
		protected DefaultPropertiesCollection _Properties;

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
		protected bool _ShouldReleaseBuffer = true;

		/// <summary>
		/// Object will not be deserialized by UnrealPackage, Can only be deserialized by calling the methods yourself.
		/// </summary>
		public bool ShouldDeserializeOnDemand
		{
			get;
			set;
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
				ReadBuffer();
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
				if( _ShouldReleaseBuffer )
				{
					ReleaseBuffer();	
				}
			}
		}

		public void ReadBuffer()
		{
			if( _Buffer != null )
			{
				ReleaseBuffer();
			}

			var buff = new byte[ExportTable.SerialSize];
			Package.Stream.Seek( ExportTable.SerialOffset, SeekOrigin.Begin ); 
			Package.Stream.Read( buff, 0, ExportTable.SerialSize ); 
			if( Package.Stream.BigEndianCode )
			{
				Array.Reverse( buff );
			}
			_Buffer = new UObjectStream( Package.Stream, ref buff );	
		}

		protected void ReleaseBuffer()
		{
			if( _Buffer == null )
				return;

			_Buffer.Dispose();
			_Buffer.Close();
			_Buffer = null;
		}

		/// <summary>
		/// Deserialize this object's structure from the _Buffer stream.
		/// </summary>
		protected virtual void Deserialize()
		{
#if DEBUG
			Console.WriteLine( "" );
			NoteRead( Name, this );
			NoteRead( "ExportSize", ExportTable.SerialSize );
#endif

			// TODO: Corrigate version
			if( _Buffer.Version >= 322 )
			{
				// TODO: Corrigate version. Fix component detection!
				//if( _Buffer.Version > 400
				//    && HasObjectFlag( Flags.ObjectFlagsHO.PropertiesObject )
				//    && HasObjectFlag( Flags.ObjectFlagsHO.ArchetypeObject ) )
				//{
				//    var componentClass = _Buffer.ReadObjectIndex();
				//    var componentName = _Buffer.ReadNameIndex();
				//}

				NetIndex = _Buffer.ReadObjectIndex();
				NoteRead( "NetIndex", NetIndex );
			}
			else
			{
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
			}

			if( !IsClassType( "Class" ) )
			{	
#if SWAT4
				if( Package.Build != UnrealPackage.GameBuild.ID.Swat4 )
				{
#endif
					// REMINDER:Ends with a NameIndex referencing to "None"; 1/4/8 bytes
					DeserializeProperties();
#if SWAT4
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
				_ShouldReleaseBuffer = false;
			}
			else
			{
				_Properties = null;
			}
		}

		/// <summary>
		///	Write this instance to the Owner.Stream at the present position. 
		/// NOTE: The package's stream must have Write access!
		/// </summary>
		public virtual void Serialize()
		{
			CopyToPackageStream();
		}

		/// <summary>
		/// Writes the present Buffer to the package's stream. Saving is not implied.
		/// NOTE: The package's stream must have Write access!
		/// </summary>
		private void CopyToPackageStream()
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

		public bool ResistsInGroup()
		{
			return Outer != null && Outer.GetClassName() == "Package";
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
			return String.Compare( GetClassName(), className, StringComparison.OrdinalIgnoreCase ) == 0; 
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
				if( String.Compare( c.ObjectName, className, StringComparison.OrdinalIgnoreCase ) == 0 )
					return true;
			} 
			return false;
		}

		/// <summary>
		/// Tests whether this Object(such as a property) is a member of a specific object, or that of its parent.
		/// </summary>
		/// <param name="membersClass">Field to test against.</param>
		/// <returns>Whether it is a member or not.</returns>
		public bool IsMember( UField membersClass )
		{
			for( var p = membersClass; p != null; p = p.Super )
			{
				if( Outer == p )
				{
					return true;
				}
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
			catch( IOException )
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
			Package.Stream.Seek( ExportTable.SerialOffset, SeekOrigin.Begin );
			Package.Stream.Read( buff, 0, ExportTable.SerialSize );
			if( Package.Stream.BigEndianCode )
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
		[System.Diagnostics.DebuggerHidden]
		[System.Diagnostics.Conditional( "DEBUG" )]
		internal void NoteRead( string varName, object varObject = null )
		{
			if( varObject == null )
			{
				Console.WriteLine( varName );
				return;
			}

			var propertyType = varObject.GetType();
			Console.WriteLine(
				"0x" + _Buffer.Position.ToString("x8").ToUpper() 
				+ " : ".PadLeft( 2, ' ' ) 
				+ varName.PadRight( 32, ' ' ) + ":" + propertyType.Name.PadRight( 32, ' ' ) 
				+ " => " + varObject 
			);
			//NoteRead( varObject );
		}

		//internal void NoteRead( object property )
		//{
		//    var propertyType = property.GetType();
		//    Console.WriteLine( 
		//        _Buffer.Position.ToString("0xx8").ToUpper() 
		//        + " : ".PadLeft( 2, ' ' ) 
		//        + propertyType + "::" + propertyType.Name.PadRight( 36, ' ' ) 
		//        + " => " + property 
		//    );
		//}

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
		/// <summary>
		///	Creates a new instance of the UELib.Core.UnknownObject class. 
		/// </summary>
		public UnknownObject()
		{
			_ShouldReleaseBuffer = false;
			ShouldDeserializeOnDemand = true;
		}

		protected override void Deserialize()
		{
			if( Package.Version > 400 && _Buffer.Length >= 12 )
			{			 
				// componentClassIndex
				_Buffer.Position += sizeof(int);
				var componentNameIndex = _Buffer.ReadNameIndex();
				if( componentNameIndex == NameIndex )
				{
					base.Deserialize();
					return;
				}
				_Buffer.Position -= 12;
			}
			base.Deserialize();
		}
	}
}