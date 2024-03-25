using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE2.DVS.Tokens
{
    [ExprToken(ExprToken.StructConst)]
    public sealed class ColorConstToken : UStruct.UByteCodeDecompiler.StructConstToken
    {
        public UColor Color;

        public override void Deserialize(IUnrealStream stream)
        {
            stream.ReadStruct(out Color);
            Decompiler.AlignSize(4);
        }

        public override string Decompile()
        {
            return $"col({Color.R},{Color.G},{Color.B},{Color.A})";
        }
    }
}
