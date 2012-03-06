using System.Drawing;
using UELib.Core;
	
namespace UELib.Engine
{
	public class UTexture : UContent
	{
		protected UArray<MipMap> _MipMaps = null;
		protected UDefaultProperty _Format;

		public UArray<MipMap> MipMaps
		{
			get{ return _MipMaps; }
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			_Format = Properties.FindPropertyByName( "Format" );
			_MipMaps = new UArray<MipMap>();
			_MipMaps.Deserialize( _Buffer, delegate( MipMap mm ){ mm.Owner = this; } );
		}

		public class MipMap : IUnrealDeserializableClass
		{
			public enum CompressionFormat
			{
				RGBA8,
			};

			public UTexture Owner;

			public uint WidthOffset;
			public int[] Pixels;
			public uint Width;
			public uint Height;
			public byte BitsWidth;
			public byte BitsHeight;

			public void Deserialize( IUnrealStream buffer )
			{
				if( buffer.Version >= 63 )
				{
					// Offset to (Width = ...)
					WidthOffset = buffer.ReadUInt32();

					long opos = buffer.Position;
					buffer.Seek( WidthOffset, System.IO.SeekOrigin.Begin );
					Width = buffer.ReadUInt32();
					Height = buffer.ReadUInt32();
					buffer.Seek( opos, System.IO.SeekOrigin.Begin );
				}

				int MipMapSize = buffer.ReadIndex();
				Pixels = new int[MipMapSize];
				switch( Owner._Format.Decompile().Substring( 6 ) )
				{
					case "TEXF_RGBA8": case "5":
						for( int i = 0; i < MipMapSize; ++ i )
						{				
	  						Pixels[i] = buffer.ReadInt32();
						}
						break;

					case "TEXF_DXT1": case "3":
						for( int i = 0; i < MipMapSize / 2; ++ i )
						{		
							byte c = buffer.ReadByte();
							Pixels[i ++] = c & 0xF0;
							Pixels[i] = c & 0x0F; 
						}

						// PostProcess:
						// 4x4 4bit per pixel, 16bit per color: 5bits red; 6bits green; 5bits blue.
						// 
						break;
				}

				// Width, Height. See above!
				buffer.Skip( 8 );
				BitsWidth = buffer.ReadByte();
				BitsHeight = buffer.ReadByte();
			}
		}
	}

	public class UPalette : UContent
	{
		private Color[] _ColorPalette = null;

		protected override void Deserialize()
		{
			base.Deserialize();

	   		int count = _Buffer.ReadIndex();
			if( count > 0 )
			{
				_ColorPalette = new Color[count];
				for( int i = 0; i < count; ++ i )
				{
					_ColorPalette[i] = Color.FromArgb( _Buffer.ReadInt32() );
				}
			}
		}
	}
}