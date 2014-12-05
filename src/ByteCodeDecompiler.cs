//#define SUPPRESS_BOOLINTEXPLOIT

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UELib.Tokens;

namespace UELib.Core
{
    using System.Text;

    public partial class UStruct
    {
        /// <summary>
        /// Decompiles the bytecodes from the 'Owner'
        /// </summary>
        public partial class UByteCodeDecompiler : IUnrealDecompilable
        {
            /// <summary>
            /// The Struct that contains the bytecode that we have to deserialize and decompile!
            /// </summary>
            private readonly UStruct _Container;

            /// <summary>
            /// Pointer to the ObjectStream buffer of 'Owner'
            /// </summary>
            private UObjectStream Buffer{ get{ return _Container.Buffer; } }

            private UnrealPackage Package{ get{ return _Container.Package; } }

            /// <summary>
            /// A collection of deserialized tokens, in their correspondence stream order.
            /// </summary>
            public List<Token> DeserializedTokens{ get; private set; }

            [System.ComponentModel.DefaultValue(-1)]
            public int CurrentTokenIndex{ get; set; }

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
                if( Buffer.Version >= vObjectSizeTo8
#if TERA
                    && Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                    )
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
                try
                {
                    _Container.EnsureBuffer();
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
                finally
                {
                    _Container.MaybeDisposeBuffer();
                }
            }

            private void DeserializeDebugToken()
            {
                Buffer.StartPeek();
                byte token = FixToken( Buffer.ReadByte() );
                Buffer.EndPeek();

                if( token == (byte)ExprToken.DebugInfo )
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
                if( tokenCode >= (byte)ExprToken.FirstNative )
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
                        if( Buffer.Version < PrimitveCastVersion )      // UE1
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
                        if( Buffer.Version < PrimitveCastVersion )      // UE1
                        {
                            tokenItem = new RotatorToVectorToken();
                        }
                        else                                            // UE2+
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
                            tokenItem = new ByteToIntToken();
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

                    case (byte)ExprToken.StateVariable:
                        tokenItem = new StateVariableToken();
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
                        if( Buffer.Version < PrimitveCastVersion )   // StringToInt
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

                    case (byte)ExprToken.EndOfScript:   //CastToken.BoolToString:
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
                    //case (byte)ExprToken.VarObject:   // See UndefinedVariable
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
                tokenItem.StoragePosition = (uint)Buffer.Position - (uint)_Container.ScriptOffset - 1;
                // IMPORTANT:Add before deserialize, due the possibility that the tokenitem might deserialize other tokens as well.
                DeserializedTokens.Add( tokenItem );
                tokenItem.Deserialize( Buffer );
                // Includes all sizes of followed tokens as well! e.g. i = i + 1; is summed here but not i = i +1; (not>>)i ++;
                tokenItem.Size = (ushort)(CodePosition - tokenPosition);
                tokenItem.StorageSize = (ushort)((uint)Buffer.Position - (uint)_Container.ScriptOffset - tokenItem.StoragePosition);
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
                //  xyz = UnknownCastToken(0x1b);
                //  UnknownCastToken(0x1b)
                //  UnknownCastToken(0x1b)
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
                        Scope               = 0,
                        If                  = 1,
                        Else                = 2,
                        ForEach             = 4,
                        Switch              = 5, Case               = 6, Default            = 7,
                        Loop                = 8
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
                DefaultParameterToken._NextParamIndex = 0;
                if( Package.Version > 300 )
                {
                    var func = _Container as UFunction;
                    if( func != null && func.Params != null )
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

                                if( _MustCommentStatement && UnrealConfig.SuppressComments )
                                    continue;

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
#endif
        }
    }
}