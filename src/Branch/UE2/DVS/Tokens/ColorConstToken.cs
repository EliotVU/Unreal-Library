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
            Script.AlignSize(4);
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.WriteStruct(Color);
            Script.AlignSize(4);
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"col({Color.R},{Color.G},{Color.B},{Color.A})";
        }
    }
}
