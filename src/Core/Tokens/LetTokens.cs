namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public class LetToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    // A = B
                    DeserializeNext();
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    return $"{DecompileNext()} = {DecompileNext()}";
                }
            }

            public class LetBoolToken : LetToken
            {
            }

            public class LetDelegateToken : LetToken
            {
            }

            public class EndParmValueToken : Token
            {
                public override string Decompile()
                {
                    return string.Empty;
                }
            }

            public class ConditionalToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    // Condition
                    DeserializeNext();

                    // Size. Used to skip ? if Condition is False.
                    stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(ushort));

                    // If TRUE expression
                    DeserializeNext();

                    // Size. Used to skip : if Condition is True.
                    stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(ushort));

                    // If FALSE expression
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return $"(({DecompileNext()}) ? {DecompileNext()} : {DecompileNext()})";
                }
            }
        }
    }
}