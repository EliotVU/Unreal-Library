using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal class. 
	/// </summary>
	public partial class UClass : UState
	{
		public class Dependency : IUnrealDeserializableClass
		{
			public Dependency()
			{
				Class = 0;
			}

			public int Class{ get; private set; }

			public void Deserialize( IUnrealStream stream )
			{
				Class = stream.ReadObjectIndex();
				stream.ReadInt32();
				stream.ReadUInt32();
			}
		}

		#region Serialized Members
		/// <summary>
		/// Flags of this class
		/// e.g. Placeable, HideDropDown, Transient and so on	
		/// </summary>
		/// <value>
		/// 32bit in UE2
		/// 64bit in UE3
		/// </value>
		internal ulong ClassFlags
		{
			get;
			private set;
		}

		/// <summary>
		/// guid(xx....) flag (UE2)
		/// Deprecated @ UE3
		/// </summary>
		public string ClassGuid
		{
			get;
			private set;
		}

		private byte _UNKNOWNBYTE;

		private int _WithinIndex;
		protected UClass _Within;
		public UClass Within
		{
			get{ return _Within; }
		}

		private int _ConfigIndex;
		/// <summary>
		/// config(UClass::Config) flag
		/// </summary>
		public string ConfigName
		{
			get{ return Package.GetIndexName( _ConfigIndex ); }
		}

		private int? _DLLNameIndex;
		public string DLLName
		{
			get{ return Package.GetIndexName( (int)_DLLNameIndex ); }
		}

		public string NativeClassName = String.Empty;

		/// <summary>
		/// A list of class dependencies that this class depends on. Includes Imports and Exports.
		/// 
		/// Deprecated @ PackageVersion:186
		/// </summary>
		public UArray<Dependency> ClassDependenciesList = null;

		/// <summary>
		/// A list of objects imported from a package.
		/// </summary>
		public List<int> PackageImportsList 			= null;

		/// <summary>
		/// Index of hidden categories names into the NameTableList.
		/// UE3
		/// </summary>
		public List<int> ComponentsList 				= null;

				/// <summary>
		/// Index of hidden categories names into the NameTableList.
		/// UE3
		/// </summary>
		public List<int> DontSortCategoriesList			= null;

		/// <summary>
		/// Index of hidden categories names into the NameTableList.
		/// </summary>
		public List<int> HideCategoriesList 			= null;

		/// <summary>
		/// Index of auto expanded categories names into the NameTableList.
		/// UE3
		/// </summary>
		public List<int> AutoExpandCategoriesList 		= null;

		/// <summary>
		/// A list of class group.
		/// </summary>
		public List<int> ClassGroupsList	 			= null;

		/// <summary>
		/// Index of auto collapsed categories names into the NameTableList.
		/// UE3
		/// </summary>
		public List<int> AutoCollapseCategoriesList 	= null;

		/// <summary>
		/// Index of (Object/Name?)
		/// UE3
		/// </summary>
		public List<int> ImplementedInterfacesList		= null;
		#endregion

		#region PostInitialized Members
		// Children
		protected List<UState> 	_ChildStates 		= new List<UState>();
		public List<UState> 	ChildStates
		{
			get{ return _ChildStates; }
		}
		#endregion

		/// <summary>
		///	Creates a new instance of the UELib.Core.UClass class. 
		/// </summary>
		public UClass()
		{
			_bReleaseBuffer = false;
		}

		// 584?
		public const uint UNKByteVersion = 547;	// Definitely not in 547(APB)

		protected override void Deserialize()
		{
			base.Deserialize();

			if( Package.Version <= 61 )
			{
				_Buffer.ReadIndex();
			}
	
			ClassFlags = _Buffer.ReadUInt32();
			NoteRead( "ClassFlags", ClassFlags );
			// TODO: Corrigate Version
			if( (Package.Version > 480 && Package.Version < UNKByteVersion)  )
			{
				_UNKNOWNBYTE = _Buffer.ReadByte();	
				NoteRead( "_UNKNOWNBYTE", _UNKNOWNBYTE );
			}

			// Both were deprecated since then
			if( Package.Version < 186 )
			{
				ClassGuid = _Buffer.ReadGuid();
				NoteRead( "ClassGuid", ClassGuid );

				{int depSize = _Buffer.ReadIndex();
				NoteRead( "DepSize", depSize );
				if( depSize > 0 )
				{
					ClassDependenciesList = new UArray<Dependency>( _Buffer, depSize );
					NoteRead( "ClassDependenciesList", ClassDependenciesList );
				}}

				PackageImportsList = DeserializeGroup();
				NoteRead( "PackageImportsList", PackageImportsList );
			}

			if( Package.Version >= 62 )
			{
				// Class Name Extends Super.Name Within _WithinIndex
				//		Config(_ConfigIndex);
				_WithinIndex = _Buffer.ReadObjectIndex();
				NoteRead( "_WithinIndex", _WithinIndex );
				_ConfigIndex = _Buffer.ReadNameIndex();
				NoteRead( "_ConfigIndex", _ConfigIndex );

				if( Package.Version >= 100 )
				{
					// FIXME: CacheExempt == HasComponents?

					//&& HasClassFlag( Flags.ClassFlags.CacheExempt )
					if( Package.Version > 300 )
					{
						{int componentsCount = _Buffer.ReadInt32();
							NoteRead( "componentsCount", componentsCount );
						if( componentsCount > 0 )
						{
							TestEndOfStream( componentsCount * 12, "Components" );
							ComponentsList = new List<int>( componentsCount );
							for( int i = 0; i < componentsCount; ++ i )
							{
								_Buffer.ReadNameIndex();
								// TODO: Corrigate Version
								if( Package.Version > 490 )	// GOW
								{
									ComponentsList.Add( _Buffer.ReadObjectIndex() );	
								}
							}
							NoteRead( "ComponentsList", ComponentsList );
						}}

						// FIXME: When was this removed. This exists for sure around 180+
						//ComponentClassToNameMap = _Buffer.ReadObjectIndex();
						//ComponentNameToDefaultObjectMap = _Buffer.ReadObjectIndex();
					}

					if( Package.Version > 400 )
					{
						// FIXME: Invalid in UT3? Swapped with HideCategories?
						{int interfacesCount = _Buffer.ReadInt32();
							NoteRead( "InterfacesCount", interfacesCount );
						if( interfacesCount > 0 )
						{
							TestEndOfStream( interfacesCount * 8, "Interfaces" );
							ImplementedInterfacesList = new List<int>( interfacesCount );
							for( int i = 0; i < interfacesCount; ++ i )
							{
								// Taken from UDN @ http://udn.epicgames.com/Three/UnrealScriptInterfaces.html
								// In C++, a native interface variable is represented as a TScriptInterface, 
								// declared in UnTemplate.h. 
								// This struct stores two pointers to the same object - a UObject pointer and a pointer of the interface type. 
								int interfaceIndex = _Buffer.ReadInt32();
								int typeIndex = _Buffer.ReadInt32();
								ImplementedInterfacesList.Add( interfaceIndex ); 
							}
							NoteRead( "ImplementedInterfacesList", ImplementedInterfacesList );
						}}
					}

					if( !Package.IsConsoleCooked() )
					{
						if( Package.Version >= 603 )
						{
							DontSortCategoriesList = DeserializeGroup();
							NoteRead( "DontSortCategoriesList", DontSortCategoriesList );
						}

						if( !HasClassFlag( Flags.ClassFlags.CollapseCategories ) || Package.Version < 200 )
						{
							HideCategoriesList = DeserializeGroup();
							NoteRead( "HideCategoriesList", HideCategoriesList );
						}

						if( Package.Version >= 185 )
						{
							AutoExpandCategoriesList = DeserializeGroup();
							NoteRead( "AutoExpandCategoriesList", AutoExpandCategoriesList );

							if( Package.Version >= 655 )
							{
								if( Package.Version > 670 )
								{
									AutoCollapseCategoriesList = DeserializeGroup();
									NoteRead( "AutoCollapseCategoriesList", AutoCollapseCategoriesList );

									if( Package.Version >= 749 )
									{
										// bForceScriptOrder
										int unk1 = _Buffer.ReadInt32();
										NoteRead( "bForceScriptOrder", unk1 );

										// TODO: Figure out what determines if DLLBind and/or ClassGroup deserializiation.
										if( Package.Version >= UnrealPackage.VClassGroup ) // V:789 HasClassFlag( Flags.ClassFlags.CacheExempt )
										{
											ClassGroupsList = DeserializeGroup();
											NoteRead( "ClassGroupsList", ClassGroupsList );

											if( Package.Version >= 813 )
											{
												if( HasObjectFlag( Flags.ObjectFlagsLO.Native ) )
												{
													NativeClassName = _Buffer.ReadName();
													NoteRead( "NativeClassName", NativeClassName );
												}
											}
										}
									}
								}
							}

							// FIXME: UNKNOWN CONDITION(invalid in V:805, V:678(DD)) Found first in(V:655)
							if( Package.Version <= 678 
								#if APB
									&& Package.Build != UnrealPackage.GameBuild.ID.APB
								#endif
								)
							{
								// TODO: Unknown
								int unk2 = _Buffer.ReadInt32();
							}	
						}					
					}

					if( Package.Version >= UnrealPackage.VDLLBind )	// V:664
					{
						_DLLNameIndex = _Buffer.ReadNameIndex();
						NoteRead( "_DLLNameIndex", _DLLNameIndex );
					}
				}
			}	
	
			// In later UE3 builds, defaultproperties are stored in separated objects named DEFAULT_namehere, 
			if( Package.Version >= 322 )
			{ 
				int defaultObjectIndex = _Buffer.ReadObjectIndex();
				NoteRead( "defaultObjectIndex", defaultObjectIndex );
				if( defaultObjectIndex > 0 )
				{
					var obj = Package.GetIndexObject( defaultObjectIndex );
					if( obj != null )
					{
						obj.BeginDeserializing();
						Properties = obj.Properties;
					}
				}
			}
			else
			{		
#if SWAT4
				if( Package.Build == UnrealPackage.GameBuild.ID.Swat4 )
				{
					// We are done here!
					return;
				}
#endif 
				NoteRead( "DefaultProperties:", null );
				DeserializeProperties();
			}
		}

		private List<int> DeserializeGroup()
		{
			List<int> groupList = null;

			int count = _Buffer.ReadIndex();
			NoteRead( "Count", count );
			if( count > 0 )
			{
				groupList = new List<int>( count );
				for( int i = 0; i < count; ++ i )
				{
					int index = _Buffer.ReadNameIndex();
					groupList.Add( index );
				}
			}
			return groupList;
		}

		public override void PostInitialize()
		{
			base.PostInitialize();
			if( _WithinIndex != 0 )
			{
				_Within = (UClass)Package.GetIndexObject( _WithinIndex );
			}
		}

		protected override void FindChildren()
		{
			base.FindChildren();
			for( var child = (UField)GetIndexObject( Children ); child != null; child = child.NextField )
			{
				if( child.IsClassType( "State" ) )
				{
					_ChildStates.Add( (UState)child );
				}			
			}
			_ChildStates.Reverse();
		}

		#region Methods
		public bool HasClassFlag( Flags.ClassFlags flag )
		{
			return (ClassFlags & (uint)flag) != 0;
		}
		#endregion
	}

	public partial class UTextBuffer : UObject
	{
		#region Serialized Members
		protected uint _Top;
		protected uint _Pos;
		#endregion

		public string ScriptText = String.Empty;

		private long _ScriptOffset;

		public UTextBuffer()
		{
			_bDeserializeOnDemand = true;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

	  		_Top = _Buffer.ReadUInt32();
			_Pos = _Buffer.ReadUInt32();

			if( Package.LicenseeVersion == (ushort)UnrealPackage.LicenseeVersions.ThiefDeadlyShadows )
			{
				// TODO: Unknown
				_Buffer.Skip( 8 );
			}

			_ScriptOffset = _Buffer.Position;
			ScriptText = _Buffer.ReadName();
		}
	}
}