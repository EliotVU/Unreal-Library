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

		/// <summary>
		/// UE3 Only
		/// </summary>
		protected int _MinAlignment
		{
			get;
			private set;
		}

		// Script
		protected uint _ScriptSize
		{
			get;
			private set;
		}

		public uint ScriptSize
		{
			get{ return _ScriptSize; }
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
	
		protected UTextBuffer _CppBuffer = null;
		public UTextBuffer CppBuffer
		{
			get{ return _CppBuffer; }
		}

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

		/// <summary>
		/// Buffer offset to the start of the struct ByteCodes
		/// </summary>
		protected long _ScriptOffset
		{
			get;
			private set;
		}
		//protected uint _CodePosition;

		public long ScriptOffset
		{
			get{ return _ScriptOffset; }
		}

		public UStruct.UByteCodeDecompiler ByteCodeManager;
		#endregion

		/// <summary>
		///	Creates a new instance of the UELib.Core.UStruct class. 
		/// </summary>
		public UStruct()
		{
			// Don't release because structs have scripts, but if ScriptSize == 0 this will still be done!
			_bReleaseBuffer = false;
		}

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
				if( _Buffer.Version > 189 && !Package.IsConsoleCooked() )
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
				// Guessed...
				// TODO: Corrigate Version
				if( _Buffer.Version > 69 )
				{
					StructFlags = _Buffer.ReadUInt32();
					NoteRead( "StructFlags", StructFlags );	
				}
			}

#if SWAT4
			if( Package.Build == UnrealPackage.GameBuild.ID.Swat4 )
			{

				int processedText = _Buffer.ReadObjectIndex();
				NoteRead( "ProcessedText", processedText );
			}
#endif

			if( !Package.IsConsoleCooked() )
			{
				Line = _Buffer.ReadUInt32();
				NoteRead( "Line", Line );
				TextPos = _Buffer.ReadUInt32();
				NoteRead( "TextPos", TextPos );
			}

			// Actually another ScriptSize variable.
			// Definitely not in moonbase(587).
			const uint MinAlignmentVersion = 587;

			// TODO: Corrigate Version
			if( _Buffer.Version > 154 && !IsPureStruct() )
			{
				// TODO: Corrigate Version
				if( (_Buffer.Version > MinAlignmentVersion 
					|| (!(this is UFunction) && GetType() != typeof(UState)))
					)
				{
					// ScriptSize
					_MinAlignment = _Buffer.ReadInt32();
					NoteRead( "_MinAlignment", _MinAlignment );
				}
			}

			// ScriptSize
			_ScriptSize = _Buffer.ReadUInt32();
			NoteRead( "_ScriptSize", _ScriptSize );
			_ScriptOffset = _Buffer.Position;

			// Code Statements
			if( _ScriptSize > 0 )
			{
				ByteCodeManager = new UByteCodeDecompiler( this );
				if( _Buffer.Version >= UnrealPackage.VIndexDeprecated )
				{
					_Buffer.Skip( (int)_ScriptSize );
				}
				else // ScriptSize is unaccurate due index sizes
				{
					ByteCodeManager.Deserialize();
				}
			}
			else
			{
				_bReleaseBuffer = true; 
			}

			// StructDefaultProperties
			// Only since UE3 and should only happen if this is a pure UStruct e.g. not some class that extends UStruct.
			if( _Buffer.Version <= 154 || !IsPureStruct() ) 
				return;

			// TODO: Corrigate Version
			if( _Buffer.Version >= 220 )
			{
				// TODO: Corrigate Version
				// Definitely not in moonbase(587)
				if( _Buffer.Version > 587 )
				{
					_Buffer.ReadUInt32();
				}
				StructFlags = _Buffer.ReadUInt32();
				NoteRead( "StructFlags", StructFlags );
			}
	
			_bReleaseBuffer = false;
			// Introduced somewhere between 129 - 178
			DeserializeProperties();
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
					_CppBuffer = (UTextBuffer)TryGetIndexObject( CppText );
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
					_ChildConstants.Add( (UConst)child );
				}
				else if( child.IsClassType( "Enum" ) )
				{
					_ChildEnums.Add( (UEnum)child );
				}	
				else if( child.IsClassType( "Struct" ) || child.IsClassType( "ScriptStruct" ) )
				{
					_ChildStructs.Add( (UStruct)child );
				}
			}

			// For some reason the order is different between functions and structs*.
			if( IsClassType( "Function" ) )
			{
				_ChildProperties.Reverse();
			}
			_ChildConstants.Reverse();
			_ChildEnums.Reverse();
			_ChildStructs.Reverse();	
		}	

		#region ByteCodeSerializing

#if OLDDECOMPILER
		/// <summary>
		/// Fix the values of UE1/UE2 tokens to match the UE3 token values.
		/// </summary>
		private byte FixToken( byte token )
		{
			if( Package.Version >= 184 && ((token >= (byte)ExprToken.Unknown && token < (byte)ExprToken.ReturnNothing) || (token > (byte)ExprToken.NoDelegate && token < (byte)ExprToken.ExtendedNative)) )
			{
		  		++ token;
			}
			return token;
		}

		protected byte PeekToken( bool testForDebug = true )
		{
			byte Token = FixToken( _Buffer.ReadByte() );
			-- _Buffer.Position;

			// Read past debug tokens
			/*if( testForDebug && Token == (byte)ExprToken.DebugInfo )
			{
				uint BP = _CodePosition;
				long P = _Buffer.Position;

				SerializeExpr();
				Token = PeekToken();

				_CodePosition = BP;
				_Buffer.Position = P;
			}*/
			return Token;
		}

		protected byte PastToken( byte num = 1 )
		{
			_Buffer.Position -= num;
			byte Token = FixToken( _Buffer.ReadByte() );	
			return Token;
		}

		protected void AddCodeSize( int size )
		{
			_CodePosition += (uint)size;
		}

		protected void AddNameIndexCodeSize()
		{
			_CodePosition += (_Buffer.Version > UnrealPackage.VNameIndex ? (uint)sizeof(long) : (uint)sizeof(int));
		}

		protected byte DeserializeExpr()
		{
	 		if( _CodePosition >= _ScriptSize )
			{
				throw new SerializationException();
			}
	  		
			byte token = FixToken( _Buffer.ReadByte() );
			AddCodeSize( sizeof(byte) );
			if(	token >= (byte)ExprToken.ExtendedNative )
			{
				#region Natives
				int NativeIndex = -1;
				if( (token & 0xF0) == (byte)ExprToken.ExtendedNative )
				{
					byte Token2 = _Buffer.ReadByte();
					AddCodeSize( sizeof(byte) );
					NativeIndex = (token - (byte)ExprToken.ExtendedNative) << 8 + Token2;	
				}
				else
				{
					NativeIndex = (int)token;
					if( NativeIndex < (byte)ExprToken.FirstNative )
					{
						throw new System.Exception( "Invalid NativeIndex" );
					}
				}

				if( NativeIndex > -1 )
				{
					NativeTable NT = GetNativeTable( NativeIndex );
					if( NT != null )
					{
						NativeType Format = (NativeType)NT.Format;
						switch( Format )
						{
							case NativeType.Function:
								while( DeserializeExpr() != (byte)ExprToken.EndFunctionParms ); 
								DeserializeDebugToken();
								break;

							case NativeType.Operator:
								DeserializeExpr();
								DeserializeExpr();
								DeserializeExpr();
								break;

							case NativeType.PreOperator:
								DeserializeExpr();
								DeserializeExpr();
								break;

							case NativeType.PostOperator:
								DeserializeExpr();
								DeserializeExpr();
								break;

							default:
								while( DeserializeExpr() != (byte)ExprToken.EndFunctionParms );
								DeserializeDebugToken();
								break;
						}
					}
					else
					{
						// Not found, still try to skip the need tokens
						while( DeserializeExpr() != (byte)ExprToken.EndFunctionParms );
						DeserializeDebugToken();
					}
					return token;
				}
				#endregion
			}
			else switch( token )
			{
				#region Casts
				case (byte)ExprToken.DynamicCast:
					_Buffer.ReadIndex();
					AddCodeSize( sizeof(int) );
					DeserializeExpr();
					break;

				case (byte)ExprToken.MetaCast:
					_Buffer.ReadIndex();
					AddCodeSize( sizeof(int) );
					DeserializeExpr();
					break;

				// Redefined, can be RotatorToVector!
				case (byte)ExprToken.PrimitiveCast: 
					// Older than this don't make use of PrimitiveCast so handle the cast right in here.
					if( _Buffer.Version < PrimitveCastVersion )
					{
						// RotatorToVector!
						DeserializeExpr();
					}
					else
					{
						// PrimitiveCast!, Skip the next cast token.
						_Buffer.ReadByte();
						AddCodeSize( sizeof(byte) );

						// Skip the cast, but deal the 'case' casts as a diff meaning
						DeserializeExpr();
					}
					break;
				#endregion

				#region Context
				case (byte)ExprToken.ClassContext:
				case (byte)ExprToken.Context:
				{
					DeserializeExpr();
					_Buffer.ReadUShort();
					AddCodeSize( sizeof(ushort) );
					_Buffer.ReadByte();
					AddCodeSize( sizeof(byte) );
					DeserializeExpr();
					break;
				}

				// Redefined, can be ByteToString!
				case (byte)ExprToken.InterfaceContext: 
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
					}
					break;

				case (byte)ExprToken.StructMember:
					if( _Buffer.Version < 300 )
					{
						_Buffer.ReadIndex();
						AddCodeSize( sizeof(int) );
						DeserializeExpr();
					}
					else
					{
						_Buffer.ReadIndex();
						AddCodeSize( sizeof(int) );
						_Buffer.ReadIndex();
						AddCodeSize( sizeof(int) );
						_Buffer.ReadUShort();
						AddCodeSize( sizeof(short) );
						DeserializeExpr();
					}
					break;
				#endregion

				#region Assigns
				case (byte)ExprToken.Let:
				case (byte)ExprToken.LetBool:
					DeserializeExpr();
					DeserializeExpr();
					break;

				// Redefined, can be FloatToBool!
				case (byte)ExprToken.LetDelegate:
					// Older than this don't make use of PrimitiveCast so handle the cast right in here.
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();

						if( _Buffer.Version < 184 )
						{
							DeserializeExpr();
						}
					}
					break;

				// Redefined, can be NameToBool!(UE1)
				case (byte)ExprToken.Conditional:
				case (byte)ExprToken.Eval:
					// Older than this don't make use of PrimitiveCast so handle the cast right in here.
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();

						// Size
						_Buffer.ReadUShort();
						AddCodeSize( sizeof(ushort) );
						DeserializeExpr();

						// Size
						_Buffer.ReadUShort();
						AddCodeSize( sizeof(ushort) );
						DeserializeExpr();
					}
					break;
				#endregion

				#region Jumps
				case (byte)ExprToken.GotoLabel:
					DeserializeExpr();
					break;

				case (byte)ExprToken.Unknown:
#if DEBUGX
					System.Windows.MessageBox.Show( "Unknown occured within " + Name + " " + GetOuterName() );
#endif
					break;

				case (byte)ExprToken.UnknownJumpOver:
					_Buffer.ReadUShort();
					AddCodeSize( sizeof(ushort) );
					break;

				case (byte)ExprToken.UnknownJumpOver2:
					_Buffer.ReadByte();
					AddCodeSize( sizeof(byte) );
					DeserializeExpr();
					break;

				case (byte)ExprToken.Jump:
					_Buffer.ReadUShort();
					AddCodeSize( sizeof(ushort) );
					break;

				case (byte)ExprToken.JumpIfNot:
					_Buffer.ReadUShort();
					AddCodeSize( sizeof(ushort) );
					DeserializeExpr();
					break;

				case (byte)ExprToken.Switch:
					_Buffer.ReadByte();
					AddCodeSize( sizeof(byte) );
					DeserializeExpr();
					break;

				case (byte)ExprToken.Case:
				{
					ushort NextOffset = _Buffer.ReadUShort();
					AddCodeSize( sizeof(ushort) );
					if( NextOffset != 0xFFFF )
					{
						DeserializeExpr();
					}
					break;
				}

				case (byte)ExprToken.DynArrayIterator:
					// RotatorToString
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();
						_Buffer.ReadUShort();
						AddCodeSize( sizeof(ushort) );
						_Buffer.ReadUShort();						
						AddCodeSize( sizeof(ushort) );
					}
					break;

				case (byte)ExprToken.Iterator:
					DeserializeExpr();
					_Buffer.ReadUShort();
					AddCodeSize( sizeof(ushort) );
					break;

				case (byte)ExprToken.IteratorNext:
					break;

				case (byte)ExprToken.IteratorPop:
					break;
				#endregion

				#region Variables
				case (byte)ExprToken.NativeParm:
				case (byte)ExprToken.DefaultVariable:
				case (byte)ExprToken.InstanceVariable:
				case (byte)ExprToken.LocalVariable:
					_Buffer.ReadIndex();
					AddCodeSize( sizeof(int) );
					break;

				case (byte)ExprToken.BoolVariable:
					DeserializeExpr();
					break;

				// Redefined, can be FloatToInt!(UE1)
				case (byte)ExprToken.DelegateProperty:
					// Older than this don't make use of PrimitiveCast so handle the cast right in here.
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						_Buffer.ReadIndex();
						AddCodeSize( sizeof(int) );
					}
					break;

				case (byte)ExprToken.UnknownVariable:
					_Buffer.ReadIndex();
					AddCodeSize( sizeof(int) );
					DeserializeExpr();
					break;

					// UE3!
				case (byte)ExprToken.DefaultParmValue:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();		
					}
					else
					{
						_Buffer.ReadUShort();
						AddCodeSize( sizeof(short) );
						DeserializeExpr();
					}
					break;
				#endregion		

				#region Misc
				case (byte)ExprToken.Nothing:
				case (byte)ExprToken.EndFunctionParms:
				case (byte)ExprToken.IntZero:
				case (byte)ExprToken.IntOne:
				case (byte)ExprToken.True:
				case (byte)ExprToken.False:
				case (byte)ExprToken.NoParm:
				case (byte)ExprToken.NoObject:
				case (byte)ExprToken.Self:
					break;

				case (byte)ExprToken.Stop:
					if( _Buffer.Version > 300 )
					{
						_Buffer.ReadUShort();
						AddCodeSize( sizeof(short) );
					}
					break;		

				// Redefined, can be VectorToBool!
				case (byte)ExprToken.NoDelegate:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					break;

				case (byte)ExprToken.Assert:
					_Buffer.ReadUShort();
					AddCodeSize( sizeof(ushort) );
					DeserializeExpr();
					break;

				case (byte)ExprToken.DebugInfo:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						_Buffer.ReadInt32();
						_Buffer.ReadInt32();
						_Buffer.ReadInt32();
						_Buffer.ReadByte();
						AddCodeSize( 13 );
					}
					break;

				case (byte)ExprToken.LabelTable:
					if( (token & 3) == 0 )
					{
						while( true )
						{
							int NameIndex = _Buffer.ReadNameIndex();
							AddNameIndexCodeSize();
							_Buffer.ReadInt32();
							AddCodeSize( sizeof(int) );
							if( String.Compare( Package.NameTableList[NameIndex].Name, "None", true ) == 0 )
 							{
								break;
							}
						}
					}
					break;

				case (byte)ExprToken.Skip:
					_Buffer.ReadUShort();
					AddCodeSize( sizeof(ushort) );
					DeserializeExpr();
					break;

				case (byte)ExprToken.StructCmpEq:
				case (byte)ExprToken.StructCmpNe:
					_Buffer.ReadIndex();
					AddCodeSize( sizeof(int) );
					DeserializeExpr();
					DeserializeExpr();
					break;

				case (byte)ExprToken.DelegateCmpEq:
				case (byte)ExprToken.DelegateCmpNe:
				case (byte)ExprToken.DelegateFunctionCmpEq:
				case (byte)ExprToken.DelegateFunctionCmpNe:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();
						DeserializeExpr();
					}
					break;

				case (byte)ExprToken.EatString:
					if( _Buffer.Version > 300 )
					{
						_Buffer.ReadUShort();
						AddCodeSize( sizeof(short) );
					}
					DeserializeExpr();
					break;

				case (byte)ExprToken.Return:
					DeserializeExpr();
					break;

				case (byte)ExprToken.New:
					DeserializeExpr();
					DeserializeExpr();
					DeserializeExpr();
					DeserializeExpr();
					if( _Buffer.Version > 180 )
					{
						DeserializeExpr();
					}
 					break;

				// End of function?
				case (byte)ExprToken.FunctionEnd:
					// Older than this don't make use of PrimitiveCast so handle the cast right in here.
					if( _Buffer.Version < PrimitveCastVersion )
					{
						// ObjectToBool!
						DeserializeExpr();
					}
					// Do Nothing
					break;

				case (byte)ExprToken.EndOfScript:	//CastToken.BoolToString:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						// Do nothing...
					}
					break;

				#endregion

				#region Constants
				case (byte)ExprToken.IntConst:
					_Buffer.ReadInt32();
					AddCodeSize( sizeof(int) );
					break;

				case (byte)ExprToken.ByteConst:
					_Buffer.ReadByte();
					AddCodeSize( sizeof(byte) );
					break;

				case (byte)ExprToken.IntConstByte:
					_Buffer.ReadByte();
					AddCodeSize( sizeof(byte) );
					break;

				case (byte)ExprToken.FloatConst:
					_Buffer.UR.ReadSingle();
					AddCodeSize( sizeof(float) );
					break;

				case (byte)ExprToken.ObjectConst:
					_Buffer.ReadIndex();
					AddCodeSize( sizeof(int) );
					break;

				case (byte)ExprToken.NameConst:
					_Buffer.ReadNameIndex();
					AddNameIndexCodeSize();
					break;

				case (byte)ExprToken.StringConst:
				{
					string XX = _Buffer.ReadASCIIString();
					AddCodeSize( XX.Length + 1 );
					break;
				}

				case (byte)ExprToken.UniStringConst:
				{
					while( true )
					{
						ushort W = _Buffer.ReadUShort();
						AddCodeSize( sizeof(ushort) );
						if( W == 0 )
						{
							break;
						}	
					}
					break;
				}

				case (byte)ExprToken.RotatorConst:
				{
					_Buffer.ReadInt32();
					AddCodeSize( sizeof(int) );
					_Buffer.ReadInt32();
					AddCodeSize( sizeof(int) );
					_Buffer.ReadInt32();
					AddCodeSize( sizeof(int) );
					break;
				}

				case (byte)ExprToken.VectorConst:
				{
					_Buffer.UR.ReadSingle();
					AddCodeSize( sizeof(float) );
					_Buffer.UR.ReadSingle();
					AddCodeSize( sizeof(float) );
					_Buffer.UR.ReadSingle();
					AddCodeSize( sizeof(float) );
					break;
				}
				#endregion

				#region Functions
					// Calling a final function
				case (byte)ExprToken.GlobalFunction:
				case (byte)ExprToken.FinalFunction:
					_Buffer.ReadNameIndex();
					AddNameIndexCodeSize();
					while( DeserializeExpr() != (byte)ExprToken.EndFunctionParms );
					DeserializeDebugToken();
					break;

				case (byte)ExprToken.VirtualFunction:
				{
					// Super? UE3
					if( _Buffer.Version >= 178 && _Buffer.Version < 500 )
					{
						_Buffer.ReadByte();
						AddCodeSize( sizeof(byte) );
					}
					_Buffer.ReadNameIndex();
					AddNameIndexCodeSize();
					while( DeserializeExpr() != (byte)ExprToken.EndFunctionParms );
					DeserializeDebugToken();
					break;
				}

				// Redefined, can be FloatToByte!(UE1)
				case (byte)ExprToken.DelegateFunction:
					// Older than this don't make use of PrimitiveCast so handle the cast right in here.
					if( _Buffer.Version < PrimitveCastVersion )
					{
						// FloatToByte!
						DeserializeExpr();
					}
					else
					{
						if( _Buffer.Version > 180 )
						{
							_Buffer.UR.ReadBoolean();
							AddCodeSize( sizeof(bool) );
						}
						_Buffer.ReadIndex();
						AddCodeSize( sizeof(int) );
						_Buffer.ReadNameIndex();
						AddNameIndexCodeSize();
						while( DeserializeExpr() != (byte)ExprToken.EndFunctionParms );
						DeserializeDebugToken();
						_bAddSemiColon = true;
					}
					break;
				#endregion

				#region Arrays
				case (byte)ExprToken.ArrayElement:
				case (byte)ExprToken.DynArrayElement:
					DeserializeExpr();
					DeserializeExpr();
					break;

				case (byte)ExprToken.DynArrayLength:
					DeserializeExpr();
					break;

				// Redefined, can be BoolToByte!
				case (byte)ExprToken.DynArrayInsert:
					// Older than this don't make use of PrimitiveCast so handle the cast right in here.
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();
						DeserializeExpr();
					}
					break;

				// Redefined, can be NameToString!
				case (byte)ExprToken.DynArrayInsertItem:
					// Older than this don't make use of PrimitiveCast so handle the cast right in here.
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();
						DeserializeExpr();
					}
					break;

				// Redefined, can be BoolToInt!
				case (byte)ExprToken.DynArrayRemove:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();
						DeserializeExpr();	
					}
					break;

				// Redefined, can be VectorToString!
				case (byte)ExprToken.DynArrayRemoveItem:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();
						DeserializeExpr();	
					}
					break;

				// Redefined, can be FloatToString!
				case (byte)ExprToken.DynArrayAdd:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();
						DeserializeExpr();	
					}
					break;

				// Redefined, can be ObjectToString!
				case (byte)ExprToken.DynArrayAddItem:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else
					{
						DeserializeExpr();
						DeserializeExpr();
						DeserializeExpr();	
					}
					break;
				#endregion

					//case (byte)CastToken.ByteToInt:
				case (byte)ExprToken.ReturnNothing:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					else if( _Buffer.Version > 300 )
					{
						_Buffer.ReadObjectIndex();
						AddCodeSize( sizeof(int) );
					}
					break;

				// UE1 Compatible
				case (byte)CastToken.StringToByte:
				//case (byte)CastToken.StringToInt:	
				//case (byte)CastToken.StringToBool:	
				case (byte)CastToken.StringToFloat:				
				case (byte)CastToken.StringToVector:		
				case (byte)CastToken.StringToRotator:	
				case (byte)CastToken.VectorToBool:	
				case (byte)CastToken.VectorToRotator:	
				case (byte)CastToken.RotatorToBool:			
				//case (byte)CastToken.ByteToString:			
				case (byte)CastToken.IntToString:					
				//case (byte)CastToken.FloatToString:		
				//case (byte)CastToken.ObjectToString:	
				//case (byte)CastToken.NameToString:				
				//case (byte)CastToken.VectorToString:		
				//case (byte)CastToken.RotatorToString:	
				//case (byte)CastToken.ByteToFloat:
				//case (byte)CastToken.ByteToBool:
				//case (byte)CastToken.ByteToInt:
				//case (byte)CastToken.IntToByte:
				//case (byte)CastToken.IntToBool:
				//case (byte)CastToken.IntToFloat:
				//case (byte)CastToken.BoolToByte:		
				//case (byte)CastToken.BoolToString:		
				//case (byte)CastToken.StringToName:
					if( _Buffer.Version < PrimitveCastVersion )
					{
						DeserializeExpr();
					}
					break;

				default:
					//System.Windows.Forms.MessageBox.Show( "Unknown Token:" + Token );
					break;
			}
			return token;
		}

		protected void DeserializeDebugToken()
		{
			#if !PUBLICRELEASE
			if( _CodePosition < _ScriptSize )
			{
				int GVersion = 0;

				_Buffer.StartPeek();
				byte token = FixToken( _Buffer.ReadByte() );	
				if(	token == (byte)ExprToken.DebugInfo )
				{
					GVersion = _Buffer.ReadInt32();
				}
				_Buffer.EndPeek();

				if( GVersion == 100 )
				{
					DeserializeExpr();
				}
			}
			#endif
		}	
		
#endif
		#endregion

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
