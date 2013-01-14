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
		// TODO: Corrigate version
		private const int PrimitveCastVersion = 100;

		// Version might actually be correct!
		private const uint VCppText = 129;
		// TODO: Corrigate version
		private const uint VStructFlags = 102;
		// TODO: Corrigate version
		private const uint VProcessedText = 129;
		// TODO: Corrigate version
		private const uint VFriendlyNameMoved = 154;
		// TODO: Corrigate version
		private const uint VStructFlagsMoved = 154;

		#region Serialized Members
		/// <summary>
		/// Index to ScriptText Object (UTextBuffer)
		/// </summary>
		public UTextBuffer ScriptText{ get; private set; }

		/// <summary>
		/// Index to ProcessedText Object (UTextBuffer)
		/// </summary>
		public UTextBuffer ProcessedText{ get; private set; }

		/// <summary>
		/// Index to CppText Object (UTextBuffer)
		/// UE3 Only
		/// </summary>
		public UTextBuffer CppText{ get; private set; }

		/// <summary>
		/// Index to the first child property.
		/// </summary>
		public int Children{ get; private set; }

		public int Line;
		public int TextPos;
		public int ByteScriptSize{ get; private set; }
		public int DataScriptSize{ get; private set; }
		protected int FriendlyNameIndex = -1;
		protected uint StructFlags{ get; set; }
		#endregion

		#region Script Members
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
	    protected override void Deserialize()
		{
			base.Deserialize();

			// --SuperField
			if( !Package.IsConsoleCooked() )
			{
				ScriptText = _Buffer.ReadObject() as UTextBuffer;
				Record( "ScriptText", ScriptText );
			}

			Children = _Buffer.ReadObjectIndex();
			Record( "Children", TryGetIndexObject( Children ) );

			if( Package.Version < VFriendlyNameMoved )
			{ 
				// Moved to UFunction in UE3
				FriendlyNameIndex = _Buffer.ReadNameIndex();
				Record( "FriendlyNameIndex", Package.Names[FriendlyNameIndex] );
			}

			if( Package.Version >= VStructFlags )
			{
				if( Package.Version >= VCppText && !Package.IsConsoleCooked() )
				{
					CppText = _Buffer.ReadObject() as UTextBuffer;
					Record( "CppText", CppText );
				}

				if( Package.Version < VStructFlagsMoved )
				{
					StructFlags = _Buffer.ReadUInt32();
					Record( "StructFlags", (StructFlags)StructFlags );	
					// Note: Bioshock inherits from the SWAT4's UE2 build.
#if BIOSHOCK
					if( Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock )
					{
						// TODO: Unknown data, might be related to the above Swat4 data.
						var unknown = _Buffer.ReadObjectIndex();
						Record( "???", TryGetIndexObject( unknown ) );
					}
#endif
					// This is high likely to be only for "Irrational Games" builds.
					if( Package.Version >= VProcessedText )
					{
						ProcessedText = _Buffer.ReadObject() as UTextBuffer;
						Record( "ProcessedText", ProcessedText );
					}
				}
			}

			if( !Package.IsConsoleCooked() )
			{
				Line = _Buffer.ReadInt32();
				Record( "Line", Line );
				TextPos = _Buffer.ReadInt32();
				Record( "TextPos", TextPos );
			}

			ByteScriptSize = _Buffer.ReadInt32();
			Record( "ByteScriptSize", ByteScriptSize );
			const int vDataScriptSize = 639;
			if( Package.Version >= vDataScriptSize )
			{
				DataScriptSize = _Buffer.ReadInt32();
				Record( "DataScriptSize", DataScriptSize );
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
			if( Package.Version >= vDataScriptSize )
			{
				_Buffer.Skip( DataScriptSize );
			}
			else
			{
				const int moonbaseVersion = 587;
				const int shadowcomplexVersion = 590;
				const int mohaVersion = 421;

				var isTrueScriptSize = Package.Version == mohaVersion || 
				(
					Package.Version >= UnrealPackage.VINDEXDEPRECATED 
					&& (Package.Version < moonbaseVersion && Package.Version > shadowcomplexVersion )
				);
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
