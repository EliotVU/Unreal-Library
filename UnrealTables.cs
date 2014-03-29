using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Collections.Generic;
using UELib.Core;

namespace UELib
{
    public struct UGenerationTableItem : IUnrealSerializableClass
    {
        public int ExportsCount;
        public int NamesCount;
        public int NetObjectsCount;

        private const int VNetObjectsCount = 322;

        public void Serialize( IUnrealStream stream )
        {
            stream.Write( ExportsCount );  
            stream.Write( NamesCount );  
            if( stream.Version >= VNetObjectsCount )
            {
                stream.Write( NetObjectsCount ); 
            }	
        }

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
            if( stream.Version >= VNetObjectsCount )
            {
                NetObjectsCount = stream.ReadInt32();
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
        public int Index{ get; internal set; }

        /// <summary>
        /// Table offset in bytes.
        /// </summary>
        public int Offset{ get; internal set; }

        /// <summary>
        /// Table size in bytes.
        /// </summary>
        public int Size{ get; internal set; }
        #endregion

        #region Methods
        public string ToString( bool shouldPrintMembers )
        {
            return shouldPrintMembers 
                ? String.Format( "\r\nTable Index:{0}\r\nTable Offset:0x{1:X8}\r\nTable Size:0x{2:X8}\r\n", Index, Offset, Size ) 
                : base.ToString();
        }
        #endregion
    }

    public sealed class UName : IUnrealSerializableClass
    {
        private const string    None = "None";
        public const int        Numeric = 0;
        internal const int      VNameNumbered = 343;

        private UNameTableItem  _NameItem;
        private int             _Number;
        public int              Number{ get; private set; }

        private string          _Text
        {
            get{ return _Number > Numeric ? _NameItem.Name + "_" + _Number : _NameItem.Name; }
        }

        private int             _Index
        {
            get{ return _NameItem.Index; }
        }

        public int              Index{ get; private set; }

        public int              Length
        {
            get{ return _Text.Length; }
        }
        
        public UName( IUnrealStream stream )
        {
            Deserialize( stream );
        }

        public UName( UNameTableItem nameItem, int num )
        {
            _NameItem = nameItem;
            _Number = num;
        }

        public bool IsNone()
        {
            return _NameItem.Name.Equals( None, StringComparison.OrdinalIgnoreCase );
        }

        public void Deserialize( IUnrealStream stream )
        {
            var index = stream.ReadNameIndex( out _Number );
            _NameItem = stream.Package.Names[index];

            Debug.Assert( _NameItem != null, "_NameItem cannot be null! " + index );
            Debug.Assert( _Number >= -1, "Invalid _Number value! " + _Number );
        }

        public void Serialize( IUnrealStream stream )
        {
            stream.WriteIndex( _Index );
            if( stream.Version >= VNameNumbered )
            {
                Console.WriteLine( _Number + " " + _Text );
                stream.Write( (uint)_Number + 1 );
            }   
        }

        public override string ToString()
        {
            return _Text;
        }

        public static bool operator ==( UName a, object b )
        {
            return Equals( a, b );
        }

        public static bool operator !=( UName a, object b )
        {
            return !Equals( a, b );
        }

        public static bool operator ==( UName a, string b )
        {
            return String.Equals( a, b );
        }

        public static bool operator !=( UName a, string b )
        {
            return !String.Equals( a, b );
        }

        public static implicit operator string( UName a )
        {
            return !Equals( a, null ) ? a._Text : null;
        }

        public static explicit operator int( UName a )
        {
            return a._Index;
        }
    }

    /// <summary>
    /// Represents a unreal name table with serialized data from a unreal package header.
    /// </summary>
    public sealed class UNameTableItem : UTableItem, IUnrealSerializableClass
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
            Name = stream.ReadText();
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

        public void Serialize( IUnrealStream stream )
        {
            stream.WriteString( Name );

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

        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==( UNameTableItem a, string b )
        {
            return String.Equals( a, b );
        }

        public static bool operator !=( UNameTableItem a, string b )
        {
            return !String.Equals( a, b );
        }

        public static implicit operator string( UNameTableItem a )
        {
            return a.Name;
        }

        public static implicit operator int( UNameTableItem a )
        {
            return a.Index;
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
        [Pure]public UNameTableItem ObjectTable{ get{ return Owner.Names[(int)ObjectName]; } }
        public UName ObjectName;

        /// <summary>
        /// Import:Name index to the class of this object
        /// Export:Object index to the class of this object
        /// -- Not Fixed
        /// </summary>
        public int ClassIndex{ get; protected set; }
        [Pure]public UObjectTableItem ClassTable{ get{ return Owner.GetIndexTable( ClassIndex ); } }
        [Pure]public virtual string ClassName{ get{ return ClassIndex != 0 ? Owner.GetIndexTable( ClassIndex ).ObjectName : "class"; } }

        /// <summary>
        /// Object index to the outer of this object
        /// -- Not Fixed
        /// </summary>
        public int OuterIndex{ get; protected set; }
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
            ClassIndex 		= stream.ReadObjectIndex();
            SuperIndex 		= stream.ReadObjectIndex();
            OuterIndex 		= stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
#if BIOSHOCK
            if( stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock && stream.Version >= 132 )
            {
                stream.Skip( sizeof(int) );
            }
#endif
            ObjectName 	= stream.ReadNameReference();	
            
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

            stream.Skip( 16 );  // Package guid
            if( stream.Version > 486 )	// 475?	 486(> Stargate Worlds)
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

    /// <summary>
    /// Represents a unreal import table with deserialized data from a unreal package header.
    /// </summary>
    public sealed class UImportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
        #region Serialized Members
        public UName PackageName;
        private UName _ClassName;

        [Pure]public override string ClassName{ get{ return _ClassName; } }
        #endregion

        public void Serialize( IUnrealStream stream )
        {
            Console.WriteLine( "Writing import " + ObjectName + " at " + stream.Position );
            stream.Write( PackageName );
            stream.Write( _ClassName );
            stream.Write( OuterTable != null ? (int)OuterTable.Object : 0 ); // Always an ordinary integer
            stream.Write( ObjectName );
        }

        public void Deserialize( IUnrealStream stream )
        {
            Console.WriteLine( "Reading import " + Index + " at " + stream.Position );
            PackageName 		= stream.ReadNameReference();
            _ClassName 			= stream.ReadNameReference();
             ClassIndex         = (int)_ClassName;
            OuterIndex 			= stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
            ObjectName 		    = stream.ReadNameReference();
        }

        #region Methods
        public override string ToString()
        {
            return ObjectName + "(" + -(Index + 1) + ")";
        }
        #endregion
    }

    public sealed class UDependencyTableItem : UTableItem, IUnrealDeserializableClass
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