using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.Nothing)]
            public class NothingToken : Token;

            [ExprToken(ExprToken.EmptyDelegate)]
            public class EmptyDelegateToken : NoneToken;

            [ExprToken(ExprToken.NoObject)]
            public class NoObjectToken : NoneToken;

            // A skipped parameter when calling a function
            [ExprToken(ExprToken.EmptyParmValue)]
            public class EmptyParmToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return ",";
                }
            }

            // Also known as an EndCode or EndFunction token.
            [ExprToken(ExprToken.EndOfScript)]
            public class EndOfScriptToken : Token;

            [ExprToken(ExprToken.Assert)]
            public class AssertToken : Token
            {
                public ushort Line;
                public byte? IsDebug;
                public Token ConditionalExpression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Line = stream.ReadUInt16();
                    Script.AlignSize(sizeof(short));

                    // FIXME: Version, verified against (RoboBlitz v369)
                    if (stream.Version >= 200)
                    {
                        IsDebug = stream.ReadByte();
                        Script.AlignSize(sizeof(byte));
                    }

                    ConditionalExpression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Line);
                    Script.AlignSize(sizeof(short));

                    if (stream.Version >= 200)
                    {
                        stream.Write(IsDebug ?? (byte)0);
                        Script.AlignSize(sizeof(byte));
                    }

                    Contract.Assert(ConditionalExpression != null);
                    Script.SerializeToken(stream, ConditionalExpression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    if (IsDebug.HasValue)
                    {
                        decompiler.PreComment = $"// DebugMode: {IsDebug}";
                    }

                    string condition = DecompileNext(decompiler);
                    decompiler.MarkSemicolon();

                    return $"assert {condition}";
                }
            }

            public abstract class ComparisonToken : Token
            {
                public UObject Object;
                public Token LeftExpression, RightExpression;

                public override void Deserialize(IUnrealStream stream)
                {
                    stream.Read(out Object);
                    Script.AlignObjectSize();

                    LeftExpression = Script.DeserializeNextToken(stream);
                    // ==
                    RightExpression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Object != null);
                    stream.WriteObject(Object);
                    Script.AlignObjectSize();

                    Contract.Assert(LeftExpression != null);
                    Script.SerializeToken(stream, LeftExpression);

                    Contract.Assert(RightExpression != null);
                    Script.SerializeToken(stream, RightExpression);
                }
            }

            [ExprToken(ExprToken.StructCmpEq)]
            public class StructCmpEqToken : ComparisonToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return $"{DecompileNext(decompiler)} == {DecompileNext(decompiler)}";
                }
            }

            [ExprToken(ExprToken.StructCmpNe)]
            public class StructCmpNeToken : ComparisonToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return $"{DecompileNext(decompiler)} != {DecompileNext(decompiler)}";
                }
            }

            public class DelegateComparisonToken : Token
            {
                public Token LeftExpression, RightExpression;
                public Token EndToken;

                public override void Deserialize(IUnrealStream stream)
                {
                    LeftExpression = Script.DeserializeNextToken(stream);
                    // ==
                    RightExpression = Script.DeserializeNextToken(stream);

                    // End
                    EndToken = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(LeftExpression != null);
                    Script.SerializeToken(stream, LeftExpression);

                    Contract.Assert(RightExpression != null);
                    Script.SerializeToken(stream, RightExpression);

                    Contract.Assert(EndToken != null);
                    Script.SerializeToken(stream, EndToken);
                }
            }

            [ExprToken(ExprToken.DelegateCmpEq)]
            public class DelegateCmpEqToken : DelegateComparisonToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    var output = $"{DecompileNext(decompiler)} == {DecompileNext(decompiler)}";
                    AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

                    return output;
                }
            }

            [ExprToken(ExprToken.DelegateCmpNe)]
            public class DelegateCmpNeToken : DelegateComparisonToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    var output = $"{DecompileNext(decompiler)} != {DecompileNext(decompiler)}";
                    AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

                    return output;
                }
            }

            [ExprToken(ExprToken.DelegateFunctionCmpEq)]
            public class DelegateFunctionCmpEqToken : DelegateComparisonToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    var output = $"{DecompileNext(decompiler)} == {DecompileNext(decompiler)}";
                    AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

                    return output;
                }
            }

            [ExprToken(ExprToken.DelegateFunctionCmpNe)]
            public class DelegateFunctionCmpNeToken : DelegateComparisonToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    var output = $"{DecompileNext(decompiler)} != {DecompileNext(decompiler)}";
                    AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

                    return output;
                }
            }

            [ExprToken(ExprToken.EatReturnValue)]
            public class EatReturnValueToken : Token
            {
                public UProperty? ReturnValueProperty;

                public override void Deserialize(IUnrealStream stream)
                {
                    // TODO: Correct version, confirmed to at least exist as of the earliest UE3-game v369(Roboblitz).
                    // -- definitely not in the older UE3 builds v186
                    if (stream.Version < 201)
                    {
                        return;
                    }

                    ReturnValueProperty = stream.ReadObject<UProperty>();
                    Script.AlignObjectSize();
                }

                public override void Serialize(IUnrealStream stream)
                {
                    if (stream.Version < 201)
                    {
                        return;
                    }

                    Contract.Assert(ReturnValueProperty != null);
                    stream.WriteObject(ReturnValueProperty);
                    Script.AlignObjectSize();
                }
            }

            [ExprToken(ExprToken.ResizeString)]
            public class ResizeStringToken : Token
            {
                public byte Length;
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Length = stream.ReadByte();
                    Script.AlignSize(sizeof(byte));

                    // Could there have been an explicit cast too maybe?
                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Length);
                    Script.AlignSize(sizeof(byte));

                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileNext(decompiler);
                }
            }

            [ExprToken(ExprToken.BeginFunction)]
            public class BeginFunctionToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    for (; ; )
                    {
                        byte elementSize = stream.ReadByte();
                        Script.AlignSize(sizeof(byte));
                        if (elementSize == 0x00)
                        {
                            break;
                        }

                        stream.ReadByte(); // bOutParam
                        Script.AlignSize(sizeof(byte));
                    }
                }

                public override void Serialize(IUnrealStream stream)
                {
                    // TODO: 
                    throw new NotImplementedException("BeginFunctionToken serialization is not implemented.");
                }
            }

            [ExprToken(ExprToken.New)]
            public class NewToken : Token
            {
                public Token OuterArgument, NameArgument, FlagsArgument;
                public Token ClassExpression;

                public Token? TemplateExpression;

                public override void Deserialize(IUnrealStream stream)
                {
                    OuterArgument = Script.DeserializeNextToken(stream);
                    NameArgument = Script.DeserializeNextToken(stream);
                    FlagsArgument = Script.DeserializeNextToken(stream);
                    ClassExpression = Script.DeserializeNextToken(stream);

                    // TODO: Corrigate Version
                    if (stream.Version > 300)
                    {
                        TemplateExpression = Script.DeserializeNextToken(stream);
                    }
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(OuterArgument != null);
                    Script.SerializeToken(stream, OuterArgument);

                    Contract.Assert(NameArgument != null);
                    Script.SerializeToken(stream, NameArgument);

                    Contract.Assert(FlagsArgument != null);
                    Script.SerializeToken(stream, FlagsArgument);

                    Contract.Assert(ClassExpression != null);
                    Script.SerializeToken(stream, ClassExpression);

                    if (stream.Version > 300)
                    {
                        Contract.Assert(TemplateExpression != null);
                        Script.SerializeToken(stream, TemplateExpression);
                    }
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    // TODO:Clean this up; make it more concise!
                    string outerStr = DecompileNext(decompiler);
                    string nameStr = DecompileNext(decompiler);
                    string flagsStr = DecompileNext(decompiler);
                    string classStr = DecompileNext(decompiler);

                    var templateStr = string.Empty;
                    // TODO: Corrigate Version
                    if (TemplateExpression != null)
                    {
                        templateStr = DecompileNext(decompiler);
                    }

                    // Handles: new [( [outer [, name [, flags]]] )] class [( template )]
                    var output = string.Empty;
                    var addComma = false;

                    if (outerStr.Length != 0)
                    {
                        output += outerStr;
                        addComma = true;
                    }

                    if (nameStr.Length != 0)
                    {
                        if (addComma)
                            output += ", ";

                        output += nameStr;
                        addComma = true;
                    }

                    if (flagsStr.Length != 0)
                    {
                        if (addComma)
                            output += ", ";

                        output += flagsStr;
                        addComma = true;
                    }

                    if (addComma)
                    {
                        output = $" ({output})";
                    }

                    if (classStr.Length != 0)
                    {
                        output += $" {classStr}";
                    }

                    if (templateStr.Length != 0)
                    {
                        output += $" ({templateStr})";
                    }

                    decompiler.MarkSemicolon();

                    return $"new{output}";
                }
            }

            [ExprToken(ExprToken.DebugInfo)]
            public class DebugInfoToken : Token
            {
                // Version, usually 100
                public int Version;
                public int Line;
                public int TextPos;
                public DebugInfo OpCode = DebugInfo.Unset;

                public string? OpCodeText;

                public override void Deserialize(IUnrealStream stream)
                {
                    Version = stream.ReadInt32();
                    Script.AlignSize(sizeof(int));
                    Line = stream.ReadInt32();
                    Script.AlignSize(sizeof(int));
                    TextPos = stream.ReadInt32();
                    Script.AlignSize(sizeof(int));
#if UNREAL2
                    // FIXME: Is this a legacy feature or U2 specific?
                    // Also in RSRS, and GBX engine
                    if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2XMP ||
                        stream.Build == BuildGeneration.Lead)
                    {
                        OpCodeText = stream.ReadAnsiNullString();
                        Script.AlignSize(OpCodeText.Length + 1);
                        if (!Enum.TryParse(OpCodeText, true, out OpCode))
                        {
                            Debug.WriteLine($"Couldn't parse OpCode '{OpCodeText}'");
                        }

                        return;
                    }
#endif
                    // At least since UT2004+
                    OpCode = (DebugInfo)stream.ReadByte();
                    Script.AlignSize(sizeof(byte));
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Version);
                    Script.AlignSize(sizeof(int));
                    stream.Write(Line);
                    Script.AlignSize(sizeof(int));
                    stream.Write(TextPos);
                    Script.AlignSize(sizeof(int));
#if UNREAL2
                    // FIXME: Is this a legacy feature or U2 specific?
                    // Also in RSRS, and GBX engine
                    if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2XMP ||
                        stream.Build == BuildGeneration.Lead)
                    {
                        stream.WriteAnsiNullString(OpCodeText);
                        Script.AlignSize(OpCodeText.Length + 1);

                        return;
                    }
#endif
                    stream.Write((byte)OpCode);
                    Script.AlignSize(sizeof(byte));
                }

#if DEBUG_HIDDENTOKENS
                public override string Decompile()
                {
                    Decompiler._MustCommentStatement = true;
                    return Enum.GetName(typeof(DebugInfo), OpCode);
                }
#endif
            }

            [ExprToken(ExprToken.LineNumber)]
            public class LineNumberToken : Token
            {
                public ushort Line;
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Line = stream.ReadUInt16();
                    Script.AlignSize(sizeof(ushort));

                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Line);
                    Script.AlignSize(sizeof(ushort));

                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileNext(decompiler);
                }
            }
        }
    }
}