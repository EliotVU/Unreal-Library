using System;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public abstract class Token : IUnrealDecompilable, IUnrealDeserializableClass
            {
                public UByteCodeDecompiler Decompiler { get; set; }

                protected UnrealPackage Package => Decompiler.Package;

                public byte RepresentToken; // Fixed(adjusted at decompile time for compatibility)

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

                public virtual void Deserialize(IUnrealStream stream)
                {
                }

                public virtual void PostDeserialized()
                {
                }

                public virtual string Decompile()
                {
                    return string.Empty;
                }

                public virtual string Disassemble()
                {
                    return $"0x{RepresentToken:X2}";
                }

                protected string DecompileNext()
                {
                    tryNext:
                    var t = Decompiler.NextToken;
                    if (t is DebugInfoToken)
                    {
                        goto tryNext;
                    }

                    try
                    {
                        return t.Decompile();
                    }
                    catch (Exception e)
                    {
                        return $"{t.GetType().Name}({e.GetType().Name})";
                    }
                }

                protected Token GrabNextToken()
                {
                    tryNext:
                    var t = Decompiler.NextToken;
                    if (t is DebugInfoToken)
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
                    return
                        $"\r\nType:{GetType().Name}\r\nToken:{RepresentToken:X2}\r\nPosition:{Position}\r\nSize:{Size}".Replace("\n", "\n"
                        + UDecompilingState.Tabs
                    );
                }
            }

            public sealed class UnknownExprToken : Token
            {
                public override string Decompile()
                {
                    return $"@UnknownExprToken(0x{RepresentToken:X2})";
                }
            }

            public sealed class UnknownCastToken : Token
            {
                public override string Decompile()
                {
                    return $"@UnknownCastToken(0x{RepresentToken:X2})";
                }
            }
        }
    }
}