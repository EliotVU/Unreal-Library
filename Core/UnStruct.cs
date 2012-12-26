using System;
using System.Collections.Generic;
using UELib.Flags;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal struct with the functionality to contain Constants, Enums, Structs and Properties. 
	/// </summary>
	[UnrealRegisterClass]
	public partial class UStruct : UField
	{
		// Greater or equal than:
		// Definitely not after 110
		private const int PrimitveCastVersion = 100;

		#region Serialized Members
		/// <summary>
		/// Index to ScriptText Object (UnTextBuffer)
		/// </summary>
		public int ScriptText{ get; private set; }

		/// <summary>
		/// Index to CppText Object (UTextBuffer)
		/// UE3 Only
		/// </summary>
		public int CppText{ get; private set; }

		/// <summary>
		/// Index to the first child property.
		/// </summary>
		public int Children{ get; private set; }

		public uint Line;
		public uint TextPos;
		public int ByteScriptSize{ get; private set; }
		public int DataScriptSize{ get; private set; }
		protected int FriendlyNameIndex = -1;
		protected uint StructFlags{ get; set; }
		#endregion

		#region Script Members
		public UTextBuffer ScriptBuffer
		{
			get;
			private set;
		}

		public UTextBuffer CppBuffer
		{
			get; 
			protected set;
		}

		public IList<UConst> Constants{ get; private set; }

		public IList<UEnum> Enums{ get; private set; }

		public IList<UStruct> Structs{ get; private set; }

		public List<UProperty> Variables{ get; private set; }
		#endregion

		#region General Members
		/// <summary>
		/// Default Properties buffer offset
		/// </summary>
		protected long _DefaultPropertiesOffset;

		//protected uint _CodePosition;

		public long ScriptOffset
		{
			get; 
			private set;
		}

		public UByteCodeDecompiler ByteCodeManager;
		#endregion

		#region Constructors
		/// <summary>
		///	Creates a new instance of the UELib.Core.UStruct class. 
		/// </summary>
		public UStruct()
		{
			// Don't release because structs have scripts, but if ScriptSize == 0 this will still be done!
			_ShouldReleaseBuffer = false;
		}

		private const uint VCppText = 190;
		private const uint VStructFlags = 101;

		protected override void Deserialize()
		{
			base.Deserialize();

			// --SuperField
			if( !Package.IsConsoleCooked() )
			{
				ScriptText = _Buffer.ReadObjectIndex();
				NoteRead( "ScriptText", GetIndexObject( ScriptText ) );
			}

			Children = _Buffer.ReadObjectIndex();
			NoteRead( "Children", GetIndexObject( Children ) );

			// TODO: Correct version
			if( _Buffer.Version > 154 /* UE3 */ )
			{
				if( _Buffer.Version >= VCppText && !Package.IsConsoleCooked() )
				{
					CppText = _Buffer.ReadInt32();
					NoteRead( "CppText", GetIndexObject( CppText ) );
				}
			}
			else
			{		
				// Moved to UFunction in UE3
				FriendlyNameIndex = _Buffer.ReadIndex();
				NoteRead( "FriendlyNameIndex", Package.Names[FriendlyNameIndex] );
#if SWAT4
				if( Package.Build == UnrealPackage.GameBuild.BuildName.Swat4 )
				{
					_Buffer.ReadIndex();
				}
#endif
				// TODO: Corrigate Version
				if( _Buffer.Version > VStructFlags )
				{
					StructFlags = _Buffer.ReadUInt32();
					NoteRead( "StructFlags", (StructFlags)StructFlags );	
				}

#if SWAT4
				if( Package.Build == UnrealPackage.GameBuild.BuildName.Swat4 )
				{
					int processedText = _Buffer.ReadObjectIndex();
					NoteRead( "ProcessedText", GetIndexObject( processedText ) );
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

			ByteScriptSize = _Buffer.ReadInt32();
			NoteRead( "ByteScriptSize", ByteScriptSize );
			const int vDataScriptSize = 639;
			if( _Buffer.Version >= vDataScriptSize )
			{
				DataScriptSize = _Buffer.ReadInt32();
				NoteRead( "DataScriptSize", DataScriptSize );
			}
			else 
			{
				DataScriptSize = ByteScriptSize;
			}
			ScriptOffset = _Buffer.Position;

			// Code Statements
			if( DataScriptSize <= 0 )
				return;

			ByteCodeManager = new UByteCodeDecompiler( this );
			if( _Buffer.Version >= vDataScriptSize )
			{
				_Buffer.Skip( DataScriptSize );
			}
			else
			{
				const int moonbaseVersion = 587;
				const int shadowcomplexVersion = 590;

				var isTrueScriptSize = _Buffer.Version >= UnrealPackage.VINDEXDEPRECATED
				    && (_Buffer.Version < moonbaseVersion && _Buffer.Version > shadowcomplexVersion );
				if( isTrueScriptSize )
				{
					_Buffer.Skip( DataScriptSize );
				}
				else
				{
					ByteCodeManager.Deserialize();
				}
			}
		}

		protected override bool CanDisposeBuffer()
		{
			return base.CanDisposeBuffer() && ByteCodeManager == null;
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
			Constants = new List<UConst>();
			Enums = new List<UEnum>();
			Structs = new List<UStruct>();
			Variables = new List<UProperty>();

			for( var child = (UField)GetIndexObject( Children ); child != null; child = child.NextField )
			{		
				if( child.GetType().IsSubclassOf( typeof(UProperty) ) )
				{
					Variables.Add( (UProperty)child );
				}
				else if( child.IsClassType( "Const" ) )
				{
					Constants.Insert( 0, (UConst)child );
				}
				else if( child.IsClassType( "Enum" ) )
				{
					Enums.Insert( 0, (UEnum)child );
				}	
				else if( child.IsClassType( "Struct" ) || child.IsClassType( "ScriptStruct" ) )
				{
					Structs.Insert( 0, (UStruct)child );
				}
			}
		}	
		#endregion

		#region Methods
		public bool HasStructFlag( StructFlags flag )
		{
			return (StructFlags & (uint)flag) != 0;
		}

		private bool IsPureStruct()
		{
			return IsClassType( "Struct" ) || IsClassType( "ScriptStruct" );
		}
		#endregion
	}
}
