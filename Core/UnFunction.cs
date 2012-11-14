using System.Collections.Generic;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal function. 
	/// </summary>
	public partial class UFunction : UStruct
	{
		#region Serialized Members
		public ushort NativeToken
		{
			get;
			private set;
		}

		public byte OperPrecedence
		{
			get;
			private set;
		}

		/// <value>
		/// 32bit in UE2
		/// 64bit in UE3
		/// </value>
		internal ulong FunctionFlags
		{
			get;
			private set;
		}

		public ushort RepOffset
		{
			get;
			private set;
		}
		#endregion

		#region PostInitialized Members
		// Children
		protected List<UProperty> _ChildLocals 		= new List<UProperty>();
		public List<UProperty> ChildLocals
		{
			get{ return _ChildLocals; }
		}

		protected List<UProperty>	_ChildParams 	= new List<UProperty>();
		public List<UProperty> ChildParams
		{
			get{ return _ChildParams; }
		}

		public UProperty ReturnProperty{ get; private set; }

		#endregion

		public string FriendlyName
		{
			get{ return FriendlyNameIndex > -1 ? Package.GetIndexName( FriendlyNameIndex ) : Name; }
		}

		/// <summary>
		///	Creates a new instance of the UELib.Core.UFunction class. 
		/// </summary>
		public UFunction(){}

		protected override void Deserialize()
		{
#if BORDERLANDS2
			if( Package.Build == UnrealPackage.GameBuild.ID.Borderlands2 )
			{
				var size = _Buffer.ReadUShort();
				_Buffer.Skip( size * 2 );
			}
#endif

			base.Deserialize();

			/*if( Package.Version > 63 )
			{
			}*/

			NativeToken = _Buffer.ReadUShort();
			NoteRead( "NativeToken", NativeToken );
			OperPrecedence = _Buffer.ReadByte();
			NoteRead( "OperPrecedence", OperPrecedence );
			if( Package.Version < 69 )
			{
				_Buffer.Skip( 5 );

				// ParmsSize, iNative, NumParms, OperPrecedence, ReturnValueOffset, FunctionFlags
			}

			FunctionFlags = _Buffer.ReadUInt32();
			NoteRead( "FunctionFlags", FunctionFlags );
			if( HasFunctionFlag( Flags.FunctionFlags.Net ) )
			{
				RepOffset = _Buffer.ReadUShort();
				NoteRead( "RepOffset", RepOffset );
			}

			// UStruct::FriendlyName is moved to UFunction in UE3
			if( Package.Version >= 189 && !Package.IsConsoleCooked() )
			{
				FriendlyNameIndex = _Buffer.ReadNameIndex();
				NoteRead( "FriendlyNameIndex", FriendlyNameIndex );
			}
		}

		protected override void FindChildren()
		{
			base.FindChildren();
			foreach( var property in _ChildProperties )
			{
				if( property.HasPropertyFlag( Flags.PropertyFlagsLO.ReturnParm ) )
				{
					ReturnProperty = property;
					continue;																																  
				}

				if( property.IsParm() )
				{
					_ChildParams.Add( property );
				}
				else
				{
					_ChildLocals.Add( property );
				}
			}
		}

		#region Methods
		public bool HasFunctionFlag( Flags.FunctionFlags flag )
		{
			return ((uint)FunctionFlags & (uint)flag) != 0; 
		}

		public bool IsOperator()
		{
			return HasFunctionFlag( Flags.FunctionFlags.Operator );
		}

		public bool IsPost()
		{
			return IsOperator() && OperPrecedence == 0;
		}

		public bool IsPre()
		{
			return IsOperator() && HasFunctionFlag( Flags.FunctionFlags.PreOperator );
		}
		#endregion
	}
}
