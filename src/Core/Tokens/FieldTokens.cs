using System.Diagnostics;
using System.Diagnostics.Contracts;
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
                public UObject Object;

                public override void Deserialize(IUnrealStream stream)
                {
                    Object = stream.ReadObject();
                    Debug.Assert(Object != null);
                    Script.AlignObjectSize();
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Object != null);
                    stream.WriteObject(Object);
                    Script.AlignObjectSize();
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler._ObjectHint = Object;

                    return Object.Name;
                }
            }

            [ExprToken(ExprToken.NativeParm)]
            public class NativeParameterToken : FieldToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
#if DEBUG
                    decompiler.MarkSemicolon();
                    decompiler.MarkCommentStatement();

                    return $"native.{base.Decompile(decompiler)}";
#else
                    return string.Empty;
#endif
                }
            }

            [ExprToken(ExprToken.InstanceVariable)]
            public class InstanceVariableToken : FieldToken;

            [ExprToken(ExprToken.LocalVariable)]
            public class LocalVariableToken : FieldToken;

            [ExprToken(ExprToken.StateVariable)]
            public class StateVariableToken : FieldToken;

            [ExprToken(ExprToken.OutVariable)]
            public class OutVariableToken : FieldToken;

            [ExprToken(ExprToken.DefaultVariable)]
            public class DefaultVariableToken : FieldToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return $"default.{base.Decompile(decompiler)}";
                }
            }

            public class UndefinedVariableToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return string.Empty;
                }
            }

            [ExprToken(ExprToken.DelegateProperty)]
            public class DelegatePropertyToken : Token
            {
                /// <summary>
                /// The function name for the delegate.
                /// 
                /// Actually the <see cref="UFunction.FriendlyName"/> when compiled, but a bug in the Unreal Engine looks up the function using its <see cref="UObject.Name"/>.
                /// </summary>
                public UName FunctionName;

                /// <summary>
                /// The generated delegate property, if any.
                /// 
                /// Null if the target function is not a delegate, or if <see cref="FunctionName"/> is <see cref="UnrealName.None"/>.
                /// </summary>
                public UDelegateProperty? Property;

                public override void Deserialize(IUnrealStream stream)
                {
                    FunctionName = Script.ReadNameAligned(stream);

                    // TODO: version. Seen in version ~648(The Ball) may have been introduced earlier, but not prior 610.
                    if (stream.Version > 610)
                    {
                        Property = stream.ReadObject<UDelegateProperty>();
                        Script.AlignObjectSize();
                    }
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Script.WriteNameAligned(stream, FunctionName);

                    if (stream.Version > 610)
                    {
                        stream.Write(Property);
                        Script.AlignObjectSize();
                    }
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return FunctionName;
                }
            }

            [ExprToken(ExprToken.DefaultParmValue)]
            public class DefaultParameterToken : Token
            {
                public ushort SkipSize { get; protected set; }

                public Token Expression, EndParmToken;

                public override void Deserialize(IUnrealStream stream)
                {
                    SkipSize = stream.ReadUInt16();
                    Script.AlignSize(sizeof(ushort));

                    Expression = Script.DeserializeNextToken(stream);
                    EndParmToken = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    long skipSizePeek = stream.Position;
                    stream.Write((ushort)0);
                    Script.AlignSize(sizeof(ushort));

                    int memorySize = Script.MemorySize;
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);

                    Contract.Assert(EndParmToken != null);
                    Script.SerializeToken(stream, EndParmToken);

                    using (stream.Peek(skipSizePeek))
                    {
                        SkipSize = (ushort)(Script.MemorySize - memorySize);
                        stream.Write(SkipSize);
                    }
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    string expression = DecompileNext(decompiler);
                    DecompileNext(decompiler); // EndParmValue

                    return expression;
                }
            }

            [ExprToken(ExprToken.BoolVariable)]
            public class BoolVariableToken : Token
            {
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileNext(decompiler);
                }
            }

            [ExprToken(ExprToken.InstanceDelegate)]
            public class InstanceDelegateToken : Token
            {
                public UName DelegateName;

                public override void Deserialize(IUnrealStream stream)
                {
                    DelegateName = Script.ReadNameAligned(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Script.WriteNameAligned(stream, DelegateName);
                }
            }
        }
    }
}