using System.Collections.Generic;
using UELib.Flags;

namespace UELib.Core
{
	public struct ULabelEntry
	{
		public string Name;
		public int Position;
	}

	/// <summary>
	/// Represents a unreal state. 
	/// </summary>
	[UnrealRegisterClass]
	public partial class UState : UStruct
	{
		private const uint VStateFlags = 101;

		#region Serialized Members
		/// <summary>
		/// Mask of current functions being probed by this class.
		/// </summary>
		private long _ProbeMask;

		/// <summary>
		/// Mask of current functions being ignored by the present state node.
		/// </summary>
		private long _IgnoreMask;

		/// <summary>
		/// Offset into the ScriptStack where the FLabelEntry persist. 
		/// </summary>
		private short _LabelTableOffset;

		/// <summary>
		/// This state's flags mask e.g. Auto, Simulated.
		/// </summary>
		private uint _StateFlags;
		#endregion

		#region Script Members
		public IList<UFunction> Functions{ get; private set; }
		#endregion

		#region Constructors
		protected override void Deserialize()
		{
			base.Deserialize();

			// TODO: Simplify ProbeMask deserialization.
			// if >= 700
			// 32b IgnoreMask
			// if < 700
			// 32b ProbeMask
			// 64b IgnoreMask
			// if < 220
			// 64b ProbeMask
			// 64b IgnoreMask

			// UE3
			if( Package.Version >= 220 )
			{
				// TODO: Corrigate Version; Somewhere between 690 - 706
				if( Package.Version < 700 )
				{
				    // TODO: Unknown!
				    int unknown = _Buffer.ReadInt32();
					Record( "???", unknown );	
				}

				_ProbeMask = _Buffer.ReadInt32();
				Record( "_ProbeMask", _ProbeMask );		
			}
			else  // UE2 and 1
			{
				_ProbeMask = _Buffer.ReadInt64();
				Record( "_ProbeMask", _ProbeMask );
			}

			// TODO: Corrigate Version; Somewhere between 690 - 706
			if( Package.Version < 700 )
		    {
		        _IgnoreMask = _Buffer.ReadInt64();	
		        Record( "_IgnoreMask", _IgnoreMask );
		    }

			_LabelTableOffset = _Buffer.ReadInt16();
			Record( "_LabelTableOffset", _LabelTableOffset );

			if( Package.Version > VStateFlags )
			{ 
				#if BORDERLANDS2
					// FIXME:Temp fix
					if( Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 )
					{
						_StateFlags = _Buffer.ReadUShort();
						goto skipStateFlags;
					}
				#endif

				_StateFlags = _Buffer.ReadUInt32();
				skipStateFlags:
				Record( "StateFlags", (StateFlags)_StateFlags );
			}
					
			if( Package.Version >= 220 )
			{ 
				int mapCount = _Buffer.ReadIndex();
				Record( "mapcount", mapCount );
				if( mapCount > 0 )
				{
					AssertEOS( mapCount * 12, "Maps" );
					_Buffer.Skip( mapCount * 12 );
					// We don't have to store this.
					// We don't use it and all that could happen is a OutOfMemory exception!
					/*_FuncMap = new Dictionary<int,int>( mapCount );
					for( int i = 0; i < mapCount; ++ i )
					{
						_FuncMap.Add( _Buffer.ReadNameIndex(), _Buffer.ReadObjectIndex() );
					} */
				}
			}
		}

		protected override void FindChildren()
		{
			base.FindChildren();
			Functions = new List<UFunction>();
			for( var child = Children; child != null; child = child.NextField )
			{
				if( child.IsClassType( "Function" ) )
				{
				 	Functions.Insert( 0, (UFunction)child );
				}
			}
		}
		#endregion

		#region Methods
		public bool HasStateFlag( StateFlags flag )
		{
			return (_StateFlags & (uint)flag) != 0;
		}

		public bool HasStateFlag( uint flag )
		{
			return (_StateFlags & flag) != 0;
		}
		#endregion
	}
}
