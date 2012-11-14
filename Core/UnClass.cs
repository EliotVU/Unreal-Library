#define TDS

using System;
using System.Collections.Generic;

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
		private ulong ClassFlags
		{
			get; set;
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
		public UClass Within
		{
			get; 
			protected set;
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

		public bool ForceScriptOrder;

		/// <summary>
		/// A list of class dependencies that this class depends on. Includes Imports and Exports.
		/// 
		/// Deprecated @ PackageVersion:186
		/// </summary>
		public UArray<Dependency> ClassDependenciesList;

		/// <summary>
		/// A list of objects imported from a package.
		/// </summary>
		public List<int> PackageImportsList;

		/// <summary>
		/// Index of hidden categories names into the NameTableList.
		/// UE3
		/// </summary>
		public List<int> ComponentsList 				= null;

				/// <summary>
		/// Index of hidden categories names into the NameTableList.
		/// UE3
		/// </summary>
		public List<int> DontSortCategoriesList;

		/// <summary>
		/// Index of hidden categories names into the NameTableList.
		/// </summary>
		public List<int> HideCategoriesList;

		/// <summary>
		/// Index of auto expanded categories names into the NameTableList.
		/// UE3
		/// </summary>
		public List<int> AutoExpandCategoriesList;

		/// <summary>
		/// A list of class group.
		/// </summary>
		public List<int> ClassGroupsList;

		/// <summary>
		/// Index of auto collapsed categories names into the NameTableList.
		/// UE3
		/// </summary>
		public List<int> AutoCollapseCategoriesList;

		/// <summary>
		/// Index of (Object/Name?)
		/// UE3
		/// </summary>
		public List<int> ImplementedInterfacesList;
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
			_ShouldReleaseBuffer = false;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			if( Package.Version <= 61 )
			{
				_Buffer.ReadIndex();
			}
	
			ClassFlags = _Buffer.ReadUInt32();
			NoteRead( "ClassFlags", ClassFlags );

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
				// TODO: Corrigate Version
				// At least since RoboBlitz(369) - 547(APB)
				if( Package.Version >= 369 && Package.Version < 547  )
				{
					_UNKNOWNBYTE = _Buffer.ReadByte();	
					NoteRead( "_UNKNOWNBYTE", _UNKNOWNBYTE );
				}

				// Class Name Extends Super.Name Within _WithinIndex
				//		Config(_ConfigIndex);
				_WithinIndex = _Buffer.ReadObjectIndex();
				NoteRead( "_WithinIndex", _WithinIndex );
				_ConfigIndex = _Buffer.ReadNameIndex();
				NoteRead( "_ConfigIndex", _ConfigIndex );

				if( Package.Version >= 100 )
				{
					if( Package.Version > 300 )
					{
						int componentsCount = _Buffer.ReadInt32();
						NoteRead( "componentsCount", componentsCount );

						if( componentsCount > 0 )
						{
							int bytes = componentsCount * (Package.Version > 490 ? 12 : 8);
							TestEndOfStream( bytes, "Components" );
							_Buffer.Skip( bytes );

							//ComponentsList = new List<int>( componentsCount );
							//for( int i = 0; i < componentsCount; ++ i )
							//{
							//    _Buffer.ReadNameIndex();
							//    // TODO: Corrigate version. Definitely not in GoW 1(490)
							//    if( Package.Version > 490 )	// GOW
							//    {
							//        ComponentsList.Add( _Buffer.ReadObjectIndex() );	
							//    }
							//}
							//NoteRead( "ComponentsList", ComponentsList );
						}

						// RoboBlitz(369)
						if( Package.Version >= 369 )
						{
							// FIXME: Invalid in UT3? Swapped with HideCategories?
							int interfacesCount = _Buffer.ReadInt32();
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
							}
						}
					}

					if( !Package.IsConsoleCooked() && !Package.Build.IsXenonCompressed )
					{
						if( Package.Version >= 603 )
						{
							DontSortCategoriesList = DeserializeGroup();
							NoteRead( "DontSortCategoriesList", DontSortCategoriesList );
						}

						HideCategoriesList = DeserializeGroup();
						NoteRead( "HideCategoriesList", HideCategoriesList );

						if( Package.Version >= 185 )
						{
							AutoExpandCategoriesList = DeserializeGroup();
							NoteRead( "AutoExpandCategoriesList", AutoExpandCategoriesList );

							if( Package.Version > 670 )
							{
								AutoCollapseCategoriesList = DeserializeGroup();
								NoteRead( "AutoCollapseCategoriesList", AutoCollapseCategoriesList );

								if( Package.Version >= 749 
									#if SPECIALFORCE2
											&& Package.Build != UnrealPackage.GameBuild.ID.SpecialForce2  
									#endif
									)
								{
									// bForceScriptOrder
									ForceScriptOrder = _Buffer.ReadInt32() > 0;
									NoteRead( "bForceScriptOrder", ForceScriptOrder );

									if( Package.Version >= UnrealPackage.VCLASSGROUP )
									{
										ClassGroupsList = DeserializeGroup();
										NoteRead( "ClassGroupsList", ClassGroupsList );

										if( Package.Version >= 813 )
										{
											NativeClassName = _Buffer.ReadName();
											NoteRead( "NativeClassName", NativeClassName );
										}
									}
								}
							}

							// FIXME: Found first in(V:655), Definitely not in APB and GoW 2
							if( Package.Version > 575 && Package.Version <= 678 )
							{
								// TODO: Unknown
								int unk2 = _Buffer.ReadInt32();
								NoteRead( "unk2", unk2 );
							}	
						}					
					}

					if( Package.Version >= UnrealPackage.VDLLBIND )
					{
						_DLLNameIndex = _Buffer.ReadNameIndex();
						NoteRead( "_DLLNameIndex", _DLLNameIndex );

#if BORDERLANDS2
						if( Package.Build == UnrealPackage.GameBuild.ID.Borderlands2 )
						{ 
							var unkval = _Buffer.ReadByte();
							NoteRead( "unkval", unkval );
						}
#endif
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
				Within = (UClass)Package.GetIndexObject( _WithinIndex );
			}
		}

		protected override void FindChildren()
		{
			base.FindChildren();
			for( var child = (UField)GetIndexObject( Children ); child != null; child = child.NextField )
			{
				if( child.IsClassType( "State" ) )
				{
					_ChildStates.Insert( 0, (UState)child );
				}			
			}
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
		public string ScriptText = String.Empty;
		#endregion

		private long _ScriptOffset;

		public UTextBuffer()
		{
			ShouldDeserializeOnDemand = true;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

	  		_Top = _Buffer.ReadUInt32();
			_Pos = _Buffer.ReadUInt32();

#if TDS
			// FIXME: Use build detection 
			if( Package.LicenseeVersion == (ushort)UnrealPackage.LicenseeVersions.ThiefDeadlyShadows )
			{
				// TODO: Unknown
				_Buffer.Skip( 8 );
			}
#endif

			_ScriptOffset = _Buffer.Position;
			ScriptText = _Buffer.ReadName();
		}
	}
}