//#define SUPPRESS_BOOLINTEXPLOIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UELib.Tokens;

/**
 * BUGS:
 *		foreach ends with a break after the end of it.
 */		

namespace UELib.Core
{
	public partial class UStruct
	{
		/// <summary>
		/// Decompiles the bytecodes from the 'Owner'
		/// </summary>
		public class UByteCodeDecompiler : IUnrealDecompileable
		{
			/// <summary>
			/// The Struct that contains the bytecode that we have to deserialize and decompile!
			/// </summary>
			[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes" )]
			public readonly UStruct Owner;

			/// <summary>
			/// Pointer to the ObjectStream buffer of 'Owner'
			/// </summary>
			private readonly UObjectStream _Buffer;

			private List<Token> _Tokens;
			/// <summary>
			/// All deserialized tokens.
			/// 
			/// Accessible after calling Deserialize()
			/// </summary>
			public List<Token> TokensCollection
			{
				get{ return _Tokens; }
			}

			public Token NextToken
			{
				get{ return _Tokens[++ _CurToken]; }
			}

			public Token PreviousToken
			{
				get{ return _Tokens[-- _CurToken]; }
			}

			public Token CurrentToken
			{
				get{ return _Tokens[_CurToken]; }
			}

			// State
			private int _CurToken = -1;
			public int CurrentIndex
			{
				get{ return _CurToken; }

				set{ _CurToken = value; }
			}

			public uint CurrentPosition
			{
				get{ return CurrentToken != null ? CurrentToken.Position : 0; }
			}

			public UByteCodeDecompiler( UStruct owner )
			{
				Owner = owner;
				_Buffer = Owner._Buffer;
			}

			#region Deserialize
			private uint _CodePosition;
			/// <summary>
			/// The current position in buffer.
			/// </summary>
			public uint CodePosition
			{
				get
				{ 
					/*if( _Buffer.Version >= 220 )
					{
						if( _Buffer.Version > 540 )
						{
							return (uint)_Buffer.Position;
						}
						else
						{
							return (uint)_Buffer.Position - (uint)Owner._ScriptOffset;
						}
					}*/
					return _CodePosition; 
				}
			}

			/// <summary>
			/// Fix the values of UE1/UE2 tokens to match the UE3 token values.
			/// </summary>
			private byte FixToken( byte token )
			{
				if( Owner.Package.Version >= 184 
					&& 
					(
						(token >= (byte)ExprToken.Unknown && token < (byte)ExprToken.ReturnNothing) 
						|| 
						(token > (byte)ExprToken.NoDelegate && token < (byte)ExprToken.ExtendedNative)) 
					)
				{
		  			++ token;
				}
				return token;
			}

			private void AddCodeSize( int size )
			{
				_CodePosition += (uint)size;
			}

			private void AddNameIndexCodeSize()
			{
				_CodePosition += (_Buffer.Version >= UnrealPackage.VNameIndex ? sizeof(long) : (uint)sizeof(int));
			}

			// Greater Than or Equal Than!
			internal const uint ObjectIndexVirtualSizeVersion = 585;//639;	  (definitely NOT since Singularity version)

			private void AddObjectIndexCodeSize()
			{
				_CodePosition += (_Buffer.Version >= ObjectIndexVirtualSizeVersion ? sizeof(long) : (uint)sizeof(int));
			}

			private bool _Deserialized;
			public void Deserialize()
			{
				if( _Deserialized )
					return;

				_Deserialized = true;
				_Buffer.Seek( Owner._ScriptOffset, System.IO.SeekOrigin.Begin );
				_CodePosition = 0;
				var codeSize = Owner.Package.Version < ObjectIndexVirtualSizeVersion ? Owner._ScriptSize : (uint)Owner._MinAlignment;
				
				_CurToken = -1;
				_Tokens = new List<Token>();
				_Labels = new List<ULabelEntry>();
				while( Owner.Package.Version > 300 
					? _Buffer.Position - Owner._ScriptOffset < Owner._ScriptSize 
					: CodePosition < codeSize )
 				{
					try
					{
						DeserializeNext();
					}
					catch( System.SystemException e )
					{
						if( e is System.IO.EndOfStreamException )
						{
							Console.WriteLine( "Couldn't backup from this error! Decompiling aborted!" );
							return;
						}
						Console.WriteLine( "Object:" + Owner.Name );
						Console.WriteLine( "Failed to deserialize token at position:" + CodePosition );
						Console.WriteLine( "Exception:" + e.Message );
						Console.WriteLine( "Stack:" + e.StackTrace );
					}
				}
			}

			private void DeserializeDebugToken()
			{
				#if !PUBLICRELEASE
				_Buffer.StartPeek();
					byte token = FixToken( _Buffer.ReadByte() );	
				_Buffer.EndPeek();

				if(	token == (byte)ExprToken.DebugInfo )
				{
					DeserializeNext();
				}
				#endif
			}

			public NativeFunctionToken FindNT( int nativeIndex )
			{
				var t = new NativeFunctionToken();
				var nt = Owner.Package.NTLPackage != null ? Owner.Package.NTLPackage.FindTable( nativeIndex ) : null;
				if( nt != null )
				{
					t.NativeTable = nt;
				}
				else
				{
					var table = Owner.Package.ExportTableList.Find( 
						e => (e.ClassName == "Function" && ((UFunction)(e.Object)).iNative == nativeIndex) 
					);

					UFunction f = null;
					if( table != null )
					{
						f = table.Object as UFunction;
					}

					if( f != null )
					{
						nt = new NativeTable();
						nt.SetFormat( f );
 						nt.OperPrecedence = f.OperPrecedence;
						nt.Name = nt.Format == (byte)NativeType.Function ? f.Name : f.FriendlyName;

						nt.ByteToken = nativeIndex;
						t.NativeTable = nt;
					}
				}
				return t;
			}

			public Token DeserializeNext( byte tokenCode = (byte)0xFF )
			{
				Token tokenItem = null;

				var tokenPosition = CodePosition;
				byte token = FixToken( tokenCode == 0xFF ? _Buffer.ReadByte() : tokenCode );

				if( tokenCode == 0xFF )
				{
					AddCodeSize( sizeof(byte) );
				}

				if(	token >= (byte)ExprToken.FirstNative )
				{
					tokenItem = FindNT( token );
				}
				else if( token >= (byte)ExprToken.ExtendedNative )
				{
					byte token2 = _Buffer.ReadByte();
					AddCodeSize( sizeof(byte) );
					tokenItem = FindNT( (token - (byte)ExprToken.ExtendedNative) << 8 | token2 );
				}
				else switch( token )
				{
					#region Cast
					case (byte)ExprToken.DynamicCast:
						tokenItem = new DynamicCastToken();
						break;

					case (byte)ExprToken.MetaCast:
						tokenItem = new MetaCastToken();
						break;

					case (byte)ExprToken.InterfaceCast:
						if( _Buffer.Version < PrimitveCastVersion )		// UE1
						{
							tokenItem = new IntToStringToken();
						}
						else
						{
							tokenItem = new InterfaceCastToken();
						}
						break;

					// Redefined, can be RotatorToVector!(UE1)
					case (byte)ExprToken.PrimitiveCast:
						if( _Buffer.Version < PrimitveCastVersion )		// UE1
						{
							tokenItem = new RotatorToVectorToken();
						}
						else											// UE2+
						{
							// Next byte represents the CastToken!
							token = _Buffer.ReadByte();
							AddCodeSize( sizeof(byte) );

							tokenItem = DeserializeCastToken( token );
							//tokenitem = new PrimitiveCastToken();
						}
						break;
					#endregion

					#region Context
					case (byte)ExprToken.ClassContext:
						tokenItem = new ClassContextToken();
						break;

					case (byte)ExprToken.InterfaceContext:
						if( _Buffer.Version < PrimitveCastVersion )
						{
							tokenItem = new ByteToStringToken(); 
						}
						else
						{
							tokenItem = new InterfaceContextToken();
						}
						break;

					case (byte)ExprToken.Context:
						tokenItem = new ContextToken();
						break;

					case (byte)ExprToken.StructMember:
						tokenItem = new StructMemberToken();
						break;
					#endregion

					#region Assigns
					case (byte)ExprToken.Let:
						tokenItem = new LetToken();
						break;

					case (byte)ExprToken.LetBool: 
						tokenItem = new LetBoolToken();
						break;

					case (byte)ExprToken.EndParmValue:
						tokenItem = new EndParmValueToken();
						break;

					// Redefined, can be FloatToBool!(UE1)
					case (byte)ExprToken.LetDelegate:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{			
							tokenItem = new FloatToBoolToken();
						}
						else
						{
							tokenItem = new LetDelegateToken();
						}
						break;

					// Redefined, can be NameToBool!(UE1)
					case (byte)ExprToken.Conditional:
						tokenItem = new ConditionalToken();
						break;

					case (byte)ExprToken.Eval: // case (byte)ExprToken.DynArrayFindStruct: case (byte)ExprToken.Conditional:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new NameToBoolToken();
						}
						else if( _Buffer.Version >= 300 )
						{
							tokenItem = new DynamicArrayFindStructToken();
						}
						else
						{
							tokenItem = new ConditionalToken();
						}
						break;
					#endregion

					#region Jumps
					case (byte)ExprToken.Return:
						tokenItem = new ReturnToken();
						break;

					case (byte)ExprToken.ReturnNothing:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new	ByteToIntToken();
						}
							// Definitely existed since GoW(490)
						else if( _Buffer.Version > 480 && (_Tokens.Count > 0 && !(_Tokens[_Tokens.Count - 1] is ReturnToken)) ) // Should only be done if the last token wasn't Return
						{
							tokenItem = new DynamicArrayInsertToken();
						}
						else 
						{
							tokenItem = new ReturnNothingToken();
						}
						break;

					case (byte)ExprToken.GotoLabel:
						tokenItem = new GoToLabelToken();
						break;

					case (byte)ExprToken.Jump:
						tokenItem = new JumpToken();
						break;

					case (byte)ExprToken.JumpIfNot:
						tokenItem = new JumpIfNotToken();
						break;

					case (byte)ExprToken.Switch:
						tokenItem = new SwitchToken();
						break;

					case (byte)ExprToken.Case:
						tokenItem = new CaseToken();
						break;

					case (byte)ExprToken.DynArrayIterator:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new RotatorToStringToken();
						}
						else
						{
							tokenItem = new ArrayIteratorToken();
						}
						break;

					case (byte)ExprToken.Iterator:
						tokenItem = new IteratorToken();
						break;

					case (byte)ExprToken.IteratorNext:
						tokenItem = new IteratorNextToken();
						break;

					case (byte)ExprToken.IteratorPop: 
						tokenItem = new IteratorPopToken();
						break;
					#endregion

					#region Variables
					case (byte)ExprToken.NativeParm:
						tokenItem = new NativeParameterToken();
						break;

					// Referenced variables that are from this function e.g. Local and params
					case (byte)ExprToken.InstanceVariable:
						tokenItem = new InstanceVariableToken();
						break;

					case (byte)ExprToken.LocalVariable:
						tokenItem = new LocalVariableToken();
						break;

					// Referenced variables that are default
					case (byte)ExprToken.UndefinedVariable:
						tokenItem = new UndefinedVariableToken();
						break;

					case (byte)ExprToken.DefaultVariable:
						tokenItem = new DefaultVariableToken();
						break;

					// UE3+
					case (byte)ExprToken.OutVariable:
						tokenItem = new OutVariableToken();
						break;

					case (byte)ExprToken.BoolVariable:
						tokenItem = new BoolVariableToken();
						break;

					// Redefined, can be FloatToInt!(UE1)
					case (byte)ExprToken.DelegateProperty:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new FloatToIntToken();
						}
						else
						{
							tokenItem = new DelegatePropertyToken();
						}
						break;

					case (byte)ExprToken.DefaultParmValue:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )	 // StringToInt
						{
							tokenItem = new StringToIntToken();
						}
						else
						{
							tokenItem = new DefaultParameterToken();
						}
						break;
					#endregion

					#region Misc
					// Redefined, can be BoolToFloat!(UE1)
					case (byte)ExprToken.DebugInfo:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new BoolToFloatToken();
						}
						else
						{
							tokenItem = new DebugInfoToken();
						}
						break;

					case (byte)ExprToken.Nothing:
						tokenItem = new NothingToken();
						break;

					case (byte)ExprToken.EndFunctionParms:
						tokenItem = new EndFunctionParmsToken();
						break;

					case (byte)ExprToken.IntZero:
						tokenItem = new IntZeroToken();
						break;

					case (byte)ExprToken.IntOne:
						tokenItem = new IntOneToken();
						break;

					case (byte)ExprToken.True:
						tokenItem = new TrueToken();
						break;

					case (byte)ExprToken.False:
						tokenItem = new FalseToken();
						break;

					case (byte)ExprToken.NoDelegate:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new IntToFloatToken();
						}
						else
						{
							tokenItem = new NoDelegateToken();
						}
						break;

						// No value passed to an optional parameter.
					case (byte)ExprToken.NoParm:
						tokenItem = new NoParmToken();
						break;

					case (byte)ExprToken.NoObject:
						tokenItem = new NoObjectToken();
						break;

					case (byte)ExprToken.Self:
						tokenItem = new SelfToken();
						break;

					// End of state code.
					case (byte)ExprToken.Stop:
						tokenItem = new StopToken();
						break;		

					case (byte)ExprToken.Assert:
						tokenItem = new AssertToken();
						break;

					case (byte)ExprToken.LabelTable:
						tokenItem = new LabelTableToken();
						break;

					case (byte)ExprToken.EndOfScript:	//CastToken.BoolToString:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new BoolToStringToken();
						}
						else
						{
							tokenItem = new EndOfScriptToken();
						}
						break;

					case (byte)ExprToken.Skip:
						tokenItem = new SkipToken();
						break;

					case (byte)ExprToken.StructCmpEq:
						tokenItem = new StructCmpEqToken();
						break;

					case (byte)ExprToken.StructCmpNe:
						tokenItem = new StructCmpNeToken();
						break;

					case (byte)ExprToken.DelegateCmpEq:
						tokenItem = new DelegateCmpEqToken();
						break;

					case (byte)ExprToken.DelegateFunctionCmpEq:
						if( _Buffer.Version < PrimitveCastVersion )
						{
							tokenItem = new IntToBoolToken();
						}
						else
						{
							tokenItem = new DelegateFunctionCmpEqToken();
						}
						break;
				
					case (byte)ExprToken.DelegateCmpNe:
						tokenItem = new DelegateCmpNeToken();
						break;

					case (byte)ExprToken.DelegateFunctionCmpNe:
						if( _Buffer.Version < PrimitveCastVersion )
						{
							tokenItem = new IntToBoolToken();
						}
						else
						{
							tokenItem = new DelegateFunctionCmpNeToken();
						}
						break;

					case (byte)ExprToken.EatString:
						tokenItem = new EatStringToken();
						break;

					case (byte)ExprToken.New:
						tokenItem = new NewToken();
						break;

					case (byte)ExprToken.FunctionEnd: // case (byte)ExprToken.DynArrayFind:
						if( _Buffer.Version < 300 )
						{
							tokenItem = new FunctionEndToken();
						}
						else
						{
							tokenItem = new DynamicArrayFindToken();
						}
						break;
					#endregion

					#region Constants
					case (byte)ExprToken.IntConst:
						tokenItem = new IntConstToken();
						break;

					case (byte)ExprToken.ByteConst:
						tokenItem = new ByteConstToken();
						break;

					case (byte)ExprToken.IntConstByte:
						tokenItem = new IntConstByteToken();
						break;

					case (byte)ExprToken.FloatConst:
						tokenItem = new FloatConstToken();
						break;

					// ClassConst?
					case (byte)ExprToken.ObjectConst:
						tokenItem = new ObjectConstToken();
						break;

					case (byte)ExprToken.NameConst:
						tokenItem = new NameConstToken();
						break;

					case (byte)ExprToken.StringConst:
						tokenItem = new StringConstToken();
						break;

					case (byte)ExprToken.UniStringConst:
						tokenItem = new UniStringConstToken();
						break;

					case (byte)ExprToken.RotatorConst:
						tokenItem = new RotatorConstToken();
						break;

					case (byte)ExprToken.VectorConst:
						tokenItem = new VectorConstToken();
						break;
					#endregion

					#region Functions
					case (byte)ExprToken.FinalFunction:
						tokenItem = new FinalFunctionToken();
						break;

					case (byte)ExprToken.VirtualFunction:
						tokenItem = new VirtualFunctionToken();
						break;

					case (byte)ExprToken.GlobalFunction:
						tokenItem = new GlobalFunctionToken();
						break;

					// Redefined, can be FloatToByte!(UE1)
					case (byte)ExprToken.DelegateFunction:
						if( _Buffer.Version < PrimitveCastVersion )
						{
							tokenItem = new FloatToByteToken();
						}
						else
						{
							tokenItem = new DelegateFunctionToken();
						}
						break;
					#endregion

					#region Arrays
					case (byte)ExprToken.ArrayElement:
						tokenItem = new ArrayElementToken();
						break;

					case (byte)ExprToken.DynArrayElement:
						tokenItem = new DynamicArrayElementToken();
						break;

					case (byte)ExprToken.DynArrayLength:
						tokenItem = new DynamicArrayLengthToken();
						break;

					case (byte)ExprToken.DynArrayInsert:
						if( _Buffer.Version < PrimitveCastVersion )
						{
							tokenItem = new BoolToByteToken();
						}
						else
						{
							tokenItem = new DynamicArrayInsertToken();
						}
						break;

					case (byte)ExprToken.DynArrayInsertItem:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new VectorToStringToken();
						}
						else
						{
							tokenItem = new DynamicArrayInsertItemToken();	
						}
						break;

					// Redefined, can be BoolToInt!(UE1)
					case (byte)ExprToken.DynArrayRemove:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new BoolToIntToken();
						}
						else
						{
							tokenItem = new DynamicArrayRemoveToken();
						}
						break;

					case (byte)ExprToken.DynArrayRemoveItem:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new NameToStringToken();
						}
						else
						{
							tokenItem = new DynamicArrayRemoveItemToken();
						}
						break;

					case (byte)ExprToken.DynArrayAdd:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new FloatToStringToken();
						}
						else
						{
							tokenItem = new DynamicArrayAddToken();
						}
						break;

					case (byte)ExprToken.DynArrayAddItem:
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							tokenItem = new ObjectToStringToken();
						}
						else
						{
							tokenItem = new DynamicArrayAddItemToken();
						}
						break;

					case (byte)ExprToken.DynArraySort:
						tokenItem = new DynamicArraySortToken();
						break;

					// See FunctionEnd and Eval
					/*case (byte)ExprToken.DynArrayFind:
						break;

					case (byte)ExprToken.DynArrayFindStruct:
						break;*/

					#endregion

					default:
					{
						#region Casts
						if( _Buffer.Version < Core.UStruct.PrimitveCastVersion )
						{
							// No other token was matched. Check if it matches any of the CastTokens
							// We don't just use PrimitiveCast detection due compatible with UE1 games
							tokenItem = DeserializeCastToken( token );
						}
						break;
						#endregion
					}	
				}

				if( tokenItem == null )
				{
					tokenItem = new UnknownExprToken();	
				}
				
				tokenItem.Decompiler = this;
				tokenItem.RepresentToken = token;
				tokenItem.Position = tokenPosition;// + (uint)Owner._ScriptOffset;
				tokenItem.StoragePosition = (uint)_Buffer.Position - (uint)Owner._ScriptOffset;
				// IMPORTANT:Add before deserialize, due the possibility that the tokenitem might deserialize other tokens as well.
				_Tokens.Add( tokenItem );
				tokenItem.Deserialize();
				// Includes all sizes of followed tokens as well! e.g. i = i + 1; is summed here but not i = i +1; (not>>)i ++;
				tokenItem.Size = (ushort)(CodePosition - tokenPosition);
				tokenItem.StorageSize = (ushort)(tokenItem.StoragePosition - ((uint)_Buffer.Position - (uint)Owner._ScriptOffset));
				tokenItem.PostDeserialized();
				return tokenItem;
			}

			[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic" )]
			private Token DeserializeCastToken( byte castToken )
			{
				Token tokenitem = null;
				switch( (Tokens.CastToken)castToken )
				{
					case Tokens.CastToken.StringToRotator:
						tokenitem = new StringToRotatorToken();
						break;

					case Tokens.CastToken.VectorToRotator:
						tokenitem = new VectorToRotatorToken();
						break;

					case Tokens.CastToken.StringToVector:
						tokenitem = new StringToVectorToken();
						break;

					case Tokens.CastToken.RotatorToVector:
						tokenitem = new RotatorToVectorToken();
						break;

					case Tokens.CastToken.IntToFloat:
						tokenitem = new IntToFloatToken();
						break;

					case Tokens.CastToken.StringToFloat:
						tokenitem = new StringToFloatToken();
						break;

					case Tokens.CastToken.BoolToFloat:
						tokenitem = new BoolToFloatToken();
						break;

					case Tokens.CastToken.StringToInt:
						tokenitem = new StringToIntToken();
						break;

					case Tokens.CastToken.FloatToInt:
						tokenitem = new FloatToIntToken();
						break;

					case Tokens.CastToken.BoolToInt:
						//++ Owner._NumBoolToInt;
						#if PUBLICRELEASE
						if( _NumIntToBool > 1 && _NumIntToBool > 1 )
						{
							for( int i = 0; i < 10; ++ i )
							{
								SerializeExpr();
								_CodePosition += (uint)(10*i);
							}
							throw new DecompilingCastException();
						}
						#else
						tokenitem = new BoolToIntToken();
						#endif
						break;

					case Tokens.CastToken.RotatorToBool:
						tokenitem = new RotatorToBoolToken();
						break;

					case Tokens.CastToken.VectorToBool:
						tokenitem = new VectorToBoolToken();
						break;

					case Tokens.CastToken.StringToBool:
						tokenitem = new StringToBoolToken();
						break;

					case Tokens.CastToken.ByteToBool:	
						tokenitem = new ByteToBoolToken();
						break;

					case Tokens.CastToken.FloatToBool:
						tokenitem = new FloatToBoolToken();
						break;

					case Tokens.CastToken.NameToBool:
						tokenitem = new NameToBoolToken();
						break;

					case Tokens.CastToken.ObjectToBool:
						tokenitem = new ObjectToBoolToken();
						break;

					case Tokens.CastToken.IntToBool:
						//++ Owner._NumIntToBool;
						#if PUBLICRELEASE
						if( _NumIntToBool > 1 && _NumIntToBool > 1 )
						{
							for( int i = 0; i < 10; ++ i )
							{
								SerializeExpr();
								_CodePosition += (uint)(10*i);
							}
							throw new DecompilingCastException();
						}
						#else	
						tokenitem = new IntToBoolToken();
						#endif
						break;

					case Tokens.CastToken.StringToByte:
						tokenitem = new StringToByteToken();
						break;

					case Tokens.CastToken.FloatToByte:
						tokenitem = new FloatToByteToken();
						break;

					case Tokens.CastToken.BoolToByte:
						tokenitem = new BoolToByteToken();
						break;

					case Tokens.CastToken.ByteToString:
						tokenitem = new ByteToStringToken();
						break;

					case Tokens.CastToken.IntToString:
						tokenitem = new IntToStringToken();
						break;

					case Tokens.CastToken.BoolToString:
						tokenitem = new BoolToStringToken();
						break;

					case Tokens.CastToken.FloatToString:
						tokenitem = new FloatToStringToken();
						break;

					case Tokens.CastToken.NameToString:
						tokenitem = new NameToStringToken();
						break;

					case Tokens.CastToken.VectorToString:
						tokenitem = new VectorToStringToken();
						break;

					case Tokens.CastToken.RotatorToString:
						tokenitem = new RotatorToStringToken();
						break;

					case Tokens.CastToken.StringToName:
						tokenitem = new StringToNameToken();
						break;

					case Tokens.CastToken.ByteToInt:
						tokenitem = new ByteToIntToken();
						break;

					case Tokens.CastToken.IntToByte:
						tokenitem = new IntToByteToken();
						break;

					case Tokens.CastToken.ByteToFloat:
						tokenitem = new ByteToFloatToken();
						break;

					case Tokens.CastToken.ObjectToString:
						tokenitem = new ObjectToStringToken();
						break;

					case Tokens.CastToken.InterfaceToString:
						tokenitem = new InterfaceToStringToken();
						break;

					case Tokens.CastToken.InterfaceToBool:
						tokenitem = new InterfaceToBoolToken();
						break;
				}

				// Unsure what this is, found in:  ClanManager1h_6T.CMBanReplicationInfo.Timer:	
				//	xyz = UnknownCastToken(0x1b);
				//	UnknownCastToken(0x1b)
				//	UnknownCastToken(0x1b)
				if( castToken == 0x1b )
					tokenitem = new FloatToIntToken();

				return tokenitem ?? (new UnknownCastToken());
			}
			#endregion

#if DECOMPILE
			#region Decompile
			public class NestManager
			{
				public UByteCodeDecompiler Decompiler;

				public class Nest : IUnrealDecompileable
				{
					[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue" )]
					public enum NestType : byte
					{
						If					= 1,
						Else				= 2, Fake				= 3,
						ForEach				= 4,
						Switch				= 5, Case				= 6, Default			= 7,
						SwitchBreak			= 8,
						Loop				= 9
					}

					/// <summary>
					/// Position of this Nest (CodePosition)
					/// </summary>
					public uint Position;

					public NestType Type;
					public Token Creator;

					public virtual string Decompile()
					{
						return String.Empty;
					}

					public bool IsPastOffset( uint position )
					{
						return position >= Position;
					}

					public override string ToString()
					{
						return "Type:" + Type + " Position:" + Position;
					}
				}

				public class NestBegin : Nest
				{
					public override string Decompile()
					{
						#if DEBUG && DEBUG_NESTS
							return "\r\n" + UDecompiler.Tabs + "// BEGIN_NEST:TYPE:" + Type;
						#endif
						return Type != NestType.Case && Type != NestType.Default 
							? UnrealConfig.PrintBeginBracket() 
							: String.Empty;
					}
				}

				public class NestEnd : Nest
				{
					public override string Decompile()
					{
						#if DEBUG && DEBUG_NESTS
							return "\r\n" + UDecompiler.Tabs + "// END_NEST:TYPE:" + Type;
						#endif
						return Type != NestType.Case && Type != NestType.Default 
							? UnrealConfig.PrintEndBracket() 
							: String.Empty;
					}
				}

				public List<Nest> Nests = new List<Nest>();

				public NestBegin AddNestBegin( Nest.NestType type, uint position, Token creator = null )
				{	
					var n = new NestBegin {Position = position, Type = type};
					Nests.Add( n );
					n.Creator = creator ?? Decompiler.CurrentToken;
					return n;
				}

				public NestEnd AddNestEnd( Nest.NestType type, uint position, Token creator = null )
				{
					var n = new NestEnd {Position = position, Type = type};
					Nests.Add( n );
					n.Creator = creator ?? Decompiler.CurrentToken;
					return n;
				}
			}

			private NestManager _Nester = null;

			// Checks if we're currently within a nest of type nestType in any stack!
			private NestManager.Nest IsWithinNest( NestManager.Nest.NestType nestType )
			{
				for( int i = _NestChain.Count - 1; i >= 0; -- i )
				{
					if( _NestChain[i].Type == nestType )
					{
						return _NestChain[i];
					}
				}
				return null;
			}

			private NestManager.Nest GetMostRecentEndNest( NestManager.Nest.NestType nestType )
			{
				for( int i = _Nester.Nests.Count - 1; i >= 0; -- i )
				{
					if( _Nester.Nests[i] is NestManager.NestEnd && _Nester.Nests[i].Type == nestType )
					{
						return _Nester.Nests[i];
					}
				}
				return null;
			}

			// Checks if the current nest is of type nestType in the current stack!
			// Only BeginNests that have been decompiled will be tested for!
			private NestManager.Nest IsInNest( NestManager.Nest.NestType nestType )
			{
				int i = _NestChain.Count - 1;
				if( i == -1 )
					return null;

				if( _NestChain[i].Type == nestType )
				{
					return _NestChain[i];
				}
				return null;
			}

			private NestManager.Nest CurNestBegin()
			{
				for( int i = _Nester.Nests.Count - 1; i >= 0; -- i )
				{
					if( _Nester.Nests[i] is NestManager.NestBegin )
					{
						return _Nester.Nests[i];
					}
				}
				return null;
			}

			private NestManager.Nest CurNestEnd()
			{
				for( int i = _Nester.Nests.Count - 1; i >= 0; -- i )
				{
					if( _Nester.Nests[i] is NestManager.NestEnd )
					{
						return _Nester.Nests[i];
					}
				}
				return null;
			}

			public void InitDecompile()
			{
				_NestChain.Clear();

				_Nester = new NestManager{Decompiler = this};
				_CurToken = -1;

				if( _Buffer.Version > 300 && Owner is UFunction )
				{
					var func = Owner as UFunction;
					// Step up to the first parameter that is optional, this is where the DefaultParameterToken will get the variable names from!
					for( DefaultParameterToken._paramNum = 0; DefaultParameterToken._paramNum < func.ChildParams.Count; ++ DefaultParameterToken._paramNum )
					{
						if( func.ChildParams[DefaultParameterToken._paramNum].HasPropertyFlag( Flags.PropertyFlagsLO.OptionalParm ) )
						{
							break;
						}
					}
				}

				// Reset these, in case of a loop in the Decompile function that did not finish due exception errors!
				_WithinClassContext = false;
				AddSemicolon = false;
				PostIncrementTabs = 0;
				PostDecrementTabs = 0;
				PreIncrementTabs = 0;
				PreDecrementTabs = 0;
				PreComment = String.Empty;
				PostComment = String.Empty;

				_TempLabels = new List<ULabelEntry>();
				if( _Labels != null )
				{
					for( int i = 0; i < _Labels.Count; ++ i )
					{
						// No duplicates, caused by having multiple goto's with the same destination
						if( !_TempLabels.Exists( (p) => p.Position == _Labels[i].Position ) )
						{
							_TempLabels.Add( _Labels[i] );
						}
					}
				}
			}

			public void Goto( ushort codeOffset )
			{
				_CurToken = _Tokens.FindIndex( (t) => t.Position == codeOffset );
			}

			/// <summary>
			/// Whether we are currently decompiling within a ClassContext token.
			/// 
			/// HACK: For static calls -> class'ClassA'.static.FuncA();
			/// </summary>
			private bool _WithinClassContext;

			public bool AddSemicolon;

			public byte PostIncrementTabs;
			public byte PostDecrementTabs;
			public byte PreIncrementTabs;
			public byte PreDecrementTabs;

			public string PreComment;
			public string PostComment;

			public string Decompile()
			{
				// Make sure that everything is deserialized!
				if( !_Deserialized )
				{
					Deserialize();
				}

				string output = String.Empty;
				string initTabs = UDecompiler.Tabs;
#if DEBUG && DEBUG_TOKENPOSITIONS
				UDecompiler.AddTabs( 3 );
#endif			
				try
				{
					//Initialize==========
					InitDecompile();	
					bool spewOutput = false;
					int tokenBeginIndex = 0;

#if DEBUG && DEBUG_FUNCTIONINFO
					output += String.Format( "\tObjectSize:{0}\r\n", Owner.Buffer.Length );
					output += String.Format( "\tScriptBytecodeSize:{0}\r\n", Owner._MinAlignment );
					output += String.Format( "\tScriptOffset:{0}\r\n", Owner._ScriptOffset );
					output += String.Format( "\tScriptStorageSize:{0}\r\n", Owner._ScriptSize );
#endif

					#if DEBUG && DEBUG_HIDDENTOKENS
						while( (_CurToken + 1) < _Tokens.Count - 1 )
					#else
						while( (_CurToken + 1) < _Tokens.Count - 3 )
					#endif	
					{
						try
						{
							//Preprocess output==========

							//Decompile chain==========
							{
								string tokenOutput; 
								Token t = NextToken;	
								output += DecompileLabels();
								try
								{
									// FIX: Formatting issue on debug-compiled packages
									if( t is DebugInfoToken )
									{
										string nesto = DecompileNests();
										if( nesto.Length != 0 )
										{
											output += nesto;
											spewOutput = true;
										}
										continue;
									}
								}
								catch( Exception e )
								{
									output += "// (" + e.GetType().Name + ")";
								}

								try
								{
									tokenBeginIndex = _CurToken;
									tokenOutput = t.Decompile();
									FunctionToken.lastOperPrecedence = 255;
								}
								catch( Exception e )
								{
									tokenOutput = t.GetType().Name + "-" + CurrentToken.GetType().Name + "(" + e.ToString() + ")";
								}

								// HACK: for multiple cases for one block of code, etc!
								if( PreDecrementTabs > 0 )
								{
									UDecompiler.RemoveTabs( PreDecrementTabs );
									PreDecrementTabs = 0;
								}

								if( PreIncrementTabs > 0 )
								{
									UDecompiler.AddTabs( PreIncrementTabs );
									PreIncrementTabs = 0;
								}

								if( !UnrealConfig.SuppressComments )
								{
									if( PreComment.Length != 0 )
									{
										tokenOutput = PreComment + "\r\n" + UDecompiler.Tabs + tokenOutput;
										PreComment = String.Empty;
									}

									if( PostComment.Length != 0 )
									{
										tokenOutput += PostComment;
										PostComment = String.Empty;
									}
								}

								//if( tokenOutput == "" )
								//{
								//    tokenOutput = "VoidToken:" + t;
								//}
								//else
								//{
								//    tokenOutput = "/*" + t.Position + "*/" + tokenOutput + "/*" + CurrentToken.Position + "*/";
								//}

								//Preprocess output==========
								

								// Previous did spew and this one spews? then a new line is required!
								if( tokenOutput.Length != 0 )
								{
									// Spew before?
									if( spewOutput )
									{
										output += "\r\n";
									}
									spewOutput = true;
								}

								#if DEBUG && DEBUG_TOKENPOSITIONS
									output += String.Format( "\t{0:x4}|{1:x4}", t.Position, CurrentToken.Position + CurrentToken.Size );
									UDecompiler.RemoveTabs( 3 );
								#endif

								if( spewOutput )
								{
									output += UDecompiler.Tabs + tokenOutput;
									// One of the decompiled tokens wanted to be ended.
									if( AddSemicolon )
									{
										output += ";";
										AddSemicolon = false;
									}
								}
								#if DEBUG && DEBUG_HIDDENTOKENS
									else
									{
										output += UDecompiler.Tabs + "// " + t.GetType().Name + ";";
									}
									UDecompiler.AddTabs( 3 );
								#endif
							}

							//Postprocess output==========
							if( PostDecrementTabs > 0 )
							{
								UDecompiler.RemoveTabs( PostDecrementTabs );
								PostDecrementTabs = 0;
							}

							if( PostIncrementTabs > 0 )
							{
								UDecompiler.AddTabs( PostIncrementTabs );
								PostIncrementTabs = 0;
							}

							try
							{
								string nesto = DecompileNests();
								if( nesto.Length != 0 )
								{
									output += nesto;
									spewOutput = true;
								}

								output += DecompileLabels( false );
								// spewOutput = true;
							}
							catch( Exception e )
							{
								output += "\r\n" + UDecompiler.Tabs + "// Failed to format nests!:" + e + "\r\n" 
									+ UDecompiler.Tabs + "// " + _Nester.Nests.Count + " & " + _Nester.Nests[_Nester.Nests.Count-1];
								spewOutput = true;		
							}

							//Reset==========
						}
						catch( Exception e )
						{
							output += "\r\n" + UDecompiler.Tabs + "// Failed to decompile this line:\r\n";
							UDecompiler.AddTabs( 1 );
							output += UDecompiler.Tabs + "/* " + FormatTokens( tokenBeginIndex, _CurToken ) + " */\r\n";
							UDecompiler.RemoveTabs( 1 );
							output += UDecompiler.Tabs + "// " + FormatTabs( e.Message );
							spewOutput = true;
						}
					}

					try
					{
						// Decompile remaining nests
						output += DecompileNests( true );
					}
					catch( Exception e )
					{
						output += "\r\n" + UDecompiler.Tabs + "// Failed to format remaining nests!:" + e.ToString() + "\r\n" 
							+ UDecompiler.Tabs + "// " + _Nester.Nests.Count + " & " + _Nester.Nests[_Nester.Nests.Count-1].ToString();
						spewOutput = true;		
					}
					
				}
				catch( Exception e )
				{
					output += UDecompiler.Tabs + "// Failed to decompile this " + Owner.Class.Name + "'s code.\r\n"
						+ UDecompiler.Tabs + _CodePosition + "\r\n"	
						+ FormatTabs( e.Message ) + "\r\n\r\n" + FormatTabs( e.StackTrace );
				}
				UDecompiler.Tabs = initTabs;
				FunctionToken.lastOperPrecedence = 255;
				return output;
			}

			private List<NestManager.Nest> _NestChain = new List<NestManager.Nest>();

			private static string FormatTabs( string nonTabbedText )
			{
				return nonTabbedText.Replace( "\n", "\n" + UDecompiler.Tabs );
			}

			private string FormatTokens( int beginIndex, int endIndex )
			{
				string output = String.Empty;
				for( int i = beginIndex; i < endIndex; ++ i )
				{
					output += _Tokens[i].GetType().Name + (i % 4 == 0 ? "\r\n" + UDecompiler.Tabs : " ");
				}
				return output;
			}

			private string DecompileLabels( bool stateLabel = true )
			{
				string output = String.Empty;
				for( int i = 0; i < _TempLabels.Count; ++ i )
				{
					if( _Tokens[_CurToken + 1].Position >= _TempLabels[i].Position )
					{
						if( ((stateLabel && !_TempLabels[i].Name.StartsWith( "J0x" )) || (!stateLabel && _TempLabels[i].Name.StartsWith( "J0x" ))) )
						{
							output += "\r\n" + (stateLabel ? _TempLabels[i].Name : UDecompiler.Tabs + _TempLabels[i].Name) + ":\r\n";

							_TempLabels.RemoveAt( i );
							-- i;
						}
					}
				}
				return output;
			}

			private string DecompileNests( bool force = false )
			{
				string output = String.Empty;

				// Give { priority hence separated loops
				for( int i = 0; i < _Nester.Nests.Count; ++ i )
				{
					if( _Nester.Nests[i] is NestManager.NestBegin ) // {
					{
						if( _Nester.Nests[i].IsPastOffset( CurrentToken.Position ) || force )
						{
							output += _Nester.Nests[i].Decompile();
							UDecompiler.AddTabs( 1 );

							_NestChain.Add( _Nester.Nests[i] );
							_Nester.Nests.RemoveAt( i -- );
						}
					}
				}

				for( int i = 0; i < _Nester.Nests.Count; ++ i )
				{
					if( _Nester.Nests[i] is NestManager.NestEnd ) // }
					{
						if( _Nester.Nests[i].IsPastOffset( CurrentToken.Position + CurrentToken.Size ) || force )
						{
							UDecompiler.RemoveTabs( 1 );
							output += _Nester.Nests[i].Decompile();

							// TODO: This should not happen!
							if( _NestChain.Count > 0 )
							{
								_NestChain.RemoveAt( _NestChain.Count - 1 );
							}
							_Nester.Nests.RemoveAt( i -- );
						}
					}
				}
				return output;
			}
			#endregion

			#region Disassemble
			[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic" )]
			public string Disassemble()
			{
				return String.Empty;
			}
			#endregion

			#region Tokens
			public class Token : IUnrealDecompileable
			{
				public UByteCodeDecompiler Decompiler;

				protected UObjectStream Buffer
				{
					get{ return Decompiler._Buffer; }
				}

				protected UnrealPackage Package
				{
					get{ return Decompiler.Owner.Package; }
				}

				public byte RepresentToken;	 // Fixed(adjusted at decompile time for compatibly)

				/// <summary>
				/// The relative position of this token.
				/// Storage--The actual token position within the Buffer. 
				/// </summary>
				public uint Position;
				public uint StoragePosition;

				/// <summary>
				/// The size of this token and its inlined tokens.
				/// Storage--The actual token size within the Buffer.
				/// </summary>
				public ushort Size;
				public ushort StorageSize;

				public override string ToString()
				{
					return String.Format( "\r\nType:{0}\r\nToken:{1:x2}\r\nPosition:{2}\r\nSize:{3}", GetType().Name, RepresentToken, Position, Size ).Replace( "\n", "\n" + UDecompiler.Tabs );
				}

				public virtual void Deserialize()
				{
				}

				public virtual void PostDeserialized()
				{
				}

				public virtual string Decompile()
				{
					return String.Empty;
				}

				public virtual string Disassemble()
				{
					return String.Format( "0x{0:x2}", RepresentToken );
				}

				public string DecompileNext()
				{
					tryNext:
					var t = Decompiler.NextToken;
					if( t is DebugInfoToken )
					{
						goto tryNext;
					}

					try
					{
						return t.Decompile();
					}
					catch( Exception e )
					{
						return t.GetType().Name + "(" + e.GetType().Name + ")";
					}
				}

				public Token GrabNextToken()
				{
					tryNext:
					var t = Decompiler.NextToken;
					if( t is DebugInfoToken )
					{
						goto tryNext;
					}
					return t;
				}

				public Token DeserializeNext()
				{
					return Decompiler.DeserializeNext();
				}

				public Token DeserializeNext( byte tokenCode )
				{
					return Decompiler.DeserializeNext( tokenCode );
				}

				public string DisassambleNext()
				{
					return Decompiler.NextToken.Disassemble();
				}
			}

			#region FunctionTokens
			public class EndFunctionParmsToken : Token
			{
				public override string Decompile()
				{
					return ")";
				}
			}

			public abstract class FunctionToken : Token
			{
				protected void DeserializeCall()
				{
					DeserializeParms();
					Decompiler.DeserializeDebugToken();
				}

				protected void DeserializeParms()
				{
					while( !(DeserializeNext() is EndFunctionParmsToken) );
				}

				protected void DeserializeBinaryOperator()
				{
					DeserializeNext();
					DeserializeNext();

					DeserializeNext(); // )
					Decompiler.DeserializeDebugToken();
				}

				protected void DeserializeUnaryOperator()
				{
					DeserializeNext();

					DeserializeNext(); // )
					Decompiler.DeserializeDebugToken();
				}

				protected string DecompilePreOperator( string operatorName )
				{
					string output = operatorName + (operatorName.Length > 1 ? " " : String.Empty) + DecompileNext();
					DecompileNext(); // )
					return output;
				}

				internal static byte lastOperPrecedence = 255;
				protected string DecompileOperator( string operatorName, byte operPrecedence = (byte)0 )
				{
					string output = DecompileNext() + " " + operatorName + " " + DecompileNext(); 
					/*if( operPrecedence < lastOperPrecedence )
					{
						output = "(" + output + ")";
					}*/
					lastOperPrecedence = operPrecedence;
					DecompileNext(); // )
					return output;
				}

				protected string DecompilePostOperator( string operatorName )
				{
					string output = operatorName + " " + DecompileNext();
					DecompileNext(); // )
					return output;
				}

				protected string DecompileCall( string functionName )
				{
					if( Decompiler._WithinClassContext )
					{
						functionName = "static." + functionName;	
				
						// Set false elsewhere as well but to be sure we set it to false here to avoid getting static calls inside the params.
						// e.g. 	
						// A1233343.DrawText(Class'BTClient_Interaction'.static.A1233332(static.Max(0, A1233328 - A1233322[A1233222].StartTime)), true);
						Decompiler._WithinClassContext = false;
					}
					string output = functionName + "(" + DecompileParms();
					return output;
				}

				protected string DecompileParms()
				{
					var outputBuilder = new List<string>();
					{Token t;
					do
					{
						// Using GrabNextToken over DecompileNest fixes a format issue when the code is compiled in debug mode.
						t = GrabNextToken();	// Skips debug tokens!
						if( t is NoParmToken )
							continue;

						outputBuilder.Add( t.Decompile() );
						lastOperPrecedence = 255;
					} while( !(t is EndFunctionParmsToken) );}

					string output = String.Empty;
					for( int i = 0; i < outputBuilder.Count; ++ i )
					{
						//if( outputBuilder[i] == ")" )
						//	break;

						output += outputBuilder[i];
						if( i < outputBuilder.Count - 2 )
						{
							// Format handling for params that were skipped(optionals), add a space if not skipped!
							output += String.IsNullOrEmpty( outputBuilder[i + 1] ) ? "," : ", ";
						}
					}

					// ----Trim all , that are decompiled in UE3 due the inserted NoParmToken's
					return output;
				}
			}

			public class FinalFunctionToken : FunctionToken
			{
				public int FunctionIndex;

				public override void Deserialize()
				{
					FunctionIndex = Buffer.ReadObjectIndex();
					Decompiler.AddObjectIndexCodeSize();

					DeserializeCall();
				}

				public override string Decompile()
				{
					string output = String.Empty;

					var function = (UFunction)Decompiler.Owner.GetIndexObject( FunctionIndex );
					if( function != null )
					{		
						// Support for non native operators.
						if( function.IsPost() )
						{
							output = DecompilePreOperator( function.FriendlyName );
						}
						else if( function.IsPre() )
						{
							output = DecompilePostOperator( function.FriendlyName );
						}
						else if( function.IsOperator() )
						{
							output = DecompileOperator( function.FriendlyName, function.OperPrecedence );	
						}
						else
						{
							// Calling Super??.
							if( function.Name == Decompiler.Owner.Name && !Decompiler._WithinClassContext )
							{
								output = "super";

								// Check if the super call is within the super class of this functions outer(class)
								var myouter = (UField)Decompiler.Owner.Outer;
								if( myouter == null || myouter.Super == null || function.GetOuterName() != myouter.Super.Name  )
								{
									// There's no super to call then do a recursive super call.
									if( Decompiler.Owner.Super == null )
									{
										output += "(" + Decompiler.Owner.GetOuterName() + ")";
									}
									else
									{
										// Different owners, then it is a deep super call.
										if( function.GetOuterName() != Decompiler.Owner.GetOuterName() )
										{
											output += "(" + function.GetOuterName() + ")";
										}
									}
								}
								output += ".";
							}
							output += DecompileCall( function.Name );
				   		}
					}	
					Decompiler.AddSemicolon = true;	
					return output;
				}
			}

			public class VirtualFunctionToken : FunctionToken
			{
				public int FunctionNameIndex;

				public override void Deserialize()
				{
					// TODO: Corrigate Version (Definitely not in GoW(490))
					if( Buffer.Version >= 178 && Buffer.Version < 490 )
					{
						byte super = Buffer.ReadByte();
						Decompiler.AddCodeSize( sizeof(byte) );
					}

					FunctionNameIndex = Buffer.ReadNameIndex();
					Decompiler.AddNameIndexCodeSize();

					DeserializeCall();
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;	
					return DecompileCall( Decompiler.Owner.Package.GetIndexName( FunctionNameIndex ) );
				}
			}

			public class GlobalFunctionToken : FunctionToken
			{
				public int FunctionNameIndex;

				public override void Deserialize()
				{
					FunctionNameIndex = Buffer.ReadNameIndex();
					Decompiler.AddNameIndexCodeSize();

					DeserializeCall();
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;	
					return "global." + DecompileCall( Decompiler.Owner.Package.GetIndexName( FunctionNameIndex ) );
				}
			}

			// TODO: Test on UDK
			public class DelegateFunctionToken : FunctionToken
			{
				public int FunctionNameIndex;

				public override void Deserialize()
				{
					// TODO: Corrigate Version
					if( Buffer.Version > 180 )
					{
						bool bLocalProp = Buffer.UR.ReadBoolean();
						Decompiler.AddCodeSize( sizeof(bool) );
					}
					int delegateIndex = Buffer.ReadObjectIndex();
					Decompiler.AddObjectIndexCodeSize();	 

					FunctionNameIndex = Buffer.ReadNameIndex();
					Decompiler.AddNameIndexCodeSize();

					DeserializeCall();
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;
					return DecompileCall( Decompiler.Owner.Package.GetIndexName( FunctionNameIndex ) );
				}
			}

			public class NativeFunctionToken : FunctionToken
			{
				public NativeTable NativeTable;

				public override void Deserialize()
				{
					if( NativeTable == null )
					{
						NativeTable = new NativeTable{ Format = (byte)NativeType.Function, Name = "UnresolvedNativeFunction_" + RepresentToken, ByteToken = RepresentToken };
					}

					switch( (NativeType)NativeTable.Format )
					{
						case NativeType.Function:
							DeserializeCall();
							break;

						case NativeType.PreOperator:
						case NativeType.PostOperator:
							DeserializeUnaryOperator();
							break;

						case NativeType.Operator:
							DeserializeBinaryOperator();
							break;

						default:
							DeserializeCall();
							break;
					}
				}

				public override string Decompile()
				{
					string output = String.Empty;
					switch( (NativeType)NativeTable.Format )
					{
						case NativeType.Function:
							output = DecompileCall( NativeTable.Name );
							break;

						case NativeType.Operator:
							output = DecompileOperator( NativeTable.Name, NativeTable.OperPrecedence );
							break;

						case NativeType.PostOperator:
							output = DecompilePostOperator( NativeTable.Name );
							break;

						case NativeType.PreOperator:
							output = DecompilePreOperator( NativeTable.Name );
							break;

						default:
							output = DecompileCall( NativeTable.Name );
							break;
			  		}
					Decompiler.AddSemicolon = true;
					return output;
				}
			}
			#endregion

			#region ContextTokens
			public class ContextToken : Token
			{
				// Definitely not in UT3(512)
				// Greater or Equal than
				public const ushort VSizeByteMoved = 548;  // > APB

				public override void Deserialize()
				{
					// A.?
					DeserializeNext();

					// SkipSize
					Buffer.ReadUShort();
					Decompiler.AddCodeSize( sizeof(ushort) );

					// Doesn't seem to exist in APB
					if( Buffer.Version >= VSizeByteMoved )
					{
						// Property
						Buffer.ReadObjectIndex();
						Decompiler.AddObjectIndexCodeSize();
					}

					// PropertyType
					Buffer.ReadByte();
					Decompiler.AddCodeSize( sizeof(byte) );	

					// Additional byte in APB?
					if( Buffer.Version > 512 && Buffer.Version < VSizeByteMoved )
					{
						Buffer.ReadByte();
						Decompiler.AddCodeSize( sizeof(byte) );	
					}

					// ?.B
					DeserializeNext();
				}

				public override string Decompile()
				{
					return DecompileNext() + "." + DecompileNext();
				}
			}

			public class ClassContextToken : ContextToken
			{
				public override string Decompile()
				{
					Decompiler._WithinClassContext = true;
					string output = base.Decompile();
					Decompiler._WithinClassContext = false;
					return output;
				}
			}

			// Not related at all with BoolVariable but it uses the exact same structure.
			public class InterfaceContextToken : BoolVariableToken
			{
			}

			public class StructMemberToken : Token
			{
				public int FieldIndex;

				public override void Deserialize()
				{
					// ?.B
					FieldIndex = Buffer.ReadObjectIndex();
					Decompiler.AddObjectIndexCodeSize();

					// TODO: Corrigate Version
					if( Buffer.Version > 300 )
					{
						// StructIndex?
						Buffer.ReadObjectIndex();	
						Decompiler.AddObjectIndexCodeSize();

						// SkipSize?
						Buffer.ReadUShort();
						Decompiler.AddCodeSize( sizeof(short) );
					}
					DeserializeNext();
				}

				public override string Decompile()
				{
					return DecompileNext() + "." + Decompiler.Owner.Package.GetIndexObjectName( FieldIndex );
				}
			}
			#endregion

			#region LetTokens
			public class LetToken : Token
			{
				public override void Deserialize()
				{
					// A = B
					DeserializeNext();	   
					DeserializeNext();	
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;	
					return DecompileNext() + " = " + DecompileNext();
				}
			}

			public class LetBoolToken : LetToken
			{
			}

			public class LetDelegateToken : LetToken
			{
			}

			public class EndParmValueToken : Token
			{
				public override string Decompile()
				{
					return String.Empty;
				}
			}

			public class ConditionalToken : Token
			{
				public override void Deserialize()
				{
					// Condition
					DeserializeNext();	

   					// Size. Used to skip ? if Condition is False.
					Buffer.ReadUShort();
					Decompiler.AddCodeSize( sizeof(ushort) );

					// If TRUE expression
					DeserializeNext();

					// Size. Used to skip : if Condition is True.
					Buffer.ReadUShort();
					Decompiler.AddCodeSize( sizeof(ushort) );

					// If FALSE expression
					DeserializeNext();	
				}

				public override string Decompile()
				{
					return "((" + DecompileNext() + ") ? " + DecompileNext() + " : " + DecompileNext() + ")";
				}
			}
			#endregion

			#region JumpTokens
			public class ReturnToken : Token
			{
				public override void Deserialize()
				{
					DeserializeNext();	
				}

				public override string Decompile()
				{
					#region CaseToken Support
					// HACK: for case's that end with a return instead of a break.
					if( Decompiler.IsInNest( NestManager.Nest.NestType.Case ) != null )
					{
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Case, Position + Size );
					}
					else if( Decompiler.IsInNest( NestManager.Nest.NestType.Default ) != null )
					{
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Default, Position + Size );
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Switch, Position + Size );
					}
					#endregion
	
					string returnValue = DecompileNext();
					Decompiler.AddSemicolon = true;
					return "return" + (returnValue.Length != 0 ? " " + returnValue : String.Empty);
				}
			}

			public class ReturnNothingToken : Token
			{
				public int ReturnObjectIndex = -1;

				public override void Deserialize()
				{
					// TODO: Corrigate Version
					if( Buffer.Version > 300 )
					{
						ReturnObjectIndex = Buffer.ReadObjectIndex();
						Decompiler.AddObjectIndexCodeSize();
					}
				}

				public override string Decompile()
				{
					#region CaseToken Support
					// HACK: for case's that end with a return instead of a break.
					if( Decompiler.IsInNest( NestManager.Nest.NestType.Case ) != null )
					{
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Case, Position + Size );
					}
					else if( Decompiler.IsInNest( NestManager.Nest.NestType.Default ) != null )
					{
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Default, Position + Size );
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Switch, Position + Size );
					}
					#endregion

					Decompiler.AddSemicolon = true;	
					return ReturnObjectIndex > -1 ? Decompiler.Owner.Package.GetIndexObjectName( ReturnObjectIndex ) : String.Empty;
				}
			}

			public class GoToLabelToken : Token
			{
				public override void Deserialize()
				{
					DeserializeNext();	
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;	
					return "goto " + DecompileNext();
				}
			}

			public class JumpToken : Token
			{
				public uint CodeOffset{ get; set; }

				public virtual uint Align( uint offset )
				{
					return offset;
				}

				public override void Deserialize()
				{
					CodeOffset = Align( Buffer.ReadUShort() );
					Decompiler.AddCodeSize( sizeof(ushort) );
				}

				public override void PostDeserialized()
				{
					if( GetType() == typeof(JumpToken) )
						Decompiler._Labels.Add( new ULabelEntry{ Name = String.Format( "J0x{0:x2}", CodeOffset ), Position = (int)CodeOffset } );
				}

				/// <summary>
				/// FORMATION ISSUESSES:
				///		1:(-> Logic remains the same)	Continue's are decompiled as Else's statements e.g.
				///			-> Original
				///			if( continueCondition )
				///			{
				///				continue;
				///			}
				///			
				///			// Actual code
				///			
				///			-> Decompiled
				///			if( continueCodition )
				///			{
				///			}
				///			else
				///			{
				///				// Actual code
				///			}
				///			
				///			
				///		2:(-> ...)	...
				///			-> Original
				///				...
				///			-> Decompiled
				///				...
				///			
				/// </summary>
				public override string Decompile()
				{
					// Break offset!
					if( CodeOffset >= Position )
					{
						//==================We're inside a Case and at the end of it!
						if( Decompiler.IsInNest( NestManager.Nest.NestType.Case ) != null )
						{
							//Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Case, Position );			
							ClearLabel();
							Decompiler.PreComment = String.Format( "// End:0x{0:x2}", CodeOffset );
							Decompiler.AddSemicolon = true;	
							return "break";
						}
						
						//==================We're inside a Default and at the end of it!
						if( Decompiler.IsInNest( NestManager.Nest.NestType.Default ) != null )
						{
							ClearLabel();
							Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Default, Position );
							Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Switch, Position );
							Decompiler.PreComment = String.Format( "// End:0x{0:x2} Break;", CodeOffset );
							Decompiler.AddSemicolon = true;	
							return "break";
						}

						var nest = Decompiler.IsInNest( NestManager.Nest.NestType.Loop );
						if( nest == null )
							nest = Decompiler.IsInNest( NestManager.Nest.NestType.ForEach );
						if( nest != null )
						{
							// Continue
							if( CodeOffset + 10 == (nest.Creator as JumpToken).CodeOffset )
							{
								Decompiler.PreComment = String.Format( "// End:0x{0:x2} Continue;", CodeOffset );
								goto gotoJump;
							}									
							// Break
							else if( CodeOffset == (nest.Creator as JumpToken).CodeOffset )
							{
								if( nest.Type == NestManager.Nest.NestType.ForEach )
								{
									ClearLabel();
									Decompiler.PreComment = String.Format( "// End:0x{0:x2}", CodeOffset );
									Decompiler.AddSemicolon = true;
									return "break";
								}

								Decompiler.PreComment = String.Format( "// End:0x{0:x2} Break;", CodeOffset );
								goto gotoJump;
							}
						}
						
						nest = Decompiler.IsInNest( NestManager.Nest.NestType.If );
						//==================We're inside a If and at the end of it!
						if( nest != null )
						{
							// We're inside a If however the kind of jump could range from: continue; break; goto; else
							var nestEnd = Decompiler.CurNestEnd();
							if( nestEnd != null )
							{
								// Else, Req:In If nest, nestends > 0, curendnest position == Position
								if( nestEnd.Position - Size == Position )
								{
									// HACK: This should be handled by Decompile()
									UDecompiler.RemoveTabs( 1 );
									Decompiler._NestChain.RemoveAt( Decompiler._NestChain.Count - 1 );
									Decompiler._Nester.Nests.Remove( nestEnd );

									Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.Else, Position );
									Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Else, CodeOffset );

									ClearLabel();

									// HACK: This should be handled by Decompile()
									return "}" + "\r\n" + 
										(UnrealConfig.SuppressComments 
										? UDecompiler.Tabs + "else" 
										: UDecompiler.Tabs + String.Format( "// End:0x{0:x2}", CodeOffset ) + "\r\n" + 
											UDecompiler.Tabs + "else");
								}
							}
						}
					}

					Decompiler.PreComment = "// This is an implied JumpToken;";
					gotoJump:
					// This is an implicit GoToToken.
					Decompiler.AddSemicolon = true;
					if( CodeOffset < Position )
					{
						Decompiler.PreComment += " Continue!";
					}
					return String.Format( "goto J0x{0:x2}", CodeOffset );
				}

				protected void ClearLabel()
				{
					int i = Decompiler._TempLabels.FindIndex( (p) => p.Position == CodeOffset );
					if( i != -1 )
					{
						Decompiler._TempLabels.RemoveAt( i );
					}
				}
			}

			public class JumpIfNotToken : JumpToken
			{
				public bool IsLoop;

				public override void Deserialize()
				{
					// CodeOffset
					base.Deserialize();

					// Condition
					DeserializeNext();
				}

				public override string Decompile()
				{		
					//CodeOffset -= (Position - 1) + (uint)(Size - 1);
					/** Could loop from here up to a JumpToken to detect whether this is a While loop. */

					string condition = DecompileNext();

					// Check whether there's a JumpToken pointing to the begin of this JumpIfNot, 
					//	if detected, we assume that this If is part of a loop.
					IsLoop = false;
					for( int i = Decompiler._CurToken + 1; i < Decompiler._Tokens.Count; ++ i )
					{
						if( Decompiler._Tokens[i] is JumpToken && ((JumpToken)Decompiler._Tokens[i]).CodeOffset == Position )
						{
							IsLoop = true;
							break;
						}
					}

					Decompiler.PreComment = String.Format( "// End:0x{0:x2} Loop:{1}", CodeOffset, IsLoop );

					string output = /*(IsLoop ? "while" : "if") +*/ "if(" + condition + ")";

					if( (CodeOffset & UInt16.MaxValue) < Position )
					{
						Decompiler.AddSemicolon = true;
						return output + "\r\n" 
							+ UDecompiler.Tabs + "\tgoto " + String.Format( "J0x{0:x2}", CodeOffset ); 
					}

					Decompiler.AddSemicolon = false;
					if( IsLoop )
					{
						Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.Loop, Position );
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Loop, CodeOffset );
					}
					else
					{
						Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.If, Position );
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.If, CodeOffset );
					}
					return output;
				}
			}

			public class SwitchToken : Token
			{
				public int ObjectIndex;
				public byte BlockSize;

				// Greater Than
				public const ushort ObjectIndexVersion = 600;

				public override void Deserialize()
				{
					// TODO: Corrigate Version
					if( Buffer.Version > ObjectIndexVersion )
					{
						// Points to the object that was passed to the switch, beware that the followed token chain contains it as well!
						ObjectIndex = Buffer.ReadObjectIndex();
						Decompiler.AddObjectIndexCodeSize();
					}

					BlockSize = Buffer.ReadByte();	// Size
					Decompiler.AddCodeSize( sizeof(byte) );

					// Expression
					DeserializeNext();
				}

				/// <summary>
				/// FORMATION ISSUESSES:	
				///		1:(-> ...)	NestEnd is based upon the break in a default case, however a case does not always break/return,
				///			causing that there will be no default with a break detected, thus no ending for the Switch block.
				///			
				///			-> Original
				///				Switch( A )
				///				{
				///					case 0:
				///						CallA();
				///				}
				///				
				///				CallB();
				///				
				///			-> Decompiled
				///				Switch( A )
				///				{
				///					case 0:
				///						CallA();
				///					default:	// End is detect of case 0 due some other hack :)
				///						CallB();	
				/// </summary>
				public override string Decompile()
				{
					Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.Switch, Position );
					// Ends at: JumpToken -> Decompile()

					// TODO: Corrigate Version
					if( Buffer.Version > ObjectIndexVersion )
					{
						Decompiler.PreComment = "// ObjectIndex:" + ObjectIndex + " BlockSize:" + BlockSize;
					}
					else Decompiler.PreComment = "// BlockSize:" + BlockSize;

					string output = "switch(" + DecompileNext() + ")";
					Decompiler.AddSemicolon = false;	// In case the decompiled token was a function call
					return output;
				}
			}

			public class CaseToken : JumpToken
			{
				public override void Deserialize()
				{
					base.Deserialize();

					if( CodeOffset != UInt16.MaxValue )
					{
						// Condition
						DeserializeNext();

						// Return
					}

					// Default
				}

				// HACK: To avoid from decrementing tabs more than once, 
				//	e.g. in a situation of case a1: case a2: case a3: that use the same block code.
				private static byte _CaseStack = 0;
				public override string Decompile()
				{
					// HACK: If this case is inside another case, end the last case to avoid broken indention.
					/// -> Original
					///		case 0:
					///		case 1:
					///		case 2:
					///			CallA();
					///			break;
					///		
					///	->(Without the hack) Decompiled
					///		case 0:
					///			case 1:
					///				case 2:
					///					CallA();
					///					break;
					if( Decompiler.IsInNest( NestManager.Nest.NestType.Switch ) == null && _CaseStack == 0 )
					{
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Default, Position );
						Decompiler.PreDecrementTabs = 1;

						++ _CaseStack;
					}
					else _CaseStack = 0;

					Decompiler.PreComment = String.Format( "// End:0x{0:x2}", CodeOffset );
					if( CodeOffset != UInt16.MaxValue )
					{
						Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.Case, Position );
						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Case, CodeOffset );

						string output = "case " + DecompileNext() + ":";
						Decompiler.AddSemicolon = false;
						return output;
					}
					
					Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.Default, Position, this );
					Decompiler.AddSemicolon = false;
					return "default:";	
				}
			}

			public class IteratorToken : JumpToken
			{
				public override void Deserialize()
				{
					// Expression
					DeserializeNext();

					base.Deserialize();
				}

				public override string Decompile()
				{				  
					Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.ForEach, Position, this );
					// HACK: Use IteratorPopToken instead as the end indicator!
					//Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.ForEach, CodeOffset );

					Decompiler.PreComment = String.Format( "// End:0x{0:x2}", CodeOffset );

					// foreach FunctionCall
					string expression = DecompileNext();
					Decompiler.AddSemicolon = false;	// Undo
					return "foreach " + expression;
				}
			}

			public class ArrayIteratorToken : JumpToken
			{
				protected bool _HasSecondParm;

				public override void Deserialize()
				{
					// Expression
					DeserializeNext();

					// Param 1
					DeserializeNext();

					_HasSecondParm = Buffer.ReadByte() > 0;
					Decompiler.AddCodeSize( sizeof(byte) );
					DeserializeNext();

					base.Deserialize();
				}

				public override string Decompile()
				{				  
					Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.ForEach, Position, this );
					// HACK: Use IteratorPopToken instead as the end indicator!
					//Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.ForEach, CodeOffset );
	
					Decompiler.PreComment = String.Format( "// End:0x{0:x2}", CodeOffset );

					// foreach ArrayVariable( Parameters )
					string output = "foreach " + DecompileNext() + "(" + DecompileNext();
					output += (_HasSecondParm ? ", " : String.Empty) + DecompileNext();
					Decompiler.AddSemicolon = false;
					return output + ")";
				}
			}

			public class IteratorNextToken : Token
			{
				public override string Decompile()
				{
					// Only output this when we are WITHIN a ForEach but not IN it.
					if( Decompiler.IsWithinNest( NestManager.Nest.NestType.ForEach ) != null )
					{
						if( Decompiler.IsInNest( NestManager.Nest.NestType.ForEach ) == null )
						{
							Decompiler.AddSemicolon = true;
							return "continue";
						}
					}
					return String.Empty;
				}
			}

			public class IteratorPopToken : Token
			{
				public override string Decompile()
				{
					// Only output this when we are WITHIN a ForEach but not IN it.
					if( Decompiler.IsWithinNest( NestManager.Nest.NestType.ForEach ) != null )
					{
						if( Decompiler.IsInNest( NestManager.Nest.NestType.ForEach ) == null )
						{
							Decompiler.AddSemicolon = true;
							return "break";
						}

						Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.ForEach, Position );
					}
					return String.Empty;
				}
			}
			#endregion

			#region FieldTokens
			public class FieldToken : Token
			{							
				public int ObjectIndex;
				public UObject Object;

				public override void Deserialize()
				{
					ObjectIndex = Buffer.ReadObjectIndex(); 
					Decompiler.AddObjectIndexCodeSize();
					Object = Decompiler.Owner.TryGetIndexObject( ObjectIndex );	
				}

				public override string Decompile()
				{
					return Object != null ? Object.Name : "@NULL(" + ObjectIndex + ")";
				}
			}

			public class NativeParameterToken : FieldToken
			{
				public override string Decompile()
				{
					return String.Empty;
				}
			}

			public class InstanceVariableToken : FieldToken
			{
			}

			public class LocalVariableToken : FieldToken
			{
			}

			public class DefaultVariableToken : FieldToken
			{
				public override string Decompile()
				{
					return "default." + base.Decompile();
				}
			}

			public class UndefinedVariableToken : Token
			{
				public override void Deserialize()
				{		  
				}

				public override string Decompile()
				{
					return String.Empty;
				}
			}

			public class OutVariableToken : FieldToken
			{
			}

			public class DelegatePropertyToken : FieldToken
			{
				public int NameIndex;

				public override void Deserialize()
				{		  
					NameIndex = Buffer.ReadNameIndex();
					Decompiler.AddNameIndexCodeSize();
					if( Buffer.Version > 184 )
					{
						base.Deserialize();
					}
				}

				public override string Decompile()
				{
					return Decompiler.Owner.Package.GetIndexName( NameIndex );
				}
			}

			public class DefaultParameterToken : Token
			{
				internal static byte _paramNum;

				public override void Deserialize()
				{
					Buffer.ReadUShort();	// Size
					Decompiler.AddCodeSize( sizeof(ushort) );

					// Expression
					DeserializeNext();	

					// EndParmValue?
					DeserializeNext();
				}

				public override string Decompile()
				{
					string output = ((UFunction)Decompiler.Owner).ChildParams[_paramNum ++].Name + " = " + DecompileNext();			
					DecompileNext();
					Decompiler.AddSemicolon = true;
					return output;
				}
			}

			public class BoolVariableToken : Token
			{
				public override void Deserialize()
				{
					DeserializeNext();
				}

				public override string Decompile()
				{
					return DecompileNext();
				}
			}

			public class UnknownVariableToken : FieldToken
			{
				public override string Decompile()
				{
					return "Unknown: " + Object;
				}
			}
			#endregion

			#region OtherTokens
			public class NothingToken : Token
			{
			}

			public class IntZeroToken : Token
			{
				public override string Decompile()
				{
 					 return "0";
				}
			}

			public class IntOneToken : Token
			{
				public override string Decompile()
				{
 					 return "1";
				}
			}

			public class TrueToken : Token
			{
				public override string Decompile()
				{
 					 return "true";
				}
			}

			public class FalseToken : Token
			{
				public override string Decompile()
				{
 					 return "false";
				}
			}

			public class SelfToken : Token
			{
				public override string Decompile()
				{
 					 return "self";
				}
			}

			public class NoneToken : Token
			{
				public override string Decompile()
				{
 					 return "none";
				}
			}

			public class NoDelegateToken : NoneToken
			{
			}

			public class NoObjectToken : NoneToken
			{
			}

			// A skipped parameter when calling a function
			public class NoParmToken : Token
			{
			}

			public class StopToken : Token
			{
				public override void Deserialize()
				{
 					/*if( Buffer.Version > 300 )
					{
						Buffer.ReadUShort();
						Decompiler.AddCodeSize( sizeof(short) );
					}*/
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;	
 					return "stop";
				}
			}
	
			public class AssertToken : Token
			{
				public override void Deserialize()
				{
					Buffer.ReadUShort();	// Line
					Decompiler.AddCodeSize( sizeof(short) );
					DeserializeNext();
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;	
 					return "assert(" + DecompileNext() + ")";
				}
			}

			private List<ULabelEntry> _Labels, _TempLabels;

			public class LabelTableToken : Token
			{
				public override void Deserialize()
				{
					int labelIndex = -1;
					int labelPos = -1;
					do
					{
						if( labelIndex != -1 )
						{
							Decompiler._Labels.Add( new ULabelEntry{ Name = Decompiler.Owner.Package.GetIndexName( labelIndex ), Position = labelPos } );
						}

						labelIndex = Buffer.ReadNameIndex();
						Decompiler.AddNameIndexCodeSize();

						labelPos = Buffer.ReadInt32();
						Decompiler.AddCodeSize( sizeof(int) );


					} while( System.String.Compare( Decompiler.Owner.Package.GetIndexName( labelIndex ), "None", System.StringComparison.OrdinalIgnoreCase ) != 0 );
				}
			}

			/// <summary>
			/// Indication of THE END OF THE FUNCTION's SCRIPT.
			/// 
			/// Difference:This is used in UE3, the FunctionEndToken is used in UE2 while UE1 doesn't have neither.
			/// </summary>
			public class EndOfScriptToken : Token
			{
			}

			/// <summary>
			/// Indication of THE END OF THE FUNCTION's SCRIPT.
			/// 
			/// Difference:This is used in UE2, the EndOfScriptToken is used in UE3 while UE1 doesn't have neither.
			/// </summary>
			public class FunctionEndToken : Token
			{
			}

			public class SkipToken : Token
			{
				public override void Deserialize()
				{
					Buffer.ReadUShort();	// Size
					Decompiler.AddCodeSize( sizeof(ushort) );	

					DeserializeNext();
				}

				public override string Decompile()
				{
 					 return "(" + DecompileNext() + ")";
				}
			}

			public class ComparisonToken : Token
			{
				public int ObjectIndex;

				public override void Deserialize()
				{
					ObjectIndex = Buffer.ReadObjectIndex();
					Decompiler.AddObjectIndexCodeSize();

					DeserializeNext();
					// ==
					DeserializeNext();
				}
			}

			public class StructCmpEqToken : ComparisonToken
			{
				public override string Decompile()
				{
 					 return DecompileNext() + " == " + DecompileNext();
				}
			}

			public class StructCmpNeToken : ComparisonToken
			{
				public override string Decompile()
				{
 					 return DecompileNext() + " != " + DecompileNext();
				}
			}	

			public class DelegateComparisonToken : Token
			{
				public override void Deserialize()
				{
					DeserializeNext();	// Left
					// ==
					DeserializeNext();	// Right

					// End
					DeserializeNext();
				}
			}

			public class DelegateCmpEqToken : DelegateComparisonToken
			{
				public override string Decompile()
				{
 					string output = DecompileNext() + " == " + DecompileNext();
					DecompileNext();
					return output;
				}
			}

			public class DelegateFunctionCmpEqToken : DelegateComparisonToken
			{
				public override string Decompile()
				{
 					string output = DecompileNext() + " == " + DecompileNext();
					DecompileNext();
					return output;
				}
			}

			public class DelegateCmpNeToken : DelegateComparisonToken
			{
				public override string Decompile()
				{
 					string output = DecompileNext() + " != " + DecompileNext();
					DecompileNext();
					return output;
				}
			}

			public class DelegateFunctionCmpNeToken : DelegateComparisonToken
			{
				public override string Decompile()
				{
 					string output = DecompileNext() + " != " + DecompileNext();
					DecompileNext();
					return output;
				}
			}

			public class EatStringToken : Token
			{
				public override void Deserialize()
				{
					// TODO: Corrigate Version
					if( Buffer.Version > 300 )
					{
						Buffer.ReadUShort();	// Size
						Decompiler.AddCodeSize( sizeof(ushort) );

						// TODO: UNKNOWN:
						// TODO: Corrigate Version	 (Definitely since UT3(512))
						if( Buffer.Version >= 490 )
						{
							Buffer.ReadUShort();
							Decompiler.AddCodeSize( sizeof(ushort) );
						}
					}
				
					// The Field
					DeserializeNext();
				}

				public override string Decompile()
				{
					// The Field
					return DecompileNext();
				}
			}

			// TODO: Implement
			public class ResizeStringToken : Token
			{
				public override void Deserialize()
				{
					Buffer.ReadByte();	// Size
					Decompiler.AddCodeSize( sizeof(byte) );
				}
			}

			// TODO: Implement
			public class BeginFunctionToken : Token
			{
				public override void Deserialize()
				{
					var topf = Decompiler.Owner as UFunction;
					Debug.Assert( topf != null, "topf != null" );
					for( int i = 0; i < topf._ChildProperties.Count; ++ i )
					{
						if( !topf._ChildProperties[i].HasPropertyFlag( Flags.PropertyFlagsLO.Parm | Flags.PropertyFlagsLO.ReturnParm ) )
							continue;

						Buffer.ReadByte(); // Size
						Decompiler.AddCodeSize( sizeof(byte) );

						Buffer.ReadByte(); // bOutParam
						Decompiler.AddCodeSize( sizeof(byte) );
					}
					Buffer.ReadByte();	// End
					Decompiler.AddCodeSize( sizeof(byte) );
				}
			}

			public class NewToken : Token
			{
				// Greater Than!
				private const uint TemplateVersion = 300;	// TODO: Corrigate Version

				public override void Deserialize()
				{
					// Outer
					DeserializeNext();	  
					// Name
					DeserializeNext();		
					// Flags
					DeserializeNext();

					// Class?
					DeserializeNext();
						
					// TODO: Corrigate Version
					if( Buffer.Version > TemplateVersion )
					{
						// Template?
						DeserializeNext();
					}
				}

				public override string Decompile()
				{
					// TODO:Clean this up; make it more concise!
					string outerStr = DecompileNext();
					string nameStr = DecompileNext();
					string flagsStr = DecompileNext();
					string classStr = DecompileNext();

					string templateStr = String.Empty;
					// TODO: Corrigate Version
					if( Buffer.Version > TemplateVersion )
					{
						templateStr = DecompileNext();
					}

					// Handles: new [( [outer [, name [, flags]]] )] class [( template )]
					string output = String.Empty;
					bool addComma = false;

					if( outerStr.Length != 0 )
					{
						output += outerStr;
						addComma = true;
					}

					if( nameStr.Length != 0 )
					{
						if( addComma )
							output += ", ";

						output += nameStr;
						addComma = true;
					}

					if( flagsStr.Length != 0 )
					{
						if( addComma )
							output += ", ";

						output += flagsStr;
						addComma = true;
					}

					if( addComma )
					{
						output = " (" + output + ")";
					}

					if( classStr.Length != 0 )
					{
						output += " " + classStr;
					}

					if( templateStr.Length != 0 )
					{
						output += " (" + templateStr + ")";
					}

					Decompiler.AddSemicolon = true;
					return "new" + output;
				}
			}

			public class DebugInfoToken : Token
			{
				public override void Deserialize()
				{
					// Version
					Buffer.ReadInt32();
					// Line
					Buffer.ReadInt32();
					// Pos
					Buffer.ReadInt32();
					// Code
					Buffer.ReadByte();
					Decompiler.AddCodeSize( 13 );
				}
			}
			#endregion

			#region Constants
			public class IntConstToken : Token
			{
				public int Value;

				public override void Deserialize()
				{
					Value = Buffer.ReadInt32();
					Decompiler.AddCodeSize( sizeof(int) );
				}

				public override string Decompile()
				{
					return String.Format( "{0}", Value ); 
				}
			}

			public class ByteConstToken : Token
			{
				public byte Value;

				public override void Deserialize()
				{
					Value = Buffer.ReadByte();
					Decompiler.AddCodeSize( sizeof(byte) );
				}

				public override string Decompile()
				{
					return String.Format( "{0}", Value ); 
				}
			}

			public class IntConstByteToken : Token
			{
				public byte Value;

				public override void Deserialize()
				{
					Value = Buffer.ReadByte();
					Decompiler.AddCodeSize( sizeof(byte) );
				}

				public override string Decompile()
				{
					return String.Format( "{0:d}", Value ); 
				}
			}

			public class FloatConstToken : Token
			{
				public float Value;

				public override void Deserialize()
				{
					Value = Buffer.UR.ReadSingle();
					Decompiler.AddCodeSize( sizeof(float) );
				}

				public override string Decompile()
				{
					return String.Format( "{0:f}", Value ).Replace( ',', '.' ); 
				}
			}

			public class ObjectConstToken : Token
			{
				public int ObjectIndex;

				public override void Deserialize()
				{
					ObjectIndex = Buffer.ReadObjectIndex();
					Decompiler.AddObjectIndexCodeSize();
				}

				public override string Decompile()
				{
					UObject obj = Decompiler.Owner.GetIndexObject( ObjectIndex );
					if( obj != null )
					{
						// class'objectclasshere'
						string Class = obj.GetClassName();
						if( String.IsNullOrEmpty( Class ) )
						{
							Class = "class";
						}
						return Class.ToLower() + "'" + obj.Name + "'"; 
					}
					return "none";
				}
			}

			public class NameConstToken : Token
			{
				public int NameIndex;

				public override void Deserialize()
				{
					NameIndex = Buffer.ReadNameIndex();
					Decompiler.AddNameIndexCodeSize();
				}

				public override string Decompile()
				{
					return "\'" + Decompiler.Owner.Package.GetIndexName( NameIndex ) + "\'";
				}
			}

			public class StringConstToken : Token
			{
				public string Value;

				public override void Deserialize()
				{
					Value = Buffer.UR.ReadASCIIString();
					Decompiler.AddCodeSize( Value.Length + 1 );	// inc null char
				}

				public override string Decompile()
				{
					return "\"" + Value.Replace( "\"", "\\\"" ).Replace( "\\", "\\\\" ) + "\"";
				}
			}

			public class UniStringConstToken : Token
			{
				public string Value;

				public override void Deserialize()
				{
					Value = Buffer.UR.ReadUnicodeString();
					Decompiler.AddCodeSize( (Value.Length * 2) + 2 );	// inc null char
				}

				public override string Decompile()
				{
					return "\"" + Value.Replace( "\"", "\\\"" ).Replace( "\\", "\\\\" ) + "\"";
				}
			}

			public class RotatorConstToken : Token
			{
				public struct Rotator
				{
					public int Pitch, Yaw, Roll;
				}

				public Rotator Value;

				public override void Deserialize()
				{
					Value.Pitch = Buffer.ReadInt32();
					Decompiler.AddCodeSize( sizeof(int) );
					Value.Yaw = Buffer.ReadInt32();
					Decompiler.AddCodeSize( sizeof(int) );
					Value.Roll = Buffer.ReadInt32();
					Decompiler.AddCodeSize( sizeof(int) );
				}

				public override string Decompile()
				{
					return "rot(" + Value.Pitch + ", " + Value.Yaw + ", " + Value.Roll + ")";
				}
			}

			public class VectorConstToken : Token
			{
				public struct Vector
				{
					public float X, Y, Z;
				}

				public Vector Value;

				public override void Deserialize()
				{
					Value.X = Buffer.UR.ReadSingle();
					Decompiler.AddCodeSize( sizeof(float) );
					Value.Y = Buffer.UR.ReadSingle();
					Decompiler.AddCodeSize( sizeof(float) );
					Value.Z = Buffer.UR.ReadSingle();
					Decompiler.AddCodeSize( sizeof(float) );
				}

				public override string Decompile()
				{
					return "vect(" + String.Format( "{0:f}", Value.X ).Replace( ',', '.' ) + ", " + String.Format( "{0:f}", Value.Y ).Replace( ',', '.' ) + ", " + String.Format( "{0:f}", Value.Z ).Replace( ',', '.' ) + ")";
				}
			}
			#endregion

			#region CastTokens
			public class CastToken : Token
			{
				public override void Deserialize()
				{
					DeserializeNext();
				}

				public override string Decompile()
				{
					return "(" + DecompileNext() + ")"; 
				}
			}

			public class PrimitiveCastToken : Token
			{
				public override void Deserialize()
				{
					DeserializeNext();
				}

				public override string Decompile()
				{
					return DecompileNext(); 
				}
			}

			public class DynamicCastToken : CastToken
			{
				public int ObjectToCastTo;

				public override void Deserialize()
				{
					ObjectToCastTo = Buffer.ReadObjectIndex();
					Decompiler.AddObjectIndexCodeSize();

					base.Deserialize();
				}

				public override string Decompile()
				{
					return Decompiler.Owner.GetIndexObject( ObjectToCastTo ).Name + base.Decompile();
				}
			}

			public class MetaCastToken : CastToken
			{
				public int ClassToCastTo;

				public override void Deserialize()
				{
					ClassToCastTo = Buffer.ReadObjectIndex();
					Decompiler.AddObjectIndexCodeSize();

					base.Deserialize();
				}

				public override string Decompile()
				{
					return "class<" + Decompiler.Owner.GetIndexObject( ClassToCastTo ).Name + ">" + base.Decompile();
				}
			}

			public class InterfaceCastToken : DynamicCastToken
			{
				/*public override string Decompile()
				{
					return Decompiler.Owner.GetIndexObject( InerfaceToCastTo ).Name + base.Decompile();
				}*/
			}

			public class RotatorToVectorToken : CastToken
			{
				public override string Decompile()
				{
					return "vector" + base.Decompile(); 
				}
			}

			public class ByteToIntToken : CastToken
			{
				public override string Decompile()
				{
					return DecompileNext();
					//return "int" + base.Decompile(); 
				}
			}

			public class ByteToFloatToken : CastToken
			{
				public override string Decompile()
				{
					return "float" + base.Decompile(); 
				}
			}

			public class ByteToBoolToken : CastToken
			{
				public override string Decompile()
				{
					return "bool" + base.Decompile(); 
				}
			}

			public class IntToByteToken : CastToken
			{
				public override string Decompile()
				{
					return "byte" + base.Decompile(); 
				}
			}

			public class IntToBoolToken : CastToken
			{
#if !SUPPRESS_BOOLINTEXPLOIT
				public override string Decompile()
				{
					return "bool" + base.Decompile(); 
				}
#endif
			}

			public class IntToFloatToken : CastToken
			{
				public override string Decompile()
				{
					return "float" + base.Decompile(); 
				}
			}

			public class BoolToByteToken : CastToken
			{
				public override string Decompile()
				{
					return "byte" + base.Decompile(); 
				}
			}

			public class BoolToIntToken : CastToken
			{
#if !SUPPRESS_BOOLINTEXPLOIT
				public override string Decompile()
				{
					return "int" + base.Decompile(); 
				}
#endif
			}

			public class BoolToFloatToken : CastToken
			{
				public override string Decompile()
				{
					return "float" + base.Decompile(); 
				}
			}

			public class FloatToByteToken : CastToken
			{
				public override string Decompile()
				{
					return "byte" + base.Decompile(); 
				}
			}

			public class FloatToIntToken : CastToken
			{
				public override string Decompile()
				{
					return "int" + base.Decompile(); 
				}
			}

			public class FloatToBoolToken : CastToken
			{
				public override string Decompile()
				{
					return "bool" + base.Decompile(); 
				}
			}

			public class ObjectToBoolToken : CastToken
			{
				public override string Decompile()
				{
					return "bool" + base.Decompile(); 
				}
			}

			public class NameToBoolToken : CastToken
			{
				public override string Decompile()
				{
					return "bool" + base.Decompile(); 
				}
			}

			public class StringToByteToken : CastToken
			{
				public override string Decompile()
				{
					return "byte" + base.Decompile(); 
				}
			}

			public class StringToIntToken : CastToken
			{
				public override string Decompile()
				{
					return "int" + base.Decompile(); 
				}
			}

			public class StringToBoolToken : CastToken
			{
				public override string Decompile()
				{
					return "bool" + base.Decompile(); 
				}
			}

			public class StringToFloatToken : CastToken
			{
				public override string Decompile()
				{
					return "float" + base.Decompile(); 
				}
			}

			public class StringToVectorToken : CastToken
			{
				public override string Decompile()
				{
					return "vector" + base.Decompile(); 
				}
			}

			public class StringToRotatorToken : CastToken
			{
				public override string Decompile()
				{
					return "rotator" + base.Decompile(); 
				}
			}

			public class VectorToBoolToken : CastToken
			{
				public override string Decompile()
				{
					return "bool" + base.Decompile(); 
				}
			}

			public class VectorToRotatorToken : CastToken
			{
				public override string Decompile()
				{
					return "rotator" + base.Decompile(); 
				}
			}

			public class RotatorToBoolToken : CastToken
			{
				public override string Decompile()
				{
					return "bool" + base.Decompile(); 
				}
			}

			public class ByteToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class IntToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class BoolToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class FloatToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class NameToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class VectorToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class RotatorToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class StringToNameToken : CastToken
			{
				public override string Decompile()
				{
					return "name" + base.Decompile(); 
				}
			}

			public class ObjectToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class InterfaceToStringToken : CastToken
			{
				public override string Decompile()
				{
					return "string" + base.Decompile(); 
				}
			}

			public class InterfaceToBoolToken : CastToken
			{
				public override string Decompile()
				{
					return DecompileNext(); 
				}
			}
			#endregion

			#region ArrayTokens
			public class ArrayElementToken : Token
			{
				public override void Deserialize()
				{
					// Key
					DeserializeNext();

					// Array
					DeserializeNext();
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;	
					string keyName = DecompileNext();
					string arrayName = DecompileNext();
					return arrayName + "[" + keyName + "]";
				}
			}

			public class DynamicArrayElementToken : ArrayElementToken
			{
			}

			public class DynamicArrayLengthToken : Token
			{
				public override void Deserialize()
				{
					// Array
					DeserializeNext();
				}

				public override string Decompile()
				{
					Decompiler.AddSemicolon = true;	
					return DecompileNext() + ".Length";
				}
			}

			// Comparison: Greater than!
			private const uint ArrayMethodEndParmsVersion = 648;	// TODO: Corrigate Version
			// Comparison: Greater than!
			private const uint ArrayMethodSizeParmsVersion = 480;	// TODO: Corrigate Version	 (Definitely before 490(GoW))

			public class DynamicArrayMethodToken : Token
			{
				protected virtual void DeserializeMethodOne( bool skipEndParms = false )
				{
					// Array
					DeserializeNext();

					if( Buffer.Version > ArrayMethodSizeParmsVersion )
					{
						// Size
						Buffer.Skip( 2 );
						Decompiler.AddCodeSize( sizeof(ushort) );
					}

					// Param 1
					DeserializeNext();

					if( Buffer.Version > ArrayMethodEndParmsVersion && !skipEndParms )
					{
						// EndParms
						DeserializeNext();
					}
				}

				protected virtual void DeserializeMethodTwo( bool skipEndParms = false )
				{
					// Array
					DeserializeNext();

					if( Buffer.Version > ArrayMethodSizeParmsVersion )
					{
						// Size
						Buffer.Skip( 2 );
						Decompiler.AddCodeSize( sizeof(ushort) );
					}

					// Param 1
					DeserializeNext();

					// Param 2
					DeserializeNext();

					if( Buffer.Version > ArrayMethodEndParmsVersion && !skipEndParms )
					{
						// EndParms
						DeserializeNext();
					}
				}

				protected string DecompileMethodOne( string functionName, bool skipEndParms = false )
				{
					Decompiler.AddSemicolon = true;	
					return DecompileNext() + "." + functionName + 
						"(" + DecompileNext() + (Buffer.Version > ArrayMethodEndParmsVersion && !skipEndParms ? DecompileNext() : ")");
				}

				protected string DecompileMethodTwo( string functionName, bool skipEndParms = false )
				{
					Decompiler.AddSemicolon = true;	
					return DecompileNext() + "." + functionName + 
						"(" + DecompileNext() + ", " + DecompileNext() + (Buffer.Version > ArrayMethodEndParmsVersion && !skipEndParms ? DecompileNext() : ")");
				}
			}

			// TODO:Byte code of this has apparently changed to ReturnNothing in UE3
			public class DynamicArrayInsertToken : DynamicArrayMethodToken
			{
				protected override void DeserializeMethodTwo( bool skipEndParms = false )
				{
					// Array
					DeserializeNext();

					// Param 1
					DeserializeNext();

					// Param 2
					DeserializeNext();

					if( Buffer.Version > ArrayMethodEndParmsVersion && !skipEndParms )
					{
						// EndParms
						DeserializeNext();
					}
				}

				public override void Deserialize()
				{
					this.DeserializeMethodTwo();
				}

				public override string Decompile()
				{
					return DecompileMethodTwo( "Insert" );
				}

				//(0x033) DynamicArrayInsertToken -> LocalVariableToken -> EndFunctionParmsToken 
			}

			public class DynamicArrayInsertItemToken : DynamicArrayMethodToken
			{
				public override void Deserialize()
				{
					DeserializeMethodTwo();
				}

				public override string Decompile()
				{
					return DecompileMethodTwo( "InsertItem" );
				}
			}

			public class DynamicArrayRemoveToken : DynamicArrayMethodToken
			{
				protected override void DeserializeMethodTwo( bool skipEndParms = false )
				{
					// Array
					DeserializeNext();

					// Param 1
					DeserializeNext();

					// Param 2
					DeserializeNext();

					if( Buffer.Version > ArrayMethodEndParmsVersion && !skipEndParms )
					{
						// EndParms
						DeserializeNext();
					}
				}

				public override void Deserialize()
				{
					this.DeserializeMethodTwo();
				}

				public override string Decompile()
				{
					return DecompileMethodTwo( "Remove" );
				}
			}

			public class DynamicArrayRemoveItemToken : DynamicArrayMethodToken
			{
				public override void Deserialize()
				{
					DeserializeMethodOne();
				}

				public override string Decompile()
				{
					return DecompileMethodOne( "RemoveItem" );
				}
			}

			public class DynamicArrayAddToken : DynamicArrayMethodToken
			{
				public override void Deserialize()
				{
					// Array
					DeserializeNext();

					// Param 1
					DeserializeNext();

					// EndParms
					DeserializeNext();
				}

				public override string Decompile()
				{
					return DecompileMethodOne( "Add" );
				}
			}

			public class DynamicArrayAddItemToken : DynamicArrayMethodToken
			{
				public override void Deserialize()
				{
					DeserializeMethodOne();
				}

				public override string Decompile()
				{
					return DecompileMethodOne( "AddItem" );
				}
			}

			// TODO: Fix the output below 	  (FIXED)
			//	(0x00C) LetToken(55) -> LocalVariableToken(9) -> DynamicArrayFindToken(45) -> LocalVariableToken(9) -> NoObjectToken(1) -> JumpIfNotToken(32) -> NativeFunctionToken(29) -> DynamicArrayFindToken(18) -> LocalVariableToken(9) -> NoObjectToken(1) -> IntConstToken(5) -> EndFunctionParmsToken(1) -> NativeFunctionToken(9) -> StringConstToken(6) -> NoParmToken(1) -> EndFunctionParmsToken(1)
			//	I = T.Find(noneif(T.Find(none-1 == ))
			public class DynamicArrayFindToken : DynamicArrayMethodToken
			{
				public override void Deserialize()
				{
					DeserializeMethodOne();
				}

				public override string Decompile()
				{
					return DecompileMethodOne( "Find" );
				}
			}

			public class DynamicArrayFindStructToken : DynamicArrayMethodToken
			{
				public override void Deserialize()
				{
					DeserializeMethodTwo();
				}

				public override string Decompile()
				{
					return DecompileMethodTwo( "Find" );
				}
			}

			public class DynamicArraySortToken : DynamicArrayMethodToken
			{
				public override void Deserialize()
				{
					DeserializeMethodOne();
				}

				public override string Decompile()
				{
					return DecompileMethodOne( "Sort" );
				}
			}
			#endregion

			public sealed class UnknownExprToken : Token
			{
				public override string Decompile()
				{
					return String.Format( "UnknownExprToken(0x{0:x2})", RepresentToken ); 
				}
			}

			public sealed class UnknownCastToken : Token
			{
				public override string Decompile()
				{
					return String.Format( "UnknownCastToken(0x{0:x2})", RepresentToken ); 
				}
			}
			#endregion
#endif
		}	
	}
}
