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
            Script.AlignObjectSize();

            stream.Read(out Negation);
            Script.AlignSize(sizeof(byte));

            stream.Read(out CodeOffset);
            Script.AlignSize(sizeof(ushort));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Variable);
            Script.AlignObjectSize();

            stream.Write(Negation);
            Script.AlignSize(sizeof(byte));

            stream.Write(CodeOffset);
            Script.AlignSize(sizeof(ushort));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            decompiler.Nester.AddNest(
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
