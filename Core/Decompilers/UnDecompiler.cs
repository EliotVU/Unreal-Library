//#define SUPPRESS_BOOLINTEXPLOIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using UELib.Tokens;

namespace UELib.Core
{
    using System.Text;

    public partial class UStruct
    {
        /// <summary>
        /// Decompiles the bytecodes from the 'Owner'
        /// </summary>
        public class UByteCodeDecompiler : IUnrealDecompilable
        {
            /// <summary>
            /// The Struct that contains the bytecode that we have to deserialize and decompile!
            /// </summary>
            private readonly UStruct _Container;

            /// <summary>
            /// Pointer to the ObjectStream buffer of 'Owner'
            /// </summary>
            private UObjectStream Buffer{ get { return _Container.Buffer; } }

            /// <summary>
            /// A collection of deserialized tokens, in their correspondence stream order.
            /// </summary>
            public List<Token> DeserializedTokens{ get; private set; }

            [System.ComponentModel.DefaultValue(-1)]
            public int CurrentTokenIndex{ get; private set; }

            public Token NextToken
            {
                get{ return DeserializedTokens[++ CurrentTokenIndex]; }
            }

            public Token PeekToken
            {
                [Pure]get{ return DeserializedTokens[CurrentTokenIndex + 1]; }
            }

            public Token PreviousToken
            {
                [Pure]get{ return DeserializedTokens[CurrentTokenIndex - 1]; }
            }

            public Token CurrentToken
            {
                [Pure]get{ return DeserializedTokens[CurrentTokenIndex]; }
            }

            public UByteCodeDecompiler( UStruct container )
            {
                _Container = container;
                AlignMemorySizes();
            }

            #region Deserialize
            /// <summary>
            /// The current simulated-memory-aligned position in @Buffer.
            /// </summary>
            private uint CodePosition{ get; set; }

            private const byte IndexMemorySize = 4;
            private byte _NameMemorySize = IndexMemorySize;
            private byte _ObjectMemorySize = IndexMemorySize;

            private void AlignMemorySizes()
            {
                const short vNameSizeTo8 = 500;
                if( Buffer.Version >= vNameSizeTo8 )
                {
                    _NameMemorySize = 8;
                }

                const short vObjectSizeTo8 = 587;
                if( Buffer.Version >= vObjectSizeTo8 )
                {
                    _ObjectMemorySize = 8;    
                }
            }

            private void AlignSize( byte size )
            {
                CodePosition += size;
            }

            private void AlignSize( int size )
            {
                AlignSize( (byte)size );
            }

            private void AlignNameSize()
            {
                AlignSize( _NameMemorySize );
            }

            private void AlignObjectSize()
            {
                AlignSize( _ObjectMemorySize );
            }

            /// <summary>
            /// Fix the values of UE1/UE2 tokens to match the UE3 token values.
            /// </summary>
            private byte FixToken( byte tokenCode )
            {
                // Adjust UE2 tokens to UE3
                if( _Container.Package.Version >= 184 
                    && 
                    (
                        (tokenCode >= (byte)ExprToken.Unknown && tokenCode < (byte)ExprToken.ReturnNothing) 
                        || 
                        (tokenCode > (byte)ExprToken.NoDelegate && tokenCode < (byte)ExprToken.ExtendedNative)) 
                    )
                {
                    ++ tokenCode;
                }


                #if APB
                if( _Container.Package.Build == UnrealPackage.GameBuild.BuildName.APB && _Container.Package.LicenseeVersion >= 32 )
                {
                    if( tokenCode == (byte)ExprToken.Return )
                    {
                        tokenCode = (byte)ExprToken.LocalVariable;
                    }
                    else if( tokenCode == (byte)ExprToken.LocalVariable )
                    {
                        tokenCode = (byte)ExprToken.Return;
                    }
                    else if( tokenCode == (byte)ExprToken.Jump )
                    {
                        tokenCode = (byte)ExprToken.JumpIfNot;
                    }
                    else if( tokenCode == (byte)ExprToken.JumpIfNot )
                    {
                        tokenCode = (byte)ExprToken.Jump;
                    }
                    else if( tokenCode == (byte)ExprToken.Case )
                    {
                        tokenCode = (byte)ExprToken.Nothing;
                    }
                    else if( tokenCode == (byte)ExprToken.Nothing )
                    {
                        tokenCode = (byte)ExprToken.Case;
                    }
                }
                #endif

                return tokenCode;
            }

            private bool _WasDeserialized;
            public void Deserialize()
            {
                if( _WasDeserialized )
                    return;

                _WasDeserialized = true;
                Buffer.Seek( _Container.ScriptOffset, System.IO.SeekOrigin.Begin );
                CodePosition = 0;
                var codeSize = _Container.ByteScriptSize;
        
                CurrentTokenIndex = -1;
                DeserializedTokens = new List<Token>();
                _Labels = new List<ULabelEntry>();
                while( CodePosition < codeSize )
                {
                    try
                    {
                        var t = DeserializeNext();
                        if( !(t is EndOfScriptToken) )
                            continue;

                        if( CodePosition < codeSize )
                        { 
                            Console.WriteLine( "End of script detected, but the loop condition is still true." );
                        }
                        break;
                    }
                    catch( SystemException e )
                    {
                        if( e is System.IO.EndOfStreamException )
                        {
                            Console.WriteLine( "Couldn't backup from this error! Decompiling aborted!" );
                            return;
                        }
                        Console.WriteLine( "Object:" + _Container.Name );
                        Console.WriteLine( "Failed to deserialize token at position:" + CodePosition );
                        Console.WriteLine( "Exception:" + e.Message );
                        Console.WriteLine( "Stack:" + e.StackTrace );
                    }
                }
            }

            private void DeserializeDebugToken()
            {
                Buffer.StartPeek();
                byte token = FixToken( Buffer.ReadByte() );	
                Buffer.EndPeek();

                if(	token == (byte)ExprToken.DebugInfo )
                {
                    DeserializeNext();
                }
            }

            private NativeFunctionToken FindNativeTable( int nativeIndex )
            {
                var nativeFuncToken = new NativeFunctionToken();
                try
                {
                    var nativeTableItem = _Container.Package.NTLPackage != null 
                        ? _Container.Package.NTLPackage.FindTableItem( nativeIndex ) 
                        : null;
                    if( nativeTableItem != null )
                    {
                        nativeFuncToken.NativeTable = nativeTableItem;
                    }
                    else
                    {
                        // TODO: Rewrite as FindChild( lambda )
                        var table = _Container.Package.Exports.Find( 
                            e => (e.ClassName == "Function" && ((UFunction)(e.Object)).NativeToken == nativeIndex) 
                        );

                        if( table != null )
                        {
                            var func = table.Object as UFunction;
                            if( func != null )
                            {
                                nativeTableItem = new NativeTableItem( func );
                                nativeFuncToken.NativeTable = nativeTableItem;
                            }
                        }
                    }
                }
                catch( ArgumentOutOfRangeException )
                {
                    // ...
                }
                return nativeFuncToken;
            }

            private Token DeserializeNext( byte tokenCode = Byte.MaxValue )
            {
                var tokenPosition = CodePosition;
                if( tokenCode == Byte.MaxValue )
                {
                    tokenCode = FixToken( Buffer.ReadByte() );
                    AlignSize( sizeof(byte) );
                }

                Token tokenItem = null;
                if(	tokenCode >= (byte)ExprToken.FirstNative )
                {
                    tokenItem = FindNativeTable( tokenCode );
                }
                else if( tokenCode >= (byte)ExprToken.ExtendedNative )
                {
                    tokenItem = FindNativeTable( (tokenCode - (byte)ExprToken.ExtendedNative) << 8 | Buffer.ReadByte() );
                    AlignSize( sizeof(byte) );
                }
                else switch( tokenCode )
                {
                    #region Cast
                    case (byte)ExprToken.DynamicCast:
                        tokenItem = new DynamicCastToken();
                        break;

                    case (byte)ExprToken.MetaCast:
                        tokenItem = new MetaCastToken();
                        break;

                    case (byte)ExprToken.InterfaceCast:
                        if( Buffer.Version < PrimitveCastVersion )		// UE1
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
                        if( Buffer.Version < PrimitveCastVersion )		// UE1
                        {
                            tokenItem = new RotatorToVectorToken();
                        }
                        else											// UE2+
                        {
                            // Next byte represents the CastToken!
                            tokenCode = Buffer.ReadByte();
                            AlignSize( sizeof(byte) );

                            tokenItem = DeserializeCastToken( tokenCode );
                            //tokenitem = new PrimitiveCastToken();
                        }
                        break;
                    #endregion

                    #region Context
                    case (byte)ExprToken.ClassContext:
                        tokenItem = new ClassContextToken();
                        break;

                    case (byte)ExprToken.InterfaceContext:
                        if( Buffer.Version < PrimitveCastVersion )
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
                        if( Buffer.Version < PrimitveCastVersion )
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
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new NameToBoolToken();
                        }
                        else if( Buffer.Version >= 300 )
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
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new	ByteToIntToken();
                        }
                            // Definitely existed since GoW(490)
                        else if( Buffer.Version > 420 && (DeserializedTokens.Count > 0 && !(DeserializedTokens[DeserializedTokens.Count - 1] is ReturnToken)) ) // Should only be done if the last token wasn't Return
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
                        if( Buffer.Version < PrimitveCastVersion )
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

                    case (byte)ExprToken.FilterEditorOnly:
                        tokenItem = new FilterEditorOnlyToken();
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
                        #if BORDERLANDS2
                            if( _Container.Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 )
                            {
                                tokenItem = new DynamicVariableToken();
                                break;
                            }
                        #endif
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
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new FloatToIntToken();
                        }
                        else
                        {
                            tokenItem = new DelegatePropertyToken();
                        }
                        break;

                    case (byte)ExprToken.DefaultParmValue:
                        if( Buffer.Version < PrimitveCastVersion )	 // StringToInt
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
                        if( Buffer.Version < PrimitveCastVersion )
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
                        if( Buffer.Version < PrimitveCastVersion )
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
                        if( Buffer.Version < PrimitveCastVersion )
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

                    case (byte)ExprToken.StructCmpNE:
                        tokenItem = new StructCmpNeToken();
                        break;

                    case (byte)ExprToken.DelegateCmpEq:
                        tokenItem = new DelegateCmpEqToken();
                        break;

                    case (byte)ExprToken.DelegateFunctionCmpEq:
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new IntToBoolToken();
                        }
                        else
                        {
                            tokenItem = new DelegateFunctionCmpEqToken();
                        }
                        break;
                
                    case (byte)ExprToken.DelegateCmpNE:
                        tokenItem = new DelegateCmpNEToken();
                        break;

                    case (byte)ExprToken.DelegateFunctionCmpNE:
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new IntToBoolToken();
                        }
                        else
                        {
                            tokenItem = new DelegateFunctionCmpNEToken();
                        }
                        break;

                    case (byte)ExprToken.InstanceDelegate:
                        tokenItem = new InstanceDelegateToken();
                        break;

                    case (byte)ExprToken.EatString:
                        tokenItem = new EatStringToken();
                        break;

                    case (byte)ExprToken.New:
                        tokenItem = new NewToken();
                        break;

                    case (byte)ExprToken.FunctionEnd: // case (byte)ExprToken.DynArrayFind:
                        if( Buffer.Version < 300 )
                        {
                            tokenItem = new EndOfScriptToken();
                        }
                        else
                        {
                            tokenItem = new DynamicArrayFindToken();
                        }
                        break;

                    case (byte)ExprToken.VarInt:
                    case (byte)ExprToken.VarFloat:
                    case (byte)ExprToken.VarByte:
                    case (byte)ExprToken.VarBool:
                    //case (byte)ExprToken.VarObject:	// See UndefinedVariable	
                        tokenItem = new DynamicVariableToken();
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
                        if( Buffer.Version < PrimitveCastVersion )
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
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new BoolToByteToken();
                        }
                        else
                        {
                            tokenItem = new DynamicArrayInsertToken();
                        }
                        break;

                    case (byte)ExprToken.DynArrayInsertItem:
                        if( Buffer.Version < PrimitveCastVersion )
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
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new BoolToIntToken();
                        }
                        else
                        {
                            tokenItem = new DynamicArrayRemoveToken();
                        }
                        break;

                    case (byte)ExprToken.DynArrayRemoveItem:
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new NameToStringToken();
                        }
                        else
                        {
                            tokenItem = new DynamicArrayRemoveItemToken();
                        }
                        break;

                    case (byte)ExprToken.DynArrayAdd:
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            tokenItem = new FloatToStringToken();
                        }
                        else
                        {
                            tokenItem = new DynamicArrayAddToken();
                        }
                        break;

                    case (byte)ExprToken.DynArrayAddItem:
                        if( Buffer.Version < PrimitveCastVersion )
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
                        if( Buffer.Version < PrimitveCastVersion )
                        {
                            // No other token was matched. Check if it matches any of the CastTokens
                            // We don't just use PrimitiveCast detection due compatible with UE1 games
                            tokenItem = DeserializeCastToken( tokenCode );
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
                tokenItem.RepresentToken = tokenCode;
                tokenItem.Position = tokenPosition;// + (uint)Owner._ScriptOffset;
                tokenItem.StoragePosition = (uint)Buffer.Position - (uint)_Container.ScriptOffset;
                // IMPORTANT:Add before deserialize, due the possibility that the tokenitem might deserialize other tokens as well.
                DeserializedTokens.Add( tokenItem );
                tokenItem.Deserialize();
                // Includes all sizes of followed tokens as well! e.g. i = i + 1; is summed here but not i = i +1; (not>>)i ++;
                tokenItem.Size = (ushort)(CodePosition - tokenPosition);
                tokenItem.StorageSize = (ushort)(tokenItem.StoragePosition 
                    - ((uint)Buffer.Position - (uint)_Container.ScriptOffset));
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
                        tokenitem = new BoolToIntToken();
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
                        tokenitem = new IntToBoolToken();
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

                    case Tokens.CastToken.InterfaceToObject:
                        tokenitem = new InterfaceToObjectToken();
                        break;

                    case Tokens.CastToken.ObjectToInterface:
                        tokenitem = new ObjectToInterfaceToken();
                        break;

                    case Tokens.CastToken.DelegateToString:
                        tokenitem = new DelegateToStringToken();
                        break;
                }

                // Unsure what this is, found in:  ClanManager1h_6T.CMBanReplicationInfo.Timer:	
                //	xyz = UnknownCastToken(0x1b);
                //	UnknownCastToken(0x1b)
                //	UnknownCastToken(0x1b)
                if( castToken == 0x1b )
                    tokenitem = new FloatToIntToken();

                return tokenitem ?? new UnknownCastToken();
            }
            #endregion

#if DECOMPILE
            #region Decompile
            public class NestManager
            {
                public UByteCodeDecompiler Decompiler;

                public class Nest : IUnrealDecompilable
                {
                    [System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue" )]
                    public enum NestType : byte
                    {
                        Scope				= 0,
                        If					= 1,
                        Else				= 2,
                        ForEach				= 4,
                        Switch				= 5, Case				= 6, Default			= 7,
                        Loop				= 8
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
                        #if DEBUG_NESTS
                            return "\r\n" + UDecompilingState.Tabs + "//<" + Type + ">";
                        #else
                        return Type != NestType.Case && Type != NestType.Default 
                            ? UnrealConfig.PrintBeginBracket() 
                            : String.Empty;
                        #endif
                    }
                }

                public class NestEnd : Nest
                {
                    public override string Decompile()
                    {
                        #if DEBUG_NESTS
                            return "\r\n" + UDecompilingState.Tabs + "//</" + Type + ">";
                        #else
                        return Type != NestType.Case && Type != NestType.Default 
                            ? UnrealConfig.PrintEndBracket() 
                            : String.Empty;
                        #endif
                    }
                }

                public readonly List<Nest> Nests = new List<Nest>();

                public void AddNest( Nest.NestType type, uint position, uint endPosition, Token creator = null )
                {	
                    creator = creator ?? Decompiler.CurrentToken;
                    Nests.Add( new NestBegin{Position = position, Type = type, Creator = creator} );
                    Nests.Add( new NestEnd{Position = endPosition, Type = type, Creator = creator} );
                }

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

            private NestManager _Nester;

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
                CurrentTokenIndex = -1;
                CodePosition = 0;

                FieldToken.LastField = null;

                // TODO: Corrigate detection and version.
                if( Buffer.Version > 300 )
                {
                    var func = _Container as UFunction;
                    if( func != null && func.Params != null && func.HasFunctionFlag( Flags.FunctionFlags.OptionalParameters ) )
                    { 
                        DefaultParameterToken._NextParamIndex = func.Params.FindIndex( 
                            p => p.HasPropertyFlag( Flags.PropertyFlagsLO.OptionalParm ) 
                        );
                    }
                }

                // Reset these, in case of a loop in the Decompile function that did not finish due exception errors!
                _IsWithinClassContext = false;
                _CanAddSemicolon = false;
                _MustCommentStatement = false;
                _PostIncrementTabs = 0;
                _PostDecrementTabs = 0;
                _PreIncrementTabs = 0;
                _PreDecrementTabs = 0;
                PreComment = String.Empty;
                PostComment = String.Empty;

                _TempLabels = new List<ULabelEntry>();
                if( _Labels != null )
                {
                    for( int i = 0; i < _Labels.Count; ++ i )
                    {
                        // No duplicates, caused by having multiple goto's with the same destination
                        if( !_TempLabels.Exists( p => p.Position == _Labels[i].Position ) )
                        {
                            _TempLabels.Add( _Labels[i] );
                        }
                    }
                }
            }

            public void JumpTo( ushort codeOffset )
            {
                var index = DeserializedTokens.FindIndex( t => t.Position == codeOffset );
                if( index == -1 )
                    return;

                CurrentTokenIndex = index;
            }

            public Token TokenAt( ushort codeOffset )
            {
                return DeserializedTokens.Find( t => t.Position == codeOffset );
            }

            /// <summary>
            /// Whether we are currently decompiling within a ClassContext token.
            /// 
            /// HACK: For static calls -> class'ClassA'.static.FuncA();
            /// </summary>
            private bool _IsWithinClassContext;
            private bool _CanAddSemicolon;
            private bool _MustCommentStatement;

            private byte _PostIncrementTabs;
            private byte _PostDecrementTabs;
            private byte _PreIncrementTabs;
            private byte _PreDecrementTabs;

            public string PreComment;
            public string PostComment;

            public string Decompile()
            {
                // Make sure that everything is deserialized!
                if( !_WasDeserialized )			   
                {
                    Deserialize();		
                }

                var output = new StringBuilder();
                // Original indention, so that we can restore it later, necessary if decompilation fails to reduce nesting indention.
                string initTabs = UDecompilingState.Tabs;

#if DEBUG_TOKENPOSITIONS
                UDecompilingState.AddTabs( 3 );
#endif			
                try
                {
                    //Initialize==========
                    InitDecompile();
                    bool spewOutput = false;
                    int tokenBeginIndex = 0;
                    Token lastStatementToken = null;

                    while( CurrentTokenIndex + 1 < DeserializedTokens.Count )
                    {
                        try
                        {
                            //Decompile chain==========
                            {
                                string tokenOutput;
                                var newToken = NextToken;
                                output.Append( DecompileLabels() );
                                try
                                {
                                    // FIX: Formatting issue on debug-compiled packages
                                    if( newToken is DebugInfoToken )
                                    {
                                        string nestsOutput = DecompileNests();
                                        if( nestsOutput.Length != 0 )
                                        {
                                            output.Append( nestsOutput );
                                            spewOutput = true;
                                        }
                                        continue;
                                    }
                                }
                                catch( Exception e )
                                {
                                    output.Append( "// (" + e.GetType().Name + ")" );
                                }

                                try
                                {
                                    tokenBeginIndex = CurrentTokenIndex;
                                    tokenOutput = newToken.Decompile();
                                    if( CurrentTokenIndex + 1 < DeserializedTokens.Count && PeekToken is EndOfScriptToken )
                                    {
                                        var firstToken = newToken is DebugInfoToken ? lastStatementToken : newToken;
                                        if( firstToken is ReturnToken )
                                        {
                                            var lastToken = newToken is DebugInfoToken ? PreviousToken : CurrentToken;
                                            if( lastToken is NothingToken || lastToken is ReturnNothingToken )
                                            {
                                                _MustCommentStatement = true;
                                            }
                                        }
                                    }
                                }
                                catch( Exception e )
                                {
                                    tokenOutput = newToken.GetType().Name + "-" + CurrentToken.GetType().Name
                                        + "(" + e + ")";
                                }

                                // HACK: for multiple cases for one block of code, etc!
                                if( _PreDecrementTabs > 0 )
                                {
                                    UDecompilingState.RemoveTabs( _PreDecrementTabs );
                                    _PreDecrementTabs = 0;
                                }

                                if( _PreIncrementTabs > 0 )
                                {
                                    UDecompilingState.AddTabs( _PreIncrementTabs );
                                    _PreIncrementTabs = 0;
                                }

                                if( !UnrealConfig.SuppressComments )
                                {
                                    if( PreComment.Length != 0 )
                                    {
                                        tokenOutput = PreComment + "\r\n" + UDecompilingState.Tabs + tokenOutput;
                                        PreComment = String.Empty;
                                    }

                                    if( PostComment.Length != 0 )
                                    {
                                        tokenOutput += PostComment;
                                        PostComment = String.Empty;
                                    }
                                }

                                //Preprocess output==========
                                {
#if DEBUG_HIDDENTOKENS
                                    if( tokenOutput.Length == 0 )
                                    {
                                        tokenOutput = "<" +  newToken.GetType().Name + "/>";
                                        _MustCommentStatement = true;
                                    }
#endif
                                    // Previous did spew and this one spews? then a new line is required!
                                    if( tokenOutput.Length != 0 )
                                    {
                                        // Spew before?
                                        if( spewOutput )
                                        {
                                            output.Append( "\r\n" );
                                        }
                                        else spewOutput = true;
                                    }

                                    if( spewOutput )
                                    {
                                        if( _MustCommentStatement )
                                        {
                                            tokenOutput = "//" + tokenOutput;
                                            _MustCommentStatement = false;
                                        }
#if DEBUG_TOKENPOSITIONS
                                        output.Append( String.Format( initTabs + "({0:X3}{1:X3})",
                                            newToken.Position,
                                            CurrentToken.Position + CurrentToken.Size
                                        ) );

                                        var orgTabs = UDecompilingState.Tabs; 
                                        var spaces = Math.Max( 3*UnrealConfig.Indention.Length, 8 );
                                        UDecompilingState.RemoveSpaces( spaces );
#endif
                                        output.Append( UDecompilingState.Tabs + tokenOutput );
#if DEBUG_TOKENPOSITIONS
                                        UDecompilingState.Tabs = orgTabs;
#endif																																		
                                        // One of the decompiled tokens wanted to be ended.
                                        if( _CanAddSemicolon )
                                        {
                                            output.Append( ";" );
                                            _CanAddSemicolon = false;
                                        }
                                    }
                                }
                                lastStatementToken = newToken;
                            }

                            //Postprocess output==========
                            if( _PostDecrementTabs > 0 )
                            {
                                UDecompilingState.RemoveTabs( _PostDecrementTabs );
                                _PostDecrementTabs = 0;
                            }

                            if( _PostIncrementTabs > 0 )
                            {
                                UDecompilingState.AddTabs( _PostIncrementTabs );
                                _PostIncrementTabs = 0;
                            }

                            try
                            {
                                string nestsOutput = DecompileNests();
                                if( nestsOutput.Length != 0 )
                                {
                                    output.Append( nestsOutput );
                                    spewOutput = true;
                                }

                                output.Append( DecompileLabels() );
                            }
                            catch( Exception e )
                            {
                                output.Append( "\r\n" + UDecompilingState.Tabs + "// Failed to format nests!:"
                                    + e + "\r\n"
                                    + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                    + _Nester.Nests[_Nester.Nests.Count - 1] );
                                spewOutput = true;
                            }
                        }
                        catch( Exception e )
                        {
                            output.Append( "\r\n" + UDecompilingState.Tabs
                                + "// Failed to decompile this line:\r\n" );
                            UDecompilingState.AddTab();
                            output.Append( UDecompilingState.Tabs + "/* "
                                + FormatTokens( tokenBeginIndex, CurrentTokenIndex ) + " */\r\n" );
                            UDecompilingState.RemoveTab();
                            output.Append( UDecompilingState.Tabs + "// " + FormatTabs( e.Message ) );
                            spewOutput = true;
                        }
                    }

                    try
                    {
                        // Decompile remaining nests
                        output.Append( DecompileNests( true ) );
                    }
                    catch( Exception e )
                    {
                        output.Append( "\r\n" + UDecompilingState.Tabs
                            + "// Failed to format remaining nests!:" + e + "\r\n"
                            + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                            + _Nester.Nests[_Nester.Nests.Count - 1] );
                    }
                }
                catch( Exception e )
                {
                    output.AppendFormat(
                        "{0} // Failed to decompile this {1}'s code." +
                            "\r\n {2} at position {3} " +
                            "\r\n Message: {4} " +
                            "\r\n\r\n StackTrace: {5}",
                        UDecompilingState.Tabs,
                        _Container.Class.Name,
                        UDecompilingState.Tabs,
                        CodePosition,
                        FormatTabs( e.Message ),
                        FormatTabs( e.StackTrace )
                    );
                }
                finally
                { 
                    UDecompilingState.Tabs = initTabs;
                }
                return output.ToString();
            }

            private readonly List<NestManager.Nest> _NestChain = new List<NestManager.Nest>();

            private static string FormatTabs( string nonTabbedText )
            {
                return nonTabbedText.Replace( "\n", "\n" + UDecompilingState.Tabs );
            }

            private string FormatTokens( int beginIndex, int endIndex )
            {
                string output = String.Empty;
                for( int i = beginIndex; i < endIndex; ++ i )
                {
                    output += DeserializedTokens[i].GetType().Name 
                        + (i % 4 == 0 ? "\r\n" + UDecompilingState.Tabs : " ");
                }
                return output;
            }

            private string DecompileLabels()
            {
                string output = String.Empty;
                for( int i = 0; i < _TempLabels.Count; ++ i )
                {
                    var label = _TempLabels[i];
                    if( PeekToken.Position < label.Position )
                        continue;

                    var isStateLabel = !label.Name.StartsWith( "J0x", StringComparison.Ordinal );

                    output += isStateLabel 
                        ? String.Format( "\r\n{0}:\r\n", label.Name ) 
                        : String.Format( "\r\n{0}:", UDecompilingState.Tabs + label.Name );
                    _TempLabels.RemoveAt( i );
                    -- i;
                }
                return output;
            }

            private string DecompileNests( bool outputAllRemainingNests = false )
            {
                string output = String.Empty;

                // Give { priority hence separated loops
                for( int i = 0; i < _Nester.Nests.Count; ++ i )
                {
                    if( !(_Nester.Nests[i] is NestManager.NestBegin) ) 
                        continue;

                    if( _Nester.Nests[i].IsPastOffset( CurrentToken.Position ) || outputAllRemainingNests )
                    {
                        output += _Nester.Nests[i].Decompile();
                        UDecompilingState.AddTab();

                        _NestChain.Add( _Nester.Nests[i] );
                        _Nester.Nests.RemoveAt( i -- );
                    }
                }

                for( int i = 0; i < _Nester.Nests.Count; ++ i )
                {
                    if( !(_Nester.Nests[i] is NestManager.NestEnd) ) 
                        continue;

                    if( _Nester.Nests[i].IsPastOffset( CurrentToken.Position + CurrentToken.Size ) 
                        || outputAllRemainingNests )
                    {
                        UDecompilingState.RemoveTab();
                        output += _Nester.Nests[i].Decompile();

                        // TODO: This should not happen!
                        if( _NestChain.Count > 0 )
                        {
                            _NestChain.RemoveAt( _NestChain.Count - 1 );
                        }
                        _Nester.Nests.RemoveAt( i -- );
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
            public abstract class Token : IUnrealDecompilable
            {
                public UByteCodeDecompiler Decompiler
                { 
                    get;
                    set;
                }

                protected UObjectStream Buffer
                {
                    get{ return Decompiler.Buffer; }
                }

                protected UnrealPackage Package
                {
                    get{ return Buffer.Package; }
                }

                public byte RepresentToken;	 // Fixed(adjusted at decompile time for compatibility)

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
                    return String.Format( "0x{0:X2}", RepresentToken );
                }

                protected string DecompileNext()
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

                protected Token GrabNextToken()
                {
                    tryNext:
                    var t = Decompiler.NextToken;
                    if( t is DebugInfoToken )
                    {
                        goto tryNext;
                    }
                    return t;
                }

                protected Token DeserializeNext()
                {
                    return Decompiler.DeserializeNext();
                }

                public override string ToString()
                {
                    return String.Format( "\r\nType:{0}\r\nToken:{1:X2}\r\nPosition:{2}\r\nSize:{3}", 
                        GetType().Name, RepresentToken, Position, Size ).Replace( "\n", "\n" 
                        + UDecompilingState.Tabs 
                    );
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

                private void DeserializeParms()
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

                private static string PrecedenceToken( Token t )
                {
                    if( !(t is FunctionToken) )
                        return t.Decompile();

                    // Always add ( and ) unless the conditions below are not met, in case of a VirtualFunctionCall.
                    var addParenthesises = true;
                    if( t is NativeFunctionToken )
                    {
                        addParenthesises = ((NativeFunctionToken)t).NativeTable.Type == FunctionType.Operator;    
                    }
                    else if( t is FinalFunctionToken )
                    {
                        addParenthesises = ((FinalFunctionToken)t).Function.IsOperator(); 
                    }
                    return addParenthesises ? String.Format( "({0})", t.Decompile() ) : t.Decompile();
                }

                protected string DecompilePreOperator( string operatorName )
                {
                    string output = operatorName + (operatorName.Length > 1 ? " " : String.Empty) + DecompileNext();
                    DecompileNext(); // )
                    return output;
                }

                protected string DecompileOperator( string operatorName )
                {
                    string output = String.Format( "{0} {1} {2}", 
                        PrecedenceToken( GrabNextToken() ), 
                        operatorName, 
                        PrecedenceToken( GrabNextToken() ) 
                    );
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
                    if( Decompiler._IsWithinClassContext )
                    {
                        functionName = "static." + functionName;	
                
                        // Set false elsewhere as well but to be sure we set it to false here to avoid getting static calls inside the params.
                        // e.g. 	
                        // A1233343.DrawText(Class'BTClient_Interaction'.static.A1233332(static.Max(0, A1233328 - A1233322[A1233222].StartTime)), true);
                        Decompiler._IsWithinClassContext = false;
                    }
                    string output = functionName + "(" + DecompileParms();
                    return output;
                }

                // TODO: Rewrite properly, also fix a suspected bug for UE3 function calls with optional params.
                private string DecompileParms()
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
                public UFunction Function;

                public override void Deserialize()
                {
                    if( Buffer.Version == 421 )
                    {
                        Decompiler.AlignSize( sizeof(int) );
                    }

                    Function = Buffer.ReadObject() as UFunction;
                    Decompiler.AlignObjectSize();

                    DeserializeCall();
                }

                public override string Decompile()
                {
                    string output = String.Empty;
                    if( Function != null )
                    {		
                        // Support for non native operators.
                        if( Function.IsPost() )
                        {
                            output = DecompilePreOperator( Function.FriendlyName );
                        }
                        else if( Function.IsPre() )
                        {
                            output = DecompilePostOperator( Function.FriendlyName );
                        }
                        else if( Function.IsOperator() )
                        {
                            output = DecompileOperator( Function.FriendlyName );	
                        }
                        else
                        {
                            // Calling Super??.
                            if( Function.Name == Decompiler._Container.Name && !Decompiler._IsWithinClassContext )
                            {
                                output = "super";

                                // Check if the super call is within the super class of this functions outer(class)
                                var myouter = (UField)Decompiler._Container.Outer;
                                if( myouter == null || myouter.Super == null || Function.GetOuterName() != myouter.Super.Name  )
                                {
                                    // There's no super to call then do a recursive super call.
                                    if( Decompiler._Container.Super == null )
                                    {
                                        output += "(" + Decompiler._Container.GetOuterName() + ")";
                                    }
                                    else
                                    {
                                        // Different owners, then it is a deep super call.
                                        if( Function.GetOuterName() != Decompiler._Container.GetOuterName() )
                                        {
                                            output += "(" + Function.GetOuterName() + ")";
                                        }
                                    }
                                }
                                output += ".";
                            }
                            output += DecompileCall( Function.Name );
                        }
                    }	
                    Decompiler._CanAddSemicolon = true;	
                    return output;
                }
            }

            public class VirtualFunctionToken : FunctionToken
            {
                public int FunctionNameIndex;

                public override void Deserialize()
                {
                    // TODO: Corrigate Version (Definitely not in MOHA, but in roboblitz(369))
                    if( Buffer.Version >= 178 && Buffer.Version < 421/*MOHA*/ )
                    {
                        byte super = Buffer.ReadByte();
                        Decompiler.AlignSize( sizeof(byte) );
                    }

                    if( Buffer.Version == 421 )
                    {
                        Decompiler.AlignSize( sizeof(int) );
                    }

                    FunctionNameIndex = Buffer.ReadNameIndex();
                    Decompiler.AlignNameSize();

                    DeserializeCall();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;	
                    return DecompileCall( Decompiler._Container.Package.GetIndexName( FunctionNameIndex ) );
                }
            }

            public class GlobalFunctionToken : FunctionToken
            {
                public int FunctionNameIndex;

                public override void Deserialize()
                {
                    FunctionNameIndex = Buffer.ReadNameIndex();
                    Decompiler.AlignNameSize();

                    DeserializeCall();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;	
                    return "global." + DecompileCall( Decompiler._Container.Package.GetIndexName( FunctionNameIndex ) );
                }
            }

            public class DelegateFunctionToken : FunctionToken
            {
                public int FunctionNameIndex;

                public override void Deserialize()
                {
                    // TODO: Corrigate Version
                    if( Buffer.Version > 180 )
                    {
                        ++ Buffer.Position;	// ReadByte()
                        Decompiler.AlignSize( sizeof(byte) );
                    }

                    // Delegate object index
                    Buffer.ReadObjectIndex();
                    Decompiler.AlignObjectSize();	
 
                    // Delegate name index
                    FunctionNameIndex = Buffer.ReadNameIndex();
                    Decompiler.AlignNameSize();

                    DeserializeCall();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return DecompileCall( Decompiler._Container.Package.GetIndexName( FunctionNameIndex ) );
                }
            }

            public class NativeFunctionToken : FunctionToken
            {
                public NativeTableItem NativeTable;

                public override void Deserialize()
                {
                    if( NativeTable == null )
                    {
                        NativeTable = new NativeTableItem
                        {
                            Type = FunctionType.Function, 
                            Name = "UnresolvedNativeFunction_" + RepresentToken, 
                            ByteToken = RepresentToken
                        };
                    }

                    switch( NativeTable.Type )
                    {
                        case FunctionType.Function:
                            DeserializeCall();
                            break;

                        case FunctionType.PreOperator:
                        case FunctionType.PostOperator:
                            DeserializeUnaryOperator();
                            break;

                        case FunctionType.Operator:
                            DeserializeBinaryOperator();
                            break;

                        default:
                            DeserializeCall();
                            break;
                    }
                }

                public override string Decompile()
                {
                    string output;
                    switch( NativeTable.Type )
                    {
                        case FunctionType.Function:
                            output = DecompileCall( NativeTable.Name );
                            break;

                        case FunctionType.Operator:
                            output = DecompileOperator( NativeTable.Name );
                            break;

                        case FunctionType.PostOperator:
                            output = DecompilePostOperator( NativeTable.Name );
                            break;

                        case FunctionType.PreOperator:
                            output = DecompilePreOperator( NativeTable.Name );
                            break;

                        default:
                            output = DecompileCall( NativeTable.Name );
                            break;
                    }
                    Decompiler._CanAddSemicolon = true;
                    return output;
                }
            }
            #endregion

            #region ContextTokens
            public class ContextToken : Token
            {
                // Definitely not in UT3(512), APB, CrimeCraft, GoW2, MoonBase and Singularity.
                // Greater or Equal than
                private const ushort VSizeByteMoved = 588;  

                public override void Deserialize()
                {
                    // A.?
                    DeserializeNext();

                    // SkipSize
                    Buffer.ReadUShort();
                    Decompiler.AlignSize( sizeof(ushort) );

                    // Doesn't seem to exist in APB
                    if( Buffer.Version >= VSizeByteMoved )
                    {
                        // Property
                        Buffer.ReadObjectIndex();
                        Decompiler.AlignObjectSize();
                    }

                    // PropertyType
                    Buffer.ReadByte();
                    Decompiler.AlignSize( sizeof(byte) );	

                    // Additional byte in APB?
                    if( Buffer.Version > 512 && Buffer.Version < VSizeByteMoved )
                    {
                        Buffer.ReadByte();
                        Decompiler.AlignSize( sizeof(byte) );	
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
                    Decompiler._IsWithinClassContext = true;
                    string output = base.Decompile();
                    Decompiler._IsWithinClassContext = false;
                    return output;
                }
            }

            public class InterfaceContextToken : Token
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

            public class StructMemberToken : Token
            {
                public UField MemberProperty;

                public override void Deserialize()
                {
                    // Property index
                    MemberProperty = Decompiler._Container.TryGetIndexObject( Buffer.ReadObjectIndex() ) as UField;
                    Decompiler.AlignObjectSize();

                    // TODO: Corrigate version. Definitely didn't exist in Roboblitz(369)
                    if( Buffer.Version > 369 )
                    {
                        // Struct index
                        Buffer.ReadObjectIndex();	
                        Decompiler.AlignObjectSize();

                        Buffer.Position ++;
                        Decompiler.AlignSize( sizeof(byte) );
                        // TODO: Corrigate version. Definitely didn't exist in MOHA(421)
                        if( Buffer.Version > 421 )
                        {
                            Buffer.Position ++;
                            Decompiler.AlignSize( sizeof(byte) );
                        }
                    }
                    // Pre-Context
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return DecompileNext() + "." + MemberProperty.Name;
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
                    Decompiler._CanAddSemicolon = true;	
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
                    Decompiler.AlignSize( sizeof(ushort) );

                    // If TRUE expression
                    DeserializeNext();

                    // Size. Used to skip : if Condition is True.
                    Buffer.ReadUShort();
                    Decompiler.AlignSize( sizeof(ushort) );

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
                    // Expression
                    DeserializeNext();	
                }

                public override string Decompile()
                {
                    #region CaseToken Support
                    // HACK: for case's that end with a return instead of a break.
                    if( Decompiler.IsInNest( NestManager.Nest.NestType.Default ) != null )
                    {
                        Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Default, Position + Size );
                        Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Switch, Position + Size );
                    }
                    #endregion
    
                    string returnValue = DecompileNext();
                    Decompiler._CanAddSemicolon = true;
                    return "return" + (returnValue.Length != 0 ? " " + returnValue : String.Empty);
                }
            }

            public class ReturnNothingToken : Token
            {
                private UObject _ReturnObject;

                public override void Deserialize()
                {
                    // TODO: Corrigate version.
                    if( Buffer.Version <= 300 )
                        return;

                    _ReturnObject = Decompiler._Container.TryGetIndexObject( Buffer.ReadObjectIndex() );
                    Decompiler.AlignObjectSize();
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
                    return _ReturnObject != null ? _ReturnObject.Name : String.Empty;
                }
            }

            public class GoToLabelToken : Token
            {
                public override void Deserialize()
                {
                    // Expression
                    DeserializeNext();	
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;	
                    return "goto " + DecompileNext();
                }
            }

            public class JumpToken : Token
            {
                public ushort CodeOffset{ get; private set; }

                public override void Deserialize()
                {
                    CodeOffset = Buffer.ReadUShort();
                    Decompiler.AlignSize( sizeof(ushort) );
                }

                public override void PostDeserialized()
                {
                    if( GetType() == typeof(JumpToken) )
                        Decompiler._Labels.Add
                        ( 
                            new ULabelEntry
                            {
                                Name = String.Format( "J0x{0:X2}", CodeOffset ), 
                                Position = CodeOffset
                            } 
                        );
                }

                protected void Commentize()
                {
                    Decompiler.PreComment = String.Format( "// End:0x{0:X2}", CodeOffset );   
                }

                protected void Commentize( string statement )
                {
                    Decompiler.PreComment = String.Format( "// End:0x{0:X2} [{1}]", CodeOffset, statement );   
                }

                protected void CommentStatement( string statement )
                {
                    Decompiler.PreComment = String.Format( "// [{0}]", statement );                       
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
                            NoJumpLabel();
                            Commentize();
                            Decompiler._CanAddSemicolon = true;	
                            return "break";
                        }
                        
                        //==================We're inside a Default and at the end of it!
                        if( Decompiler.IsInNest( NestManager.Nest.NestType.Default ) != null )
                        {
                            NoJumpLabel();
                            Commentize();
                            Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Default, Position );
                            Decompiler._Nester.AddNestEnd( NestManager.Nest.NestType.Switch, Position );
                            Decompiler._CanAddSemicolon = true;	
                            return "break";
                        }

                        var nest = Decompiler.IsWithinNest( NestManager.Nest.NestType.ForEach );
                        if( nest != null )
                        {
                            var dest = Decompiler.TokenAt( CodeOffset );
                            if( dest != null )
                            {
                                if( dest is IteratorPopToken )
                                {
                                    if( Decompiler.PreviousToken is IteratorNextToken )
                                    {
                                        NoJumpLabel();
                                        return String.Empty;
                                    }

                                    NoJumpLabel();
                                    Commentize();
                                    Decompiler._CanAddSemicolon = true;
                                    return "break";
                                }

                                if( dest is IteratorNextToken )
                                {
                                    NoJumpLabel();
                                    Commentize();
                                    Decompiler._CanAddSemicolon = true;
                                    return "continue";
                                }
                            }
                        }

                        nest = Decompiler.IsWithinNest( NestManager.Nest.NestType.Loop );
                        if( nest != null )
                        {
                            var dest = nest.Creator as JumpToken;
                            if( dest != null )
                            {
                                if( CodeOffset + 10 == dest.CodeOffset )
                                {
                                    CommentStatement( "Explicit Continue" );
                                    goto gotoJump;
                                }

                                if( CodeOffset == dest.CodeOffset )
                                {
                                    CommentStatement( "Explicit Break" );
                                    goto gotoJump;
                                }
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
                                    // HACK: This should be handled by UByteCodeDecompiler.Decompile()
                                    UDecompilingState.RemoveTabs( 1 );
                                    Decompiler._NestChain.RemoveAt( Decompiler._NestChain.Count - 1 );
                                    Decompiler._Nester.Nests.Remove( nestEnd );

                                    Decompiler._Nester.AddNest( NestManager.Nest.NestType.Else, Position, CodeOffset );

                                    NoJumpLabel();

                                    // HACK: This should be handled by UByteCodeDecompiler.Decompile()
                                    return "}" + "\r\n" + 
                                        (UnrealConfig.SuppressComments 
                                        ? UDecompilingState.Tabs + "else" 
                                        : UDecompilingState.Tabs + String.Format( "// End:0x{0:X2}", CodeOffset ) + "\r\n" + 
                                            UDecompilingState.Tabs + "else");
                                }
                            }
                        }
                    }

                    if( CodeOffset < Position )
                    {
                        CommentStatement( "Loop Continue" );
                    }
                    gotoJump:
                    // This is an implicit GoToToken.
                    Decompiler._CanAddSemicolon = true;
                    return String.Format( "goto J0x{0:X2}", CodeOffset );
                }

                private void NoJumpLabel()
                {
                    int i = Decompiler._TempLabels.FindIndex( p => p.Position == CodeOffset );
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
                    // Check whether there's a JumpToken pointing to the begin of this JumpIfNot, 
                    //	if detected, we assume that this If is part of a loop.
                    IsLoop = false;
                    for( int i = Decompiler.CurrentTokenIndex + 1; i < Decompiler.DeserializedTokens.Count; ++ i )
                    {
                        if( Decompiler.DeserializedTokens[i] is JumpToken && ((JumpToken)Decompiler.DeserializedTokens[i]).CodeOffset == Position )
                        {
                            IsLoop = true;
                            break;
                        }
                    }

                    Commentize();
                    if( IsLoop )
                    {
                        Decompiler.PreComment += " [Loop If]";
                    }

                    string condition = DecompileNext();
                    string output = /*(IsLoop ? "while" : "if") +*/ "if(" + condition + ")";

                    if( (CodeOffset & UInt16.MaxValue) < Position )
                    {
                        Decompiler._CanAddSemicolon = true;
                        return output + "\r\n" 
                            + UDecompilingState.Tabs + UnrealConfig.Indention 
                            + String.Format( "goto J0x{0:X2}", CodeOffset ); 
                    }

                    Decompiler._CanAddSemicolon = false;
                    Decompiler._Nester.AddNest( IsLoop 
                        ? NestManager.Nest.NestType.Loop 
                        : NestManager.Nest.NestType.If, 
                        Position, CodeOffset, this 
                    );
                    return output;
                }
            }

            public class FilterEditorOnlyToken : JumpToken
            {
                public override string Decompile()
                {
                    Decompiler._Nester.AddNest( NestManager.Nest.NestType.Scope, Position, CodeOffset );
                    Commentize();
                    return "filtereditoronly";
                }
            }

            public class SwitchToken : Token
            {
                public ushort PropertyType;

                public override void Deserialize()
                {
                    if( Buffer.Version >= 600 )
                    {
                        // Points to the object that was passed to the switch, 
                        // beware that the followed token chain contains it as well!
                        Buffer.ReadObjectIndex();
                        Decompiler.AlignObjectSize();
                    }

                    // TODO: Corrigate version
                    if( Buffer.Version >= 536 && Buffer.Version <= 587 )
                    {
                        PropertyType = Buffer.ReadUShort();
                        Decompiler.AlignSize( sizeof(ushort) );
                    }
                    else
                    {
                        PropertyType = Buffer.ReadByte();
                        Decompiler.AlignSize( sizeof(byte) );
                    }

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

                    string expr = DecompileNext();
                    Decompiler._CanAddSemicolon = false;	// In case the decompiled token was a function call
                    return "switch(" + expr + ")";
                }
            }

            public class CaseToken : JumpToken
            {
                public override void Deserialize()
                {
                    base.Deserialize();
                    if( CodeOffset != UInt16.MaxValue )
                    {
                        DeserializeNext();	// Condition
                    }	// Else "Default:"
                }

                // HACK: To avoid from decrementing tabs more than once, 
                //	e.g. in a situation of case a1: case a2: case a3: that use the same block code.
                private static byte _CaseStack;
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
                        Decompiler._PreDecrementTabs = 1;

                        ++ _CaseStack;
                    }
                    else _CaseStack = 0;

                    Commentize();
                    if( CodeOffset != UInt16.MaxValue )
                    {
                        Decompiler._Nester.AddNest( NestManager.Nest.NestType.Case, Position, CodeOffset );
                        string output = "case " + DecompileNext() + ":";
                        Decompiler._CanAddSemicolon = false;
                        return output;
                    }
                    
                    Decompiler._Nester.AddNestBegin( NestManager.Nest.NestType.Default, Position, this );
                    Decompiler._CanAddSemicolon = false;
                    return "default:";	
                }
            }

            public class IteratorToken : JumpToken
            {
                public override void Deserialize()
                {
                    DeserializeNext();	// Expression
                    base.Deserialize();
                }

                public override string Decompile()
                {				  
                    Decompiler._Nester.AddNest( NestManager.Nest.NestType.ForEach, Position, CodeOffset, this );
                    Commentize();

                    // foreach FunctionCall
                    string expression = DecompileNext();
                    Decompiler._CanAddSemicolon = false;	// Undo
                    return "foreach " + expression;
                }
            }

            public class ArrayIteratorToken : JumpToken
            {
                protected bool HasSecondParm;

                public override void Deserialize()
                {
                    // Expression
                    DeserializeNext();

                    // Param 1
                    DeserializeNext();

                    HasSecondParm = Buffer.ReadByte() > 0;
                    Decompiler.AlignSize( sizeof(byte) );
                    DeserializeNext();

                    base.Deserialize();
                }

                public override string Decompile()
                {				  
                    Decompiler._Nester.AddNest( NestManager.Nest.NestType.ForEach, Position, CodeOffset, this );
    
                    Commentize();

                    // foreach ArrayVariable( Parameters )
                    string output = "foreach " + DecompileNext() + "(" + DecompileNext();
                    output += (HasSecondParm ? ", " : String.Empty) + DecompileNext();
                    Decompiler._CanAddSemicolon = false;
                    return output + ")";
                }
            }

            public class IteratorNextToken : Token
            {
                public override string Decompile()
                {
                    if( Decompiler.PeekToken is IteratorPopToken )
                    {
                        return String.Empty;
                    }
                    Decompiler._CanAddSemicolon = true;
                    return "continue";
                }
            }

            public class IteratorPopToken : Token
            {
                public override string Decompile()
                {
                    if( Decompiler.PreviousToken is IteratorNextToken 
                        || Decompiler.PeekToken is ReturnToken )
                    {
                        return String.Empty;
                    }
                    Decompiler._CanAddSemicolon = true;
                    return "break";
                }
            }
            #endregion

            #region FieldTokens
            public abstract class FieldToken : Token
            {							
                public UObject Object{ get; private set; }
                public static UObject LastField{ get; internal set; }

                public override void Deserialize()
                {
                    Object = Decompiler._Container.TryGetIndexObject( Buffer.ReadObjectIndex() );
                    Decompiler.AlignObjectSize();	
                }

                public override string Decompile()
                {
                    LastField = Object;
                    return Object != null ? Object.Name : "@NULL";
                }
            }

            public class NativeParameterToken : FieldToken
            {
                public override string Decompile()
                {
#if DEBUG
                    Decompiler._CanAddSemicolon = true;
                    Decompiler._MustCommentStatement = true;
                    return "native." + base.Decompile();
#else
                    return String.Empty;	
#endif
                }
            }

            public class InstanceVariableToken : FieldToken{}
            public class LocalVariableToken : FieldToken{}
            public class OutVariableToken : FieldToken{}

            public class DefaultVariableToken : FieldToken
            {
                public override string Decompile()
                {
                    return "default." + base.Decompile();
                }
            }

            public class DynamicVariableToken : Token
            {
                protected int LocalIndex;

                public override void Deserialize()
                {
                    LocalIndex = Buffer.ReadInt32();	
                    Decompiler.AlignSize( sizeof(int) );
                }

                public override string Decompile()
                {
                    return "UnknownLocal_" + LocalIndex;
                }
            }

            public class UndefinedVariableToken : Token
            {
                public override string Decompile()
                {
                    return String.Empty;
                }
            }

            public class DelegatePropertyToken : FieldToken
            {
                public int NameIndex;

                public override void Deserialize()
                {		  
                    // FIXME: MOHA or general?
                    if( Buffer.Version == 421 )
                    {
                        Decompiler.AlignSize( sizeof(int) );
                    }

                    NameIndex = Buffer.ReadNameIndex();
                    Decompiler.AlignNameSize();
                    // TODO: Corrigate version. Definitely not in Mirrors Edge(536)
                    if( Buffer.Version > 536 )
                    {
                        base.Deserialize();
                    }
                }

                public override string Decompile()
                {
                    return Decompiler._Container.Package.GetIndexName( NameIndex );
                }
            }

            public class DefaultParameterToken : Token
            {
                internal static int     _NextParamIndex;
                private UField          _NextParam
                {
                    get{ return ((UFunction)Decompiler._Container).Params[_NextParamIndex++]; }
                }

                public override void Deserialize()
                {
                    Buffer.ReadUShort();	// Size
                    Decompiler.AlignSize( sizeof(ushort) );

                    // FIXME: MOHA or general?
                    if( Buffer.Version == 421 )
                    {
                        Decompiler.AlignSize( sizeof(ushort) );
                    }

                    DeserializeNext();	// Expression
                    DeserializeNext();	// EndParmValue
                }

                public override string Decompile()
                {
                    string expression = DecompileNext();		
                    DecompileNext();	// EndParmValue
                    Decompiler._CanAddSemicolon = true;
                    return String.Format( "{0} = {1}", _NextParam.Name, expression );
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
            #endregion

            #region OtherTokens
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

            public class NothingToken : Token
            {
                public override void Deserialize()
                {
                    if( Buffer.Version == 421 )
                        Decompiler.AlignSize( sizeof(int ) );
                }

                public override string Decompile()
                {
                    // Empty default option parameter!
                    ++ DefaultParameterToken._NextParamIndex;
                    return String.Empty;
                }
            }
            public class NoDelegateToken : NoneToken{}
            public class NoObjectToken : NoneToken{}

            // A skipped parameter when calling a function
            public class NoParmToken : Token{}
            public class EndOfScriptToken : Token{}

            public class StopToken : Token
            {
                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;	
                    return "stop";
                }
            }
    
            public class AssertToken : Token
            {
                public bool DebugMode;

                public override void Deserialize()
                {
                    Buffer.ReadUShort();	// Line
                    Decompiler.AlignSize( sizeof(short) );

                    // TODO: Corrigate version, at least known since Mirrors Edge(536)
                    if( Buffer.Version >= 536 )
                    {
                        DebugMode = Buffer.ReadByte() > 0;
                        Decompiler.AlignSize( sizeof(byte) );
                    }
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    if( Buffer.Version >= 536 )
                    {
                        Decompiler.PreComment = "// DebugMode:" + DebugMode;
                    }
                    string expr = DecompileNext();
                    Decompiler._CanAddSemicolon = true;	
                    return "assert(" + expr + ")";
                }
            }

            private List<ULabelEntry> _Labels, _TempLabels;
            public class LabelTableToken : Token
            {
                public override void Deserialize()
                {
                    var label = String.Empty;
                    int labelPos = -1;
                    do
                    {
                        if( label != String.Empty )
                        {
                            Decompiler._Labels.Add
                            ( 
                                new ULabelEntry
                                {
                                    Name = label, 
                                    Position = labelPos
                                } 
                            );
                        }
                        label = Buffer.ReadName();
                        Decompiler.AlignNameSize();
                        labelPos = Buffer.ReadInt32();
                        Decompiler.AlignSize( sizeof(int) );
                    } while( String.Compare( label, "None", StringComparison.OrdinalIgnoreCase ) != 0 );
                }
            }

            public class SkipToken : Token
            {
                public override void Deserialize()
                {
                    Buffer.ReadUShort();	// Size
                    Decompiler.AlignSize( sizeof(ushort) );	

                    DeserializeNext();
                }

                public override string Decompile()
                {
                     return DecompileNext();
                }
            }

            public abstract class ComparisonToken : Token
            {
                public override void Deserialize()
                {
                    Buffer.ReadObjectIndex();
                    Decompiler.AlignObjectSize();

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

            public class DelegateCmpNEToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    string output = DecompileNext() + " != " + DecompileNext();
                    DecompileNext();
                    return output;
                }
            }

            public class DelegateFunctionCmpNEToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    string output = DecompileNext() + " != " + DecompileNext();
                    DecompileNext();
                    return output;
                }
            }

            public class InstanceDelegateToken : Token
            {
                public override void Deserialize()
                {
                    Buffer.ReadNameIndex();
                    Decompiler.AlignNameSize();
                }
            }

            public class EatStringToken : Token
            {
                public override void Deserialize()
                {
                    // TODO: Corrigate Version(Lowest known version 369(Roboblitz))
                    if( Buffer.Version > 300 )
                    {
                        Buffer.ReadUShort();	// Size
                        Decompiler.AlignSize( sizeof(ushort) );

                        // TODO: UNKNOWN:
                        Buffer.ReadUShort();
                        Decompiler.AlignSize( sizeof(ushort) );
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
                    Decompiler.AlignSize( sizeof(byte) );
                }
            }

            // TODO: Implement
            public class BeginFunctionToken : Token
            {
                public override void Deserialize()
                {
                    var topFunc = Decompiler._Container as UFunction;
                    Debug.Assert( topFunc != null, "topf != null" );
                    foreach( UProperty property in topFunc.Variables )
                    {
                        if( !property.HasPropertyFlag( Flags.PropertyFlagsLO.Parm | Flags.PropertyFlagsLO.ReturnParm ) )
                            continue;

                        Buffer.ReadByte(); // Size
                        Decompiler.AlignSize( sizeof(byte) );

                        Buffer.ReadByte(); // bOutParam
                        Decompiler.AlignSize( sizeof(byte) );
                    }
                    Buffer.ReadByte();	// End
                    Decompiler.AlignSize( sizeof(byte) );
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

                    Decompiler._CanAddSemicolon = true;
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
                    Decompiler.AlignSize( 13 );
                }
            }
            #endregion

            #region Constants
            public class IntConstToken : Token
            {
                public int Value{ get; private set; }

                public override void Deserialize()
                {
                    Value = Buffer.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                }

                public override string Decompile()
                {
                    return String.Format( "{0}", Value ); 
                }
            }

            public class ByteConstToken : Token
            {
                public byte Value{ get; private set; }

                private enum ENetRole
                {
                    ROLE_None = 0,
                    ROLE_DumbProxy = 1,
                    ROLE_SimulatedProxy = 2,
                    ROLE_AutonomousProxy = 3,
                    ROLE_Authority = 4
                }

                private enum ENetRole3
                {
                    ROLE_None = 0,
                    ROLE_SimulatedProxy = 1,
                    ROLE_AutonomousProxy = 2,
                    ROLE_Authority = 3,
                    ROLE_MAX = 4
                }

                private enum ENetMode
                {
                    NM_Standalone = 0,
                    NM_DedicatedServer = 1,
                    NM_ListenServer = 2,
                    NM_Client = 3,
                    NM_MAX = 4
                }

                public override void Deserialize()
                {
                    Value = Buffer.ReadByte();
                    Decompiler.AlignSize( sizeof(byte) );
                }

                public override string Decompile()
                {
                    if( FieldToken.LastField != null )
                    {
                        switch( FieldToken.LastField.Outer.Name + FieldToken.LastField.Name )
                        {
                            case "ActorRemoteRole":
                            case "ActorRole":
                                return Enum.GetName( Buffer.Version >= 220 ? typeof(ENetRole3) : typeof(ENetRole), Value );

                            case "LevelInfoNetMode":
                            case "WorldInfoNetMode":
                                return Enum.GetName( typeof(ENetMode), Value );
                        }
                    }
                    return Value.ToString( CultureInfo.InvariantCulture ); 
                }
            }

            public class IntConstByteToken : Token
            {
                public byte Value{ get; private set; }

                public override void Deserialize()
                {
                    Value = Buffer.ReadByte();
                    Decompiler.AlignSize( sizeof(byte) );
                }

                public override string Decompile()
                {
                    return String.Format( "{0:d}", Value ); 
                }
            }

            public class FloatConstToken : Token
            {
                public float Value{ get; private set; }

                public override void Deserialize()
                {
                    Value = Buffer.UR.ReadSingle();
                    Decompiler.AlignSize( sizeof(float) );
                }

                public override string Decompile()
                {
                    return Value.ToUFloat(); 
                }
            }

            public class ObjectConstToken : Token
            {
                public int ObjectIndex{ get; private set; }

                public override void Deserialize()
                {
                    ObjectIndex = Buffer.ReadObjectIndex();
                    Decompiler.AlignObjectSize();
                }

                public override string Decompile()
                {
                    UObject obj = Decompiler._Container.GetIndexObject( ObjectIndex );
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
                public int NameIndex{ get; private set; }

                public override void Deserialize()
                {
                    NameIndex = Buffer.ReadNameIndex();
                    Decompiler.AlignNameSize();
                }

                public override string Decompile()
                {
                    return "\'" + Decompiler._Container.Package.GetIndexName( NameIndex ) + "\'";
                }
            }

            public class StringConstToken : Token
            {
                public string Value{ get; private set; }

                public override void Deserialize()
                {
                    Value = Buffer.UR.ReadAnsi();
                    Decompiler.AlignSize( Value.Length + 1 );	// inc null char
                }

                public override string Decompile()
                {
                    return "\"" + Value.Escape() + "\"";
                }
            }

            public class UniStringConstToken : Token
            {
                public string Value{ get; private set; }

                public override void Deserialize()
                {
                    Value = Buffer.UR.ReadUnicode();
                    Decompiler.AlignSize( (Value.Length * 2) + 2 );	// inc null char
                }

                public override string Decompile()
                {
                    return "\"" + Value.Escape() + "\"";
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
                    Decompiler.AlignSize( sizeof(int) );
                    Value.Yaw = Buffer.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                    Value.Roll = Buffer.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                }

                public override string Decompile()
                {
                    return "rot(" + Value.Pitch + ", " + Value.Yaw + ", " + Value.Roll + ")";
                }
            }

            public class VectorConstToken : Token
            {
                public float X, Y, Z;

                public override void Deserialize()
                {
                    X = Buffer.UR.ReadSingle();
                    Decompiler.AlignSize( sizeof(float) );
                    Y = Buffer.UR.ReadSingle();
                    Decompiler.AlignSize( sizeof(float) );
                    Z = Buffer.UR.ReadSingle();
                    Decompiler.AlignSize( sizeof(float) );
                }

                public override string Decompile()
                {
                    return String.Format( "vect({0}, {1}, {2})", X.ToUFloat(), Y.ToUFloat(), Z.ToUFloat() );
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

            public abstract class ObjectCastToken : CastToken
            {
                public UObject CastedObject;

                public override void Deserialize()
                {
                    CastedObject = Buffer.ReadObject();
                    Decompiler.AlignObjectSize();

                    base.Deserialize();
                }   
            }

            public class DynamicCastToken : ObjectCastToken
            {
                public override string Decompile()
                {
                    return CastedObject.Name + base.Decompile();
                }
            }

            public class MetaCastToken : ObjectCastToken
            {
                public override string Decompile()
                {
                    return "class<" + CastedObject.Name + ">" + base.Decompile();
                }
            }

            public class InterfaceCastToken : ObjectCastToken{}

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
                    return "bool" + base.Decompile(); 
                }
            }

            public class InterfaceToObjectToken : CastToken{}
            public class ObjectToInterfaceToken : CastToken{}

            public class DelegateToStringToken : CastToken
            {
                public override string Decompile()
                {
                    return "string" + base.Decompile(); 
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
                    Decompiler._CanAddSemicolon = true;	
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
                    Decompiler._CanAddSemicolon = true;	
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
                        Decompiler.AlignSize( sizeof(ushort) );
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
                        Decompiler.AlignSize( sizeof(ushort) );
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
                    Decompiler._CanAddSemicolon = true;	
                    return DecompileNext() + "." + functionName + 
                        "(" + DecompileNext() + (Buffer.Version > ArrayMethodEndParmsVersion && !skipEndParms ? DecompileNext() : ")");
                }

                protected string DecompileMethodTwo( string functionName, bool skipEndParms = false )
                {
                    Decompiler._CanAddSemicolon = true;	
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
                    DeserializeMethodTwo();
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
                    DeserializeMethodTwo();
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
                    return String.Format( "@UnknownExprToken(0x{0:X2})", RepresentToken ); 
                }
            }

            public sealed class UnknownCastToken : Token
            {
                public override string Decompile()
                {
                    return String.Format( "@UnknownCastToken(0x{0:X2})", RepresentToken ); 
                }
            }
            #endregion
#endif
        }	
    }
}
