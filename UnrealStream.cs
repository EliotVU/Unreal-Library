// WARNING: You might get a brain stroke from reading the code below :O
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UELib.Core;

namespace UELib
{
	public interface IUnrealStream
	{
		string Name{ get; }
		void Dispose();
		void Close();

		UnrealPackage Package{ get; }

		/// <summary>
		/// The version of the package this stream is working for.
		/// </summary>
		uint Version{ get; }

		/// <summary>
		/// Reads the next bytes as characters either in ASCII or Unicode with prefix string length.
		/// </summary>
		/// <returns></returns>
		string ReadString();

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
		private readonly IUnrealStream _UnrealStream;

		private uint _Version
		{
			get{ return _UnrealStream.Version; }
		}

		public UnrealWriter( Stream stream ) : base( stream )
		{
			_UnrealStream = stream as IUnrealStream;
		}

		public void WriteName( string name )
		{
			Write( name.Length );
			Write( name.ToCharArray(), 0, 0 );
			Write( '\0' );
		}
	}

	/// <summary>
	/// Wrapper for Streams with specific functions for deserializing UELib.UnrealPackage.
	/// </summary>
	public class UnrealReader : BinaryReader
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes" )]
		protected readonly IUnrealStream _UnrealStream;
		private readonly Encoding _MyEncoding;

		private uint _Version
		{
			get{ return _UnrealStream.Version; }
		}

		public UnrealReader( Stream stream, Encoding enc ) : base( stream, enc )
		{
			_UnrealStream = stream as IUnrealStream;
			_MyEncoding = enc;
		}

		public string ReadText()
		{
#if DEBUG || BINARYMETADATA
			var lastPosition = _UnrealStream.Position;
#endif
			if( _Version < UnrealPackage.VSIZEPREFIXDEPRECATED )
			{
				return ReadAnsi();
			}

			int unfixedSize; var size = (unfixedSize = 
#if BIOSHOCK 
				_UnrealStream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock ? -ReadIndex() :
#endif
				ReadIndex()) < 0 ? -unfixedSize : unfixedSize;
			System.Diagnostics.Debug.Assert( size < 1000000000, "Dangerous string size detected! IT'S OVER 9000 THOUSAND!" );
			if( unfixedSize > 0 ) // ANSI	 	
			{
				var strBytes = new byte[size - 1];
				Read( strBytes, 0, size - 1 );
				++ BaseStream.Position; // null
				if( _MyEncoding == Encoding.BigEndianUnicode )
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
				var strBytes = new byte[(size * 2) - 2];
				Read( strBytes, 0, (size * 2) - 2 );
				BaseStream.Position += 2; // null
				// Convert Byte Str to a String type
				if( _MyEncoding == Encoding.BigEndianUnicode )
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
			if( _MyEncoding == Encoding.BigEndianUnicode )
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
			if( _MyEncoding == Encoding.BigEndianUnicode )
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
			const byte proceededValue = 0xFF - isProceeded;	// 7F

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
			if( _Version >= 343 
#if BIOSHOCK
				|| _UnrealStream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock
#endif
				)
			{
				var num = ReadUInt32();
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
			if( _UnrealStream.Version >= 343 
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
	}

	public class UPackageStream : FileStream, IUnrealStream
	{
		public UnrealPackage Package{ get; set; }

		/// <inheritdoc/>
		public uint Version
		{
			get{ return Package != null ? Package.Version : 0; }
		}

		public UnrealReader UR{ get; private set; }
		public UnrealWriter UW{ get; private set; }

		public long LastPosition{ get; set; }

		public readonly bool BigEndianCode;
		public bool Chunked;

		public UPackageStream( string path, FileMode mode, FileAccess access ) : base( path, mode, access )
		{
			UR = null;
			UW = null;
			if( CanRead && mode == FileMode.Open )
			{
				var bytes = new byte[4];
				Read( bytes, 0, 4 );
				uint readSignature = BitConverter.ToUInt32( bytes, 0 );
				if( readSignature == UnrealPackage.Signature_BigEndian )
				{
					Console.WriteLine( "Encoding:BigEndian" );
					BigEndianCode = true;
				}

				if( !UnrealConfig.SuppressSignature 
					&& readSignature != UnrealPackage.Signature 
					&& readSignature != UnrealPackage.Signature_BigEndian )
				{
					throw new FileLoadException( path + " isn't a UnrealPackage file!" );
				}
				Position = 4;
				UR = new UnrealReader( this, BigEndianCode ? Encoding.BigEndianUnicode : Encoding.Unicode );		
			}

			if( CanWrite )
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
		public string ReadString()
		{
			return UR.ReadText();
		}

		/// <summary>
		///	Reads a string with no known length, ends when the first \0 char is reached.
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
		///	Reads a Guid of type A-B-C-D.
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
		///	Skip a amount of bytes.
		/// </summary>
		public void Skip( int bytes )
		{
			Position += bytes;
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

		public readonly bool BigEndianCode;
		
		public UObjectStream( UPackageStream str, ref byte[] buffer ) : base( buffer )
		{
			UW = null;
			UR = null;
			Package = str.Package;
			BigEndianCode = str.BigEndianCode;
			if( CanRead )
			{
				UR = new UnrealReader( this, BigEndianCode ? Encoding.BigEndianUnicode : Encoding.Unicode );
			}

			if( CanWrite )
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
		/// Reads a float converted to a unreal float string format.
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>The read float converted to a unreal float string format</returns>
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
		public string ReadString()
		{
			return UR.ReadText();
		}

		/// <summary>
		///	Reads a string with no known length, ends when the first \0 char is reached.
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
		///	Reads a Guid of type A-B-C-D.
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
		///	Skip a amount of bytes.
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
	}
}
