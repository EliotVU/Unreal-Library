using System;
using System.Diagnostics;
using UELib.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public class NothingToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.MOHA)
                        Decompiler.AlignSize(sizeof(int));
                }

                public override string Decompile()
                {
                    // Empty default option parameter!
                    ++DefaultParameterToken._NextParamIndex;
                    return string.Empty;
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
                public override string Decompile()
                {
                    return ",";
                }
            }

            public class EndOfScriptToken : Token
            {
            }

            public class AssertToken : Token
            {
                public ushort Line;
                public bool DebugMode;

                public override void Deserialize(IUnrealStream stream)
                {
                    Line = stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(short));

                    // TODO: Corrigate version, at least known since Mirrors Edge(536)
                    if (stream.Version >= 536)
                    {
                        DebugMode = stream.ReadByte() > 0;
                        Decompiler.AlignSize(sizeof(byte));
                    }

                    DeserializeNext();
                }

                public override string Decompile()
                {
                    if (Package.Version >= 536)
                    {
                        Decompiler.PreComment = "// DebugMode:" + DebugMode;
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

            public class StructCmpEqToken : ComparisonToken
            {
                public override string Decompile()
                {
                    return $"{DecompileNext()} == {DecompileNext()}";
                }
            }

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

            public class DelegateCmpEqToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    var output = $"{DecompileNext()} == {DecompileNext()}";
                    DecompileNext();
                    return output;
                }
            }

            public class DelegateFunctionCmpEqToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    var output = $"{DecompileNext()} == {DecompileNext()}";
                    DecompileNext();
                    return output;
                }
            }

            public class DelegateCmpNEToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    var output = $"{DecompileNext()} != {DecompileNext()}";
                    DecompileNext();
                    return output;
                }
            }

            public class DelegateFunctionCmpNEToken : DelegateComparisonToken
            {
                public override string Decompile()
                {
                    var output = $"{DecompileNext()} != {DecompileNext()}";
                    DecompileNext();
                    return output;
                }
            }

            public class EatReturnValueToken : Token
            {
                // Null if version < 200
                [CanBeNull] public UProperty ReturnValueProperty;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    // TODO: Correct version, confirmed to at least exist as of the earliest UE3-game v369(Roboblitz).
                    // -- definitely not in the older UE3 builds v186
                    if (stream.Version < 200) return;
                    
                    ReturnValueProperty = stream.ReadObject() as UProperty;
                    Decompiler.AlignObjectSize();
                }
            }

            public class CastStringSizeToken : Token
            {
                public byte Size;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    Size = stream.ReadByte();
                    Decompiler.AlignSize(sizeof(byte));
                }
                
                // TODO: Decompile format?
            }

            public class BeginFunctionToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    var structContainer = Decompiler._Container;
                    for (var field = structContainer.Children; field != null; field = field.NextField)
                    {
                        var property = field as UProperty;
                        if (property == null)
                        {
                            continue;
                        }

                        if (!property.HasPropertyFlag(Flags.PropertyFlagsLO.Parm | Flags.PropertyFlagsLO.ReturnParm))
                            continue;
                        
                        stream.ReadByte(); // Size
                        Decompiler.AlignSize(sizeof(byte));

                        stream.ReadByte(); // bOutParam
                        Decompiler.AlignSize(sizeof(byte));
                    }

                    stream.ReadByte(); // End
                    Decompiler.AlignSize(sizeof(byte));
                }
            }

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
                    // Also in RSRS
                    if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2XMP)
                    {
                        OpCodeText = stream.ReadASCIIString();
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
#if BIOSHOCK
            public class LogFunctionToken : FunctionToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    // NothingToken(0x0B) twice
                    DeserializeCall();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    // FIXME: Reverse-order of params?
                    return DecompileCall("log");
                }
            }
#endif
        }
    }
}