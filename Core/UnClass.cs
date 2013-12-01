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
        public struct Dependency : IUnrealSerializableClass
        {
            public int Class{ get; private set; }

            public void Serialize( IUnrealStream stream )
            {
                // TODO: Implement code
            }

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

                const int vHideCategoriesOldOrder = 539;

                // TODO: Corrigate Version
                if( Package.Version >= 100 )
                {
                    // TODO: Corrigate Version
                    if( Package.Version >= 220 )
                    {
                        // TODO: Corrigate Version
                        if( Package.Version <= vHideCategoriesOldOrder )
                        {
                            DeserializeHideCategories();
                        }

                        DeserializeComponentsMap();

                        // RoboBlitz(369)
                        // TODO: Corrigate Version
                        if( Package.Version >= 369 )
                        {
                            DeserializeInterfaces();
                        }
                    }

                    if( !Package.IsConsoleCooked() && !Package.Build.IsXenonCompressed )
                    {
                        if( Package.Version >= 603 )
                        {
                            DontSortCategories = DeserializeGroup( "DontSortCategories" );
                        }

                        // TODO: Corrigate Version
                        if( Package.Version < 220 || Package.Version > vHideCategoriesOldOrder )
                        {
                            DeserializeHideCategories();
                        }

                        // TODO: Corrigate Version
                        if( Package.Version >= 185 )
                        {
                            // 490:GoW1, 576:CrimeCraft
                            if( (!HasClassFlag( Flags.ClassFlags.CollapseCategories )) 
                                || Package.Version <= vHideCategoriesOldOrder || Package.Version >= 576 )
                            { 
                                AutoExpandCategories = DeserializeGroup( "AutoExpandCategories" );
                            }

                            if( Package.Version > 670 )
                            {
                                AutoCollapseCategories = DeserializeGroup( "AutoCollapseCategories" );

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
#if REMEMBERME
                        if( Package.Build == UnrealPackage.GameBuild.BuildName.RememberMe )
                        {
                            var unknownName = _Buffer.ReadNameReference();
                            Record( "??RM_Name", unknownName );
                        }
#endif
#if DISHONORED
                        if( Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored )
                        {
                            ClassGroups = DeserializeGroup( "ClassGroups" );
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

        private void DeserializeInterfaces()
        {
            // See http://udn.epicgames.com/Three/UnrealScriptInterfaces.html
            int interfacesCount = _Buffer.ReadInt32();
            Record( "Implements.Count", interfacesCount );
            if( interfacesCount <= 0 )
                return;

            AssertEOS( interfacesCount*8, "Implemented" );
            ImplementedInterfaces = new List<int>( interfacesCount );
            for( int i = 0; i < interfacesCount; ++ i )
            {
                int interfaceIndex = _Buffer.ReadInt32();
                Record( "Implemented.InterfaceIndex", interfaceIndex );
                int typeIndex = _Buffer.ReadInt32();
                Record( "Implemented.TypeIndex", typeIndex );
                ImplementedInterfaces.Add( interfaceIndex );
            }
        }

        private void DeserializeHideCategories()
        {
            HideCategories = DeserializeGroup( "HideCategories" );
        }

        private void DeserializeComponentsMap()
        {
            int componentsCount = _Buffer.ReadInt32();
            Record( "Components.Count", componentsCount );
            if( componentsCount <= 0 )
                return;

            // NameIndex/ObjectIndex
            int numBytes = componentsCount*12;
            AssertEOS( numBytes, "Components" );
            _Buffer.Skip( numBytes );
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
            Record( String.Format( "{0}.Count", groupName ), count );
            if( count > 0 )
            {
                var groupList = new List<int>( count );
                for( int i = 0; i < count; ++ i )
                {
                    int index = _Buffer.ReadNameIndex();
                    groupList.Add( index );

                    Record( String.Format( "{0}({1})", groupName, Package.GetIndexName( index ) ), index );
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