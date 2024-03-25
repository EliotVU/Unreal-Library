using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorXYToken : UStruct.UByteCodeDecompiler.Token
    {
        public float X, Y;

        public override void Deserialize(IUnrealStream stream)
        {
            X = stream.ReadFloat();
            Decompiler.AlignSize(sizeof(float));

            Y = stream.ReadFloat();
            Decompiler.AlignSize(sizeof(float));
        }

        public override string Decompile()
        {
            return $"vectxy({PropertyDisplay.FormatLiteral(X)}, {PropertyDisplay.FormatLiteral(Y)})";
        }
    }
}