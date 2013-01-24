using System;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal field. 
	/// </summary>
	public partial class UField : UObject
	{
		#region Serialized Members
		public UField Super{ get; private set; }
		public UField NextField{ get; private set; }
		#endregion

		#region Script Members
		/// <summary>
		/// Initialized by the UMetaData object,
		/// This Meta contains comments and other meta related info that belongs to this instance.
		/// </summary>
		public UMetaData.UMetaField Meta;
		#endregion

		#region Constructors
		protected override void Deserialize()
		{
			base.Deserialize();

			// _SuperIndex got moved into UStruct since 700+
			if( Package.Version < 756 
#if SPECIALFORCE2
				|| Package.Build == UnrealPackage.GameBuild.BuildName.SpecialForce2
#endif
				)
			{
				Super = GetIndexObject( _Buffer.ReadObjectIndex() ) as UField;
				Record( "Super", Super );

				NextField = GetIndexObject( _Buffer.ReadObjectIndex() ) as UField;
				Record( "NextField", NextField );
			}
			else
			{
				NextField = GetIndexObject( _Buffer.ReadObjectIndex() ) as UField;
				Record( "NextField", NextField );

				// Should actually resist in UStruct
				if( this is UStruct )
				{
					Super = GetIndexObject( _Buffer.ReadObjectIndex() ) as UField;
					Record( "Super", Super );
				}
			}
		}
		#endregion

		#region Methods
		public bool Extends( string classType )
		{
			for( var field = Super; field != null; field = field.Super )
			{
				if( String.Equals( field.Name, classType, StringComparison.OrdinalIgnoreCase ) )
				{
					return true;
				}
			}
			return false;
		}
		#endregion
	}
}
