using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

                public byte RepresentToken;

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

                public override string ToString()
                {
                    return
                        $"\r\nType:{GetType().Name}\r\nToken:{RepresentToken:X2}\r\nPosition:{Position}\r\nSize:{Size}"
                            .Replace("\n", "\n"
                                           + UDecompilingState.Tabs
                            );
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