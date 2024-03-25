using UELib.ObjectModel.Annotations;
using UELib.Tokens;

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

            [ExprToken(ExprToken.NativeParm)]
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

            [ExprToken(ExprToken.InstanceVariable)]
            public class InstanceVariableToken : FieldToken
            {
            }

            [ExprToken(ExprToken.LocalVariable)]
            public class LocalVariableToken : FieldToken
            {
            }

            [ExprToken(ExprToken.StateVariable)]
            public class StateVariableToken : FieldToken
            {
            }

            [ExprToken(ExprToken.OutVariable)]
            public class OutVariableToken : FieldToken
            {
            }

            [ExprToken(ExprToken.DefaultVariable)]
            public class DefaultVariableToken : FieldToken
            {
                public override string Decompile()
                {
                    return $"default.{base.Decompile()}";
                }
            }

            public class UndefinedVariableToken : Token
            {
                public override string Decompile()
                {
                    return string.Empty;
                }
            }

            [ExprToken(ExprToken.DelegateProperty)]
            public class DelegatePropertyToken : FieldToken
            {
                public UName PropertyName;

                public override void Deserialize(IUnrealStream stream)
                {
                    PropertyName = ReadName(stream);

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

            [ExprToken(ExprToken.DefaultParmValue)]
            public class DefaultParameterToken : Token
            {
                public ushort Size;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    Size = stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(ushort));

                    DeserializeNext(); // Expression
                    DeserializeNext(); // EndParmValue
                }

                public override string Decompile()
                {
                    string expression = DecompileNext();
                    DecompileNext(); // EndParmValue
                    return expression;
                }
            }

            [ExprToken(ExprToken.BoolVariable)]
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

            [ExprToken(ExprToken.InstanceDelegate)]
            public class InstanceDelegateToken : Token
            {
                public UName DelegateName;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    DelegateName = ReadName(stream);
                }
            }
        }
    }
}
