using UELib.Core;
using UELib.Core.Tokens;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.BL2.Tokens
{
    [ExprToken(ExprToken.LocalVariable)]
    public class LocalVariableToken<T> : UStruct.UByteCodeDecompiler.Token
    {
        public int LocalIndex;

        public override void Deserialize(IUnrealStream stream)
        {
            LocalIndex = stream.ReadInt32();
            Decompiler.AlignSize(sizeof(int));
        }

        public override string Decompile()
        {
            return TokenFactory.CreateGeneratedName($"LOCAL_{typeof(T).Name}_{LocalIndex}");
        }
    }
}
