using System;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public abstract class Token : IUnrealDecompilable, IUnrealDeserializableClass
            {
                public UByteCodeDecompiler Decompiler
                {
                    get;
                    set;
                }

                protected UnrealPackage Package
                {
                    get{ return Decompiler.Package; }
                }

                public byte RepresentToken;  // Fixed(adjusted at decompile time for compatibility)

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

                public virtual void Deserialize( IUnrealStream stream )
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
        }
    }
}