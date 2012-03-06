using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UELib
{	
	using UELib.Core;

	public class CompressedChunk : IUnrealDeserializableClass
	{
		public int UncompressedOffset = 0;
 		public int UncompressedSize = 0;
		public int CompressedOffset = 0;
		public int CompressedSize = 0;
		public CompressedChunkHeader Header;

		public CompressedChunk()
		{
		}

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
			Header.Deserialize( inStream );

			outStream.Seek( UncompressedOffset, System.IO.SeekOrigin.Begin );
			foreach( var block in Header.Blocks	)
			{
				var buffer = block.Decompress();
				outStream.Write( buffer, 0, buffer.Length );
			}
		}

		public struct CompressedChunkHeader : IUnrealDeserializableClass
		{
			public uint Signature;
			public int BlockSize;
			public int CompressedSize;
			public int UncompressedSize;

			public UArray<CompressedChunkBlock> Blocks;

			public void Deserialize( IUnrealStream stream )
			{
				Signature = stream.ReadUInt32();
				if( Signature != UnrealPackage.Signature )
				{
					throw new System.IO.FileLoadException( "Unrecognized signature!" );
				}
				BlockSize = stream.ReadInt32();
				CompressedSize = stream.ReadInt32();
				UncompressedSize = stream.ReadInt32();

				int blockCount = (int)Math.Ceiling( (float)UncompressedSize / (float)BlockSize );
				Blocks = new UArray<CompressedChunkBlock>( stream, blockCount );
			}

			public struct CompressedChunkBlock : IUnrealDeserializableClass
			{
				public int CompressedSize;
				public int UncompressedSize;
				public byte[] CompressedData;

				public void Deserialize( IUnrealStream stream )
				{
					CompressedSize = stream.ReadInt32();
					UncompressedSize = stream.ReadInt32();

					CompressedData = new byte[CompressedSize];
					stream.Read( CompressedData, 0, CompressedSize ); 
				}

				public byte[] Decompress()
				{
					byte[] DecompressedData = new byte[UncompressedSize];
					ManagedLZO.MiniLZO.Decompress( CompressedData, DecompressedData );
					return DecompressedData;
				}
			}
		}

		public bool IsChunked( long offset )
		{
			return offset >= UncompressedOffset && offset < UncompressedOffset + UncompressedSize;
		}
	}
}
