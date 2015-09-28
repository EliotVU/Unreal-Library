using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace UELib
{
    /// <summary>
    /// Represents a unreal export table with deserialized data from a unreal package header.
    /// </summary>
    public sealed class UExportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
        private const uint VArchetype = 220;
        private const uint VObjectFlagsToULONG = 195;
        private const uint VSerialSizeConditionless = 249;

        #region Serialized Members
        /// <summary>
        /// Object index to the Super(parent) object of structs.
        /// -- Not Fixed
        /// </summary>
        public int SuperIndex{ get; private set; }
        [Pure]public UObjectTableItem SuperTable{ get{ return Owner.GetIndexTable( SuperIndex ); } }
        [Pure]public string SuperName{ get{ var table = SuperTable; return table != null ? table.ObjectName : String.Empty; } }

        /// <summary>
        /// Object index.
        /// -- Not Fixed
        /// </summary>
        public int ArchetypeIndex{ get; private set; }
        [Pure]public UObjectTableItem ArchetypeTable{ get{ return Owner.GetIndexTable( ArchetypeIndex ); } }
        [Pure]public string ArchetypeName{ get{ var table = ArchetypeTable; return table != null ? table.ObjectName : String.Empty; } }

        /// <summary>
        /// Object flags, such as Public, Protected and Private.
        /// 32bit aligned.
        /// </summary>
        public ulong ObjectFlags;

        /// <summary>
        /// Object size in bytes.
        /// </summary>
        public int SerialSize;

        /// <summary>
        /// Object offset in bytes. Starting from the beginning of a file.
        /// </summary>
        public int SerialOffset;

        public uint ExportFlags;
        //public Dictionary<int, int> Components;
        //public List<int> NetObjects;
        #endregion

        // @Warning - Only supports Official builds.
        public void Serialize( IUnrealStream stream )
        {
            stream.Write( ClassTable.Object );
            stream.Write( SuperTable.Object );
            stream.Write( (int)OuterTable.Object );

            stream.Write( ObjectName );

            if( stream.Version >= VArchetype )
            {
                ArchetypeIndex = stream.ReadInt32();
            }
            stream.UW.Write( stream.Version >= VObjectFlagsToULONG ? ObjectFlags : (uint)ObjectFlags );
            stream.WriteIndex( SerialSize );    // Assumes SerialSize has been updated to @Object's buffer size.
            if( SerialSize > 0 || stream.Version >= VSerialSizeConditionless )
            {
                // SerialOffset has to be set and written after this object has been serialized.
                stream.WriteIndex( SerialOffset );  // Assumes the same as @SerialSize comment.
            }

            // TODO: Continue.
        }

        public void Deserialize( IUnrealStream stream )
        {
            ClassIndex      = stream.ReadObjectIndex();
            SuperIndex      = stream.ReadObjectIndex();
            OuterIndex      = stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
#if BIOSHOCK
            if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock && stream.Version >= 132 )
            {
                stream.Skip( sizeof(int) );
            }
#endif
            ObjectName  = stream.ReadNameReference();

            if( stream.Version >= VArchetype )
            {
                ArchetypeIndex = stream.ReadInt32();
            }

            _ObjectFlagsOffset = stream.Position;
            ObjectFlags = stream.ReadUInt32();
            if( stream.Version >= VObjectFlagsToULONG
#if BIOSHOCK
                || (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock && stream.Package.LicenseeVersion >= 40)
#endif
                )
            {
                ObjectFlags = (ObjectFlags << 32) | stream.ReadUInt32();
            }

            SerialSize = stream.ReadIndex();
            if( SerialSize > 0 || stream.Version >= VSerialSizeConditionless )
            {
                SerialOffset = stream.ReadIndex();
            }
#if BIOSHOCK
            if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock && stream.Version >= 130 )
            {
                stream.Skip( sizeof(int) );
            }
#endif
            if( stream.Version < 220 )
                return;

            if( stream.Version < 543
#if ALPHAPROTOCOL
                && stream.Package.Build != UnrealPackage.GameBuild.BuildName.AlphaProtcol
#endif
#if TRANSFORMERS
                && (stream.Package.Build != UnrealPackage.GameBuild.BuildName.Transformers || stream.Package.LicenseeVersion < 37)
#endif
                )
            {
                int componentMapCount = stream.ReadInt32();
                stream.Skip( componentMapCount * 12 );
                //if( componentMapCount > 0 )
                //{
                //    Components = new Dictionary<int, int>( componentMapCount );
                //    for( int i = 0; i < componentMapCount; ++ i )
                //    {
                //        Components.Add( stream.ReadNameIndex(), stream.ReadObjectIndex() );
                //    }
                //}
            }

            if( stream.Version < 247 )
                return;

            ExportFlags = stream.ReadUInt32();
            if( stream.Version < 322 )
                return;
#if TRANSFORMERS
            if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.Transformers && stream.Package.LicenseeVersion >= 116 )
            {
                var flag = stream.ReadByte();
                if( flag == 0 )
                {
                    return;
                }
            }
#endif
#if BIOSHOCK
            if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock_Infinite )
            {
                var unk = stream.ReadUInt32();
                if( unk == 1 )
                {
                    var flags = stream.ReadUInt32();
                    if( (flags & 1) != 0x0 )
                    {
                        stream.ReadUInt32();
                    }
                    stream.Skip( 16 );  // guid
                    stream.ReadUInt32();    // 01000020
                }
                return;
            }
#endif
#if MKKE
            if( stream.Package.Build != UnrealPackage.GameBuild.BuildName.MKKE )
            {            
#endif
                int netObjectCount = stream.ReadInt32();
                stream.Skip( netObjectCount * 4 );
                //if( netObjectCount > 0 )
                //{
                //    NetObjects = new List<int>( netObjectCount );
                //    for( int i = 0; i < netObjectCount; ++ i )
                //    {
                //        NetObjects.Add( stream.ReadObjectIndex() );
                //    }
                //}
#if MKKE
            }
#endif
            stream.Skip( 16 );  // Package guid
            if( stream.Version > 486 )  // 475?  486(> Stargate Worlds)
            {
                stream.Skip( 4 ); // Package flags
            }
        }

        #region Writing Methods
        private long _ObjectFlagsOffset;

        /// <summary>
        /// Updates the ObjectFlags inside the Stream to the current set ObjectFlags of this Table
        /// </summary>
        public void WriteObjectFlags()
        {
            Owner.Stream.Seek( _ObjectFlagsOffset, SeekOrigin.Begin );
            Owner.Stream.UW.Write( (uint)ObjectFlags );
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return ObjectName + "(" + Index + 1 + ")";
        }
        #endregion
    }
}