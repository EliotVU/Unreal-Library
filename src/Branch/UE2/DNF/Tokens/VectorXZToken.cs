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
            Script.AlignSize(sizeof(float));

            Z = stream.ReadFloat();
            Script.AlignSize(sizeof(float));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(X);
            Script.AlignSize(sizeof(float));

            stream.Write(Z);
            Script.AlignSize(sizeof(float));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"vectxz({PropertyDisplay.FormatLiteral(X)}, {PropertyDisplay.FormatLiteral(Z)})";
        }
    }
}