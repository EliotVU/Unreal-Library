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
            public abstract class Token : IUnrealDecompilable, IUnrealDeserializableClass, IAcceptable
            {
                public UByteCodeDecompiler Decompiler { get; set; }

                protected UnrealPackage Package => Decompiler._Package;

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

                public virtual void PostDeserialized()
                {
                }

                public virtual string Decompile()
                {
                    return string.Empty;
                }

                protected string DecompileNext()
                {
                tryNext:
                    var token = Decompiler.NextToken;
                    if (token is DebugInfoToken) goto tryNext;

                    return token.Decompile();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected T NextToken<T>()
                    where T : Token
                {
                    return (T)NextToken();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected Token NextToken()
                {
                tryNext:
                    var t = Decompiler.NextToken;
                    if (t is DebugInfoToken) goto tryNext;

                    return t;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void SkipCurrentToken()
                {
                    ++Decompiler.CurrentTokenIndex;
                }

                /// <summary>
                /// Asserts that the token that we want to skip is indeed of the correct type, this also skips past any <see cref="DebugInfoToken"/>.
                /// </summary>
                /// <typeparam name="T">The type of the token that we want to assert.</typeparam>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected void AssertSkipCurrentToken<T>()
                    where T : Token
                {
                tryNext:
                    var token = Decompiler.NextToken;
                    if (token is DebugInfoToken) goto tryNext;
                    // This assertion will fail in most cases if the native indexes are a mismatch.
#if STRICT
                    Debug.Assert(token is T, $"Expected to skip a token of type '{typeof(T)}', but got '{token.GetType()}'");
#endif
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected Token DeserializeNext()
                {
                    return Decompiler.DeserializeNext();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected T DeserializeNext<T>()
                    where T : Token
                {
                    return (T)Decompiler.DeserializeNext();
                }

                /// <summary>
                /// Wrapper for IUnrealStream.ReadNameReference to handle memory alignment as well as differences between builds.
                ///
                /// In Batman4 tokens with name references have been reduced to only serialize the index reference, except for NameConstToken among others.
                ///
                /// TODO: Maybe wrap the IUnrealStream underlying type instead, and implement all memory alignment logic in there.
                /// </summary>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected UName ReadName(IUnrealStream stream)
                {
#if BATMAN
                    // (Only for byte-codes) No int32 numeric followed after a name index for Batman4
                    if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
                    {
                        return ReadNameNoNumber(stream);
                    }
#endif
                    var name = stream.ReadNameReference();
                    Decompiler.AlignNameSize();
                    return name;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected UName ReadNameNoNumber(IUnrealStream stream)
                {
                    int nameIndex = stream.ReadInt32();
                    Decompiler.AlignSize(sizeof(int));
                    var name = new UName(stream.Package.Names[nameIndex], 0);
                    return name;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                protected T ReadObject<T>(IUnrealStream stream)
                    where T : UObject
                {
                    var obj = stream.ReadObject<T>();
                    Decompiler.AlignObjectSize();
                    return obj;
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

            public class BadToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
#if STRICT
                    Debug.Fail($"Bad expression token 0x{OpCode:X2}");
#endif
                }

                public override string Decompile()
                {
                    Decompiler.PreComment = $"// {FormatTokenInfo(this)}";
                    return default;
                }
            }

            public class UnresolvedToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
#if DEBUG_HIDDENTOKENS
                    Debug.WriteLine("Detected an unresolved token.");
#endif
                }

                public override string Decompile()
                {
                    Decompiler.PreComment = $"// {FormatTokenInfo(this)}";
                    return default;
                }
            }
        }
    }
}
