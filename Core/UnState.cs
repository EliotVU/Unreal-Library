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
		private ulong _ProbeMask;

		/// <summary>
		/// Mask of current functions being ignored by the present state node.
		/// </summary>
		private ulong _IgnoreMask;

		/// <summary>
		/// Offset into the ScriptStack where the FLabelEntry persist. 
		/// </summary>
		private ushort _LabelTableOffset;

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

			// UE3
			if( Package.Version >= 220 )
			{
				// TODO: Corrigate Version; Somewhere between 690 - 706
				if( _Buffer.Version < 700 )
				{
				    // TODO: Unknown!
				    int unk1 = _Buffer.ReadInt32();
					NoteRead( "unk1", unk1 );	
				}

				_ProbeMask = _Buffer.ReadUInt32();
				NoteRead( "_ProbeMask", _ProbeMask );		
			}
			else  // UE2 and 1
			{
				_ProbeMask = _Buffer.ReadUInt64();
				NoteRead( "_ProbeMask", _ProbeMask );
			}

			// TODO: Corrigate Version; Somewhere between 690 - 706
			if( _Buffer.Version < 700 )
		    {
		        _IgnoreMask = _Buffer.ReadUInt64();	
		        NoteRead( "_IgnoreMask", _IgnoreMask );
		    }

			_LabelTableOffset = _Buffer.ReadUShort();
			NoteRead( "_LabelTableOffset", _LabelTableOffset );

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
				NoteRead( "StateFlags", (StateFlags)_StateFlags );
			}
					
			if( Package.Version >= 220 )
			{ 
				int mapCount = _Buffer.ReadIndex();
				NoteRead( "mapcount", mapCount );
				if( mapCount > 0 )
				{
					TestEndOfStream( mapCount * 12, "Maps" );
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
			for( var child = (UField)GetIndexObject( Children ); child != null; child = child.NextField )
			{
				if( child.IsClassType( "Function" ) )
				{
				 	Functions.Insert( 0, (UFunction)child );
				}
			}
		}
		#endregion

		#region Methods
		public bool HasStateFlag( Flags.StateFlags flag )
		{
			return (_StateFlags & (uint)flag) != 0;
		}

		public bool HasStateFlag( uint flag )
		{
			return (_StateFlags & flag) != 0;
		}

		protected void TestEndOfStream( int size, string testSubject = "" )
		{
			if( size > (_Buffer.Length - _Buffer.Position) )
			{
				throw new DeserializationException( Name + ": Allocation past end of stream detected! Size:" + size + " Subject:" + testSubject );
			}
			//System.Diagnostics.Debug.Assert( size <= (_Buffer.Length - _Buffer.Position), Name + ": Allocation past end of stream detected! " + size );
		}
		#endregion
	}
}
