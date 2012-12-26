using System;
using System.Linq;

namespace UELib
{	
	using Core;

	public class CompressedChunk : IUnrealDeserializableClass
	{
		public int UncompressedOffset;
 		public int UncompressedSize;
		public int CompressedOffset;
		public int CompressedSize;
		private CompressedChunkHeader _Header;

		public void Deserialize( IUnrealStream stream )
		{
			UncompressedOffset = stream.ReadInt32();
			UncompressedSize = stream.ReadInt32();
			CompressedOffset = stream.ReadInt32();
			CompressedSize = stream.ReadInt32();
		}

		public void Decompress( UPackageStream inStream, UPackageStream outStream )
		{
			inStream.Seek( CompressedOffset, System.IO.SeekOrigin.Begin );
			_Header.Deserialize( inStream );

			outStream.Seek( UncompressedOffset, System.IO.SeekOrigin.Begin );
			foreach( var buffer in _Header.Blocks.Select( block => block.Decompress() ) )
			{
				outStream.Write( buffer, 0, buffer.Length );
			}
		}

		public struct CompressedChunkHeader : IUnrealDeserializableClass
		{
			private uint _Signature;
			private int _BlockSize;
			private int _CompressedSize;
			private int _UncompressedSize;

			public UArray<CompressedChunkBlock> Blocks;

			public void Deserialize( IUnrealStream stream )
			{
				_Signature = stream.ReadUInt32();
				if( _Signature != UnrealPackage.Signature )
				{
					throw new System.IO.FileLoadException( "Unrecognized signature!" );
				}
				_BlockSize = stream.ReadInt32();
				_CompressedSize = stream.ReadInt32();
				_UncompressedSize = stream.ReadInt32();

				int blockCount = (int)Math.Ceiling( _UncompressedSize / (float)_BlockSize );
				Blocks = new UArray<CompressedChunkBlock>( stream, blockCount );
			}

			public struct CompressedChunkBlock : IUnrealDeserializableClass
			{
				private int _CompressedSize;
				private int _UncompressedSize;
				private byte[] _CompressedData;

				public void Deserialize( IUnrealStream stream )
				{
					_CompressedSize = stream.ReadInt32();
					_UncompressedSize = stream.ReadInt32();

					_CompressedData = new byte[_CompressedSize];
					stream.Read( _CompressedData, 0, _CompressedSize ); 
				}

				public byte[] Decompress()
				{
					var decompressedData = new byte[_UncompressedSize];
					//ManagedLZO.MiniLZO.Decompress( CompressedData, DecompressedData );
					return decompressedData;
				}
			}
		}

		public bool IsChunked( long offset )
		{
			return offset >= UncompressedOffset && offset < UncompressedOffset + UncompressedSize;
		}
	}
}
