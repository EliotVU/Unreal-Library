using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UELib.Core;

namespace UELib
{
    public struct UGenerationTableItem : IUnrealDeserializableClass
    {
        /// <summary>
        /// Amount of exported objects that resist within a package.
        /// </summary>
        public int ExportsCount;

        /// <summary>
        /// Amount of unique names that resist within a package.
        /// </summary>
        public int NamesCount;
        public int NetObjectsCount;

        public void Deserialize( IUnrealStream stream )
        {
#if APB
            if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.APB && stream.Package.LicenseeVersion >= 32 )
            {
                stream.Skip( 16 );
            }
#endif

            ExportsCount = stream.ReadInt32();
            NamesCount = stream.ReadInt32();
            if( stream.Version >= 322 )
            {
                NetObjectsCount = stream.ReadInt32();

                //if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.Hawken )
                //{
                //    stream.Skip( 4 );
                //}
            }		
        }
    }

    /// <summary>
    /// Represents a basic file table.
    /// </summary>
    public abstract class UTableItem
    {
        #region PreInitialized Members
        /// <summary>
        /// Index into the table's enumerable.
        /// </summary>
        public int Index;

        /// <summary>
        /// Table offset in bytes.
        /// </summary>
        public int Offset;

        /// <summary>
        /// Table size in bytes.
        /// </summary>
        public int Size;
        #endregion
    }

    /// <summary>
    /// Represents a unreal name table with serialized data from a unreal package header.
    /// </summary>
    public sealed class UNameTableItem : UTableItem, IUnrealDeserializableClass
    {
        #region Serialized Members
        /// <summary>
        /// Object Name
        /// </summary>
        public string Name = String.Empty;

        /// <summary>
        /// Object Flags, such as LoadForEdit, LoadForServer, LoadForClient
        /// </summary>
        /// <value>
        /// 32bit in UE2
        /// 64bit in UE3
        /// </value>
        public ulong Flags;
        #endregion

        private const int QWORDVersion = 141;

        public void Deserialize( IUnrealStream stream )
        {
            Name = stream.ReadString();
            Flags = stream.Version >= QWORDVersion ? stream.ReadUInt64() : stream.ReadUInt32();					
#if DEOBFUSCATE
            // De-obfuscate names that contain unprintable characters!
            foreach( char c in Name )
            {
                if( !char.IsLetterOrDigit( c ) )
                {
                    Name = "N" + TableIndex + "_OBF";
                    break;
                }
            }
#endif
        }

        #region Writing Methods
        /// <summary>
        /// Updates the Name inside the Stream to the current set Name of this Table
        /// </summary>
        /// <param name="stream">Stream to Update</param>
        public void WriteName( UPackageStream stream )
        {
            stream.Seek( Offset, SeekOrigin.Begin );

            int size = stream.ReadIndex();
            byte[] rawName = Encoding.ASCII.GetBytes( Name );
            stream.UW.Write( rawName, 0, size - 1 );
        }

        /// <summary>
        /// Updates the Flags inside the Stream to the current set Flags of this Table
        /// </summary>
        /// <param name="stream">Stream to Update</param>
        public void WriteFlags( UPackageStream stream )
        {
            stream.Seek( Offset, SeekOrigin.Begin );
            stream.ReadString();
            if( stream.Version < QWORDVersion )
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

        public static bool operator ==( UNameTableItem a, string b )
        {
            return a.Name == b;
        }

        public static bool operator !=( UNameTableItem a, string b )
        {
            return a.Name != b;
        }

        private bool Equals( UNameTableItem other )
        {
            return string.Equals( Name, other.Name );
        }

        public override bool Equals( object obj )
        {
            if( ReferenceEquals( null, obj ) ) return false;
            if( ReferenceEquals( this, obj ) ) return true;
            return obj is UNameTableItem && Equals( (UNameTableItem)obj );
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Represents a unreal table with general deserialized data from a unreal package header.
    /// </summary>
    public abstract class UObjectTableItem : UTableItem, IBuffered
    {
        #region PreInitialized Members
        /// <summary>
        /// Reference to the UnrealPackage this object resists in
        /// </summary>
        public UnrealPackage Owner;

        /// <summary>
        /// Reference to the serialized object based on this table.
        /// 
        /// Only valid if Owner != null and Owner is fully serialized or on demand.
        /// </summary>
        public UObject Object;
        #endregion

        #region Serialized Members
        /// <summary>
        /// Name index to the name of this object
        /// -- Fixed
        /// </summary>
        public int ObjectIndex;
        [Pure]public UNameTableItem ObjectTable{ get{ return Owner.Names[ObjectIndex]; } }
        [Pure]public string ObjectName{ get{ return ObjectNumber > 0 ? Owner.GetIndexName( ObjectIndex ) + "_" + ObjectNumber : Owner.GetIndexName( ObjectIndex ); } }
        public int ObjectNumber;

        /// <summary>
        /// Import:Name index to the class of this object
        /// Export:Object index to the class of this object
        /// -- Not Fixed
        /// </summary>
        public int ClassIndex;
        [Pure]public UObjectTableItem ClassTable{ get{ return Owner.GetIndexTable( ClassIndex ); } }
        [Pure]public string ClassName
        { 
            get
            {
                if( this is UImportTableItem )
                {
                    return Owner.GetIndexName( ClassIndex );
                }
                return ClassIndex != 0 ? Owner.GetIndexTable( ClassIndex ).ObjectName : "class";
            }
        }

        /// <summary>
        /// Object index to the outer of this object
        /// -- Not Fixed
        /// </summary>
        public int OuterIndex;
        [Pure]public UObjectTableItem OuterTable{ get{ return Owner.GetIndexTable( OuterIndex ); } }
        [Pure]public string OuterName{ get{ var table = OuterTable; return table != null ? table.ObjectName : String.Empty; } }
        #endregion

        #region IBuffered
        public virtual byte[] CopyBuffer()
        {
            var buff = new byte[Size];
            Owner.Stream.Seek( Offset, SeekOrigin.Begin );
            Owner.Stream.Read( buff, 0, Size );
            if( Owner.Stream.BigEndianCode )
            {
                Array.Reverse( buff );
            }
            return buff;
        }

        [Pure]
        public IUnrealStream GetBuffer()
        {
            return Owner.Stream;
        }

        [Pure]
        public int GetBufferPosition()
        {
            return Offset;
        }

        [Pure]
        public int GetBufferSize()
        {
            return Size;
        }

        [Pure]
        public string GetBufferId( bool fullName = false )
        {
            return fullName ? Owner.PackageName + "." + ObjectName + ".table" : ObjectName + ".table";
        }
        #endregion
    }

    /// <summary>
    /// Represents a unreal export table with deserialized data from a unreal package header.
    /// </summary>
    public sealed class UExportTableItem : UObjectTableItem, IUnrealDeserializableClass
    {
        #region Serialized Members
        /// <summary>
        /// Object index to the Super(parent) object of structs.
        /// -- Not Fixed
        /// </summary>
        public int SuperIndex;
        [Pure]public UObjectTableItem SuperTable{ get{ return Owner.GetIndexTable( SuperIndex ); } }
        [Pure]public string SuperName{ get{ var table = SuperTable; return table != null ? table.ObjectName : String.Empty; } }

        /// <summary>
        /// Object index.
        /// -- Not Fixed
        /// </summary>
        public int ArchetypeIndex;
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
        public Dictionary<int, int> Components;
        public List<int> NetObjects;
        #endregion

        public void Deserialize( IUnrealStream stream )
        {
            ClassIndex 		= stream.ReadObjectIndex();
            SuperIndex 		= stream.ReadObjectIndex();
            OuterIndex 		= stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
#if BIOSHOCK
            if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock && stream.Version >= 132 )
            {
                stream.Skip( sizeof(int) );
            }
#endif
            ObjectIndex 	= stream.ReadNameIndex( out ObjectNumber );	
            
            if( stream.Version >= 220 )
            {
                ArchetypeIndex = stream.ReadInt32();
            }

            _ObjectFlagsOffset = stream.Position;	
            ObjectFlags = stream.ReadUInt32();
            if( stream.Version >= 195 
#if BIOSHOCK
                || (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock && stream.Package.LicenseeVersion >= 40) 
#endif
                )
            {
                ObjectFlags = (ObjectFlags << 32) | stream.ReadUInt32();
            }

            SerialSize = stream.ReadIndex();
            if( SerialSize > 0 || stream.Version >= 249 )
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

            if( stream.Version < 543 )
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

            // NetObjectCount
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
            stream.Skip( 16 ); // GUID
            if( stream.Version > 486 )	// 475?	 486(> Stargate Worlds)
            {
                // Depends?
                stream.ReadInt32();
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
    }

    /// <summary>
    /// Represents a unreal import table with deserialized data from a unreal package header.
    /// </summary>
    public sealed class UImportTableItem : UObjectTableItem, IUnrealDeserializableClass
    {
        #region Serialized Members
        /// <summary>
        /// Name index to the package that contains this object class
        /// -- Fixed
        /// </summary>
        public int PackageIndex;
        [Pure]public string PackageName{ get{ return PackageNumber > 0 ? Owner.GetIndexName( PackageIndex ) + "_" + PackageNumber : Owner.GetIndexName( PackageIndex ); } }
        public int PackageNumber;
        #endregion

        public void Deserialize( IUnrealStream stream )
        {
            PackageIndex 		= stream.ReadNameIndex( out PackageNumber );
            ClassIndex 			= stream.ReadNameIndex();
            OuterIndex 			= stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
            ObjectIndex 		= stream.ReadNameIndex( out ObjectNumber );
        }
    }

    public sealed class UDependencyTableItem : UObjectTableItem, IUnrealDeserializableClass
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