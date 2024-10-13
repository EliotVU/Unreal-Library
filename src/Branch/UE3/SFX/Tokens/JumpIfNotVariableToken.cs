using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.SFX.Tokens
{
    [ExprToken(ExprToken.JumpIfNot)]
    public class JumpIfNotVariableToken : UStruct.UByteCodeDecompiler.JumpIfNotToken
    {
        public UObject Variable;
        public byte Negation;

        public override void Deserialize(IUnrealStream stream)
        {
            stream.Read(out Variable);
            Decompiler.AlignObjectSize();

            stream.Read(out Negation);
            Decompiler.AlignSize(sizeof(byte));

            stream.Read(out CodeOffset);
            Decompiler.AlignSize(sizeof(ushort));
        }

        public override string Decompile()
        {
            Decompiler.Nester.AddNest(
                UStruct.UByteCodeDecompiler.NestManager.Nest.NestType.If,
                Position, CodeOffset,
                this
            );

            bool isNegated = Negation != 0;
            return isNegated
                ? $"if(!{Variable.Name})"
                : $"if({Variable.Name})";
        }
    }
}
