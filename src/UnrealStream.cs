﻿// WARNING: You might get a brain stroke from reading the code below :O
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UELib.JsonDecompiler.Core;

namespace UELib.JsonDecompiler
{
    public interface IUnrealStream : IDisposable
    {
        string Name{ get; }

        UnrealPackage Package{ get; }
        UnrealReader UR{ get; }
        UnrealWriter UW{ get; }

        bool BigEndianCode{ get; }

        /// <summary>
        /// The version of the package this stream is working for.
        /// </summary>
        uint Version{ get; }

        /// <summary>
        /// Reads the next bytes as characters either in ASCII or Unicode with prefix string length.
        /// </summary>
        /// <returns></returns>
        string ReadText();

        /// <summary>
        /// Reads the next bytes as a index to an Object.
        /// </summary>
        /// <returns></returns>
        int ReadObjectIndex();
        UObject ReadObject();
        UObject ParseObject( int index );

        /// <summary>
        /// Reads the next bytes as a index to an NameTable.
        /// </summary>
        /// <returns></returns>
        int ReadNameIndex();
        UName ReadNameReference();
        string ParseName( int index );

        /// <summary>
        /// Reads the next bytes as a index to an NameTable.
        /// </summary>
        /// <returns></returns>
        int ReadNameIndex( out int num );

        /// <summary>
        /// Reads the next bytes as a index.
        /// </summary>
        /// <returns></returns>
        int ReadIndex();

        /// <summary>
        /// Reads the next 4 bytes as a float converted to an Unreal float format.
        /// </summary>
        /// <returns></returns>
        float ReadFloat();

        byte ReadByte();

        short ReadInt16();
        ushort ReadUInt16();

        int ReadInt32();
        uint ReadUInt32();

        long ReadInt64();
        ulong ReadUInt64();

        void Skip( int bytes );

        // Stream
        long Length{ get; }
        long Position{ get; set; }
        long LastPosition{ get; set; }
        int Read( byte[] array, int offset, int count );
        long Seek( long offset, SeekOrigin origin );
    }

    public class UnrealWriter : BinaryWriter
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes" )]
        private IUnrealStream _UnrealStream;

        private uint _Version
        {
            get{ return _UnrealStream.Version; }
        }

        public UnrealWriter( Stream stream ) : base( stream )
        {
            _UnrealStream = stream as IUnrealStream;
        }

        public void WriteText( string s )
        {
            Write( s.Length );
            Write( s.ToCharArray(), 0, 0 );
            Write( '\0' );
        }

        // TODO: Add support for Unicode, and Unreal Engine 1 & 2.
        public void WriteString( string s )
        {
            WriteIndex( s.Length + 1 );
            var bytes = Encoding.ASCII.GetBytes( s );
            Write( bytes, 0, bytes.Count() );
            Write( (byte)0 );
        }

        public void WriteIndex( int index )
        {
            if( _Version >= UnrealPackage.VINDEXDEPRECATED )
            {
                Write( index );
            }
            else
            {
                throw new InvalidDataException( "UE1 and UE2 are not supported for writing indexes!" );
            }
        }

        protected override void Dispose( bool disposing )
        {
            base.Dispose( disposing );
            if( !disposing )
                return;

            _UnrealStream = null;
        }
    }

    /// <summary>
    /// Wrapper for Streams with specific functions for deserializing UELib.UnrealPackage.
    /// </summary>
    public class UnrealReader : BinaryReader
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes" )]
        private IUnrealStream _UnrealStream;

        private uint _Version
        {
            get{ return _UnrealStream.Version; }
        }

        public UnrealReader( Stream stream ) : base( stream )
        {
            _UnrealStream = stream as IUnrealStream;
        }

        /// <summary>
        /// Reads a string that was serialized for Unreal Packages, these strings use a positive or negative size to:
        /// - indicate that the bytes were encoded in ASCII.
        /// - indicate that the bytes were encoded in Unicode.
        /// 
        /// Several exceptions may exist in games such as Transformers, and Bioshock, etc.
        /// </summary>
        /// <returns>A string in either ASCII or Unicode.</returns>
        public string ReadText()
        {
#if DEBUG || BINARYMETADATA
            var lastPosition = _UnrealStream.Position;
#endif
            // Very old packages use a simple Ansi encoding.
            if( _Version < UnrealPackage.VSIZEPREFIXDEPRECATED )
            {
                return ReadAnsi();
            }

            byte[] strBytes;
            int unfixedSize; var size = (unfixedSize =
#if BIOSHOCK
                // In Bioshock packages always give a positive Size despite being Unicode, so we reverse this.
                _UnrealStream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock ? -ReadIndex() :
#endif
                ReadIndex()) < 0 ? -unfixedSize : unfixedSize;
            System.Diagnostics.Debug.Assert( size < 1000000, "Dangerous string size detected! IT'S OVER 9000 THOUSAND!" );
            if( unfixedSize > 0 ) // ANSI
            {
#if TRANSFORMERS
                // No null-termination in Transformer games.
                if( _UnrealStream.Package.Build == UnrealPackage.GameBuild.BuildName.Transformers && _UnrealStream.Package.LicenseeVersion >= 181 )
                {
                    strBytes = new byte[size];
                    Read( strBytes, 0, size);
                    goto reverse;
                }
#endif
                strBytes = new byte[size - 1];
                Read( strBytes, 0, size - 1 );
                ++ BaseStream.Position; // null

            reverse:
                // The Read function always reverses all read bytes, but since strings are an exception to BigEndianCode, we have to re-reverse the bytes.
                if( _UnrealStream.BigEndianCode )
                {
                    Array.Reverse( strBytes );
                }
#if DEBUG || BINARYMETADATA
                _UnrealStream.LastPosition = lastPosition;
#endif
                return Encoding.ASCII.GetString( strBytes );
            }

            if( unfixedSize < 0 ) // UNICODE
            {
                size *= 2;
#if TRANSFORMERS
                // No null-termination in Transformer games.
                if( _UnrealStream.Package.Build == UnrealPackage.GameBuild.BuildName.Transformers && _UnrealStream.Package.LicenseeVersion >= 181 )
                {
                    strBytes = new byte[size];
                    Read( strBytes, 0, size);
                    goto reverse;
                }
#endif
                strBytes = new byte[size - 2];
                Read( strBytes, 0, size - 2 );
                BaseStream.Position += 2; // null

            reverse:
                // The Read function always reverses all read bytes, but since strings are an exception to BigEndianCode, we have to re-reverse the bytes.
                if( _UnrealStream.BigEndianCode )
                {
                    Array.Reverse( strBytes );
                }
#if DEBUG || BINARYMETADATA
                _UnrealStream.LastPosition = lastPosition;
#endif
                return Encoding.Unicode.GetString( strBytes );
            }
#if DEBUG || BINARYMETADATA
                _UnrealStream.LastPosition = lastPosition;
#endif
            return String.Empty;
        }

        public string ReadAnsi()
        {
            var strBytes = new List<byte>();
            nextChar:
                var BYTE = ReadByte();
                if( BYTE != '\0' )
                {
                    strBytes.Add( BYTE );
                    goto nextChar;
                }
            var s = Encoding.UTF8.GetString( strBytes.ToArray() );
            if( _UnrealStream.BigEndianCode )
            {
                Enumerable.Reverse( s );
            }
            return s;
        }

        public string ReadUnicode()
        {
            var strBytes = new List<byte>();
            nextWord:
                var w = ReadUInt16();
                if( w != 0 )
                {
                    strBytes.Add( (byte)(w & 0xFF00) );
                    strBytes.Add( (byte)(w & 0x00FF) );
                    goto nextWord;
                }
            var s = Encoding.Unicode.GetString( strBytes.ToArray() );
            if( _UnrealStream.BigEndianCode )
            {
                Enumerable.Reverse( s );
            }
            return s;
        }

        public int ReadIndex()
        {
            if( _Version >= UnrealPackage.VINDEXDEPRECATED )
            {
                return ReadInt32();
            }
#if DEBUG || BINARYMETADATA
            var lastPosition = _UnrealStream.Position;
#endif
            const byte isIndiced = 0x40; // 7th bit
            const byte isNegative = 0x80; // 8th bit
            const byte value = 0xFF - isIndiced - isNegative; // 3F
            const byte isProceeded = 0x80; // 8th bit
            const byte proceededValue = 0xFF - isProceeded; // 7F

            int index = 0;
            byte b0 = ReadByte();
            if( (b0 & isIndiced) != 0 )
            {
                byte b1 = ReadByte();
                if( (b1 & isProceeded) != 0 )
                {
                    byte b2 = ReadByte();
                    if( (b2 & isProceeded) != 0 )
                    {
                        byte b3 = ReadByte();
                        if( (b3 & isProceeded) != 0 )
                        {
                            byte b4 = ReadByte();
                            index = b4;
                        }
                        index = (index << 7) + (b3 & proceededValue);
                    }
                    index = (index << 7) + (b2 & proceededValue);
                }
                index = (index << 7) + (b1 & proceededValue);
            }
#if DEBUG || BINARYMETADATA
            _UnrealStream.LastPosition = lastPosition;
#endif
            return (b0 & isNegative) != 0 // The value is negative or positive?.
                ? -((index << 6) + (b0 & value))
                : ((index << 6) + (b0 & value));
        }

        public long ReadNameIndex()
        {
#if DEBUG || BINARYMETADATA
            var lastPosition = _UnrealStream.Position;
#endif
            var index = ReadIndex();
            if( _Version >= UName.VNameNumbered
#if BIOSHOCK
                || _UnrealStream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock
#endif
                )
            {
                var num = ReadUInt32()-1;
#if DEBUG || BINARYMETADATA
                _UnrealStream.LastPosition = lastPosition;
#endif
                return (long)((ulong)num << 32) | (uint)index;
            }
            return index;
        }

        public int ReadNameIndex( out int num )
        {
#if DEBUG || BINARYMETADATA
            var lastPosition = _UnrealStream.Position;
#endif
            var index = ReadIndex();
            if( _UnrealStream.Version >= UName.VNameNumbered
#if BIOSHOCK
                || _UnrealStream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock
#endif
                )
            {
                num = ReadInt32()-1;
#if DEBUG || BINARYMETADATA
                _UnrealStream.LastPosition = lastPosition;
#endif
                return index;
            }
            num = -1;
            return index;
        }

        public static int ReadIndexFromBuffer( byte[] value, IUnrealStream stream )
        {
            if( stream.Version >= UnrealPackage.VINDEXDEPRECATED )
            {
                return BitConverter.ToInt32( value, 0 );
            }

            int index = 0;
            byte b0 = value[0];
            if( (b0 & 0x40) != 0 )
            {
                byte b1 = value[1];
                if( (b1 & 0x80) != 0 )
                {
                    byte b2 = value[2];
                    if( (b2 & 0x80) != 0 )
                    {
                        byte b3 = value[3];
                        if( (b3 & 0x80) != 0 )
                        {
                            byte b4 = value[4];
                            index = b4;
                        }
                        index = (index << 7) + (b3 & 0x7F);
                    }
                    index = (index << 7) + (b2 & 0x7F);
                }
                index = (index << 7) + (b1 & 0x7F);
            }
            return (b0 & 0x80) != 0 // The value is negative or positive?.
                ? -((index << 6) + (b0 & 0x3F))
                : ((index << 6) + (b0 & 0x3F));
        }

        public string ReadGuid()
        {
            // A, B, C, D
            var guidBuffer = new byte[16];
            Read( guidBuffer, 0, 16 );
            var g = new Guid( guidBuffer );
            return g.ToString();
        }

        protected override void Dispose( bool disposing )
        {
            base.Dispose( disposing );
            if( !disposing )
                return;

            _UnrealStream = null;
        }
    }

    public class UPackageStream : FileStream, IUnrealStream
    {
        public UnrealPackage Package{ get; private set; }

        /// <inheritdoc/>
        public uint Version
        {
            get{ return Package != null ? Package.Version : 0; }
        }

        public UnrealReader UR{ get; private set; }
        public UnrealWriter UW{ get; private set; }

        public long LastPosition{ get; set; }

        public bool BigEndianCode{ get; private set; }
        public bool IsChunked{ get{ return Package.CompressedChunks != null && Package.CompressedChunks.Any(); } }

        public UPackageStream( string path, FileMode mode, FileAccess access ) : base( path, mode, access, FileShare.ReadWrite )
        {
            UR = null;
            UW = null;
            InitBuffer();
        }

        private void InitBuffer()
        {
            if( CanRead && UR == null )
            {
                UR = new UnrealReader( this );
            }

            if( CanWrite && UW == null )
            {
                UW = new UnrealWriter( this );
            }
        }

        public void PostInit( UnrealPackage package )
        {
            Package = package;

            if( !CanRead )
                return;

            if( package.Decoder != null )
            {
                package.Decoder.PreDecode( this );
            }

            var bytes = new byte[4];
            Read( bytes, 0, 4 );
            var readSignature = BitConverter.ToUInt32( bytes, 0 );
            if( readSignature == UnrealPackage.Signature_BigEndian )
            {
                Console.WriteLine( "Encoding:BigEndian" );
                BigEndianCode = true;
            }

            if( !UnrealConfig.SuppressSignature
                && readSignature != UnrealPackage.Signature
                && readSignature != UnrealPackage.Signature_BigEndian )
            {
                throw new FileLoadException( package.PackageName + " isn't an UnrealPackage!" );
            }
            Position = 4;
        }

        /// <summary>
        /// Called as soon the build for @Package is detected.
        /// </summary>
        /// <param name="build"></param>
        public void BuildDetected( UnrealPackage.GameBuild build )
        {
            if( Package.Decoder != null )
            {
                Package.Decoder.DecodeBuild( this, build );
            }
        }

        public override int Read( byte[] array, int offset, int count )
        {
#if DEBUG || BINARYMETADATA
            LastPosition = Position;
#endif
            var r = base.Read( array, offset, count );
            if( BigEndianCode && r > 1 )
            {
                Array.Reverse( array, 0, r );
            }
            // Give the decoder a chance to modify the read output.
            return (Package != null && Package.Decoder != null)
                ? Package.Decoder.DecodeRead( array, offset, count )
                : r;
        }

        /// <summary>
        /// Reads a float converted to a unreal float string format.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read float converted to a unreal float string format</returns>
        public float ReadFloat()
        {
            return UR.ReadSingle();
        }

        #region Macros
        // Macros
        /// <summary>
        /// overidden: Reads a byte
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read byte</returns>
        public new byte ReadByte()
        {
#if DEBUG || BINARYMETADATA
            LastPosition = Position;
#endif
            return UR.ReadByte();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 16bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read ushort</returns>
        public ushort ReadUShort()
        {
            return UR.ReadUInt16();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 32bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read uint</returns>
        public uint ReadUInt32()
        {
            return UR.ReadUInt32();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 64bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read ulong</returns>
        public ulong ReadUInt64()
        {
            return UR.ReadUInt64();
        }

        /// <summary>
        /// Reads a Signed Integer of 16bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read int</returns>
        public short ReadInt16()
        {
            return UR.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return UR.ReadUInt16();
        }

        /// <summary>
        /// Reads a Signed Integer of 32bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read int</returns>
        public int ReadInt32()
        {
            return UR.ReadInt32();
        }

        /// <summary>
        /// Reads a Signed Integer of 64bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read long</returns>
        public long ReadInt64()
        {
            return UR.ReadInt64();
        }

        /// <summary>
        /// Reads a Name/String with no known size, expecting that the next bytes are the size of the string.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read string without the end \0 char</returns>
        public string ReadText()
        {
            return UR.ReadText();
        }

        /// <summary>
        /// Reads a string with no known length, ends when the first \0 char is reached.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read string</returns>
        public string ReadASCIIString()
        {
            return UR.ReadAnsi();
        }

        /// <summary>
        /// Unreal Engine 1 and 2
        /// Compact indices exist so that small numbers can be stored efficiently.
        /// An index named "Index" is stored as a series of 1-5 consecutive bytes.
        ///
        /// Unreal Engine 3
        /// The index is based on a Int32
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read index</returns>
        public int ReadIndex()
        {
            return UR.ReadIndex();
        }

        /// <summary>
        /// Same as ReadIndex, just a placeholder for UE3 compatibly.
        /// </summary>
        /// <returns></returns>
        public int ReadObjectIndex()
        {
            return UR.ReadIndex();
        }

        public UObject ReadObject()
        {
            return Package.GetIndexObject( ReadObjectIndex() );
        }

        public UObject ParseObject( int index )
        {
            return Package.GetIndexObject( index );
        }

        /// <summary>
        /// Same as ReadIndex except this one handles differently if the version is something above UE3.
        /// </summary>
        /// <returns>The read 64bit index casted to a 32bit index.</returns>
        public int ReadNameIndex()
        {
            return (int)UR.ReadNameIndex();
        }

        public UName ReadNameReference()
        {
            return new UName( this );
        }

        public string ParseName( int index )
        {
            return Package.GetIndexName( index );
        }

        /// <summary>
        /// Same as ReadIndex except this one handles differently if the version is something above UE3.
        /// </summary>
        /// <returns>The read 64bit index casted to a 32bit index.</returns>
        public int ReadNameIndex( out int num )
        {
            return UR.ReadNameIndex( out num );
        }

        /// <summary>
        /// Reads a Guid of type A-B-C-D.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read guid</returns>
        public string ReadGuid()
        {
            return UR.ReadGuid();
        }
        #endregion

        /// <summary>
        /// Skip a amount of bytes.
        /// </summary>
        public void Skip( int bytes )
        {
            Position += bytes;
        }

        protected override void Dispose( bool disposing )
        {
            if( !disposing )
                return;

            UR = null;
            UW = null;
        }
    }

    public class UObjectStream : MemoryStream, IUnrealStream
    {
        public string Name{ get{ return Package.Stream.Name; } }
        /// <summary>
        /// The package I am streaming for.
        /// </summary>
        public UnrealPackage Package{ get; private set; }

        /// <inheritdoc/>
        public uint Version
        {
            get { return Package != null ? Package.Version : 0; }
        }

        public UnrealReader UR{ get; private set; }
        public UnrealWriter UW{ get; private set; }

        private long _PeekStartPosition;
        public long LastPosition{ get; set; }
        public bool BigEndianCode { get { return Package.Stream.BigEndianCode; } }

        public UObjectStream( IUnrealStream stream )
        {
            UW = null;
            UR = null;
            Package = stream.Package;
            InitBuffer();
        }

        public UObjectStream( IUnrealStream str, byte[] buffer ) : base( buffer, true )
        {
            UW = null;
            UR = null;
            Package = str.Package;
            InitBuffer();
        }

        private void InitBuffer()
        {
            if( CanRead && UR == null )
            {
                UR = new UnrealReader( this );
            }

            if( CanWrite && UW == null )
            {
                UW = new UnrealWriter( this );
            }
        }

        public override int Read( byte[] array, int offset, int count )
        {
#if DEBUG || BINARYMETADATA
            LastPosition = Position;
#endif
            int r = base.Read( array, offset, count );
            if( BigEndianCode && r > 1 )
            {
                Array.Reverse( array, 0, r );
            }
            return r;
        }

        /// <summary>
        /// Reads a float
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>The read float</returns>
        public float ReadFloat()
        {
            return UR.ReadSingle();
        }

        #region Macros
        // Macros
        /// <summary>
        /// overidden: Reads a byte
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read byte</returns>
        public new byte ReadByte()
        {
#if DEBUG || BINARYMETADATA
            LastPosition = Position;
#endif
            return UR.ReadByte();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 16bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read ushort</returns>
        public ushort ReadUShort()
        {
            return UR.ReadUInt16();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 32bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read uint</returns>
        public uint ReadUInt32()
        {
            return UR.ReadUInt32();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 64bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read ulong</returns>
        public ulong ReadUInt64()
        {
            return UR.ReadUInt64();
        }

        /// <summary>
        /// Reads a Signed Integer of 16bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read int</returns>
        public short ReadInt16()
        {
            return UR.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return UR.ReadUInt16();
        }

        /// <summary>
        /// Reads a Signed Integer of 32bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read int</returns>
        public int ReadInt32()
        {
            return UR.ReadInt32();
        }

        /// <summary>
        /// Reads a Signed Integer of 64bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read long</returns>
        public long ReadInt64()
        {
            return UR.ReadInt64();
        }

        /// <summary>
        /// Reads a Name/String with no known size, expecting that the next bytes are the size of the string.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read string without the end \0 char</returns>
        public string ReadText()
        {
            return UR.ReadText();
        }

        /// <summary>
        /// Reads a string with no known length, ends when the first \0 char is reached.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read string</returns>
        public string ReadASCIIString()
        {
            return UR.ReadAnsi();
        }

        /// <summary>
        /// Unreal Engine 1 and 2
        /// Compact indices exist so that small numbers can be stored efficiently.
        /// An index named "Index" is stored as a series of 1-5 consecutive bytes.
        ///
        /// Unreal Engine 3
        /// The index is based on a Int32
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read index</returns>
        public int ReadIndex()
        {
            return UR.ReadIndex();
        }

        /// <summary>
        /// Same as ReadIndex, just a placeholder for UE3 compatibly.
        /// </summary>
        /// <returns></returns>
        public int ReadObjectIndex()
        {
            return UR.ReadIndex();
        }

        public UObject ReadObject()
        {
            return Package.GetIndexObject( ReadObjectIndex() );
        }

        public UObject ParseObject( int index )
        {
            return Package.GetIndexObject( index );
        }

        /// <summary>
        /// Same as ReadIndex except this one handles differently if the version is something above UE3.
        /// </summary>
        /// <returns>The read 64bit index casted to a 32bit index.</returns>
        public int ReadNameIndex()
        {
            return (int)UR.ReadNameIndex();
        }

        public UName ReadNameReference()
        {
            return new UName( this );
        }

        public string ParseName( int index )
        {
            return Package.GetIndexName( index );
        }

        /// <summary>
        /// Same as ReadIndex except this one handles differently if the version is something above UE3.
        /// </summary>
        /// <returns>The read 64bit index casted to a 32bit index.</returns>
        public int ReadNameIndex( out int num )
        {
            return UR.ReadNameIndex( out num );
        }

        /// <summary>
        /// Reads a Guid of type A-B-C-D.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read guid</returns>
        public string ReadGuid()
        {
            return UR.ReadGuid();
        }
        #endregion

        /// <summary>
        /// Skip a amount of bytes.
        /// </summary>
        /// <param name="bytes">The amount of bytes to skip.</param>
        public void Skip( int bytes )
        {
            Position += bytes;
        }

        /// <summary>
        /// Start peeking, without advancing the stream position.
        /// </summary>
        public void StartPeek()
        {
            _PeekStartPosition = Position;
        }

        /// <summary>
        /// Stop peeking, the original position is restored.
        /// </summary>
        public void EndPeek()
        {
            Position = _PeekStartPosition;
        }

        internal void DisposeBuffer()
        {
            if( UR != null )
            {
                UR.Dispose();
                UR = null;
            }

            if( UW != null )
            {
               UW.Dispose();
               UW = null;
            }

            // Closed by the above, but this is just for ensurance.
            Dispose();
        }
    }

    /// <summary>
    /// Methods that shouldn't be duplicated between UObjectStream and UPackageStream.
    /// </summary>
    public static class UnrealStreamImplementations
    {
        public static string ReadName( this IUnrealStream stream )
        {
            int num;
            var name = stream.Package.GetIndexName( stream.ReadNameIndex( out num ) );
            if( num > UName.Numeric )
            {
                name += "_" + num;
            }
            return name;
        }

        public static void Write( this IUnrealStream stream, UName name )
        {
            name.Serialize( stream );
        }

        public static void Write( this IUnrealStream stream, UObjectTableItem obj )
        {
            stream.UW.WriteIndex( obj != null ? (int)obj.Object : 0 );
        }

        public static void Write( this IUnrealStream stream, UObject obj )
        {
            stream.UW.WriteIndex( obj != null ? (int)obj : 0 );
        }

        public static void Write( this IUnrealStream stream, short number )
        {
            stream.UW.Write( number );
        }

        public static void Write( this IUnrealStream stream, ushort number )
        {
            stream.UW.Write( number );
        }

        public static void Write( this IUnrealStream stream, int number )
        {
            stream.UW.Write( number );
        }

        public static void Write( this IUnrealStream stream, uint number )
        {
            stream.UW.Write( number );
        }

        public static void Write( this IUnrealStream stream, long number )
        {
            stream.UW.Write( number );
        }

        public static void Write( this IUnrealStream stream, ulong number )
        {
            stream.UW.Write( number );
        }

        public static void Write( this IUnrealStream stream, byte[] buffer, int index, int count )
        {
            stream.UW.Write( buffer, index, count );
        }

        public static void WriteIndex( this IUnrealStream stream, int index )
        {
            stream.UW.WriteIndex( index );
        }

        // Don't overload Write() because this string writes using the Unreal instead of the .NET format.
        public static void WriteString( this IUnrealStream stream, string s )
        {
            stream.UW.WriteString( s );
        }
    }
}