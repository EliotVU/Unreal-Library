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
            Script.AlignSize(sizeof(int));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Pitch);
            Script.AlignSize(sizeof(int));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"rotpitch({PropertyDisplay.FormatLiteral(Pitch)})";
        }
    }
}