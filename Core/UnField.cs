using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal field. 
	/// </summary>
	public partial class UField : UObject
	{
		#region Serialized Members
		/// <summary>
		/// Index to the parent UField object.
		/// </summary>
		protected int _SuperIndex
		{
			get;
			private set;
		}

		/// <summary>
		/// Index to the next UField object.
		/// </summary>
		protected int _NextIndex
		{
			get;
			private set;
		}
		#endregion

		#region PostInitialized Members
		// UField???
		protected UField _Super = null;
		public UField Super
		{
			get{ return _Super; }
		}

		protected UField _NextField = null;
		public UField NextField
		{
			get{ return _NextField; }
		}

		/// <summary>
		/// Initialized by the UMetaData object,
		/// This Meta contains comments and other meta related info that belongs to this instance.
		/// 
		/// UE3 Only!
		/// </summary>
		public UMetaData.UMetaField Meta = null;
		#endregion

		/// <summary>
		///	Creates a new instance of the UELib.Core.UField class. 
		/// </summary>
		public UField(){}

		// REMINDER:
		//	Other:
		//		Class:SuperIndex->NextIndex
		//		Const:NoneIndex->SuperIndex->NextIndex
		//	UE3:756
		//		Class:ObjectIndex->NextIndex->SuperIndex
		//		Const:ObjectIndex->NoneIndex->NextIndex
		//	Conclusion:
		//		SuperIndex was moved from UField to UStruct
		protected override void Deserialize()
		{
			base.Deserialize();

			// UDK_09_2010?

			// _SuperIndex got moved into UStruct since 700+
			if( _Buffer.Version < 756 )
			{
				// Index to the parent UField object; if any
				_SuperIndex = _Buffer.ReadObjectIndex();
				NoteRead( "_SuperIndex", _SuperIndex );

				// Index to the next UField object; if any
				_NextIndex = _Buffer.ReadObjectIndex();
				NoteRead( "_NextIndex", _NextIndex );
			}
			else
			{
				// Index to the next UField object; if any
				_NextIndex = _Buffer.ReadObjectIndex();
				NoteRead( "_NextIndex", _NextIndex );

				// Should actually resist in UStruct
				if( this is UStruct )
				{
					// Index to the parent UField object; if any
					_SuperIndex = _Buffer.ReadObjectIndex();
					NoteRead( "_SuperIndex", _SuperIndex );
				}
			}

			// PROT: Solution to Hex-Edit protections that are done by changing the NextIndex value to an unlogic object in Classes.
			try
			{
				// PostInitialize exception; these objects are needed at PostInitialize() so it is really important to link them here rather than in PostInitialize.
				if( _SuperIndex != 0 )
				{
					_Super = (UField)GetIndexObject( _SuperIndex );
				}

				if( _NextIndex != 0 )
				{
					_NextField = (UField)GetIndexObject( _NextIndex );
				}
			}
			catch{}
		}

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
	}
}
