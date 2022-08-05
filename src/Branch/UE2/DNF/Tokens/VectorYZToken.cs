using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorYZToken : UStruct.UByteCodeDecompiler.Token
    {
        public float Y, Z;

        public override void Deserialize(IUnrealStream stream)
        {
            Y = stream.ReadFloat();
            Decompiler.AlignSize(sizeof(float));

            Z = stream.ReadFloat();
            Decompiler.AlignSize(sizeof(float));
        }

        public override string Decompile()
        {
            return $"vectyz({PropertyDisplay.FormatLiteral(Y)}, {PropertyDisplay.FormatLiteral(Z)})";
        }
    }
}