using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorZToken : UStruct.UByteCodeDecompiler.Token
    {
        public float Z;

        public override void Deserialize(IUnrealStream stream)
        {
            Z = stream.ReadFloat();
            Script.AlignSize(sizeof(float));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Z);
            Script.AlignSize(sizeof(float));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"vectz({PropertyDisplay.FormatLiteral(Z)})";
        }
    }
}