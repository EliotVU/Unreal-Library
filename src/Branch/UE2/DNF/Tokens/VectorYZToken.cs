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
            Script.AlignSize(sizeof(float));

            Z = stream.ReadFloat();
            Script.AlignSize(sizeof(float));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Y);
            Script.AlignSize(sizeof(float));

            stream.Write(Z);
            Script.AlignSize(sizeof(float));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"vectyz({PropertyDisplay.FormatLiteral(Y)}, {PropertyDisplay.FormatLiteral(Z)})";
        }
    }
}