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
            Decompiler.AlignSize(sizeof(float));
        }

        public override string Decompile()
        {
            return $"vectz({PropertyDisplay.FormatLiteral(Z)})";
        }
    }
}