using System;
using System.Drawing;
using UELib.Core;
	
namespace UELib.Engine
{
	[UnrealRegisterClass]
	public class UTexture : UObject, IUnrealViewable
	{
		protected UDefaultProperty _Format;

		public UArray<MipMap> MipMaps{ get; private set; }

		public UTexture()
		{
			ShouldDeserializeOnDemand = true;
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			_Format = Properties.Find( "Format" );
			MipMaps = new UArray<MipMap>();
			MipMaps.Deserialize( _Buffer, delegate( MipMap mm ){ mm.Owner = this; } );
		}

		public class MipMap : IUnrealSerializableClass
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

            public void Serialize( IUnrealStream stream )
            {
                throw new NotImplementedException();
            }

			public void Deserialize( IUnrealStream stream )
			{
				if( stream.Version >= 63 )
				{
					// Offset to (Width = ...)
					WidthOffset = stream.ReadUInt32();

					long opos = stream.Position;
					stream.Seek( WidthOffset, System.IO.SeekOrigin.Begin );
					Width = stream.ReadUInt32();
					Height = stream.ReadUInt32();
					stream.Seek( opos, System.IO.SeekOrigin.Begin );
				}

				int mipMapSize = stream.ReadIndex();
				Pixels = new int[mipMapSize];
				switch( Owner._Format.Decompile().Substring( 6 ) )
				{
					case "TEXF_RGBA8": case "5":
						for( int i = 0; i < mipMapSize; ++ i )
						{				
	  						Pixels[i] = stream.ReadInt32();
						}
						break;

					case "TEXF_DXT1": case "3":
						for( int i = 0; i < mipMapSize / 2; ++ i )
						{		
							byte c = stream.ReadByte();
							Pixels[i ++] = c & 0xF0;
							Pixels[i] = c & 0x0F; 
						}

						// PostProcess:
						// 4x4 4bit per pixel, 16bit per color: 5bits red; 6bits green; 5bits blue.
						// 
						break;
				}

				// Width, Height. See above!
				stream.Skip( 8 );
				BitsWidth = stream.ReadByte();
				BitsHeight = stream.ReadByte();
			}
		}
	}

	[UnrealRegisterClass]
	public class UPalette : UObject, IUnrealViewable
	{
		private Color[] _ColorPalette;

		public UPalette()
		{
			ShouldDeserializeOnDemand = true;
		}

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