using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorXZToken : UStruct.UByteCodeDecompiler.Token
    {
        public float X, Z;

        public override void Deserialize(IUnrealStream stream)
        {
            X = stream.ReadFloat();
            Decompiler.AlignSize(sizeof(float));

            Z = stream.ReadFloat();
            Decompiler.AlignSize(sizeof(float));
        }

        public override string Decompile()
        {
            return $"vectxz({PropertyDisplay.FormatLiteral(X)}, {PropertyDisplay.FormatLiteral(Z)})";
        }
    }
}