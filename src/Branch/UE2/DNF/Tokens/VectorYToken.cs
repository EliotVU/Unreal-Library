using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorYToken : UStruct.UByteCodeDecompiler.Token
    {
        public float Y;

        public override void Deserialize(IUnrealStream stream)
        {
            Y = stream.ReadFloat();
            Decompiler.AlignSize(sizeof(float));
        }

        public override string Decompile()
        {
            return $"vecty({PropertyDisplay.FormatLiteral(Y)})";
        }
    }
}