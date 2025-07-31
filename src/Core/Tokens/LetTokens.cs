using System.Diagnostics.Contracts;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.Let)]
            public class LetToken : Token
            {
                public Token LeftExpression, RightExpression;

                public override void Deserialize(IUnrealStream stream)
                {
                    // A = B
                    LeftExpression = Script.DeserializeNextToken(stream);
                    RightExpression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(LeftExpression != null);
                    Script.SerializeToken(stream, LeftExpression);

                    Contract.Assert(RightExpression != null);
                    Script.SerializeToken(stream, RightExpression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();

                    return $"{DecompileNext(decompiler)} = {DecompileNext(decompiler)}";
                }
            }

            [ExprToken(ExprToken.LetBool)]
            public class LetBoolToken : LetToken;

            [ExprToken(ExprToken.LetDelegate)]
            public class LetDelegateToken : LetToken;

            [ExprToken(ExprToken.EndParmValue)]
            public class EndParmValueToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return string.Empty;
                }
            }

            [ExprToken(ExprToken.Conditional)]
            public class ConditionalToken : Token
            {
                public Token ConditionalExpression;

                public ushort LeftSkipSize { get; private set; }
                public Token LeftExpression;

                public ushort RightSkipSize { get; private set; }
                public Token RightExpression;

                public override void Deserialize(IUnrealStream stream)
                {
                    // Condition
                    ConditionalExpression = Script.DeserializeNextToken(stream);

                    // Size. Used to skip ? if Condition is False.
                    LeftSkipSize = stream.ReadUInt16();
                    Script.AlignSize(sizeof(ushort));

                    // If TRUE expression
                    LeftExpression = Script.DeserializeNextToken(stream);

                    // Size. Used to skip : if Condition is True.
                    RightSkipSize = stream.ReadUInt16();
                    Script.AlignSize(sizeof(ushort));

                    // If FALSE expression
                    RightExpression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(ConditionalExpression != null);
                    Script.SerializeToken(stream, ConditionalExpression);

                    // Size. Used to skip ? if Condition is False.
                    long leftSkipSize = stream.Position;
                    stream.Write((ushort)0);
                    Script.AlignSize(sizeof(ushort));

                    Contract.Assert(LeftExpression != null);
                    Script.SerializeToken(stream, LeftExpression);

                    using (stream.Peek(leftSkipSize))
                    {
                        LeftSkipSize = (ushort)LeftExpression.Size;
                        stream.Write(LeftSkipSize);
                    }

                    // Size. Used to skip : if Condition is True.
                    long rightSkipSize = stream.Position;
                    stream.Write((ushort)0);
                    Script.AlignSize(sizeof(ushort));

                    Contract.Assert(RightExpression != null);
                    Script.SerializeToken(stream, RightExpression);

                    using (stream.Peek(rightSkipSize))
                    {
                        RightSkipSize = (ushort)RightExpression.Size;
                        stream.Write(RightSkipSize);
                    }
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return
                        $"(({DecompileNext(decompiler)}) " +
                        $"? {DecompileNext(decompiler)} " +
                        $": {DecompileNext(decompiler)})";
                }
            }
        }
    }
}