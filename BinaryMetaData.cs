using System.Collections.Generic;

namespace UELib
{
	public class BinaryMetaData
	{
		public struct BinaryField : IUnrealDecompilable
		{
			public string Name;
			public object Tag;
			public long Position;
			public long Size;

			public string Decompile()
			{
				return Tag != null ? Tag.ToString() : "NULL";
			}
		}

		public readonly IList<BinaryField> Fields;

		public BinaryMetaData()
		{
			Fields = new List<BinaryField>();	
		}

		[System.Diagnostics.Conditional( "DEBUG" )]
		public void AddField( string name, object tag, long position, long size )
		{
			Fields.Add
			( 
				new BinaryField
				{
					Name = name,
					Tag = tag,
					Position = position,
					Size = size
				}
			);
		}
	}
}
