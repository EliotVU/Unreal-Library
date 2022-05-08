namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public abstract class FieldToken : Token
            {
                public UObject Object { get; private set; }
                public static UObject LastField { get; internal set; }

                public override void Deserialize(IUnrealStream stream)
                {
                    Object = stream.ReadObject();
                    Decompiler.AlignObjectSize();
                }

                public override string Decompile()
                {
                    LastField = Object;
                    return Object != null ? Object.Name : "@NULL";
                }
            }

            public class NativeParameterToken : FieldToken
            {
                public override string Decompile()
                {
#if DEBUG
                    Decompiler._CanAddSemicolon = true;
                    Decompiler._MustCommentStatement = true;
                    return $"native.{base.Decompile()}";
#else
                    return string.Empty;
#endif
                }
            }

            public class InstanceVariableToken : FieldToken
            {
            }

            public class LocalVariableToken : FieldToken
            {
            }

            public class StateVariableToken : FieldToken
            {
            }

            public class OutVariableToken : FieldToken
            {
            }

            public class DefaultVariableToken : FieldToken
            {
                public override string Decompile()
                {
                    return $"default.{base.Decompile()}";
                }
            }

            public class DynamicVariableToken : Token
            {
                protected int LocalIndex;

                public override void Deserialize(IUnrealStream stream)
                {
                    LocalIndex = stream.ReadInt32();
                    Decompiler.AlignSize(sizeof(int));
                }

                public override string Decompile()
                {
                    return $"UnknownLocal_{LocalIndex}";
                }
            }

            public class UndefinedVariableToken : Token
            {
                public override string Decompile()
                {
                    return string.Empty;
                }
            }

            public class DelegatePropertyToken : FieldToken
            {
                public UName PropertyName;

                public override void Deserialize(IUnrealStream stream)
                {
                    if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.MOHA)
                    {
                        Decompiler.AlignSize(sizeof(int));
                    }

                    PropertyName = stream.ReadNameReference();
                    Decompiler.AlignNameSize();

                    // TODO: Corrigate version. Seen in version ~648(The Ball) may have been introduced earlier, but not prior 610.
                    if (stream.Version > 610)
                    {
                        base.Deserialize(stream);
                    }
                }

                public override string Decompile()
                {
                    return PropertyName;
                }
            }

            public class DefaultParameterToken : Token
            {
                internal static int _NextParamIndex;

                private UField _NextParam
                {
                    get
                    {
                        try
                        {
                            return ((UFunction)Decompiler._Container).Params[_NextParamIndex++];
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }

                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadUInt16(); // Size
                    Decompiler.AlignSize(sizeof(ushort));

                    if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.MOHA)
                    {
                        Decompiler.AlignSize(sizeof(ushort));
                    }

                    DeserializeNext(); // Expression
                    DeserializeNext(); // EndParmValue
                }

                public override string Decompile()
                {
                    string expression = DecompileNext();
                    DecompileNext(); // EndParmValue
                    Decompiler._CanAddSemicolon = true;
                    var param = _NextParam;
                    string paramName = param != null ? param.Name : $"@UnknownOptionalParam_{_NextParamIndex - 1}";
                    return $"{paramName} = {expression}";
                }
            }

            public class BoolVariableToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return DecompileNext();
                }
            }

            public class InstanceDelegateToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadNameIndex();
                    Decompiler.AlignNameSize();
                }
            }
        }
    }
}