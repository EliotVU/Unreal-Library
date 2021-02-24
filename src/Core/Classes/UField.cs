﻿using System;
using System.Diagnostics.Contracts;

namespace UELib.JsonDecompiler.Core
{
    /// <summary>
    /// Represents a unreal field.
    /// </summary>
    public partial class UField : UObject
    {
        #region Serialized Members
        public UField Super{ get; private set; }
        public UField NextField{ get; private set; }
        #endregion

        #region Script Members
        /// <summary>
        /// Initialized by the UMetaData object,
        /// This Meta contains comments and other meta related info that belongs to this instance.
        /// </summary>
        public UMetaData.UFieldData MetaData;
        #endregion

        #region Constructors
        protected override void Deserialize()
        {
            base.Deserialize();

            // _SuperIndex got moved into UStruct since 700+
            if( (Package.Version < 756
#if SPECIALFORCE2
                || Package.Build == UnrealPackage.GameBuild.BuildName.SpecialForce2
#endif
#if TRANSFORMERS
                || Package.Build == UnrealPackage.GameBuild.BuildName.Transformers
#endif
                )
#if BIOSHOCK
                && Package.Build != UnrealPackage.GameBuild.BuildName.Bioshock_Infinite
#endif
                )
            {
                Super = GetIndexObject( _Buffer.ReadObjectIndex() ) as UField;
                Record( "Super", Super );

                NextField = GetIndexObject( _Buffer.ReadObjectIndex() ) as UField;
                Record( "NextField", NextField );
            }
            else
            {
                NextField = GetIndexObject( _Buffer.ReadObjectIndex() ) as UField;
                Record( "NextField", NextField );

                // Should actually resist in UStruct
                if( this is UStruct )
                {
                    Super = GetIndexObject( _Buffer.ReadObjectIndex() ) as UField;
                    Record( "Super", Super );
                }
            }
        }
        #endregion

        #region Methods
        [Pure]public string GetSuperGroup()
        {
            string group = String.Empty;
            for( var field = Super; field != null; field = field.Super )
            {
                group = field.Name + "." + group;
            }
            return group + Name;
        }

        [Pure]public bool Extends( string classType )
        {
            for( var field = Super; field != null; field = field.Super )
            {
                if( String.Equals( field.Name, classType, StringComparison.OrdinalIgnoreCase ) )
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}