using System;
using System.Collections.Generic;
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
            public class EndFunctionParmsToken : Token
            {
            }

            public abstract class FunctionToken : Token
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected UName DeserializeFunctionName(IUnrealStream stream)
                {
                    return ReadName(stream);
                }

                protected virtual void DeserializeCall(IUnrealStream stream)
                {
                    DeserializeParms();
                    Decompiler.DeserializeDebugToken();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private void DeserializeParms()
                {
#pragma warning disable 642
                    while (!(DeserializeNext() is EndFunctionParmsToken)) ;
#pragma warning restore 642
                }

                private static string PrecedenceToken(Token t)
                {
                    if (!(t is FunctionToken))
                        return t.Decompile();

                    // Always add ( and ) unless the conditions below are not met, in case of a VirtualFunctionCall.
                    var addParenthesis = true;
                    switch (t)
                    {
                        case NativeFunctionToken token:
                            addParenthesis = token.NativeItem.Type == FunctionType.Operator;
                            break;
                        case FinalFunctionToken token:
                            addParenthesis = token.Function.IsOperator();
                            break;
                    }

                    return addParenthesis 
                        ? $"({t.Decompile()})" 
                        : t.Decompile();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private bool NeedsSpace(string operatorName)
                {
                    return char.IsUpper(operatorName[0])
                           || char.IsLower(operatorName[0]);
                }

                protected string DecompilePreOperator(string operatorName)
                {
                    string operand = DecompileNext();
                    AssertSkipCurrentToken<EndFunctionParmsToken>();

                    // Only space out if we have a non-symbol operator name.
                    return NeedsSpace(operatorName)
                        ? $"{operatorName} {operand}"
                        : $"{operatorName}{operand}";
                }

                protected string DecompileOperator(string operatorName)
                {
                    var output =
                        $"{PrecedenceToken(NextToken())} {operatorName} {PrecedenceToken(NextToken())}";
                    AssertSkipCurrentToken<EndFunctionParmsToken>();
                    return output;
                }

                protected string DecompilePostOperator(string operatorName)
                {
                    string operand = DecompileNext();
                    AssertSkipCurrentToken<EndFunctionParmsToken>();

                    // Only space out if we have a non-symbol operator name.
                    return NeedsSpace(operatorName)
                        ? $"{operand} {operatorName}"
                        : $"{operand}{operatorName}";
                }

                protected string DecompileCall(string functionName)
                {
                    if (Decompiler._IsWithinClassContext)
                    {
                        functionName = $"static.{functionName}";

                        // Set false elsewhere as well but to be sure we set it to false here to avoid getting static calls inside the params.
                        // e.g.
                        // A1233343.DrawText(Class'BTClient_Interaction'.static.A1233332(static.Max(0, A1233328 - A1233322[A1233222].StartTime)), true);
                        Decompiler._IsWithinClassContext = false;
                    }

                    string arguments = DecompileParms();
                    var output = $"{functionName}({arguments})";
                    return output;
                }

                private string DecompileParms()
                {
                    var tokens = new List<Tuple<Token, string>>();
                    {
                    next:
                        var t = NextToken();
                        tokens.Add(Tuple.Create(t, t.Decompile()));
                        if (!(t is EndFunctionParmsToken))
                            goto next;
                    }

                    var output = new StringBuilder();
                    for (var i = 0; i < tokens.Count; ++i)
                    {
                        var t = tokens[i].Item1; // Token
                        string v = tokens[i].Item2; // Value

                        switch (t)
                        {
                            // Skipped optional parameters
                            case EmptyParmToken _:
                                output.Append(v);
                                break;

                            // End ")"
                            case EndFunctionParmsToken _:
                                output = new StringBuilder(output.ToString().TrimEnd(','));
                                break;

                            // Any passed values
                            default:
                                {
                                    if (i != tokens.Count - 1 && i > 0) // Skipped optional parameters
                                    {
                                        output.Append(v == string.Empty ? "," : ", ");
                                    }

                                    output.Append(v);
                                    break;
                                }
                        }
                    }

                    return output.ToString();
                }
            }

            [ExprToken(ExprToken.FinalFunction)]
            public class FinalFunctionToken : FunctionToken
            {
                public UFunction Function;

                public override void Deserialize(IUnrealStream stream)
                {
                    Function = stream.ReadObject<UFunction>();
                    Decompiler.AlignObjectSize();

                    DeserializeCall(stream);
                }

                public override string Decompile()
                {
                    var output = string.Empty;
                    // Support for non native operators.
                    if (Function.IsPost())
                    {
                        output = DecompilePreOperator(Function.FriendlyName);
                    }
                    else if (Function.IsPre())
                    {
                        output = DecompilePostOperator(Function.FriendlyName);
                    }
                    else if (Function.IsOperator())
                    {
                        output = DecompileOperator(Function.FriendlyName);
                    }
                    else
                    {
                        // Calling Super??.
                        if (Function.Name == Decompiler._Container.Name && !Decompiler._IsWithinClassContext)
                        {
                            output = "super";

                            // Check if the super call is within the super class of this functions outer(class)
                            var container = Decompiler._Container;
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

                        output += DecompileCall(Function.Name);
                    }

                    Decompiler._CanAddSemicolon = true;
                    return output;
                }
            }

            [ExprToken(ExprToken.VirtualFunction)]
            public class VirtualFunctionToken : FunctionToken
            {
                public UName FunctionName;

                public override void Deserialize(IUnrealStream stream)
                {
#if UE3Proto
                    // FIXME: Version
                    if (stream.Version >= 178 && stream.Version < 200)
                    {
                        byte isSuper = stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                    }
#endif
                    FunctionName = DeserializeFunctionName(stream);
                    DeserializeCall(stream);
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return DecompileCall(FunctionName);
                }
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

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return $"global.{DecompileCall(FunctionName)}";
                }
            }

            [ExprToken(ExprToken.DelegateFunction)]
            public class DelegateFunctionToken : FunctionToken
            {
                public byte? IsLocal;
                public UProperty DelegateProperty;
                public UName FunctionName;

                public override void Deserialize(IUnrealStream stream)
                {
                    // FIXME: Version
                    if (stream.Version >= (uint)PackageObjectLegacyVersion.IsLocalAddedToDelegateFunctionToken)
                    {
                        IsLocal = stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                    }

                    DelegateProperty = stream.ReadObject<UProperty>();
                    Decompiler.AlignObjectSize();

                    FunctionName = DeserializeFunctionName(stream);
                    DeserializeCall(stream);
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return DecompileCall(FunctionName);
                }
            }

            [ExprToken(ExprToken.NativeFunction)]
            public class NativeFunctionToken : FunctionToken
            {
                public NativeTableItem NativeItem;

                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeCall(stream);
                }

                public override string Decompile()
                {
                    string output;
                    switch (NativeItem.Type)
                    {
                        case FunctionType.Function:
                            output = DecompileCall(NativeItem.Name);
                            break;

                        case FunctionType.Operator:
                            output = DecompileOperator(NativeItem.Name);
                            break;

                        case FunctionType.PostOperator:
                            output = DecompilePostOperator(NativeItem.Name);
                            break;

                        case FunctionType.PreOperator:
                            output = DecompilePreOperator(NativeItem.Name);
                            break;

                        default:
                            output = DecompileCall(NativeItem.Name);
                            break;
                    }

                    Decompiler._CanAddSemicolon = true;
                    return output;
                }
            }
        }
    }
}
