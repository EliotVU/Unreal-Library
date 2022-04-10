//#define SUPPRESS_BOOLINTEXPLOIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UELib.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    using System.Linq;
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
            private UObjectStream Buffer => _Container.Buffer;

            private UnrealPackage Package => _Container.Package;

            /// <summary>
            /// A collection of deserialized tokens, in their correspondence stream order.
            /// </summary>
            public List<Token> DeserializedTokens { get; private set; }

            [System.ComponentModel.DefaultValue(-1)]
            public int CurrentTokenIndex { get; set; }

            public Token NextToken => DeserializedTokens[++CurrentTokenIndex];

            public Token PeekToken => DeserializedTokens[CurrentTokenIndex + 1];

            public Token PreviousToken => DeserializedTokens[CurrentTokenIndex - 1];

            public Token CurrentToken => DeserializedTokens[CurrentTokenIndex];

            public UByteCodeDecompiler(UStruct container)
            {
                _Container = container;
                AlignMemorySizes();
            }

            #region Deserialize

            /// <summary>
            /// The current simulated-memory-aligned position in @Buffer.
            /// </summary>
            private int CodePosition { get; set; }

            private const byte IndexMemorySize = 4;
            private byte _NameMemorySize = IndexMemorySize;
            private byte _ObjectMemorySize = IndexMemorySize;

            private void AlignMemorySizes()
            {
                const short vNameSizeTo8 = 500;
                if (Buffer.Version >= vNameSizeTo8) _NameMemorySize = 8;

                const short vObjectSizeTo8 = 587;
                if (Buffer.Version >= vObjectSizeTo8
#if TERA
                    && Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                   )
                    _ObjectMemorySize = 8;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AlignSize(int size)
            {
                CodePosition += size;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AlignNameSize()
            {
                CodePosition += _NameMemorySize;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AlignObjectSize()
            {
                CodePosition += _ObjectMemorySize;
            }

            [CanBeNull] private Dictionary<byte, byte> _ByteCodeMap;
#if APB
            private static readonly Dictionary<byte, byte> ByteCodeMap_BuildApb = new Dictionary<byte, byte>
            {
                { (byte)ExprToken.Return, (byte)ExprToken.LocalVariable },
                { (byte)ExprToken.LocalVariable, (byte)ExprToken.Return },
                { (byte)ExprToken.Jump, (byte)ExprToken.JumpIfNot },
                { (byte)ExprToken.JumpIfNot, (byte)ExprToken.Jump },
                { (byte)ExprToken.Case, (byte)ExprToken.Nothing },
                { (byte)ExprToken.Nothing, (byte)ExprToken.Case }
            };
#endif
            private void SetupByteCodeMap()
            {

#if APB
                if (Package.Build == UnrealPackage.GameBuild.BuildName.APB &&
                    Package.LicenseeVersion >= 32)
                    _ByteCodeMap = ByteCodeMap_BuildApb;
#endif
            }

            /// <summary>
            /// Fix the values of UE1/UE2 tokens to match the UE3 token values.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private byte FixToken(byte tokenCode)
            {
                if (_ByteCodeMap != null)
                {
                    _ByteCodeMap.TryGetValue(tokenCode, out tokenCode);
                    return tokenCode;
                }

                // Adjust UE2 tokens to UE3
                // TODO: Use ByteCodeMap
                if (Package.Version >= 184
                    &&
                    (
                        tokenCode >= (byte)ExprToken.Unknown && tokenCode < (byte)ExprToken.ReturnNothing
                        ||
                        tokenCode > (byte)ExprToken.NoDelegate && tokenCode < (byte)ExprToken.ExtendedNative)
                   )
                    return ++tokenCode;
                return tokenCode;
            }

            private bool _WasDeserialized;

            public void Deserialize()
            {
                if (_WasDeserialized)
                    return;

                _WasDeserialized = true;
                try
                {
                    _Container.EnsureBuffer();
                    Buffer.Seek(_Container.ScriptOffset, SeekOrigin.Begin);
                    CodePosition = 0;
                    int codeSize = _Container.ByteScriptSize;

                    CurrentTokenIndex = -1;
                    DeserializedTokens = new List<Token>();
                    _Labels = new List<ULabelEntry>();

                    SetupByteCodeMap();

                    while (CodePosition < codeSize)
                        try
                        {
                            var token = DeserializeNext();
                            if (!(token is EndOfScriptToken))
                                continue;

                            if (CodePosition < codeSize)
                                Console.WriteLine("End of script detected, but the loop condition is still true.");

                            break;
                        }
                        catch (EndOfStreamException error)
                        {
                            Console.WriteLine("Couldn't backup from this error! Decompiling aborted!");
                            break;
                        }
                        catch (SystemException e)
                        {
                            Console.WriteLine("Object:" + _Container.Name);
                            Console.WriteLine("Failed to deserialize token at position:" + CodePosition);
                            Console.WriteLine("Exception:" + e.Message);
                            Console.WriteLine("Stack:" + e.StackTrace);
                        }
                }
                finally
                {
                    _Container.MaybeDisposeBuffer();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void DeserializeDebugToken()
            {
                Buffer.StartPeek();
                byte tokenCode = FixToken(Buffer.ReadByte());
                Buffer.EndPeek();

                if (tokenCode == (byte)ExprToken.DebugInfo) DeserializeNext();
            }

            private NativeFunctionToken CreateNativeToken(ushort nativeIndex)
            {
                var nativeTableItem = _Container.Package.NTLPackage?.FindTableItem(nativeIndex);
                return new NativeFunctionToken
                {
                    NativeItem = nativeTableItem
                };
            }

            private Token DeserializeNext(byte tokenCode = byte.MaxValue)
            {
                int tokenPosition = CodePosition;
                if (tokenCode == byte.MaxValue)
                {
                    tokenCode = Buffer.ReadByte();
                    AlignSize(sizeof(byte));
                }

                Token token = null;
                if (tokenCode >= (byte)ExprToken.ExtendedNative)
                {
                    if (tokenCode >= (byte)ExprToken.FirstNative)
                    {
                        token = CreateNativeToken(tokenCode);
                    }
                    else
                    {
                        byte extendedByte = Buffer.ReadByte();
                        AlignSize(sizeof(byte));

                        var nativeToken = (ushort)(((tokenCode - (byte)ExprToken.ExtendedNative) << 8) | extendedByte);
                        token = CreateNativeToken(nativeToken);
                    }
                }
                else
                {
                    tokenCode = FixToken(tokenCode);
                    switch (tokenCode)
                    {
                        #region Cast

                        case (byte)ExprToken.DynamicCast:
                            token = new DynamicCastToken();
                            break;

                        case (byte)ExprToken.MetaCast:
                            token = new MetaCastToken();
                            break;

                        case (byte)ExprToken.InterfaceCast:
                            if (Buffer.Version < PrimitveCastVersion) // UE1
                                token = new IntToStringToken();
                            else
                                token = new InterfaceCastToken();

                            break;

                        // Redefined, can be RotatorToVector!(UE1)
                        case (byte)ExprToken.PrimitiveCast:
                            if (Buffer.Version < PrimitveCastVersion) // UE1
                            {
                                token = new RotatorToVectorToken();
                            }
                            else // UE2+
                            {
                                // Next byte represents the CastToken!
                                tokenCode = Buffer.ReadByte();
                                AlignSize(sizeof(byte));
                                token = DeserializeCastToken(tokenCode);
                            }

                            break;

                        #endregion

                        #region Context

                        case (byte)ExprToken.ClassContext:
                            token = new ClassContextToken();
                            break;

                        case (byte)ExprToken.InterfaceContext:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new ByteToStringToken();
                            else
                                token = new InterfaceContextToken();

                            break;

                        case (byte)ExprToken.Context:
                            token = new ContextToken();
                            break;

                        case (byte)ExprToken.StructMember:
                            token = new StructMemberToken();
                            break;

                        #endregion

                        #region Assigns

                        case (byte)ExprToken.Let:
                            token = new LetToken();
                            break;

                        case (byte)ExprToken.LetBool:
                            token = new LetBoolToken();
                            break;

                        case (byte)ExprToken.EndParmValue:
                            token = new EndParmValueToken();
                            break;

                        // Redefined, can be FloatToBool!(UE1)
                        case (byte)ExprToken.LetDelegate:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new FloatToBoolToken();
                            else
                                token = new LetDelegateToken();

                            break;

                        // Redefined, can be NameToBool!(UE1)
                        case (byte)ExprToken.Conditional:
                            token = new ConditionalToken();
                            break;

                        case (byte)ExprToken.Eval
                            : // case (byte)ExprToken.DynArrayFindStruct: case (byte)ExprToken.Conditional:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new NameToBoolToken();
                            else if (Buffer.Version >= 300)
                                token = new DynamicArrayFindStructToken();
                            else
                                token = new ConditionalToken();

                            break;

                        #endregion

                        #region Jumps

                        case (byte)ExprToken.Return:
                            token = new ReturnToken();
                            break;

                        case (byte)ExprToken.ReturnNothing:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new ByteToIntToken();
                            // Definitely existed since GoW(490)
                            else if (Buffer.Version > 420 && DeserializedTokens.Count > 0 &&
                                     !(DeserializedTokens[DeserializedTokens.Count - 1] is
                                         ReturnToken)) // Should only be done if the last token wasn't Return
                                token = new DynamicArrayInsertToken();
                            else
                                token = new ReturnNothingToken();

                            break;

                        case (byte)ExprToken.GotoLabel:
                            token = new GoToLabelToken();
                            break;

                        case (byte)ExprToken.Jump:
                            token = new JumpToken();
                            break;

                        case (byte)ExprToken.JumpIfNot:
                            token = new JumpIfNotToken();
                            break;

                        case (byte)ExprToken.Switch:
                            token = new SwitchToken();
                            break;

                        case (byte)ExprToken.Case:
                            token = new CaseToken();
                            break;

                        case (byte)ExprToken.DynArrayIterator:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new RotatorToStringToken();
                            else
                                token = new ArrayIteratorToken();

                            break;

                        case (byte)ExprToken.Iterator:
                            token = new IteratorToken();
                            break;

                        case (byte)ExprToken.IteratorNext:
                            token = new IteratorNextToken();
                            break;

                        case (byte)ExprToken.IteratorPop:
                            token = new IteratorPopToken();
                            break;

                        case (byte)ExprToken.FilterEditorOnly:
                            token = new FilterEditorOnlyToken();
                            break;

                        #endregion

                        #region Variables

                        case (byte)ExprToken.NativeParm:
                            token = new NativeParameterToken();
                            break;

                        // Referenced variables that are from this function e.g. Local and params
                        case (byte)ExprToken.InstanceVariable:
                            token = new InstanceVariableToken();
                            break;

                        case (byte)ExprToken.LocalVariable:
                            token = new LocalVariableToken();
                            break;

                        case (byte)ExprToken.StateVariable:
                            token = new StateVariableToken();
                            break;

                        // Referenced variables that are default
                        case (byte)ExprToken.UndefinedVariable:
#if BORDERLANDS2
                            if (_Container.Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2)
                            {
                                token = new DynamicVariableToken();
                                break;
                            }
#endif
                            token = new UndefinedVariableToken();
                            break;

                        case (byte)ExprToken.DefaultVariable:
                            token = new DefaultVariableToken();
                            break;

                        // UE3+
                        case (byte)ExprToken.OutVariable:
                            token = new OutVariableToken();
                            break;

                        case (byte)ExprToken.BoolVariable:
                            token = new BoolVariableToken();
                            break;

                        // Redefined, can be FloatToInt!(UE1)
                        case (byte)ExprToken.DelegateProperty:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new FloatToIntToken();
                            else
                                token = new DelegatePropertyToken();

                            break;

                        case (byte)ExprToken.DefaultParmValue:
                            if (Buffer.Version < PrimitveCastVersion) // StringToInt
                                token = new StringToIntToken();
                            else
                                token = new DefaultParameterToken();

                            break;

                        #endregion

                        #region Misc

                        // Redefined, can be BoolToFloat!(UE1)
                        case (byte)ExprToken.DebugInfo:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new BoolToFloatToken();
                            else
                                token = new DebugInfoToken();

                            break;

                        case (byte)ExprToken.Nothing:
                            token = new NothingToken();
                            break;

                        case (byte)ExprToken.EndFunctionParms:
                            token = new EndFunctionParmsToken();
                            break;

                        case (byte)ExprToken.IntZero:
                            token = new IntZeroToken();
                            break;

                        case (byte)ExprToken.IntOne:
                            token = new IntOneToken();
                            break;

                        case (byte)ExprToken.True:
                            token = new TrueToken();
                            break;

                        case (byte)ExprToken.False:
                            token = new FalseToken();
                            break;

                        case (byte)ExprToken.NoDelegate:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new IntToFloatToken();
                            else
                                token = new NoDelegateToken();

                            break;

                        // No value passed to an optional parameter.
                        case (byte)ExprToken.NoParm:
                            token = new NoParmToken();
                            break;

                        case (byte)ExprToken.NoObject:
                            token = new NoObjectToken();
                            break;

                        case (byte)ExprToken.Self:
                            token = new SelfToken();
                            break;

                        // End of state code.
                        case (byte)ExprToken.Stop:
                            token = new StopToken();
                            break;

                        case (byte)ExprToken.Assert:
                            token = new AssertToken();
                            break;

                        case (byte)ExprToken.LabelTable:
                            token = new LabelTableToken();
                            break;

                        case (byte)ExprToken.EndOfScript: //CastToken.BoolToString:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new BoolToStringToken();
                            else
                                token = new EndOfScriptToken();

                            break;

                        case (byte)ExprToken.Skip:
                            token = new SkipToken();
                            break;

                        case (byte)ExprToken.StructCmpEq:
                            token = new StructCmpEqToken();
                            break;

                        case (byte)ExprToken.StructCmpNE:
                            token = new StructCmpNeToken();
                            break;

                        case (byte)ExprToken.DelegateCmpEq:
                            token = new DelegateCmpEqToken();
                            break;

                        case (byte)ExprToken.DelegateFunctionCmpEq:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new IntToBoolToken();
                            else
                                token = new DelegateFunctionCmpEqToken();

                            break;

                        case (byte)ExprToken.DelegateCmpNE:
                            token = new DelegateCmpNEToken();
                            break;

                        case (byte)ExprToken.DelegateFunctionCmpNE:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new IntToBoolToken();
                            else
                                token = new DelegateFunctionCmpNEToken();

                            break;

                        case (byte)ExprToken.InstanceDelegate:
                            token = new InstanceDelegateToken();
                            break;

                        case (byte)ExprToken.EatReturnValue:
                            token = new EatReturnValueToken();
                            break;

                        case (byte)ExprToken.New:
                            token = new NewToken();
                            break;

                        case (byte)ExprToken.FunctionEnd: // case (byte)ExprToken.DynArrayFind:
                            if (Buffer.Version < 300)
                                token = new EndOfScriptToken();
                            else
                                token = new DynamicArrayFindToken();

                            break;

                        case (byte)ExprToken.VarInt:
                        case (byte)ExprToken.VarFloat:
                        case (byte)ExprToken.VarByte:
                        case (byte)ExprToken.VarBool:
                            //case (byte)ExprToken.VarObject:   // See UndefinedVariable
                            token = new DynamicVariableToken();
                            break;

                        #endregion

                        #region Constants

                        case (byte)ExprToken.IntConst:
                            token = new IntConstToken();
                            break;

                        case (byte)ExprToken.ByteConst:
                            token = new ByteConstToken();
                            break;

                        case (byte)ExprToken.IntConstByte:
                            token = new IntConstByteToken();
                            break;

                        case (byte)ExprToken.FloatConst:
                            token = new FloatConstToken();
                            break;

                        // ClassConst?
                        case (byte)ExprToken.ObjectConst:
                            token = new ObjectConstToken();
                            break;

                        case (byte)ExprToken.NameConst:
                            token = new NameConstToken();
                            break;

                        case (byte)ExprToken.StringConst:
                            token = new StringConstToken();
                            break;

                        case (byte)ExprToken.UniStringConst:
                            token = new UniStringConstToken();
                            break;

                        case (byte)ExprToken.RotatorConst:
                            token = new RotatorConstToken();
                            break;

                        case (byte)ExprToken.VectorConst:
                            token = new VectorConstToken();
                            break;

                        #endregion

                        #region Functions

                        case (byte)ExprToken.FinalFunction:
                            token = new FinalFunctionToken();
                            break;

                        case (byte)ExprToken.VirtualFunction:
                            token = new VirtualFunctionToken();
                            break;

                        case (byte)ExprToken.GlobalFunction:
                            token = new GlobalFunctionToken();
                            break;

                        // Redefined, can be FloatToByte!(UE1)
                        case (byte)ExprToken.DelegateFunction:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new FloatToByteToken();
                            else
                                token = new DelegateFunctionToken();

                            break;

                        #endregion

                        #region Arrays

                        case (byte)ExprToken.ArrayElement:
                            token = new ArrayElementToken();
                            break;

                        case (byte)ExprToken.DynArrayElement:
                            token = new DynamicArrayElementToken();
                            break;

                        case (byte)ExprToken.DynArrayLength:
                            token = new DynamicArrayLengthToken();
                            break;

                        case (byte)ExprToken.DynArrayInsert:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new BoolToByteToken();
                            else
                                token = new DynamicArrayInsertToken();

                            break;

                        case (byte)ExprToken.DynArrayInsertItem:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new VectorToStringToken();
                            else
                                token = new DynamicArrayInsertItemToken();

                            break;

                        // Redefined, can be BoolToInt!(UE1)
                        case (byte)ExprToken.DynArrayRemove:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new BoolToIntToken();
                            else
                                token = new DynamicArrayRemoveToken();

                            break;

                        case (byte)ExprToken.DynArrayRemoveItem:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new NameToStringToken();
                            else
                                token = new DynamicArrayRemoveItemToken();

                            break;

                        case (byte)ExprToken.DynArrayAdd:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new FloatToStringToken();
                            else
                                token = new DynamicArrayAddToken();

                            break;

                        case (byte)ExprToken.DynArrayAddItem:
                            if (Buffer.Version < PrimitveCastVersion)
                                token = new ObjectToStringToken();
                            else
                                token = new DynamicArrayAddItemToken();

                            break;

                        case (byte)ExprToken.DynArraySort:
                            token = new DynamicArraySortToken();
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

                                if (Buffer.Version < PrimitveCastVersion)
                                    // No other token was matched. Check if it matches any of the CastTokens
                                    // We don't just use PrimitiveCast detection due compatible with UE1 games
                                    token = DeserializeCastToken(tokenCode);

                                break;

                                #endregion
                            }
                    }
                }

                if (token == null) token = new UnknownExprToken();
                AddToken(token, tokenCode, tokenPosition);
                return token;
            }

            private void AddToken(Token token, byte tokenCode, int tokenPosition)
            {
                DeserializedTokens.Add(token);
                token.Decompiler = this;
                token.RepresentToken = tokenCode;
                token.Position = (uint)tokenPosition; // + (uint)Owner._ScriptOffset;
                token.StoragePosition = (uint)Buffer.Position - (uint)_Container.ScriptOffset - 1;
                token.Deserialize(Buffer);
                // Includes all sizes of followed tokens as well! e.g. i = i + 1; is summed here but not i = i +1; (not>>)i ++;
                token.Size = (ushort)(CodePosition - tokenPosition);
                token.StorageSize =
                    (ushort)(Buffer.Position - _Container.ScriptOffset - token.StoragePosition);
                token.PostDeserialized();
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            private Token DeserializeCastToken(byte castToken)
            {
                Token token;
                switch ((Tokens.CastToken)castToken)
                {
                    case Tokens.CastToken.StringToRotator:
                        token = new StringToRotatorToken();
                        break;

                    case Tokens.CastToken.VectorToRotator:
                        token = new VectorToRotatorToken();
                        break;

                    case Tokens.CastToken.StringToVector:
                        token = new StringToVectorToken();
                        break;

                    case Tokens.CastToken.RotatorToVector:
                        token = new RotatorToVectorToken();
                        break;

                    case Tokens.CastToken.IntToFloat:
                        token = new IntToFloatToken();
                        break;

                    case Tokens.CastToken.StringToFloat:
                        token = new StringToFloatToken();
                        break;

                    case Tokens.CastToken.BoolToFloat:
                        token = new BoolToFloatToken();
                        break;

                    case Tokens.CastToken.StringToInt:
                        token = new StringToIntToken();
                        break;

                    case Tokens.CastToken.FloatToInt:
                        token = new FloatToIntToken();
                        break;

                    case Tokens.CastToken.BoolToInt:
                        token = new BoolToIntToken();
                        break;

                    case Tokens.CastToken.RotatorToBool:
                        token = new RotatorToBoolToken();
                        break;

                    case Tokens.CastToken.VectorToBool:
                        token = new VectorToBoolToken();
                        break;

                    case Tokens.CastToken.StringToBool:
                        token = new StringToBoolToken();
                        break;

                    case Tokens.CastToken.ByteToBool:
                        token = new ByteToBoolToken();
                        break;

                    case Tokens.CastToken.FloatToBool:
                        token = new FloatToBoolToken();
                        break;

                    case Tokens.CastToken.NameToBool:
                        token = new NameToBoolToken();
                        break;

                    case Tokens.CastToken.ObjectToBool:
                        token = new ObjectToBoolToken();
                        break;

                    case Tokens.CastToken.IntToBool:
                        token = new IntToBoolToken();
                        break;

                    case Tokens.CastToken.StringToByte:
                        token = new StringToByteToken();
                        break;

                    case Tokens.CastToken.FloatToByte:
                        token = new FloatToByteToken();
                        break;

                    case Tokens.CastToken.BoolToByte:
                        token = new BoolToByteToken();
                        break;

                    case Tokens.CastToken.ByteToString:
                        token = new ByteToStringToken();
                        break;

                    case Tokens.CastToken.IntToString:
                        token = new IntToStringToken();
                        break;

                    case Tokens.CastToken.BoolToString:
                        token = new BoolToStringToken();
                        break;

                    case Tokens.CastToken.FloatToString:
                        token = new FloatToStringToken();
                        break;

                    case Tokens.CastToken.NameToString:
                        token = new NameToStringToken();
                        break;

                    case Tokens.CastToken.VectorToString:
                        token = new VectorToStringToken();
                        break;

                    case Tokens.CastToken.RotatorToString:
                        token = new RotatorToStringToken();
                        break;

                    case Tokens.CastToken.StringToName:
                        token = new StringToNameToken();
                        break;

                    case Tokens.CastToken.ByteToInt:
                        token = new ByteToIntToken();
                        break;

                    case Tokens.CastToken.IntToByte:
                        token = new IntToByteToken();
                        break;

                    case Tokens.CastToken.ByteToFloat:
                        token = new ByteToFloatToken();
                        break;

                    case Tokens.CastToken.ObjectToString:
                        token = new ObjectToStringToken();
                        break;

                    case Tokens.CastToken.InterfaceToString:
                        token = new InterfaceToStringToken();
                        break;

                    case Tokens.CastToken.InterfaceToBool:
                        token = new InterfaceToBoolToken();
                        break;

                    case Tokens.CastToken.InterfaceToObject:
                        token = new InterfaceToObjectToken();
                        break;

                    case Tokens.CastToken.ObjectToInterface:
                        token = new ObjectToInterfaceToken();
                        break;

                    case Tokens.CastToken.DelegateToString:
                        token = new DelegateToStringToken();
                        break;

                    default:
                        token = new UnknownCastToken();
                        break;
                }

                // Unsure what this is, found in:  ClanManager1h_6T.CMBanReplicationInfo.Timer:
                //  xyz = UnknownCastToken(0x1b);
                //  UnknownCastToken(0x1b)
                //  UnknownCastToken(0x1b)
                if (castToken == 0x1b)
                    token = new FloatToIntToken();

                return token;
            }

            #endregion

#if DECOMPILE

            #region Decompile

            public class NestManager
            {
                public UByteCodeDecompiler Decompiler;

                public class Nest : IUnrealDecompilable
                {
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
                        "CA1008:EnumsShouldHaveZeroValue")]
                    public enum NestType : byte
                    {
                        Scope = 0,
                        If = 1,
                        Else = 2,
                        ForEach = 4,
                        Switch = 5,
                        Case = 6,
                        Default = 7,
                        Loop = 8
                    }

                    /// <summary>
                    /// Position of this Nest (CodePosition)
                    /// </summary>
                    public uint Position;

                    public NestType Type;
                    public Token Creator;

                    public virtual string Decompile()
                    {
                        return string.Empty;
                    }

                    public bool IsPastOffset(int position)
                    {
                        return position >= Position;
                    }

                    public override string ToString()
                    {
                        return $"Type:{Type} Position:0x{Position:X3}";
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
                            : string.Empty;
#endif
                    }
                }

                public class NestEnd : Nest
                {
                    public JumpToken HasElseNest;

                    public override string Decompile()
                    {
#if DEBUG_NESTS
                        return "\r\n" + UDecompilingState.Tabs + "//</" + Type + ">";
#else
                        return Type != NestType.Case && Type != NestType.Default
                            ? UnrealConfig.PrintEndBracket()
                            : string.Empty;
#endif
                    }
                }

                public readonly List<Nest> Nests = new List<Nest>();

                public void AddNest(Nest.NestType type, uint position, uint endPosition, Token creator = null)
                {
                    creator = creator ?? Decompiler.CurrentToken;
                    Nests.Add(new NestBegin { Position = position, Type = type, Creator = creator });
                    Nests.Add(new NestEnd { Position = endPosition, Type = type, Creator = creator });
                }

                public NestBegin AddNestBegin(Nest.NestType type, uint position, Token creator = null)
                {
                    var n = new NestBegin { Position = position, Type = type };
                    Nests.Add(n);
                    n.Creator = creator ?? Decompiler.CurrentToken;
                    return n;
                }

                public NestEnd AddNestEnd(Nest.NestType type, uint position, Token creator = null)
                {
                    var n = new NestEnd { Position = position, Type = type };
                    Nests.Add(n);
                    n.Creator = creator ?? Decompiler.CurrentToken;
                    return n;
                }

                public bool TryAddNestEnd(Nest.NestType type, uint pos)
                {
                    foreach (var nest in Decompiler._Nester.Nests)
                        if (nest.Type == type && nest.Position == pos)
                            return false;

                    Decompiler._Nester.AddNestEnd(type, pos);
                    return true;
                }
            }

            private NestManager _Nester;

            // Checks if we're currently within a nest of type nestType in any stack!
            private NestManager.Nest IsWithinNest(NestManager.Nest.NestType nestType)
            {
                for (int i = _NestChain.Count - 1; i >= 0; --i)
                    if (_NestChain[i].Type == nestType)
                        return _NestChain[i];

                return null;
            }

            // Checks if the current nest is of type nestType in the current stack!
            // Only BeginNests that have been decompiled will be tested for!
            private NestManager.Nest IsInNest(NestManager.Nest.NestType nestType)
            {
                int i = _NestChain.Count - 1;
                if (i == -1)
                    return null;

                return _NestChain[i].Type == nestType ? _NestChain[i] : null;
            }

            public void InitDecompile()
            {
                _NestChain.Clear();

                _Nester = new NestManager { Decompiler = this };
                CurrentTokenIndex = -1;
                CodePosition = 0;

                FieldToken.LastField = null;

                // TODO: Corrigate detection and version.
                DefaultParameterToken._NextParamIndex = 0;
                if (Package.Version > 300)
                {
                    var func = _Container as UFunction;
                    if (func?.Params != null)
                        DefaultParameterToken._NextParamIndex = func.Params.FindIndex(
                            p => p.HasPropertyFlag(Flags.PropertyFlagsLO.OptionalParm)
                        );
                }

                // Reset these, in case of a loop in the Decompile function that did not finish due exception errors!
                _IsWithinClassContext = false;
                _CanAddSemicolon = false;
                _MustCommentStatement = false;
                _PostIncrementTabs = 0;
                _PostDecrementTabs = 0;
                _PreIncrementTabs = 0;
                _PreDecrementTabs = 0;
                PreComment = string.Empty;
                PostComment = string.Empty;

                _TempLabels = new List<(ULabelEntry, int)>();
                if (_Labels != null)
                    for (var i = 0; i < _Labels.Count; ++i)
                    {
                        // No duplicates, caused by having multiple goto's with the same destination
                        int index = _TempLabels.FindIndex(p => p.entry.Position == _Labels[i].Position);
                        if (index == -1)
                        {
                            _TempLabels.Add((_Labels[i], 1));
                        }
                        else
                        {
                            var data = _TempLabels[index];
                            data.refs++;
                            _TempLabels[index] = data;
                        }
                    }
            }

            public void JumpTo(ushort codeOffset)
            {
                int index = DeserializedTokens.FindIndex(t => t.Position == codeOffset);
                if (index == -1)
                    return;

                CurrentTokenIndex = index;
            }

            public Token TokenAt(ushort codeOffset)
            {
                return DeserializedTokens.Find(t => t.Position == codeOffset);
            }

            /// <summary>
            /// True if we are currently decompiling within a ClassContext token.
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
                if (!_WasDeserialized) Deserialize();

                var output = new StringBuilder();
                // Original indention, so that we can restore it later, necessary if decompilation fails to reduce nesting indention.
                string initTabs = UDecompilingState.Tabs;

#if DEBUG_TOKENPOSITIONS
                UDecompilingState.AddTabs(3);
#endif
                try
                {
                    //Initialize==========
                    InitDecompile();
                    var spewOutput = false;
                    var tokenBeginIndex = 0;
                    Token lastStatementToken = null;

                    while (CurrentTokenIndex + 1 < DeserializedTokens.Count)
                    {
                        //Decompile chain==========
                        {
                            string tokenOutput;
                            var newToken = NextToken;

                            // To ensure we print generated labels within a nesting block.
                            string labelsOutput = DecompileLabelForToken(CurrentToken, spewOutput);
                            if (labelsOutput != string.Empty) output.AppendLine(labelsOutput);

                            try
                            {
                                // FIX: Formatting issue on debug-compiled packages
                                if (newToken is DebugInfoToken)
                                {
                                    string nestsOutput = DecompileNests();
                                    if (nestsOutput != string.Empty)
                                    {
                                        output.Append(nestsOutput);
                                        spewOutput = true;
                                    }

                                    continue;
                                }
                            }
                            catch (Exception e)
                            {
                                output.Append($"// ({e.GetType().Name})");
                            }

                            try
                            {
                                tokenBeginIndex = CurrentTokenIndex;
                                tokenOutput = newToken.Decompile();
                                if (CurrentTokenIndex + 1 < DeserializedTokens.Count &&
                                    PeekToken is EndOfScriptToken)
                                {
                                    var firstToken = newToken is DebugInfoToken ? lastStatementToken : newToken;
                                    if (firstToken is ReturnToken)
                                    {
                                        var lastToken = newToken is DebugInfoToken ? PreviousToken : CurrentToken;
                                        if (lastToken is NothingToken || lastToken is ReturnNothingToken)
                                            _MustCommentStatement = true;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                output.AppendLine("\r\n"
                                                  + UDecompilingState.Tabs + "/* Statement decompilation error: "
                                                  + e.Message);

                                UDecompilingState.AddTab();
                                string tokensOutput = FormatAndDecompileTokens(tokenBeginIndex, CurrentTokenIndex + 1);
                                output.Append(UDecompilingState.Tabs);
                                output.Append(tokensOutput);
                                UDecompilingState.RemoveTab();

                                output.AppendLine("\r\n"
                                                  + UDecompilingState.Tabs
                                                  + "*/");

                                tokenOutput = "/*@Error*/";
                            }

                            // HACK: for multiple cases for one block of code, etc!
                            if (_PreDecrementTabs > 0)
                            {
                                UDecompilingState.RemoveTabs(_PreDecrementTabs);
                                _PreDecrementTabs = 0;
                            }

                            if (_PreIncrementTabs > 0)
                            {
                                UDecompilingState.AddTabs(_PreIncrementTabs);
                                _PreIncrementTabs = 0;
                            }

                            if (_MustCommentStatement && UnrealConfig.SuppressComments)
                                continue;

                            if (!UnrealConfig.SuppressComments)
                            {
                                if (PreComment != string.Empty)
                                {
                                    tokenOutput = PreComment + (string.IsNullOrEmpty(tokenOutput)
                                        ? tokenOutput
                                        : "\r\n" + UDecompilingState.Tabs + tokenOutput);

                                    PreComment = string.Empty;
                                }

                                if (PostComment != string.Empty)
                                {
                                    tokenOutput += PostComment;
                                    PostComment = string.Empty;
                                }
                            }

                            //Preprocess output==========
                            {
#if DEBUG_HIDDENTOKENS
                                if (tokenOutput.Length == 0) tokenOutput = ";";
#endif
#if DEBUG_TOKENPOSITIONS
#endif
                                // Previous did spew and this one spews? then a new line is required!
                                if (tokenOutput != string.Empty)
                                {
                                    // Spew before?
                                    if (spewOutput)
                                        output.Append("\r\n");
                                    else spewOutput = true;
                                }

                                if (spewOutput)
                                {
#if DEBUG_TOKENPOSITIONS
                                    string orgTabs = UDecompilingState.Tabs;
                                    int spaces = Math.Max(3 * UnrealConfig.Indention.Length, 4);
                                    UDecompilingState.RemoveSpaces(spaces);

                                    var tokens = GetTokensAt(tokenBeginIndex, CurrentTokenIndex + 1)
                                        .Select(FormatTokenInfo);
                                    string tokensInfo = !tokens.Any()
                                        ? FormatTokenInfo(newToken)
                                        : string.Join(", ", tokens);

                                    output.Append(UDecompilingState.Tabs);
                                    output.AppendLine($"  [{tokensInfo}] ");
                                    output.Append(UDecompilingState.Tabs);
                                    output.Append(
                                        $"  (+{newToken.Position:X3}  {CurrentToken.Position + CurrentToken.Size:X3}) ");
#endif
                                    if (_MustCommentStatement)
                                    {
                                        output.Append(UDecompilingState.Tabs);
                                        output.Append($"//{tokenOutput}");
                                        _MustCommentStatement = false;
                                    }
                                    else
                                    {
                                        output.Append(UDecompilingState.Tabs);
                                        output.Append(tokenOutput);
                                    }
#if DEBUG_TOKENPOSITIONS
                                    UDecompilingState.Tabs = orgTabs;
#endif
                                    // One of the decompiled tokens wanted to be ended.
                                    if (_CanAddSemicolon)
                                    {
                                        output.Append(";");
                                        _CanAddSemicolon = false;
                                    }
                                }
                            }
                            lastStatementToken = newToken;
                        }

                        //Postprocess output==========
                        if (_PostDecrementTabs > 0)
                        {
                            UDecompilingState.RemoveTabs(_PostDecrementTabs);
                            _PostDecrementTabs = 0;
                        }

                        if (_PostIncrementTabs > 0)
                        {
                            UDecompilingState.AddTabs(_PostIncrementTabs);
                            _PostIncrementTabs = 0;
                        }

                        try
                        {
                            string nestsOutput = DecompileNests();
                            if (nestsOutput != string.Empty)
                            {
                                output.Append(nestsOutput);
                                spewOutput = true;
                            }
                        }
                        catch (Exception e)
                        {
                            output.Append("\r\n" + UDecompilingState.Tabs + "// Failed to format nests!:"
                                          + e + "\r\n"
                                          + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                          + _Nester.Nests[_Nester.Nests.Count - 1]);
                            spewOutput = true;
                        }
                    }

                    try
                    {
                        // Decompile remaining nests
                        output.Append(DecompileNests(true));
                    }
                    catch (Exception e)
                    {
                        output.Append("\r\n" + UDecompilingState.Tabs
                                             + "// Failed to format remaining nests!:" + e + "\r\n"
                                             + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                             + _Nester.Nests[_Nester.Nests.Count - 1]);
                    }
                }
                catch (Exception e)
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
                        FormatTabs(e.Message),
                        FormatTabs(e.StackTrace)
                    );
                }
                finally
                {
                    UDecompilingState.Tabs = initTabs;
                }

                return output.ToString();
            }

            private readonly List<NestManager.Nest> _NestChain = new List<NestManager.Nest>();

            private static string FormatTabs(string nonTabbedText)
            {
                return nonTabbedText.Replace("\n", "\n" + UDecompilingState.Tabs);
            }

            private IEnumerable<Token> GetTokensAt(int beginIndex, int endIndex)
            {
                int count = Math.Abs(endIndex - beginIndex);
                var tokens = new Token[count];
                for (int i = beginIndex, j = 0; i < endIndex && i < DeserializedTokens.Count; ++i, ++j)
                    tokens[j] = DeserializedTokens[i];
                return tokens;
            }

            private string FormatTokenInfo(Token token)
            {
                return $"{token.GetType().Name} (0x{token.RepresentToken:X2})";
            }

            private string FormatAndDecompileTokens(int beginIndex, int endIndex)
            {
                var output = string.Empty;
                for (int i = beginIndex; i < endIndex && i < DeserializedTokens.Count; ++i)
                {
                    var t = DeserializedTokens[i];
                    try
                    {
                        output += "\r\n" + UDecompilingState.Tabs +
                                  $"{FormatTokenInfo(t)} << {t.Decompile()}";
                    }
                    catch (Exception e)
                    {
                        output += "\r\n" + UDecompilingState.Tabs +
                                  $"{FormatTokenInfo(t)} << {e.GetType().FullName}";
                        try
                        {
                            output += "\r\n" + UDecompilingState.Tabs + "(";
                            UDecompilingState.AddTab();
                            string inlinedTokens = FormatAndDecompileTokens(i + 1, CurrentTokenIndex);
                            UDecompilingState.RemoveTab();
                            output += inlinedTokens
                                      + "\r\n" + UDecompilingState.Tabs
                                      + ")";
                        }
                        finally
                        {
                            i += CurrentTokenIndex - beginIndex;
                        }
                    }
                }

                return output;
            }

            private string DecompileLabelForToken(Token token, bool appendNewline)
            {
                var output = new StringBuilder();
                int labelIndex = _TempLabels.FindIndex((l) => l.entry.Position == token.Position);
                if (labelIndex == -1) return string.Empty;

                var labelEntry = _TempLabels[labelIndex].entry;
                bool isStateLabel = !labelEntry.Name.StartsWith("J0x", StringComparison.Ordinal);
                string statementOutput = isStateLabel
                    ? $"{labelEntry.Name}:\r\n"
                    : $"{UDecompilingState.Tabs}{labelEntry.Name}:";
                if (appendNewline) output.Append("\r\n");

                output.Append(statementOutput);

                _TempLabels.RemoveAt(labelIndex);
                return output.ToString();
            }

            private string DecompileNests(bool outputAllRemainingNests = false)
            {
                var output = string.Empty;

                // Give { priority hence separated loops
                for (var i = 0; i < _Nester.Nests.Count; ++i)
                {
                    if (!(_Nester.Nests[i] is NestManager.NestBegin))
                        continue;

                    if (_Nester.Nests[i].IsPastOffset((int)CurrentToken.Position) || outputAllRemainingNests)
                    {
                        output += _Nester.Nests[i].Decompile();
                        UDecompilingState.AddTab();

                        _NestChain.Add(_Nester.Nests[i]);
                        _Nester.Nests.RemoveAt(i--);
                    }
                }

                for (int i = _Nester.Nests.Count - 1; i >= 0; i--)
                    if (_Nester.Nests[i] is NestManager.NestEnd nestEnd
                        && (outputAllRemainingNests ||
                            nestEnd.IsPastOffset((int)CurrentToken.Position + CurrentToken.Size)))
                    {
                        var topOfStack = _NestChain[_NestChain.Count - 1];
                        if (topOfStack.Type == NestManager.Nest.NestType.Default &&
                            nestEnd.Type != NestManager.Nest.NestType.Default)
                        {
                            // Automatically close default when one of its outer nest closes
                            output += $"\r\n{UDecompilingState.Tabs}break;";
                            UDecompilingState.RemoveTab();
                            _NestChain.RemoveAt(_NestChain.Count - 1);

                            // We closed off the last case, it's safe to close of the switch as well
                            if (nestEnd.Type != NestManager.Nest.NestType.Switch)
                            {
                                var switchScope = _NestChain[_NestChain.Count - 1];
                                if (switchScope.Type == NestManager.Nest.NestType.Switch)
                                {
                                    output += $"\r\n{UDecompilingState.Tabs}}}";
                                    UDecompilingState.RemoveTab();
                                    _NestChain.RemoveAt(_NestChain.Count - 1);
                                }
                                else
                                {
                                    output += $"/* Tried to find Switch scope, found {switchScope.Type} instead */";
                                }
                            }
                        }

                        UDecompilingState.RemoveTab();
                        output += nestEnd.Decompile();

                        topOfStack = _NestChain[_NestChain.Count - 1];
                        if (topOfStack.Type != nestEnd.Type)
                            output += $"/* !MISMATCHING REMOVE, tried {nestEnd.Type} got {topOfStack}! */";
                        _NestChain.RemoveAt(_NestChain.Count - 1);

                        _Nester.Nests.RemoveAt(i);
                        if (nestEnd.HasElseNest != null)
                        {
                            output += $"\r\n{UDecompilingState.Tabs}else{UnrealConfig.PrintBeginBracket()}";
                            UDecompilingState.AddTab();
                            var begin = new NestManager.NestBegin
                            {
                                Type = NestManager.Nest.NestType.Else,
                                Creator = nestEnd.HasElseNest,
                                Position = nestEnd.Position
                            };
                            var end = new NestManager.NestEnd
                            {
                                Type = NestManager.Nest.NestType.Else,
                                Creator = nestEnd.HasElseNest,
                                Position = nestEnd.HasElseNest.CodeOffset
                            };
                            _Nester.Nests.Add(end);
                            _NestChain.Add(begin);
                        }
                    }

                return output;
            }

            #endregion

            #region Disassemble

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            public string Disassemble()
            {
                return string.Empty;
            }

            #endregion

#endif
        }
    }
}