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
            Script.AlignSize(sizeof(float));

            Y = stream.ReadFloat();
            Script.AlignSize(sizeof(float));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(X);
            Script.AlignSize(sizeof(float));

            stream.Write(Y);
            Script.AlignSize(sizeof(float));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"vectxy({PropertyDisplay.FormatLiteral(X)}, {PropertyDisplay.FormatLiteral(Y)})";
        }
    }
}