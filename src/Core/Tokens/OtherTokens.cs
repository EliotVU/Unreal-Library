using System;
using System.Diagnostics;
using UELib.Annotations;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.Nothing)]
            public class NothingToken : Token
            {
            }

            [ExprToken(ExprToken.EmptyDelegate)]
            public class EmptyDelegateToken : NoneToken
            {
            }

            [ExprToken(ExprToken.NoObject)]
            public class NoObjectToken : NoneToken
            {
            }

            // A skipped parameter when calling a function
            [ExprToken(ExprToken.EmptyParmValue)]
            public class EmptyParmToken : Token
            {
                public override string Decompile()
                {
                    return ",";
                }
            }

            // Also known as an EndCode or EndFunction token.
            [ExprToken(ExprToken.EndOfScript)]
            public class EndOfScriptToken : Token
            {
            }

            [ExprToken(ExprToken.Assert)]
            public class AssertToken : Token
            {
                public ushort Line;
                public byte? IsDebug;

                public override void Deserialize(IUnrealStream stream)
                {
                    Line = stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(short));

                    // FIXME: Version, verified against (RoboBlitz v369)
                    if (stream.Version >= 200)
                    {
                        IsDebug = stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                    }

                    DeserializeNext();
                }

                public override string Decompile()
                {
                    if (IsDebug.HasValue)
                    {
                        Decompiler.PreComment = $"// DebugMode: {IsDebug}";
                    }

                    string condition = DecompileNext();
                    Decompiler._CanAddSemicolon = true;
                    return $"assert({condition})";
                }
            }

            public abstract class ComparisonToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadObjectIndex();
                    Decompiler.AlignObjectSize();

                    DeserializeNext();
                    // ==
                    DeserializeNext();
                }
            }

            [ExprToken(ExprToken.StructCmpEq)]
            public class StructCmpEqToken : ComparisonToken
            {
                public override string Decompile()
                {
                    return $"{DecompileNext()} == {DecompileNext()}";
                }
            }

            [ExprToken(ExprToken.StructCmpNe)]
            public class StructCmpNeToken : ComparisonToken
            {
                public override string Decompile()
                {
                    return $"{DecompileNext()} != {DecompileNext()}";
                }
            }

            public class DelegateComparisonToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeNext(); // Left
                    // ==
                    DeserializeNext(); // Right

                    // End
                    DeserializeNext();
                }
            }

            [ExprToken(ExprToken.DelegateCmpEq)]
            public class DelegateCmpEqToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    var output = $"{DecompileNext()} == {DecompileNext()}";
                    DecompileNext();
                    return output;
                }
            }
            
            [ExprToken(ExprToken.DelegateCmpNe)]
            public class DelegateCmpNeToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    var output = $"{DecompileNext()} != {DecompileNext()}";
                    DecompileNext();
                    return output;
                }
            }
            
            [ExprToken(ExprToken.DelegateFunctionCmpEq)]
            public class DelegateFunctionCmpEqToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    var output = $"{DecompileNext()} == {DecompileNext()}";
                    DecompileNext();
                    return output;
                }
            }

            [ExprToken(ExprToken.DelegateFunctionCmpNe)]
            public class DelegateFunctionCmpNeToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    var output = $"{DecompileNext()} != {DecompileNext()}";
                    DecompileNext();
                    return output;
                }
            }
            
            [ExprToken(ExprToken.EatReturnValue)]
            public class EatReturnValueToken : Token
            {
                // Null if version < 200
                [CanBeNull] public UProperty ReturnValueProperty;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    // TODO: Correct version, confirmed to at least exist as of the earliest UE3-game v369(Roboblitz).
                    // -- definitely not in the older UE3 builds v186
                    if (stream.Version < 200) return;
                    
                    ReturnValueProperty = stream.ReadObject<UProperty>();
                    Decompiler.AlignObjectSize();
                }
            }

            [ExprToken(ExprToken.ResizeString)]
            public class ResizeStringToken : Token
            {
                public byte Length;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    Length = stream.ReadByte();
                    Decompiler.AlignSize(sizeof(byte));

                    // Could there have been an explicit cast too maybe?
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return DecompileNext();
                }
            }

            [ExprToken(ExprToken.BeginFunction)]
            public class BeginFunctionToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    for (;;)
                    {
                        byte elementSize = stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                        if (elementSize == 0x00)
                        {
                            break;
                        }

                        stream.ReadByte(); // bOutParam
                        Decompiler.AlignSize(sizeof(byte));
                    }
                }
            }

            [ExprToken(ExprToken.New)]
            public class NewToken : Token
            {
                // Greater Than!
                private const uint TemplateVersion = 300; // TODO: Corrigate Version

                public override void Deserialize(IUnrealStream stream)
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
                    if (stream.Version > TemplateVersion)
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

                    var templateStr = string.Empty;
                    // TODO: Corrigate Version
                    if (Package.Version > TemplateVersion)
                    {
                        templateStr = DecompileNext();
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

                    Decompiler._CanAddSemicolon = true;
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
                
                [CanBeNull] public string OpCodeText;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    Version = stream.ReadInt32();
                    Decompiler.AlignSize(4);
                    Line = stream.ReadInt32();
                    Decompiler.AlignSize(4);
                    TextPos = stream.ReadInt32();
                    Decompiler.AlignSize(4);
#if UNREAL2
                    // FIXME: Is this a legacy feature or U2 specific?
                    // Also in RSRS, and GBX engine
                    if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2XMP)
                    {
                        OpCodeText = stream.ReadAnsiNullString();
                        Decompiler.AlignSize(OpCodeText.Length + 1);
                        Decompiler._Container.Record(nameof(OpCodeText), OpCodeText);
                        if (!Enum.TryParse(OpCodeText, true, out OpCode))
                        {
                            Debug.WriteLine($"Couldn't parse OpCode '{OpCodeText}'");
                        }

                        return;
                    }
#endif                    
                    // At least since UT2004+
                    OpCode = (DebugInfo)stream.ReadByte();
                    Decompiler.AlignSize(1);
                    Decompiler._Container.Record(nameof(OpCode), OpCode);
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
                
                public override void Deserialize(IUnrealStream stream)
                {
                    Line = stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(ushort));
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return DecompileNext();
                }
            }
        }
    }
}
