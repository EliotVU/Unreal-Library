using System.Collections.Generic;

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
	public partial class UState : UStruct
	{
		#region Serialized Members

		/// <summary>
		/// Mask of current functions being probed by this class.
		/// </summary>
		protected ulong _ProbeMask;

		/// <summary>
		/// Mask of current functions being ignored by the present state node.
		/// </summary>
		protected ulong _IgnoreMask;

		/// <summary>
		/// Offset into the ScriptStack where the FLabelEntry persist. 
		/// </summary>
		protected ushort _LabelTableOffset;

		/// <summary>
		/// This state's flags mask e.g. Auto, Simulated.
		/// </summary>
		internal uint StateFlags
		{
			get;
			private set;
		}
									   
		//internal Dictionary<int,int> _FuncMap;
		#endregion

		#region PostInitializedMembers
		protected List<UFunction> _ChildFunctions = new List<UFunction>();
		public List<UFunction> ChildFunctions
		{
			get
			{
				return _ChildFunctions;
			}
		}
		#endregion

		public const int ProbeMin = 300;
		public const int ProbeMax = 364;

		public bool IsProbing( int nameIndex )
		{
			return (nameIndex < ProbeMin) || (nameIndex >= ProbeMax) || (_ProbeMask & ((ulong)1 << (nameIndex - ProbeMin))) != 0;
		}

		/// <summary>
		/// Creates a new instance of the UELib.Core.UState class. 
		/// </summary>
		public UState(){}

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
			StateFlags = _Buffer.ReadUInt32();
			NoteRead( "StateFlags", StateFlags );

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

		protected void TestEndOfStream( int size, string testSubject = "" )
		{
			if( size > (_Buffer.Length - _Buffer.Position) )
			{
				throw new SerializationException( Name + ": Allocation past end of stream detected! Size:" + size + " Subject:" + testSubject );
			}
			//System.Diagnostics.Debug.Assert( size <= (_Buffer.Length - _Buffer.Position), Name + ": Allocation past end of stream detected! " + size );
		}

		protected override void FindChildren()
		{
			base.FindChildren();
			for( var child = (UField)GetIndexObject( Children ); child != null; child = child.NextField )
			{
				if( child.IsClassType( "Function" ) )
				{
				 	_ChildFunctions.Add( (UFunction)child );
				}
			}
			_ChildFunctions.Reverse();
		}

		#region Methods
		public bool HasStateFlag( Flags.StateFlags flag )
		{
			return (StateFlags & (uint)flag) != 0;
		}
		#endregion
	}
}
