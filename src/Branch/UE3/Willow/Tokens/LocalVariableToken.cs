using UELib.Core;
using UELib.Core.Tokens;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.Willow.Tokens
{
    [ExprToken(ExprToken.LocalVariable)]
    public class LocalVariableToken<T> : UStruct.UByteCodeDecompiler.Token
    {
        public int LocalIndex;

        public override void Deserialize(IUnrealStream stream)
        {
            LocalIndex = stream.ReadInt32();
            Script.AlignSize(sizeof(int));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(LocalIndex);
            Script.AlignSize(sizeof(int));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return TokenFactory.CreateGeneratedName($"LOCAL_{typeof(T).Name}_{LocalIndex}");
        }
    }
}
