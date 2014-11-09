using System;
using System.Diagnostics;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public class NothingToken : Token
            {
                public override void Deserialize( IUnrealStream stream )
                {
                    if( stream.Version == 421 )
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
            public class NoParmToken : Token
            {
                public override string Decompile()
                {
                    return ",";
                }
            }
            public class EndOfScriptToken : Token{}

            public class AssertToken : Token
            {
                public bool DebugMode;

                public override void Deserialize( IUnrealStream stream )
                {
                    stream.ReadUInt16();    // Line
                    Decompiler.AlignSize( sizeof(short) );

                    // TODO: Corrigate version, at least known since Mirrors Edge(536)
                    if( stream.Version >= 536 )
                    {
                        DebugMode = stream.ReadByte() > 0;
                        Decompiler.AlignSize( sizeof(byte) );
                    }
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    if( Package.Version >= 536 )
                    {
                        Decompiler.PreComment = "// DebugMode:" + DebugMode;
                    }
                    string expr = DecompileNext();
                    Decompiler._CanAddSemicolon = true;
                    return "assert(" + expr + ")";
                }
            }

            public abstract class ComparisonToken : Token
            {
                public override void Deserialize( IUnrealStream stream )
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
                public override void Deserialize( IUnrealStream stream )
                {
                    DeserializeNext();  // Left
                    // ==
                    DeserializeNext();  // Right

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

            public class EatStringToken : Token
            {
                public override void Deserialize( IUnrealStream stream )
                {
                    // TODO: Corrigate Version(Lowest known version 369(Roboblitz))
                    if( stream.Version > 300 )
                    {
                        stream.ReadObjectIndex();
                        Decompiler.AlignObjectSize();
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
                public override void Deserialize( IUnrealStream stream )
                {
                    stream.ReadByte();  // Size
                    Decompiler.AlignSize( sizeof(byte) );
                }
            }

            // TODO: Implement
            public class BeginFunctionToken : Token
            {
                public override void Deserialize( IUnrealStream stream )
                {
                    var topFunc = Decompiler._Container as UFunction;
                    Debug.Assert( topFunc != null, "topf != null" );
                    foreach( var property in topFunc.Variables )
                    {
                        if( !property.HasPropertyFlag( Flags.PropertyFlagsLO.Parm | Flags.PropertyFlagsLO.ReturnParm ) )
                            continue;

                        stream.ReadByte(); // Size
                        Decompiler.AlignSize( sizeof(byte) );

                        stream.ReadByte(); // bOutParam
                        Decompiler.AlignSize( sizeof(byte) );
                    }
                    stream.ReadByte();  // End
                    Decompiler.AlignSize( sizeof(byte) );
                }
            }

            public class NewToken : Token
            {
                // Greater Than!
                private const uint TemplateVersion = 300;   // TODO: Corrigate Version

                public override void Deserialize( IUnrealStream stream )
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
                    if( stream.Version > TemplateVersion )
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
                    if( Package.Version > TemplateVersion )
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
                public override void Deserialize( IUnrealStream stream )
                {
                    // Version
                    stream.ReadInt32();
                    // Line
                    stream.ReadInt32();
                    // Pos
                    stream.ReadInt32();
                    // Code
                    stream.ReadByte();
                    Decompiler.AlignSize( 13 );
                }
            }
        }
    }
}