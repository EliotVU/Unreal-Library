using System.Diagnostics.Contracts;
using UELib.Branch;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.ArrayElement)]
            public class ArrayElementToken : Token
            {
                /// <summary>
                /// The index argument expression for the array expression.
                ///
                /// e.g. `Array[0]`
                /// </summary>
                public Token IndexArgument;

                /// <summary>
                /// The array expression for the index argument.
                ///
                /// e.g. `Expression[0]`
                /// </summary>
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    // Key
                    IndexArgument = Script.DeserializeNextToken(stream);

                    // Array
                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(IndexArgument != null);
                    Script.SerializeToken(stream, IndexArgument);

                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    string indexExpression = DecompileNext(decompiler);
                    string primaryExpression = DecompileNext(decompiler);

                    return $"{primaryExpression}[{indexExpression}]";
                }
            }

            [ExprToken(ExprToken.DynArrayElement)]
            public class DynamicArrayElementToken : ArrayElementToken;

            [ExprToken(ExprToken.DynArrayLength)]
            public class DynamicArrayLengthToken : Token
            {
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    // Array
                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    return $"{DecompileNext(decompiler)}.Length";
                }
            }

            public abstract class DynamicArrayMethodToken : Token
            {
                public Token Expression;

                public ushort SkipSize { get; protected set; }

                public Token? Argument1;
                public Token? Argument2;

                /// <summary>
                /// Null if not deserialized.
                /// </summary>
                public Token? EndToken;

                protected void DeserializeOneParamMethodWithSkip(IUnrealStream stream,
                                                                 uint skipSizeVersion = (uint)PackageObjectLegacyVersion
                                                                     .SkipSizeAddedToArrayTokenIntrinsics)
                {
                    // Array
                    Expression = Script.DeserializeNextToken(stream);

                    if (stream.Version >= skipSizeVersion)
                    {
                        SkipSize = stream.ReadUInt16();
                        Script.AlignSize(sizeof(ushort));
                    }

                    // Param 1
                    Argument1 = Script.DeserializeNextToken(stream);

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        EndToken = Script.DeserializeNextToken(stream);
                    }

                    Script.DeserializeNextDebugToken(stream);
                }

                protected void SerializeOneParamMethodWithSkip(IUnrealStream stream,
                                                               uint skipSizeVersion = (uint)PackageObjectLegacyVersion
                                                                   .SkipSizeAddedToArrayTokenIntrinsics)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);

                    long skipSizePeek = stream.Position;
                    if (stream.Version >= skipSizeVersion)
                    {
                        stream.Write((ushort)0);
                        Script.AlignSize(sizeof(ushort));
                    }

                    int memorySize = Script.MemorySize;
                    Contract.Assert(Argument1 != null);
                    Script.SerializeToken(stream, Argument1);

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // TODO: Should create the end token if it is null (version migration) 
                        Contract.Assert(EndToken != null);
                        Script.SerializeToken(stream, EndToken);
                    }

                    if (stream.Version >= skipSizeVersion)
                    {
                        SkipSize = (ushort)(Script.MemorySize - memorySize);
                        using (stream.Peek(skipSizePeek))
                        {
                            stream.Write(SkipSize);
                        }
                    }
                }

                protected void DeserializeOneParamMethodNoSkip(IUnrealStream stream)
                {
                    // Array
                    Expression = Script.DeserializeNextToken(stream);

                    // Param 1
                    Argument1 = Script.DeserializeNextToken(stream);

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        EndToken = Script.DeserializeNextToken(stream);
                    }

                    Script.DeserializeNextDebugToken(stream);
                }

                protected void SerializeOneParamMethodNoSkip(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);

                    Contract.Assert(Argument1 != null);
                    Script.SerializeToken(stream, Argument1);

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // TODO: Should create the end token if it is null (version migration) 
                        Contract.Assert(EndToken != null);
                        Script.SerializeToken(stream, EndToken);
                    }
                }

                protected void DeserializeTwoParamMethodNoSkip(IUnrealStream stream)
                {
                    // Array
                    Expression = Script.DeserializeNextToken(stream);

                    // Param 1
                    Argument1 = Script.DeserializeNextToken(stream);

                    // Param 2
                    Argument2 = Script.DeserializeNextToken(stream);

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        EndToken = Script.DeserializeNextToken(stream);
                    }

                    Script.DeserializeNextDebugToken(stream);
                }

                protected void SerializeTwoParamMethodNoSkip(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);

                    Contract.Assert(Argument1 != null);
                    Script.SerializeToken(stream, Argument1);

                    Contract.Assert(Argument2 != null);
                    Script.SerializeToken(stream, Argument2);

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // TODO: Should create the end token if it is null (version migration) 
                        Contract.Assert(EndToken != null);
                        Script.SerializeToken(stream, EndToken);
                    }
                }

                protected void DeserializeTwoParamMethodWithSkip(IUnrealStream stream,
                                                                 uint skipSizeVersion = (uint)PackageObjectLegacyVersion
                                                                     .SkipSizeAddedToArrayTokenIntrinsics)
                {
                    // Array
                    Expression = Script.DeserializeNextToken(stream);

                    if (stream.Version >= skipSizeVersion)
                    {
                        SkipSize = stream.ReadUInt16();
                        Script.AlignSize(sizeof(ushort));
                    }

                    // Param 1
                    Argument1 = Script.DeserializeNextToken(stream);

                    // Param 2
                    Argument2 = Script.DeserializeNextToken(stream);

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        EndToken = Script.DeserializeNextToken(stream);
                    }

                    Script.DeserializeNextDebugToken(stream);
                }

                protected void SerializeTwoParamMethodWithSkip(IUnrealStream stream,
                                                               uint skipSizeVersion = (uint)PackageObjectLegacyVersion
                                                                   .SkipSizeAddedToArrayTokenIntrinsics)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);

                    long skipSizePeek = stream.Position;
                    if (stream.Version >= skipSizeVersion)
                    {
                        stream.Write((ushort)0);
                        Script.AlignSize(sizeof(ushort));
                    }

                    int memorySize = Script.MemorySize;
                    Contract.Assert(Argument1 != null);
                    Script.SerializeToken(stream, Argument1);

                    Contract.Assert(Argument2 != null);
                    Script.SerializeToken(stream, Argument2);

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        Script.SerializeToken(stream, EndToken);
                    }

                    if (stream.Version >= skipSizeVersion)
                    {
                        SkipSize = (ushort)(Script.MemorySize - memorySize);
                        using (stream.Peek(skipSizePeek))
                        {
                            stream.Write(SkipSize);
                        }
                    }
                }

                protected string DecompileOneParamMethod(string functionName, UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    string context = DecompileNext(decompiler);
                    string param1 = DecompileNext(decompiler);

                    if (EndToken != null)
                    {
                        // EndParms
                        AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);
                    }

                    return $"{context}.{functionName}({param1})";
                }

                protected string DecompileTwoParamMethod(string functionName, UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    string context = DecompileNext(decompiler);
                    string param1 = DecompileNext(decompiler);
                    string param2 = DecompileNext(decompiler);

                    if (EndToken != null)
                    {
                        // EndParms
                        AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);
                    }

                    return $"{context}.{functionName}({param1}, {param2})";
                }
            }

            [ExprToken(ExprToken.DynArrayFind)]
            public class DynamicArrayFindToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(
                        stream, (uint)PackageObjectLegacyVersion.SkipSizeAddedToArrayFindTokenIntrinsics);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeOneParamMethodWithSkip(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileOneParamMethod("Find", decompiler);
                }
            }

            [ExprToken(ExprToken.DynArrayFindStruct)]
            public class DynamicArrayFindStructToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(
                        stream, (uint)PackageObjectLegacyVersion.SkipSizeAddedToArrayFindTokenIntrinsics);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeOneParamMethodWithSkip(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileTwoParamMethod("Find", decompiler);
                }
            }

            [ExprToken(ExprToken.DynArraySort)]
            public class DynamicArraySortToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeOneParamMethodWithSkip(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileOneParamMethod("Sort", decompiler);
                }
            }

            [ExprToken(ExprToken.DynArrayAdd)]
            public class DynamicArrayAddToken : DynamicArrayMethodToken
            {
                // Ugly copy, but this is the only array token that always has an EndParms regardless of engine version.
                public override void Deserialize(IUnrealStream stream)
                {
                    // Array
                    Expression = Script.DeserializeNextToken(stream);

                    // Param 1
                    Argument1 = Script.DeserializeNextToken(stream);

                    // EndParms
                    EndToken = Script.DeserializeNextToken(stream);

                    Script.DeserializeNextDebugToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);

                    Contract.Assert(Argument1 != null);
                    Script.SerializeToken(stream, Argument1);

                    Contract.Assert(EndToken != null);
                    Script.SerializeToken(stream, EndToken);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    string context = DecompileNext(decompiler);
                    string param1 = DecompileNext(decompiler);

                    AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

                    return $"{context}.Add({param1})";
                }
            }

            [ExprToken(ExprToken.DynArrayAddItem)]
            public class DynamicArrayAddItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeOneParamMethodWithSkip(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileOneParamMethod("AddItem", decompiler);
                }
            }

            [ExprToken(ExprToken.DynArrayInsert)]
            public class DynamicArrayInsertToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeTwoParamMethodNoSkip(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeTwoParamMethodNoSkip(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileTwoParamMethod("Insert", decompiler);
                }
            }

            [ExprToken(ExprToken.DynArrayInsertItem)]
            public class DynamicArrayInsertItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeTwoParamMethodWithSkip(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeTwoParamMethodWithSkip(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileTwoParamMethod("InsertItem", decompiler);
                }
            }

            [ExprToken(ExprToken.DynArrayRemove)]
            public class DynamicArrayRemoveToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeTwoParamMethodNoSkip(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeTwoParamMethodNoSkip(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileTwoParamMethod("Remove", decompiler);
                }
            }

            [ExprToken(ExprToken.DynArrayRemoveItem)]
            public class DynamicArrayRemoveItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeOneParamMethodWithSkip(stream);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileOneParamMethod("RemoveItem", decompiler);
                }
            }
        }
    }
}