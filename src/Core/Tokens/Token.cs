using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;
using UELib.UnrealScript;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public abstract class Token : IUnrealSerializableClass, IAcceptable
            {
                internal UByteCodeScript Script { get; set; }

                /// <summary>
                /// The raw serialized byte-code for this token.
                ///
                /// e.g. if this token were to represent an expression token such as <see cref="ExprToken.StructMember"/>
                /// then the OpCode will read 0x36 for UE2, but 0x35 for UE3.
                /// This can be useful for tracking or re-writing a token to a file.
                /// </summary>
                public byte OpCode;

                /// <summary>
                /// The read position of this token in memory relative to <see cref="UStruct.ScriptOffset"/>.
                /// </summary>
                public int Position, StoragePosition;

                /// <summary>
                /// The read size of this token in memory, inclusive of child tokens.
                /// </summary>
                public short Size, StorageSize;

                public virtual void Deserialize(IUnrealStream stream)
                {
                }

                public virtual void Serialize(IUnrealStream stream)
                {
                }

                [Obsolete("No longer of use")]
                public virtual void PostDeserialized()
                {
                }

                public virtual string Decompile(UByteCodeDecompiler decompiler)
                {
                    return string.Empty;
                }

                protected string DecompileNext(UByteCodeDecompiler decompiler)
                {
                tryNext:
                    var token = decompiler.NextToken;
                    if (token is DebugInfoToken) goto tryNext;

                    return token.Decompile(decompiler);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected T NextToken<T>(UByteCodeDecompiler decompiler)
                    where T : Token
                {
                    return (T)NextToken(decompiler);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected Token NextToken(UByteCodeDecompiler decompiler)
                {
                tryNext:
                    var t = decompiler.NextToken;
                    if (t is DebugInfoToken) goto tryNext;

                    return t;
                }

                /// <summary>
                /// Asserts that the token that we want to skip is indeed of the correct type, this also skips past any <see cref="DebugInfoToken"/>.
                /// </summary>
                /// <param name="decompiler"></param>
                /// <typeparam name="T">The type of the token that we want to assert.</typeparam>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void AssertSkipCurrentToken<T>(UByteCodeDecompiler decompiler)
                    where T : Token
                {
                tryNext:
                    var token = decompiler.NextToken;
                    if (token is DebugInfoToken) goto tryNext;
                    // This assertion will fail in most cases if the native indexes are a mismatch.
#if DEBUG
                    Services.LibServices.LogService.SilentAssert(token is T, $"Expected to skip a token of type '{typeof(T)}', but got '{token.GetType()}'");
#endif
                }

                [Obsolete("Use Script.DeserializeNextToken(IUnrealStream)")]
                protected Token DeserializeNext(IUnrealStream stream)
                {
                    return Script.DeserializeNextToken(stream);
                }

                [Obsolete("Use Script.DeserializeNextToken<T>(IUnrealStream)")]
                protected T DeserializeNext<T>(IUnrealStream stream)
                    where T : Token
                {
                    return (T)Script.DeserializeNextToken(stream);
                }

                public void Accept(IVisitor visitor)
                {
                    visitor.Visit(this);
                }

                public TResult Accept<TResult>(IVisitor<TResult> visitor)
                {
                    return visitor.Visit(this);
                }

                public ExprToken GetExprToken()
                {
                    var type = GetType();
                    var exprTokenAttr = type.GetCustomAttribute<ExprTokenAttribute>();
                    return exprTokenAttr.ExprToken;
                }

                public override int GetHashCode() => GetType().GetHashCode();

                public override string ToString()
                {
                    return
                        $"\r\nInstantiated Type: {GetType().Name}" +
                        $"\r\nSerialized OpCode: {OpCode:X2}h" +
                        $"\r\nOffset: {PropertyDisplay.FormatOffset(Position)}" +
                        $"\r\nSize: {PropertyDisplay.FormatOffset(Size)}";
                }
            }

            public sealed class BadToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    Services.LibServices.LogService.SilentAssert(false, $"Bad expression token 0x{OpCode:X2}");
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.PreComment = $"// {FormatTokenInfo(this)}";
                    return string.Empty;
                }
            }

            public sealed class UnresolvedToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    Services.LibServices.Debug("Detected an unresolved token.");
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.PreComment = $"// {FormatTokenInfo(this)}";
                    return string.Empty;
                }
            }
        }
    }
}