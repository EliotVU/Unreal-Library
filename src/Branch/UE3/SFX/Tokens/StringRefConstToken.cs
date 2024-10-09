using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.SFX.Tokens
{
    [ExprToken(ExprToken.StringConst)]
    public class StringRefConstToken : UStruct.UByteCodeDecompiler.Token
    {
        public int Value;
        
        public override void Deserialize(IUnrealStream stream)
        {
            stream.Read(out Value);
            Decompiler.AlignSize(sizeof(int));
        }

        public override string Decompile()
        {
            return base.Decompile();
        }
    }
}