using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib;
using UELib.Core;

namespace UELib.Engine
{
	/// <summary>
	/// Unreal Font.
	/// 
	/// I was bored :D!
	/// </summary>
	public class UFont : UContent
	{
		public struct FontCharacter : IUnrealDeserializableClass
		{
			public int StartU, StartV;
			public int USize, VSize;
			byte TextureIndex;

			public void Deserialize( IUnrealStream stream )
			{
				StartU = stream.ReadInt32();
				StartV = stream.ReadInt32();

				USize = stream.ReadInt32();
				VSize = stream.ReadInt32();

				TextureIndex = (byte)stream.ReadByte();
			}
		};

		public List<FontCharacter> Characters = new List<FontCharacter>();

		public UFont()
		{
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int count = _Buffer.ReadIndex();
			for( int i = 0; i < count; ++ i )
			{
				var FC = new FontCharacter();
				FC.Deserialize( _Buffer );
				Characters.Add( FC );
			}

			// Textures

			// Kerning
			_Buffer.ReadInt32();

			// Remap

			_Buffer.UR.ReadBoolean();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic" )]
		private void SerializeCharacter()
		{

		}
	}
}
