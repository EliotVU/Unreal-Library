using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorXToken : UStruct.UByteCodeDecompiler.Token
    {
        public float X;

        public override void Deserialize(IUnrealStream stream)
        {
            X = stream.ReadFloat();
            Decompiler.AlignSize(sizeof(float));
        }

        public override string Decompile()
        {
            return $"vectx({PropertyDisplay.FormatLiteral(X)})";
        }
    }
}