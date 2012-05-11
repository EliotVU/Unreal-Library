//#define PUBLICRELEASE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UELib
{
	/*public static class UStreamExt
	{
		public static int ReadNameIndex( this IUnrealStream stream )
		{
			if( stream.Version > UnrealPackage.VNameIndex )
			{
				int index = stream.UR.ReadInt32();
				stream.Skip( 4 );	// Unknown meaning.
				return index;
			}
			return stream.UR.ReadIndex();
		}
	}*/

	public interface IUnrealStream
	{
		UnrealPackage Package{ get; }
		UnrealReader UR{ get; }
		UnrealWriter UW{ get; }

		/// <summary>
		/// The version of the package this stream is working for.
		/// </summary>
		uint Version{ get; }

		/// <summary>
		/// Reads the next bytes as characters either in ASCII or Unicode with prefix string length.
		/// </summary>
		/// <returns></returns>
		string ReadName();

		/// <summary>
		/// Reads the next bytes as a index to an Object.
		/// </summary>
		/// <returns></returns>
		int ReadObjectIndex();

		/// <summary>
		/// Reads the next bytes as a index to an NameTable.
		/// </summary>
		/// <returns></returns>
		int ReadNameIndex();

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
		/// Reads the next 4 bytes as a ABCD guid converted to a string.
		/// </summary>
		/// <returns></returns>
		string ReadGuid();

		/// <summary>
		/// Reads the next 4 bytes as a float converted to an Unreal float format.
		/// </summary>
		/// <returns></returns>
		string ReadUFloat();

		byte ReadByte();
		ushort ReadUShort();

		int ReadInt32();
		uint ReadUInt32();

		long ReadInt64();
		ulong ReadUInt64();

		void Skip( int bytes );
		void StartPeek();
		void StartPeek( long peekPosition );
		void EndPeek();

		// Stream
		long Length{ get; }
		long Position{ get; set; }
		int Read( byte[] array, int offset, int count );
		long Seek( long offset, SeekOrigin origin );
	}	  

	public class UnrealWriter : BinaryWriter
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes" )]
		protected readonly IUnrealStream _UnrealStream;

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
		private Encoding MyEncoding;

		private uint _Version
		{
			get{ return _UnrealStream.Version; }
		}

		public UnrealReader( Stream stream, Encoding enc ) : base( stream, enc )
		{
			_UnrealStream = stream as IUnrealStream;
			MyEncoding = enc;
		}

		public string ReadName( int size )
		{
			// Note size 0 is ignored!
			if( size > 0 ) // ASCII	 	
			{
				if( size > 1000000000 )
				{
					System.Diagnostics.Debug.Assert( false, "Dangerous string size detected! IT'S OVER 9000 THOUSAND!" );
				}

				// Read the name without the end char
				var strBytes = new byte[size - 1];
				Read( strBytes, 0, size - 1 );

				// Read end char but do nothing with it
				++ BaseStream.Position;
				//BinReader.ReadByte();

				// Convert Byte Str to a String type
				if( MyEncoding == Encoding.BigEndianUnicode )
				{
					Array.Reverse( strBytes );
				}
				return Encoding.ASCII.GetString( strBytes );
			}
			#if !PUBLICRELEASE
			if( size < 0 ) // Unicode
			{
				if( -size > 1000000000 )
				{
					System.Diagnostics.Debug.Assert( false, "Dangerous string size detected! IT'S OVER 9000 THOUSAND!" );
				}
				// Read the name without the end char
				var strBytes = new byte[(-size * 2) - 2];
				Read( strBytes, 0, (-size * 2) - 2 );

				// Read end char but do nothing with it
				BaseStream.Position += 2;

				// Convert Byte Str to a String type
				if( MyEncoding == Encoding.BigEndianUnicode )
				{
					Array.Reverse( strBytes );
				}
				return Encoding.Unicode.GetString( strBytes );
			}
#endif
			return String.Empty;
		}

		public string ReadName()
		{
			if( _Version < UnrealPackage.VSizePrefixDeprecated )
			{
				return ReadASCIIString();
			}

			int size =
			#if !PUBLICRELEASE
				ReadIndex();
			#else 
				ReadByte();
			#endif

			return ReadName( size );
		}

		public string ReadASCIIString()
		{
			var strBytes = new List<byte>();
			while( true )
			{
				byte BYTE = ReadByte();
				if( BYTE != '\0' )
				{
					strBytes.Add( BYTE );
					continue;
				}
				break;
			}
			string s = Encoding.ASCII.GetString( strBytes.ToArray() );
			if( MyEncoding == Encoding.BigEndianUnicode )
			{
				s.Reverse();
			}
			return s;
		}

		public string ReadUnicodeString()
		{
			var strBytes = new List<byte>();
			while( true )
			{
				ushort w = ReadUInt16();
				if( w != 0 )
				{
					strBytes.Add( (byte)(w & 0xFF00) );
					strBytes.Add( (byte)(w & 0x00FF) );
					continue;
				}
				break;
			}
			string s = Encoding.Unicode.GetString( strBytes.ToArray() );
			if( MyEncoding == Encoding.BigEndianUnicode )
			{
				s.Reverse();
			}
			return s;
		}

		public int ReadIndex()
		{
			if( _Version >= UnrealPackage.VIndexDeprecated )
			{
				return ReadInt32();
			}

			int index = 0;
			byte b0 = ReadByte();
			if( (b0 & 0x40) != 0 )
			{
				byte b1 = ReadByte();
				if( (b1 & 0x80) != 0 )
				{
					byte b2 = ReadByte();
					if( (b2 & 0x80) != 0 )
					{
						byte b3 = ReadByte();
						if( (b3 & 0x80) != 0 )
						{
							byte b4 = ReadByte();
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

		public long ReadNameIndex()
		{
			if( _Version >= 343 )
			{
				//var index = ReadInt64();	  // BigEndian?
				var index = ReadInt32();
				var num = ReadInt32();
				return (long)index | ((long)num << 32);		
			}
			return ReadIndex();
		}

		public static int ReadIndexFromBuffer( byte[] bytes, UnrealPackage package )
		{
			if( package.Version >= UELib.UnrealPackage.VIndexDeprecated )
			{
				return BitConverter.ToInt32( bytes, 0 );
			}

			int index = 0;
			byte b0 = bytes[0];
			if( (b0 & 0x40) != 0 )
			{
				byte b1 = bytes[1];
				if( (b1 & 0x80) != 0 )
				{
					byte b2 = bytes[2];
					if( (b2 & 0x80) != 0 )
					{
						byte b3 = bytes[3];
						if( (b3 & 0x80) != 0 )
						{
							byte b4 = bytes[4];
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

		public ulong ReadQWORDFlags()
		{
			if( _Version >= 195 )   // This isn't true for all flags, just ObjectFlags, thus not ClassFlags etc!
			{
				ulong flags = ReadUInt64();
				//ulong flags = BitConverter.ToUInt64(BitConverter.GetBytes( ReadUInt64() ).Reverse().ToArray(), 0);
				//flags = ((uint)(flags & 0x00000000FFFFFFFFU) << 32 ) | (uint)((flags & 0xFFFFFFFF00000000U) >> 32);
				return flags;
			}
			else
			{
				ulong flags = ReadUInt32();
				return flags;
			}
		}

		public string ReadGuid()
		{
			// A, B, C, D
			var GUIDBuffer = new byte[16];
			Read( GUIDBuffer, 0, 16 );
			Guid G = new Guid( GUIDBuffer );
			return G.ToString();
		}
	}

	public class UPackageStream : FileStream, IUnrealStream
	{
		/// <summary>
		/// The package I am streaming for.
		/// </summary>
		public UnrealPackage Package{ get; set; }

		/// <inheritdoc/>
		public uint Version
		{
			get{ return Package != null ? Package.Version : (uint)0; }
		}

		protected UnrealReader _UR = null;
		public UnrealReader UR
		{
			get{ return _UR; }
		}

		protected UnrealWriter _UW = null;
		public UnrealWriter UW
		{
			get{ return _UW; }
		}

		private long _PeekStartPosition = 0;

		internal bool _BigEndianCode = false;

		public UPackageStream( string path, FileMode mode, FileAccess access ) : base( path, mode, access )
		{
			if( CanRead && mode == FileMode.Open )
			{
				byte[] bytes = new byte[4];
				this.Read( bytes, 0, 4 );
				uint sig = BitConverter.ToUInt32( bytes, 0 );
				if( sig == UnrealPackage.Signature_BigEndian )
				{
					Console.WriteLine( "\t\tEncoding:BigEndian" );
					_BigEndianCode = true;
				}

				if( sig != UnrealPackage.Signature && sig != UnrealPackage.Signature_BigEndian )
				{
					throw new System.IO.FileLoadException( path + " isn't a UnrealPackage file!" );
				}
				Position = 4;
				_UR = new UnrealReader( this, _BigEndianCode ? Encoding.BigEndianUnicode : Encoding.Unicode );		
			}

			if(	CanWrite )
			{
				_UW = new UnrealWriter( this );
			}
		}

		public override int Read( byte[] array, int offset, int count )
		{
			int r = base.Read( array, offset, count ); 
			if( _BigEndianCode && r > 1 )
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
		public string ReadUFloat()
		{
			return String.Format( "{0:f}", _UR.ReadSingle() ).Replace( ',', '.' );
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
			return _UR.ReadByte();
		}

		/// <summary>
		/// Reads a Unsigned Integer of 16bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read ushort</returns>
		public ushort ReadUShort()
		{
			return _UR.ReadUInt16();
		}

		/// <summary>
		/// Reads a Unsigned Integer of 32bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read uint</returns>
		public uint ReadUInt32()
		{
			return _UR.ReadUInt32();
		}

		/// <summary>
		/// Reads a Unsigned Integer of 64bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read ulong</returns>
		public ulong ReadUInt64()
		{
			return _UR.ReadUInt64();
		}

		/// <summary>
		/// Reads a Signed Integer of 32bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read int</returns>
		public int ReadInt32()
		{
			return _UR.ReadInt32();
		}

		/// <summary>
		/// Reads a Signed Integer of 64bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read long</returns>
		public long ReadInt64()
		{
			return _UR.ReadInt64();
		}

		/// <summary>
		/// Reads a Name/String with no known size, expecting that the next bytes are the size of the string.
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read string without the end \0 char</returns>
		public string ReadName()
		{
			return _UR.ReadName();
		}

		/// <summary>
		///	Reads a string with no known length, ends when the first \0 char is reached.
		///	
		/// Advances the position.
		/// </summary>
		/// <returns>the read string</returns>
		public string ReadASCIIString()
		{
			return _UR.ReadASCIIString();
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
			return _UR.ReadIndex();
		}

		/// <summary>
		/// Same as ReadIndex, just a placeholder for UE3 compatibly.
		/// </summary>
		/// <returns></returns>
		public int ReadObjectIndex()
		{
			return _UR.ReadIndex();
		}

		/// <summary>
		/// Same as ReadIndex except this one handles differently if the version is something above UE3.
		/// </summary>
		/// <returns>The read 64bit index casted to a 32bit index.</returns>
		public int ReadNameIndex()
		{
			return (int)_UR.ReadNameIndex();
		}

		/// <summary>
		/// Same as ReadIndex except this one handles differently if the version is something above UE3.
		/// </summary>
		/// <returns>The read 64bit index casted to a 32bit index.</returns>
		public int ReadNameIndex( out int num )
		{
			var index = _UR.ReadNameIndex();
			if( Version >= 343 )
			{
				num = (int)((ulong)(index) & 0xFFFFFFFF00000000) - 1;
			}

			num = -1;
			return (int)index;
		}

		/// <summary>
		///	Reads a Guid of type A-B-C-D.
		///	
		/// Advances the position.
		/// </summary>
		/// <returns>the read guid</returns>
		public string ReadGuid()
		{
			return _UR.ReadGuid();
		}
		#endregion

		/// <summary>
		///	Skip a amount of bytes.
		/// </summary>
		public void Skip( int num )
		{
			Position += num;
		}

		/// <summary>
		/// Start peeking, without advancing the stream position.
		/// </summary>
		public void StartPeek()
		{
			_PeekStartPosition = Position;
		}

		/// <summary>
		/// Start peeking, without advancing the stream position and start at a new position.
		/// </summary>
		public void StartPeek( long peekPosition )
		{
			_PeekStartPosition = Position;
			Position = peekPosition;
		}

		/// <summary>
		/// Stop peeking, the original position is restored.
		/// </summary>
		public void EndPeek()
		{
			Position = _PeekStartPosition;
		}	

		public bool Chunked = false;
	}

	public class UObjectStream : MemoryStream, IUnrealStream
	{
		/// <summary>
		/// The package I am streaming for.
		/// </summary>
		public UnrealPackage Package{ get; set; }

		/// <inheritdoc/>
		public uint Version
		{
			get { return Package != null ? Package.Version : (uint)0; }
		}

		private UnrealReader _UR = null;
		public UnrealReader UR
		{
			get { return _UR; }
		}

		private UnrealWriter _UW = null;
		public UnrealWriter UW
		{
			get { return _UW; }
		}

		private long _PeekStartPosition = 0;
		private bool _BigEndianCode = false;

		public UObjectStream( UPackageStream str, ref byte[] buffer ) : base( buffer )
		{
			Package = str.Package;
			_BigEndianCode = str._BigEndianCode;
			if( CanRead )
			{
				_UR = new UnrealReader( this, _BigEndianCode ? Encoding.BigEndianUnicode : Encoding.Unicode );
			}

			if( CanWrite )
			{
				_UW = new UnrealWriter( this );
			}
		}

		public override int Read( byte[] array, int offset, int count )
		{
			int r = base.Read( array, offset, count ); 
			if( _BigEndianCode && r > 1 )
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
		public string ReadUFloat()
		{
			return String.Format( "{0:f}", _UR.ReadSingle() ).Replace( ',', '.' );
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
			return _UR.ReadByte();
		}

		/// <summary>
		/// Reads a Unsigned Integer of 16bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read ushort</returns>
		public ushort ReadUShort()
		{
			return _UR.ReadUInt16();
		}

		/// <summary>
		/// Reads a Unsigned Integer of 32bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read uint</returns>
		public uint ReadUInt32()
		{
			return _UR.ReadUInt32();
		}

		/// <summary>
		/// Reads a Unsigned Integer of 64bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read ulong</returns>
		public ulong ReadUInt64()
		{
			return _UR.ReadUInt64();
		}

		/// <summary>
		/// Reads a Signed Integer of 32bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read int</returns>
		public int ReadInt32()
		{
			return _UR.ReadInt32();
		}

		/// <summary>
		/// Reads a Signed Integer of 64bits
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read long</returns>
		public long ReadInt64()
		{
			return _UR.ReadInt64();
		}

		/// <summary>
		/// Reads a Name/String with no known size, expecting that the next bytes are the size of the string.
		/// 
		/// Advances the position.
		/// </summary>
		/// <returns>the read string without the end \0 char</returns>
		public string ReadName()
		{
			return _UR.ReadName();
		}

		/// <summary>
		///	Reads a string with no known length, ends when the first \0 char is reached.
		///	
		/// Advances the position.
		/// </summary>
		/// <returns>the read string</returns>
		public string ReadASCIIString()
		{
			return _UR.ReadASCIIString();
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
			return _UR.ReadIndex();
		}

		/// <summary>
		/// Same as ReadIndex, just a placeholder for UE3 compatibly.
		/// </summary>
		/// <returns></returns>
		public int ReadObjectIndex()
		{
			return _UR.ReadIndex();
		}
		
		/// <summary>
		/// Same as ReadIndex except this one handles differently if the version is something above UE3.
		/// </summary>
		/// <returns>The read 64bit index casted to a 32bit index.</returns>
		public int ReadNameIndex()
		{
			if( Version >= 343 )
			{
				int index = _UR.ReadInt32();
				Skip( 4 );	// NAME_index if > 0
				return index;		
			}
			return _UR.ReadIndex();
		}

		/// <summary>
		/// Same as ReadIndex except this one handles differently if the version is something above UE3.
		/// </summary>
		/// <returns>The read 64bit index casted to a 32bit index.</returns>
		public int ReadNameIndex( out int num )
		{
			if( Version >= 343 )				
			{
				int index = _UR.ReadInt32();	 // Number. For example Model_1.
				num = _UR.ReadInt32()-1;
				return index;
			}

			num = -1;
			return _UR.ReadIndex();
		}

		/// <summary>
		///	Reads a Guid of type A-B-C-D.
		///	
		/// Advances the position.
		/// </summary>
		/// <returns>the read guid</returns>
		public string ReadGuid()
		{
			return _UR.ReadGuid();
		}
		#endregion

		/// <summary>
		///	Skip a amount of bytes.
		/// </summary>
		/// <param name="num">The amount of bytes to skip.</param>
		public void Skip( int num )
		{
			Position += num;
		}

		/// <summary>
		/// Start peeking, without advancing the stream position.
		/// </summary>
		public void StartPeek()
		{
			_PeekStartPosition = Position;
		}

		/// <summary>
		/// Start peeking, without advancing the stream position, and start at a new position.
		/// </summary>
		/// <param name="peekPosition">The initial start position.</param>
		public void StartPeek( long peekPosition )
		{
			_PeekStartPosition = Position;
			Position = peekPosition;
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
