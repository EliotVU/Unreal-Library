namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            private const uint ArrayMethodEndParmsVersion = 648; // TODO: Corrigate Version

            private const uint
                ArrayMethodSizeParmsVersion = 480; // TODO: Corrigate Version   (Definitely before 490(GoW))

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

            public class DynamicArrayElementToken : ArrayElementToken
            {
            }

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

            // TODO:Byte code of this has apparently changed to ReturnNothing in UE3
            public class DynamicArrayMethodToken : Token
            {
                protected virtual void DeserializeMethodOne(IUnrealStream stream, bool skipEndParms = false)
                {
                    // Array
                    DeserializeNext();

                    if (stream.Version > ArrayMethodSizeParmsVersion)
                    {
                        // Size
                        stream.Skip(2);
                        Decompiler.AlignSize(sizeof(ushort));
                    }

                    // Param 1
                    DeserializeNext();

                    if (stream.Version > ArrayMethodEndParmsVersion && !skipEndParms)
                    {
                        // EndParms
                        DeserializeNext();
                    }
                }

                protected virtual void DeserializeMethodTwo(IUnrealStream stream, bool skipEndParms = false)
                {
                    // Array
                    DeserializeNext();

                    if (stream.Version > ArrayMethodSizeParmsVersion)
                    {
                        // Size
                        stream.Skip(2);
                        Decompiler.AlignSize(sizeof(ushort));
                    }

                    // Param 1
                    DeserializeNext();

                    // Param 2
                    DeserializeNext();

                    if (stream.Version > ArrayMethodEndParmsVersion && !skipEndParms)
                    {
                        // EndParms
                        DeserializeNext();
                    }
                }

                protected string DecompileMethodOne(string functionName, bool skipEndParms = false)
                {
                    Decompiler._CanAddSemicolon = true;
                    return DecompileNext() + "." + functionName +
                           "(" + DecompileNext() + (Package.Version > ArrayMethodEndParmsVersion && !skipEndParms
                               ? DecompileNext()
                               : ")");
                }

                protected string DecompileMethodTwo(string functionName, bool skipEndParms = false)
                {
                    Decompiler._CanAddSemicolon = true;
                    return DecompileNext() + "." + functionName +
                           "(" + DecompileNext() + ", " + DecompileNext() +
                           (Package.Version > ArrayMethodEndParmsVersion && !skipEndParms ? DecompileNext() : ")");
                }
            }

            public class DynamicArrayFindToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeMethodOne(stream);
                }

                public override string Decompile()
                {
                    return DecompileMethodOne("Find");
                }
            }

            public class DynamicArrayFindStructToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeMethodTwo(stream);
                }

                public override string Decompile()
                {
                    return DecompileMethodTwo("Find");
                }
            }

            public class DynamicArraySortToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeMethodOne(stream);
                }

                public override string Decompile()
                {
                    return DecompileMethodOne("Sort");
                }
            }

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
                }

                public override string Decompile()
                {
                    return DecompileMethodOne("Add");
                }
            }

            public class DynamicArrayAddItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeMethodOne(stream);
                }

                public override string Decompile()
                {
                    return DecompileMethodOne("AddItem");
                }
            }

            public class DynamicArrayInsertToken : DynamicArrayMethodToken
            {
                protected override void DeserializeMethodTwo(IUnrealStream stream, bool skipEndParms = false)
                {
                    // Array
                    DeserializeNext();

                    // Param 1
                    DeserializeNext();

                    // Param 2
                    DeserializeNext();

                    if (stream.Version > ArrayMethodEndParmsVersion && !skipEndParms)
                    {
                        // EndParms
                        DeserializeNext();
                    }
                }

                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeMethodTwo(stream);
                }

                public override string Decompile()
                {
                    return DecompileMethodTwo("Insert");
                }

                //(0x033) DynamicArrayInsertToken -> LocalVariableToken -> EndFunctionParmsToken
            }

            public class DynamicArrayInsertItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeMethodTwo(stream);
                }

                public override string Decompile()
                {
                    return DecompileMethodTwo("InsertItem");
                }
            }

            public class DynamicArrayRemoveToken : DynamicArrayMethodToken
            {
                protected override void DeserializeMethodTwo(IUnrealStream stream, bool skipEndParms = false)
                {
                    // Array
                    DeserializeNext();

                    // Param 1
                    DeserializeNext();

                    // Param 2
                    DeserializeNext();

                    if (stream.Version > ArrayMethodEndParmsVersion && !skipEndParms)
                    {
                        // EndParms
                        DeserializeNext();
                    }
                }

                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeMethodTwo(stream);
                }

                public override string Decompile()
                {
                    return DecompileMethodTwo("Remove");
                }
            }

            public class DynamicArrayRemoveItemToken : DynamicArrayMethodToken
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeMethodOne(stream);
                }

                public override string Decompile()
                {
                    return DecompileMethodOne("RemoveItem");
                }
            }
        }
    }
}