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
                public override void Deserialize(IUnrealStream stream)
                {
                    // Key
                    DeserializeNext();

                    // Array
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    string keyExpression = DecompileNext();
                    string primaryExpression = DecompileNext();
                    return $"{primaryExpression}[{keyExpression}]";
                }
            }

            [ExprToken(ExprToken.DynArrayElement)]
            public class DynamicArrayElementToken : ArrayElementToken
            {
            }

            [ExprToken(ExprToken.DynArrayLength)]
            public class DynamicArrayLengthToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    // Array
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return $"{DecompileNext()}.Length";
                }
            }

            public abstract class DynamicArrayMethodToken : Token
            {
                protected void DeserializeOneParamMethodWithSkip(IUnrealStream stream, uint skipSizeVersion = (uint)PackageObjectLegacyVersion.SkipSizeAddedToArrayTokenIntrinsics)
                {
                    // Array
                    DeserializeNext();

                    if (stream.Version >= skipSizeVersion)
                    {
                        // Size
                        stream.Skip(2);
                        Decompiler.AlignSize(sizeof(ushort));
                    }

                    // Param 1
                    DeserializeNext();

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        DeserializeNext();
                    }
                    
                    Decompiler.DeserializeDebugToken();
                }
                
                protected void DeserializeOneParamMethodNoSkip(IUnrealStream stream)
                {
                    // Array
                    DeserializeNext();

                    // Param 1
                    DeserializeNext();

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        DeserializeNext();
                    }

                    Decompiler.DeserializeDebugToken();
                }
                
                protected void DeserializeTwoParamMethodNoSkip(IUnrealStream stream)
                {
                    // Array
                    DeserializeNext();

                    // Param 1
                    DeserializeNext();

                    // Param 2
                    DeserializeNext();

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        DeserializeNext();
                    }

                    Decompiler.DeserializeDebugToken();
                }
                
                protected void DeserializeTwoParamMethodWithSkip(IUnrealStream stream, uint skipSizeVersion = (uint)PackageObjectLegacyVersion.SkipSizeAddedToArrayTokenIntrinsics)
                {
                    // Array
                    DeserializeNext();

                    if (stream.Version >= skipSizeVersion)
                    {
                        // Size
                        stream.Skip(2);
                        Decompiler.AlignSize(sizeof(ushort));
                    }

                    // Param 1
                    DeserializeNext();

                    // Param 2
                    DeserializeNext();

                    if (stream.Version >= (uint)PackageObjectLegacyVersion.EndTokenAppendedToArrayTokenIntrinsics)
                    {
                        // EndParms
                        DeserializeNext();
                    }
                    
                    Decompiler.DeserializeDebugToken();
                }

                protected string DecompileOneParamMethod(string functionName)
                {
                    Decompiler._CanAddSemicolon = true;
                    string context = DecompileNext();
                    string param1 = DecompileNext();
                    return $"{context}.{functionName}({param1})";
                }

                protected string DecompileTwoParamMethod(string functionName)
                {
                    Decompiler._CanAddSemicolon = true;
                    string context = DecompileNext();
                    string param1 = DecompileNext();
                    string param2 = DecompileNext();
                    return $"{context}.{functionName}({param1}, {param2})";
                }
            }

            [ExprToken(ExprToken.DynArrayFind)]
            public class DynamicArrayFindToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(stream, (uint)PackageObjectLegacyVersion.SkipSizeAddedToArrayFindTokenIntrinsics);
                }
                
                public override string Decompile()
                {
                    return DecompileOneParamMethod("Find");
                }
            }

            [ExprToken(ExprToken.DynArrayFindStruct)]
            public class DynamicArrayFindStructToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(stream, (uint)PackageObjectLegacyVersion.SkipSizeAddedToArrayFindTokenIntrinsics);
                }

                public override string Decompile()
                {
                    return DecompileTwoParamMethod("Find");
                }
            }

            [ExprToken(ExprToken.DynArraySort)]
            public class DynamicArraySortToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(stream);
                }

                public override string Decompile()
                {
                    return DecompileOneParamMethod("Sort");
                }
            }

            [ExprToken(ExprToken.DynArrayAdd)]
            public class DynamicArrayAddToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    // Array
                    DeserializeNext();

                    // Param 1
                    DeserializeNext();

                    // EndParms
                    DeserializeNext();
                    
                    Decompiler.DeserializeDebugToken();
                }

                public override string Decompile()
                {
                    return DecompileOneParamMethod("Add");
                }
            }

            [ExprToken(ExprToken.DynArrayAddItem)]
            public class DynamicArrayAddItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(stream);
                }

                public override string Decompile()
                {
                    return DecompileOneParamMethod("AddItem");
                }
            }

            [ExprToken(ExprToken.DynArrayInsert)]
            public class DynamicArrayInsertToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeTwoParamMethodNoSkip(stream);
                }

                public override string Decompile()
                {
                    return DecompileTwoParamMethod("Insert");
                }
            }

            [ExprToken(ExprToken.DynArrayInsertItem)]
            public class DynamicArrayInsertItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeTwoParamMethodWithSkip(stream);
                }

                public override string Decompile()
                {
                    return DecompileTwoParamMethod("InsertItem");
                }
            }

            [ExprToken(ExprToken.DynArrayRemove)]
            public class DynamicArrayRemoveToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeTwoParamMethodNoSkip(stream);
                }

                public override string Decompile()
                {
                    return DecompileTwoParamMethod("Remove");
                }
            }

            [ExprToken(ExprToken.DynArrayRemoveItem)]
            public class DynamicArrayRemoveItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeOneParamMethodWithSkip(stream);
                }

                public override string Decompile()
                {
                    return DecompileOneParamMethod("RemoveItem");
                }
            }
        }
    }
}
