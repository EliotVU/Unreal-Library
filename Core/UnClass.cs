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
        private ulong   ClassFlags{ get; set; }

        public string   ClassGuid{ get; private set; }
        public UClass   Within{ get; private set; }
        public UName    ConfigName{ get; private set; }
        public UName    DLLBindName{ get; private set; }
        public string   NativeClassName = String.Empty;
        public bool     ForceScriptOrder;

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
                Record( "oldClassRecordSize", oldClassRecordSize );
            }

#if BIOSHOCK
            if( Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock )
            {
                var unknown = _Buffer.ReadInt32();
                Record( "???Bioshock_Int32", unknown );
            }
#endif
    
            ClassFlags = _Buffer.ReadUInt32();
            Record( "ClassFlags", (ClassFlags)ClassFlags );

            // Both were deprecated since then
            // TODO: Corrigate Version
            if( Package.Version < 140 )
            {
                ClassGuid = _Buffer.ReadGuid();
                Record( "ClassGuid", ClassGuid );

                int depSize = _Buffer.ReadIndex();
                Record( "DepSize", depSize );
                if( depSize > 0 )
                {
                    ClassDependencies = new UArray<Dependency>( _Buffer, depSize );
                    Record( "ClassDependencies", ClassDependencies );
                }

                PackageImports = DeserializeGroup( "PackageImports" );
                Record( "PackageImports", PackageImports );
            }

            if( Package.Version >= 62 )
            {
                // TODO: Corrigate Version
                // At least since Bioshock(140) - 547(APB)
                if( Package.Version >= 140 && Package.Version < 547  )
                {
                    var unknown = _Buffer.ReadByte();	
                    Record( "???", unknown );
                }

                // Class Name Extends Super.Name Within _WithinIndex
                //		Config(_ConfigIndex);
                Within = _Buffer.ReadObject() as UClass;
                Record( "Within", Within );
                ConfigName = _Buffer.ReadNameReference();
                Record( "ConfigName", ConfigName );

                // TODO: Corrigate Version
                if( Package.Version >= 100 )
                {
                    // TODO: Corrigate Version
                    if( Package.Version >= 220 )
                    {
                        // TODO: Corrigate Version
                        if( Package.Version <= 512 )
                        {
                            HideCategories = DeserializeGroup( "HideCategories" );
                            Record( "HideCategories", HideCategories );
                        }

                        int componentsCount = _Buffer.ReadInt32();
                        Record( "componentsCount", componentsCount );
                        if( componentsCount > 0 )
                        {
                            // NameIndex/ObjectIndex
                            int bytes = componentsCount * 12;
                            AssertEOS( bytes, "Components" );
                            _Buffer.Skip( bytes );
                        }

                        // RoboBlitz(369)
                        // TODO: Corrigate Version
                        if( Package.Version >= 369 )
                        {
                            // See http://udn.epicgames.com/Three/UnrealScriptInterfaces.html
                            int interfacesCount = _Buffer.ReadInt32();
                            Record( "InterfacesCount", interfacesCount );
                            if( interfacesCount > 0 )
                            {
                                AssertEOS( interfacesCount * 8, "ImplementedInterfaces" );
                                ImplementedInterfaces = new List<int>( interfacesCount );
                                for( int i = 0; i < interfacesCount; ++ i )
                                {
                                    int interfaceIndex = _Buffer.ReadInt32();
                                    int typeIndex = _Buffer.ReadInt32();
                                    ImplementedInterfaces.Add( interfaceIndex ); 
                                }
                                Record( "ImplementedInterfaces", ImplementedInterfaces );
                            }
                        }
                    }

                    if( !Package.IsConsoleCooked() && !Package.Build.IsXenonCompressed )
                    {
                        if( Package.Version >= 603 )
                        {
                            DontSortCategories = DeserializeGroup( "DontSortCategories" );
                            Record( "DontSortCategories", DontSortCategories );
                        }

                        // TODO: Corrigate Version
                        if( Package.Version < 220 || Package.Version > 512 )
                        {
                            HideCategories = DeserializeGroup( "HideCategories" );
                            Record( "HideCategories", HideCategories );
                        }

                        // TODO: Corrigate Version
                        if( Package.Version >= 185 )
                        {
                            // 490:GoW1, 576:CrimeCraft
                            if( (!HasClassFlag( Flags.ClassFlags.CollapseCategories )) 
                                || Package.Version <= 490 || Package.Version >= 576 )
                            { 
                                AutoExpandCategories = DeserializeGroup( "AutoExpandCategories" );
                                Record( "AutoExpandCategories", AutoExpandCategories );
                            }

                            if( Package.Version > 670 )
                            {
                                AutoCollapseCategories = DeserializeGroup( "AutoCollapseCategories" );
                                Record( "AutoCollapseCategories", AutoCollapseCategories );

                                if( Package.Version >= 749 
                                    #if SPECIALFORCE2
                                        && Package.Build != UnrealPackage.GameBuild.BuildName.SpecialForce2  
                                    #endif
                                    )
                                {
                                    // bForceScriptOrder
                                    ForceScriptOrder = _Buffer.ReadInt32() > 0;
                                    Record( "ForceScriptOrder", ForceScriptOrder );

#if DISHONORED
                                    if( Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored )
                                    {
                                        var unk = _Buffer.ReadNameIndex();
                                        Record( "??DISHONORED_NameIndex", Package.Names[unk] );
                                    }
#endif

                                    if( Package.Version >= UnrealPackage.VCLASSGROUP )
                                    {
#if DISHONORED
                                        if( Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored )
                                        {
                                            NativeClassName = _Buffer.ReadText();
                                            Record( "NativeClassName", NativeClassName ); 
                                            goto skipClassGroups;
                                        }
#endif
                                        ClassGroups = DeserializeGroup( "ClassGroups" );
                                        Record( "ClassGroups", ClassGroups );
                                        if( Package.Version >= 813 )
                                        {
                                            NativeClassName = _Buffer.ReadText();
                                            Record( "NativeClassName", NativeClassName );
                                        }
                                    }
#if DISHONORED
                                    skipClassGroups:;
#endif
                                }
                            }

                            // FIXME: Found first in(V:655), Definitely not in APB and GoW 2
                            // TODO: Corrigate Version
                            if( Package.Version > 575 && Package.Version < 678 )
                            {
                                int unk2 = _Buffer.ReadInt32();
                                Record( "??Int32", unk2 );

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
                        DLLBindName = _Buffer.ReadNameReference();
                        Record( "DLLBindName", DLLBindName );
#if DISHONORED
                        if( Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored )
                        {
                            ClassGroups = DeserializeGroup( "ClassGroups" );
                            Record( "ClassGroups", ClassGroups );   
                        }
#endif
#if BORDERLANDS2
                        if( Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 )
                        { 
                            var unkval = _Buffer.ReadByte();
                            Record( "??BL2_Byte", unkval );
                        }
#endif
                    }
                }
            }	
    
            // In later UE3 builds, defaultproperties are stored in separated objects named DEFAULT_namehere, 
            // TODO: Corrigate Version
            if( Package.Version >= 322 )
            { 
                Default = _Buffer.ReadObject();
                Record( "Default", Default );
                if( Default != null )
                {
                    Default.BeginDeserializing();
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

        protected override void FindChildren()
        {
            base.FindChildren();
            States = new List<UState>();
            for( var child = Children; child != null; child = child.NextField )
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
            Record( groupName + "Count", count );
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