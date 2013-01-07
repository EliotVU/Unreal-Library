using System;

namespace UELib.Core
{
	[UnrealRegisterClass]
	public partial class UTextBuffer : UObject
	{
		#region Serialized Members
		protected uint _Top;
		protected uint _Pos;
		public string ScriptText = String.Empty;
		#endregion

		#region Constructors
		public UTextBuffer()
		{
			ShouldDeserializeOnDemand = true;
		}

		protected override void Deserialize()
		{
			base.Deserialize();
			_Top = _Buffer.ReadUInt32();
			_Pos = _Buffer.ReadUInt32();

#if THIEFDEADLYSHADOWS
			if( Package.Build == UnrealPackage.GameBuild.BuildName.Thief_DS )
			{
				// TODO: Unknown
				_Buffer.Skip( 8 );
			}
#endif
			ScriptText = _Buffer.ReadText();
		}
		#endregion
	}
}