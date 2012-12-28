using System;
using System.Collections.Generic;
using UELib.Flags;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal class. 
	/// </summary>
	[UnrealRegisterClass]
	public partial class UClass : UState
	{
		public struct Dependency : IUnrealDeserializableClass
		{
			public int Class{ get; private set; }

			public void Deserialize( IUnrealStream stream )
			{
				Class = stream.ReadObjectIndex();

				// Deep
				stream.ReadInt32();

				// ScriptTextCRC
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

		private int _DLLNameIndex;
		public string DLLName
		{
			get{ return Package.GetIndexName( _DLLNameIndex ); }
		}

		public string NativeClassName = String.Empty;

		public bool ForceScriptOrder;

		/// <summary>
		/// A list of class dependencies that this class depends on. Includes Imports and Exports.
		/// 
		/// Deprecated @ PackageVersion:186
		/// </summary>
		public UArray<Dependency> ClassDependencies;

		/// <summary>
		/// A list of objects imported from a package.
		/// </summary>
		public IList<int> PackageImports;

		/// <summary>
		/// Index of component names into the NameTableList.
		/// UE3
		/// </summary>
		public IList<int> Components = null;

		/// <summary>
		/// Index of unsorted categories names into the NameTableList.
		/// UE3
		/// </summary>
		public IList<int> DontSortCategories;

		/// <summary>
		/// Index of hidden categories names into the NameTableList.
		/// </summary>
		public IList<int> HideCategories;

		/// <summary>
		/// Index of auto expanded categories names into the NameTableList.
		/// UE3
		/// </summary>
		public IList<int> AutoExpandCategories;

		/// <summary>
		/// A list of class group.
		/// </summary>
		public IList<int> ClassGroups;

		/// <summary>
		/// Index of auto collapsed categories names into the NameTableList.
		/// UE3
		/// </summary>
		public IList<int> AutoCollapseCategories;

		/// <summary>
		/// Index of (Object/Name?)
		/// UE3
		/// </summary>
		public IList<int> ImplementedInterfaces;
		#endregion

		#region Script Members
		public IList<UState> States{ get; protected set; }
		#endregion

		#region Constructors
		protected override void Deserialize()
		{
			base.Deserialize();

			if( Package.Version <= 61 )
			{
				var oldClassRecordSize = _Buffer.ReadIndex();
				NoteRead( "oldClassRecordSize", oldClassRecordSize );
			}

#if BIOSHOCK
			if( Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock )
			{
				var unknown = _Buffer.ReadInt32();
				NoteRead( "???", unknown );
			}
#endif
	
			ClassFlags = _Buffer.ReadUInt32();
			NoteRead( "ClassFlags", (ClassFlags)ClassFlags );

			// Both were deprecated since then
			if( Package.Version < 140 )
			{
				ClassGuid = _Buffer.ReadGuid();
				NoteRead( "ClassGuid", ClassGuid );

				int depSize = _Buffer.ReadIndex();
				NoteRead( "DepSize", depSize );
				if( depSize > 0 )
				{
					ClassDependencies = new UArray<Dependency>( _Buffer, depSize );
					NoteRead( "ClassDependenciesList", ClassDependencies );
				}

				PackageImports = DeserializeGroup( "PackageImportsList" );
				NoteRead( "PackageImportsList", PackageImports );
			}

			if( Package.Version >= 62 )
			{
				// TODO: Corrigate Version
				// At least since RoboBlitz(369) - 547(APB)
				if( Package.Version >= 140 && Package.Version < 547  )
				{
					var unknown = _Buffer.ReadByte();	
					NoteRead( "???", unknown );
				}

				// Class Name Extends Super.Name Within _WithinIndex
				//		Config(_ConfigIndex);
				_WithinIndex = _Buffer.ReadObjectIndex();
				NoteRead( "_WithinIndex", GetIndexObject( _WithinIndex ) );
				_ConfigIndex = _Buffer.ReadNameIndex();
				NoteRead( "_ConfigIndex", Package.Names[_ConfigIndex] );

				if( Package.Version >= 100 )
				{
					if( Package.Version > 300 )
					{
						int componentsCount = _Buffer.ReadInt32();
						NoteRead( "componentsCount", componentsCount );
						if( componentsCount > 0 )
						{
							// NameIndex/ObjectIndex
							int bytes = componentsCount * (Package.Version > 490 ? 12 : 8);
							TestEndOfStream( bytes, "Components" );
							_Buffer.Skip( bytes );
						}

						// RoboBlitz(369)
						if( Package.Version >= 369 )
						{
							// See http://udn.epicgames.com/Three/UnrealScriptInterfaces.html
							int interfacesCount = _Buffer.ReadInt32();
							NoteRead( "InterfacesCount", interfacesCount );
							if( interfacesCount > 0 )
							{
								TestEndOfStream( interfacesCount * 8, "Interfaces" );
								ImplementedInterfaces = new List<int>( interfacesCount );
								for( int i = 0; i < interfacesCount; ++ i )
								{
									int interfaceIndex = _Buffer.ReadInt32();
									int typeIndex = _Buffer.ReadInt32();
									ImplementedInterfaces.Add( interfaceIndex ); 
								}
								NoteRead( "ImplementedInterfacesList", ImplementedInterfaces );
							}
						}
					}

					if( !Package.IsConsoleCooked() && !Package.Build.IsXenonCompressed )
					{
						if( Package.Version >= 603 )
						{
							DontSortCategories = DeserializeGroup( "DontSortCategoriesList" );
							NoteRead( "DontSortCategoriesList", DontSortCategories );
						}

						HideCategories = DeserializeGroup( "HideCategoriesList" );
						NoteRead( "HideCategoriesList", HideCategories );

						if( Package.Version >= 185 )
						{
							// 490:GoW1, 576:CrimeCraft
							if( (!HasClassFlag( Flags.ClassFlags.CollapseCategories ) 
								|| Package.Version <= 490 || Package.Version >= 576) 
							)
							{ 
								AutoExpandCategories = DeserializeGroup( "AutoExpandCategoriesList" );
								NoteRead( "AutoExpandCategoriesList", AutoExpandCategories );
							}

							if( Package.Version > 670 )
							{
								AutoCollapseCategories = DeserializeGroup( "AutoCollapseCategoriesList" );
								NoteRead( "AutoCollapseCategoriesList", AutoCollapseCategories );

								if( Package.Version >= 749 
									#if SPECIALFORCE2
										&& Package.Build != UnrealPackage.GameBuild.BuildName.SpecialForce2  
									#endif
									)
								{
									// bForceScriptOrder
									ForceScriptOrder = _Buffer.ReadInt32() > 0;
									NoteRead( "bForceScriptOrder", ForceScriptOrder );

									if( Package.Version >= UnrealPackage.VCLASSGROUP )
									{
										ClassGroups = DeserializeGroup( "ClassGroupsList" );
										NoteRead( "ClassGroupsList", ClassGroups );

										if( Package.Version >= 813 )
										{
											NativeClassName = _Buffer.ReadString();
											NoteRead( "NativeClassName", NativeClassName );
										}
									}
								}
							}

							// FIXME: Found first in(V:655), Definitely not in APB and GoW 2
							if( Package.Version > 575 && Package.Version < 678 )
							{
								int unk2 = _Buffer.ReadInt32();
								NoteRead( "??Int32", unk2 );

								#if SINGULARITY
								if( Package.Build == UnrealPackage.GameBuild.BuildName.Singularity )
								{ 
									_Buffer.Skip( 8 );
								}
								#endif
							}	
						}					
					}

					if( Package.Version >= UnrealPackage.VDLLBIND )
					{
						_DLLNameIndex = _Buffer.ReadNameIndex();
						NoteRead( "_DLLNameIndex", Package.Names[_DLLNameIndex] );
#if BORDERLANDS2
						if( Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 )
						{ 
							var unkval = _Buffer.ReadByte();
							NoteRead( "??BL2_Byte", unkval );
						}
#endif
					}
				}
			}	
	
			// In later UE3 builds, defaultproperties are stored in separated objects named DEFAULT_namehere, 
			if( Package.Version >= 322 )
			{ 
				_Default = GetIndexObject( _Buffer.ReadObjectIndex() );
				NoteRead( "defaultObjectIndex", _Default );
				if( _Default != null )
				{
					_Default.BeginDeserializing();
				}
			}
			else
			{		
#if SWAT4
				if( Package.Build == UnrealPackage.GameBuild.BuildName.Swat4 )
				{
					// We are done here!
					return;
				}
#endif 
				DeserializeProperties();
			}
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
			States = new List<UState>();
			for( var child = (UField)GetIndexObject( Children ); child != null; child = child.NextField )
			{
				if( child.IsClassType( "State" ) )
				{
					States.Insert( 0, (UState)child );
				}			
			}
		}
		#endregion

		#region Methods
		private IList<int> DeserializeGroup( string groupName = "List" )
		{
			int count = _Buffer.ReadIndex();
			NoteRead( groupName + "Count", count );
			if( count > 0 )
			{
				var groupList = new List<int>( count );
				for( int i = 0; i < count; ++ i )
				{
					int index = _Buffer.ReadNameIndex();
					groupList.Add( index );
				}
				return groupList;
			}
			return null;
		}

		public bool HasClassFlag( ClassFlags flag )
		{
			return (ClassFlags & (uint)flag) != 0;
		}

		public bool HasClassFlag( uint flag )
		{
			return (ClassFlags & flag) != 0;
		}
		#endregion
	}
}