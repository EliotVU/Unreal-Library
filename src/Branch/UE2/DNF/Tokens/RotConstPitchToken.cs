using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class RotConstPitchToken : UStruct.UByteCodeDecompiler.Token
    {
        public int Pitch;

        public override void Deserialize(IUnrealStream stream)
        {
            Pitch = stream.ReadInt32();
            Decompiler.AlignSize(sizeof(int));
        }

        public override string Decompile()
        {
            return $"rotpitch({PropertyDisplay.FormatLiteral(Pitch)})";
        }
    }
}