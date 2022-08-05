using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class RotConstRollToken : UStruct.UByteCodeDecompiler.Token
    {
        public int Roll;

        public override void Deserialize(IUnrealStream stream)
        {
            Roll = stream.ReadInt32();
            Decompiler.AlignSize(sizeof(int));
        }

        public override string Decompile()
        {
            return $"rotroll({PropertyDisplay.FormatLiteral(Roll)})";
        }
    }
}