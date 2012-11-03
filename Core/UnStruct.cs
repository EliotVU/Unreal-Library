using System;
using System.Collections.Generic;
using UELib.Tokens;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal struct with the functionality to contain Constants, Enums, Structs and Properties. 
	/// </summary>
	public partial class UStruct : UField
	{
		// Greater or equal than:
		// Definitely not after 110
		internal const int PrimitveCastVersion = 100;

		#region Serialized Members
		/// <summary>
		/// Index to ScriptText Object (UnTextBuffer)
		/// </summary>
		public int ScriptText
		{
			get;
			private set;
		}

		/// <summary>
		/// Index to CppText Object (UTextBuffer)
		/// UE3 Only
		/// </summary>
		public int CppText
		{
			get;
			private set;
		}

		/// <summary>
		/// Index to the first child property.
		/// </summary>
		public int Children
		{
			get;
			private set;
		}

		public uint Line;
		public uint TextPos;

		protected int _MinAlignment
		{
			get;
			private set;
		}

		public uint ScriptSize
		{
			get;
			private set;
		}

		protected int _FriendlyNameIndex = -1;

		/// <summary>
		/// UE2 Only?
		/// </summary>
		internal uint StructFlags
		{
			get;
			private set;
		}
		#endregion

		#region PostInitialized Members
		public UTextBuffer ScriptBuffer
		{
			get;
			private set;
		}

		public UTextBuffer CppBuffer{ get; protected set; }

		protected List<UConst> _ChildConstants = new List<UConst>();
		public List<UConst> ChildConstants
		{
			get{ return _ChildConstants; }
		}

		protected List<UEnum> _ChildEnums = new List<UEnum>();
		public List<UEnum> ChildEnums
		{
			get{ return _ChildEnums; }
		}

		protected List<UStruct> _ChildStructs = new List<UStruct>();
		public List<UStruct> ChildStructs
		{
			get{ return _ChildStructs; }
		}

		protected List<UProperty> _ChildProperties = new List<UProperty>();
		public List<UProperty> ChildProperties
		{
			get{ return _ChildProperties; }
		}
		#endregion

		#region General Members
		/// <summary>
		/// Default Properties buffer offset
		/// </summary>
		protected long _DefaultPropertiesOffset;

		//protected uint _CodePosition;

		public long ScriptOffset{ get; protected set; }

		public UStruct.UByteCodeDecompiler ByteCodeManager;
		#endregion

		/// <summary>
		///	Creates a new instance of the UELib.Core.UStruct class. 
		/// </summary>
		public UStruct()
		{
			// Don't release because structs have scripts, but if ScriptSize == 0 this will still be done!
			_ShouldReleaseBuffer = false;
		}

		private const uint VCppText = 190;

		protected override void Deserialize()
		{
			base.Deserialize();

			// --SuperField
			if( !Package.IsConsoleCooked() )
			{
				ScriptText = _Buffer.ReadObjectIndex();
				NoteRead( "ScriptText", ScriptText );
			}

			Children = _Buffer.ReadObjectIndex();
			NoteRead( "Children", Children );

			// TODO: Correct version
			if( _Buffer.Version > 154 /* UE3 */ )
			{
				if( _Buffer.Version >= VCppText && !Package.IsConsoleCooked() )
				{
					CppText = _Buffer.ReadInt32();
					NoteRead( "CppText", CppText );
				}
			}
			else
			{		
				// Moved to UFunction in UE3
				_FriendlyNameIndex = _Buffer.ReadIndex();
				NoteRead( "_FriendlyNameIndex", _FriendlyNameIndex );
#if SWAT4
				if( Package.Build == UnrealPackage.GameBuild.ID.Swat4 )
				{
					_Buffer.ReadIndex();
				}
#endif
				// TODO: Corrigate Version
				if( _Buffer.Version > 69 )
				{
					StructFlags = _Buffer.ReadUInt32();
					NoteRead( "StructFlags", StructFlags );	
				}

#if SWAT4
				if( Package.Build == UnrealPackage.GameBuild.ID.Swat4 )
				{
					int processedText = _Buffer.ReadObjectIndex();
					NoteRead( "ProcessedText", processedText );
				}
#endif
			}

			if( !Package.IsConsoleCooked() )
			{
				Line = _Buffer.ReadUInt32();
				NoteRead( "Line", Line );
				TextPos = _Buffer.ReadUInt32();
				NoteRead( "TextPos", TextPos );
			}

			var scriptSkipSize = 0;
			// ScriptSize
			ScriptSize = _Buffer.ReadUInt32();
			NoteRead( "_ScriptSize", ScriptSize );
			
			if( _Buffer.Version >= 639 )   // 639
			{
				// ScriptSize
				_MinAlignment = _Buffer.ReadInt32();
				NoteRead( "_MinAlignment", _MinAlignment );

				scriptSkipSize = _MinAlignment;
			}
			else 
			{
				scriptSkipSize = (int)ScriptSize;
			}
			ScriptOffset = _Buffer.Position;

			// Code Statements
			if( ScriptSize > 0 )
			{
				ByteCodeManager = new UByteCodeDecompiler( this );
				// ScriptSize is not a true size in UT2004 and below and MoonBase's version (587)
				if( _Buffer.Version >= UnrealPackage.VINDEXDEPRECATED && _Buffer.Version != 587 )	// 587(MoonBase)
				{
					_Buffer.Skip( scriptSkipSize );
				}
				else // ScriptSize is unaccurate due index sizes
				{
					ByteCodeManager.Deserialize();
				}
			}
			else
			{
				_ShouldReleaseBuffer = true; 
			}

			// TODO: Corrigate Version.
			if( IsPureStruct() && _Buffer.Version >= 220 )
			{
				StructFlags = _Buffer.ReadUInt32();
				NoteRead( "StructFlags", StructFlags );

				_ShouldReleaseBuffer = false;
				// Introduced somewhere between 129 - 178
				DeserializeProperties();
			}
		}

		public override void PostInitialize()
		{
			base.PostInitialize();

			try
			{
				if( Children != 0 )
				{
					FindChildren();
				}
		
				// Found by UnStruct::Serialize
				if( ScriptText != 0 )
				{
					ScriptBuffer = (UTextBuffer)TryGetIndexObject( ScriptText ); 
					if( ScriptBuffer != null )
					{
						// Hardcoded because some packages such as CtryTags.u have a different name for the TextBuffer
						ScriptBuffer.Name = "ScriptText";
					}
				}

				if( CppText != 0 )
				{
					CppBuffer = (UTextBuffer)TryGetIndexObject( CppText );
				}
			}
			catch( InvalidCastException ice )
			{
				Console.WriteLine( ice.Message );
			}
		}

		protected virtual void FindChildren()
		{
			for( var child = (UField)GetIndexObject( Children ); child != null; child = child.NextField )
			{		
				if( child.GetType().IsSubclassOf( typeof(UProperty) ) )
				{
					_ChildProperties.Add( (UProperty)child );
				}
				else if( child.IsClassType( "Const" ) )
				{
					_ChildConstants.Insert( 0, (UConst)child );
				}
				else if( child.IsClassType( "Enum" ) )
				{
					_ChildEnums.Insert( 0, (UEnum)child );
				}	
				else if( child.IsClassType( "Struct" ) || child.IsClassType( "ScriptStruct" ) )
				{
					_ChildStructs.Insert( 0, (UStruct)child );
				}
			}
		}	

		#region Methods
		public bool HasStructFlag( Flags.StructFlags flag )
		{
			return (StructFlags & (uint)flag) != 0;
		}

		public bool IsPureStruct()
		{
			return IsClassType( "Struct" ) || IsClassType( "ScriptStruct" );
		}
		#endregion
	}
}
