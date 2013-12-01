using System;
using UELib.Core;

namespace UELib
{
	public class UnrealMod : IUnrealDeserializableClass
	{
		public const uint Signature = 0x9FE3C5A3;

		public struct FileSummary : IUnrealDeserializableClass
		{
			public uint FileTableOffset;
			public uint FileSize;
			public uint Version;
			public uint CRC32;

			public void Deserialize( IUnrealStream stream )
			{
				FileTableOffset = stream.ReadUInt32();
				FileSize = stream.ReadUInt32();
				Version = stream.ReadUInt32();
				CRC32 = stream.ReadUInt32();
			}
		}

		public FileSummary Summary;

		// Table values are not initialized!
		public class FileTable : UTableItem, IUnrealSerializableClass
		{
			public string FileName;
			public uint SerialOffset;
			public uint SerialSize;
			public uint FileFlags;

			/*[Flags]
			public enum Flags : uint
			{
				NoSystem = 0x03
			}*/

            public void Serialize( IUnrealStream stream )
            {
                throw new NotImplementedException();
            }

			public void Deserialize( IUnrealStream stream )
			{
				FileName = stream.ReadText();
				SerialOffset = (uint)stream.ReadIndex();
				SerialSize = (uint)stream.ReadIndex();
				FileFlags = stream.ReadUInt32();
			}
		}

		public UArray<FileTable> FileTableList;

		public void Deserialize( IUnrealStream stream )
		{
			if( stream.ReadUInt32() != Signature )
			{
				throw new System.IO.FileLoadException( stream + " isn't a UnrealMod file!" );
			}

			Summary = new FileSummary();
			Summary.Deserialize( stream );

			stream.Seek( Summary.FileTableOffset, System.IO.SeekOrigin.Begin );
			FileTableList = new UArray<FileTable>( stream );	
		}
	}
}
