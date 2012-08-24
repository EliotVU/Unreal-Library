using UELib.Core;

namespace UELib.Engine
{
	/// <summary>
	/// Any view/extract related classes should subclass this.
	/// </summary>
	public class UContent : UObject, IUnrealViewable
	{
		public UContent()
		{
			_bDeserializeOnDemand = true;
		}

		public virtual void View()
		{
		}
	}

	// Package reference or Group
	public class UPackage : UObject
	{
	}

	public class UModel : UContent
	{
	}
	
	public class USound : UContent, IUnrealExportable
	{
		public string[] ExportableExtensions
		{ 
			get{ return new[]{"wav"}; }
		}

		protected byte[] SoundBuffer;

		public bool CompatableExport()
		{
			return Package.Version <= 129;
		}

		public void SerializeExport( string desiredExportExtension, System.IO.FileStream exportStream )
		{
			switch( desiredExportExtension )
			{
				case "wav":
					exportStream.Write( SoundBuffer, 0, SoundBuffer.Length );
					break;
			}
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			_Buffer.Skip( 9 );

			var soundSize = _Buffer.ReadIndex();
			SoundBuffer = new byte[soundSize];
			_Buffer.Read( SoundBuffer, 0, soundSize );
		}
	}
}
