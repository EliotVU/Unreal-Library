using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using UELib.Branch;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.EndFunctionParms)]
            public class EndFunctionParmsToken : Token;

            public abstract class FunctionToken : Token
            {
                /// <summary>
                /// The function that is to be "invoked"
                ///
                /// May be null, in case of a virtual function and/or missing import.
                /// </summary>
                public abstract UFunction? FunctionCallee { get; }

                public List<Token>? Arguments; // Includes the EndFunctionParmsToken.

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected UName DeserializeFunctionName(IUnrealStream stream)
                {
                    return Script.ReadNameAligned(stream);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void SerializeFunctionName(IUnrealStream stream, in UName name)
                {
                    Script.WriteNameAligned(stream, name);
                }

                protected virtual void DeserializeCall(IUnrealStream stream)
                {
                    DeserializeParms(stream);
                    Script.DeserializeNextDebugToken(stream);
                }

                protected virtual void SerializeCall(IUnrealStream stream)
                {
                    SerializeParms(stream);
                    // no debug token to be handled here, because we require a function reference for in order to serialize the correct debug token.
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private void DeserializeParms(IUnrealStream stream)
                {
                    Arguments = [];

                    Token? token;
                    do
                    {
                        token = Script.DeserializeNextToken(stream);
                        Arguments.Add(token);
                    } while (token is not EndFunctionParmsToken);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private void SerializeParms(IUnrealStream stream)
                {
                    if (Arguments == null)
                    {
                        return;
                    }

                    foreach (var argument in Arguments)
                    {
                        Script.SerializeToken(stream, argument);
                    }
                }

                private static string PrecedenceToken(Token token, UByteCodeDecompiler decompiler)
                {
                    if (token is not FunctionToken)
                    {
                        return token.Decompile(decompiler);
                    }

                    // Always add ( and ) unless the conditions below are not met, in case of a VirtualFunctionCall.
                    var addParenthesis = true;
                    switch (token)
                    {
                        case NativeFunctionToken nativeToken:
                            addParenthesis = nativeToken.NativeItem.Type == FunctionType.Operator;
                            break;
                        case FinalFunctionToken finalToken:
                            addParenthesis = finalToken.Function.IsOperator();
                            break;
                    }

                    return addParenthesis
                        ? $"({token.Decompile(decompiler)})"
                        : token.Decompile(decompiler);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private bool NeedsSpace(string operatorName)
                {
                    return char.IsUpper(operatorName[0])
                           || char.IsLower(operatorName[0]);
                }

                protected string DecompilePreOperator(string operatorName, UByteCodeDecompiler decompiler)
                {
                    string operand = DecompileNext(decompiler);
                    AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

                    // Only space out if we have a non-symbol operator name.
                    return NeedsSpace(operatorName)
                        ? $"{operatorName} {operand}"
                        : $"{operatorName}{operand}";
                }

                protected string DecompileOperator(string operatorName, UByteCodeDecompiler decompiler)
                {
                    var output =
                        $"{PrecedenceToken(NextToken(decompiler), decompiler)} {operatorName} {PrecedenceToken(NextToken(decompiler), decompiler)}";
                    AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

                    return output;
                }

                protected string DecompilePostOperator(string operatorName, UByteCodeDecompiler decompiler)
                {
                    string operand = DecompileNext(decompiler);
                    AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

                    // Only space out if we have a non-symbol operator name.
                    return NeedsSpace(operatorName)
                        ? $"{operand} {operatorName}"
                        : $"{operand}{operatorName}";
                }

                protected string DecompileCall(string functionName, UByteCodeDecompiler decompiler)
                {
                    if (decompiler.Context.IsStatic)
                    {
                        functionName = $"static.{functionName}";

                        // Set false elsewhere as well but to be sure we set it to false here to avoid getting static calls inside the params.
                        // e.g.
                        // A1233343.DrawText(Class'BTClient_Interaction'.static.A1233332(static.Max(0, A1233328 - A1233322[A1233222].StartTime)), true);
                        decompiler.Context.IsStatic = false;
                    }

                    string arguments = DecompileArguments(decompiler);
                    var output = $"{functionName}({arguments})";

                    return output;
                }

                private string DecompileArguments(UByteCodeDecompiler decompiler)
                {
                    var callee = FunctionCallee;
                    var parm = callee?._Children;

                    var output = new StringBuilder();
                    int argumentCount = Arguments.Count;
                    for (int i = 0; i < argumentCount; ++i)
                    {
                        // Pass the context so that we can resolve byte/int literals to their corresponding enum tag.
                        if (parm != null)
                        {
                            decompiler.Context.Object = parm;
                            parm = parm.NextField;
                        }

                        var token = NextToken(decompiler);
                        switch (token)
                        {
                            case EmptyParmToken:
                                output.Append(',');
                                break;

                            // Don't append the ')', it will be handled by the caller.
                            case EndFunctionParmsToken:
                                break;

                            default:
                                if (i > 0 && i + 1 < argumentCount)
                                {
                                    output.Append(", ");
                                }

                                output.Append(token.Decompile(decompiler));
                                break;
                        }
                    }

                    return output.ToString().TrimEnd(','); // trim trailing arguments to optional parameters e.g. "SetFacingPolicy(1,,);"
                }
            }

            [ExprToken(ExprToken.FinalFunction)]
            public class FinalFunctionToken : FunctionToken
            {
                public UFunction Function;

                public override void Deserialize(IUnrealStream stream)
                {
                    Function = stream.ReadObject<UFunction>();
                    Script.AlignObjectSize();

                    DeserializeCall(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.WriteObject(Function);
                    Script.AlignObjectSize();

                    SerializeCall(stream);

                    // Not working yet, but, this should be handled during the token building phase instead of here.
                    // The potential NothingToken is also not captured by the Deserialize method, so it will still be re-serialized as a separate 'statement'.
                    //if (Function.HasFunctionFlag(FunctionFlag.Latent))
                    //{
                    //    Script.SerializeDebugToken(stream, DebugInfo.PrevStackLatent);
                    //    Script.SerializeToken(stream, ExprToken.Nothing);
                    //    Script.SerializeDebugToken(stream, DebugInfo.NewStackLatent);
                    //}
                    //else
                    //{
                    //    Script.SerializeDebugToken(stream, DebugInfo.EFP);
                    //}

                    //if (Function.IsOperator() && !Function.EnumerateFields<UProperty>().Any(property =>
                    //        property.IsParm() && property.HasPropertyFlag(PropertyFlag.SkipParm)))
                    //{
                    //    Script.SerializeDebugToken(stream, DebugInfo.EFPOper);
                    //}
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    var output = string.Empty;
                    // Support for non native operators.
                    if (Function.IsPost())
                    {
                        output = DecompilePreOperator(Function.FriendlyName, decompiler);
                    }
                    else if (Function.IsPre())
                    {
                        output = DecompilePostOperator(Function.FriendlyName, decompiler);
                    }
                    else if (Function.IsOperator())
                    {
                        output = DecompileOperator(Function.FriendlyName, decompiler);
                    }
                    else
                    {
                        // Calling Super??.
                        if (Function.Name == decompiler._Container.Name && !decompiler.Context.IsStatic)
                        {
                            output = "super";

                            // Check if the super call is within the super class of this functions outer(class)
                            var container = decompiler._Container;
                            var context = (UField)container.Outer;
                            // ReSharper disable once PossibleNullReferenceException
                            var contextFuncOuterName = context.Name;
                            // ReSharper disable once PossibleNullReferenceException
                            var callFuncOuterName = Function.Outer.Name;
                            if (context.Super == null || callFuncOuterName != context.Super.Name)
                            {
                                // If there's no super to call, then we have a recursive call.
                                if (container.Super == null)
                                {
                                    output += $"({contextFuncOuterName})";
                                }
                                else
                                {
                                    // Different owners, then it is a deep super call.
                                    if (callFuncOuterName != contextFuncOuterName)
                                    {
                                        output += $"({callFuncOuterName})";
                                    }
                                }
                            }

                            output += ".";
                        }

                        output += DecompileCall(Function.Name, decompiler);
                    }

                    decompiler.MarkSemicolon();

                    return output;
                }

                public override UFunction FunctionCallee => Function;
            }

            [ExprToken(ExprToken.VirtualFunction)]
            public class VirtualFunctionToken : FunctionToken
            {
                public UName FunctionName;

                public override void Deserialize(IUnrealStream stream)
                {
                    // FIXME: Version, seen in EndWar (222) and R6Vegas (v241), gone at least since RoboBlitz (369)
                    if (stream.Version >= (uint)PackageObjectLegacyVersion.UE3 &&
                        stream.Version <= 241)
                    {
                        byte isSuper = stream.ReadByte();
                        Script.AlignSize(sizeof(byte));
                    }

                    FunctionName = DeserializeFunctionName(stream);
                    DeserializeCall(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    // FIXME: Version, seen in EndWar (222) and R6Vegas (v241), gone at least since RoboBlitz (369)
                    if (stream.Version >= (uint)PackageObjectLegacyVersion.UE3 &&
                        stream.Version <= 241)
                    {
                        // What about recursive calls? Also doesn't check if the function is private (which can happen).
                        bool isSuper = Script.Source.FindField<UFunction>(FunctionName).Outer != Script.Source;
                        stream.Write(isSuper ? (byte)1 : (byte)0);
                        Script.AlignSize(sizeof(byte));
                    }

                    SerializeFunctionName(stream, FunctionName);
                    SerializeCall(stream);
                    Script.SerializeDebugToken(stream, DebugInfo.EFP);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    return DecompileCall(FunctionName, decompiler);
                }

                // TODO: Maybe take the UState's virtual table into consideration?
                public override UFunction? FunctionCallee => Script.Source.FindField<UFunction>(FunctionName);
            }

            [ExprToken(ExprToken.GlobalFunction)]
            public class GlobalFunctionToken : FunctionToken
            {
                public UName FunctionName;

                public override void Deserialize(IUnrealStream stream)
                {
                    FunctionName = DeserializeFunctionName(stream);
                    DeserializeCall(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeFunctionName(stream, FunctionName);
                    SerializeCall(stream);
                    Script.SerializeDebugToken(stream, DebugInfo.EFP);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    return $"global.{DecompileCall(FunctionName, decompiler)}";
                }

                // TODO: Maybe take the UState's virtual table into consideration?
                public override UFunction? FunctionCallee => Script.Source.FindField<UFunction>(FunctionName);
            }

            [ExprToken(ExprToken.DelegateFunction)]
            public class DelegateFunctionToken : FunctionToken
            {
                public byte? IsLocal;
                public UDelegateProperty DelegateProperty;
                public UName FunctionName;

                public override void Deserialize(IUnrealStream stream)
                {
                    // FIXME: Version
                    if (stream.Version >= (uint)PackageObjectLegacyVersion.IsLocalAddedToDelegateFunctionToken)
                    {
                        IsLocal = stream.ReadByte();
                        Script.AlignSize(sizeof(byte));
                    }

                    DelegateProperty = stream.ReadObject<UDelegateProperty>();
                    Script.AlignObjectSize();

                    FunctionName = DeserializeFunctionName(stream);
                    DeserializeCall(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(DelegateProperty != null);

                    // FIXME: Version
                    if (stream.Version >= (uint)PackageObjectLegacyVersion.IsLocalAddedToDelegateFunctionToken)
                    {
                        // Yeah, Epic didn't consider locals of states...
                        IsLocal = DelegateProperty.Outer is UFunction ? (byte)1 : (byte)0;
                        stream.Write(IsLocal.Value);
                        Script.AlignSize(sizeof(byte));
                    }

                    stream.WriteObject(DelegateProperty);
                    Script.AlignObjectSize();

                    SerializeFunctionName(stream, FunctionName);
                    SerializeCall(stream);
                    Script.SerializeDebugToken(stream, DebugInfo.EFP);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    return DecompileCall(FunctionName, decompiler);
                }

                public override UFunction? FunctionCallee => DelegateProperty.Function ?? Script.Source.FindField<UFunction>(FunctionName);
            }

            [ExprToken(ExprToken.NativeFunction)]
            public class NativeFunctionToken : FunctionToken
            {
                public NativeTableItem NativeItem;

                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeCall(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeCall(stream);
                    Script.SerializeDebugToken(stream, DebugInfo.EFP);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    string output;
                    switch (NativeItem.Type)
                    {
                        case FunctionType.Function:
                            output = DecompileCall(NativeItem.Name, decompiler);
                            break;

                        case FunctionType.Operator:
                            output = DecompileOperator(NativeItem.Name, decompiler);
                            break;

                        case FunctionType.PostOperator:
                            output = DecompilePostOperator(NativeItem.Name, decompiler);
                            break;

                        case FunctionType.PreOperator:
                            output = DecompilePreOperator(NativeItem.Name, decompiler);
                            break;

                        default:
                            output = DecompileCall(NativeItem.Name, decompiler);
                            break;
                    }

                    decompiler.MarkSemicolon();

                    return output;
                }

                // No checks for 'native', after all the NTL definition may be dated.
                public override UFunction? FunctionCallee => Script.Source.FindField<UFunction>(NativeItem.Name);
            }
        }
    }
}
