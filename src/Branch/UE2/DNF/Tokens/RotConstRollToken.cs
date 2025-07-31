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
            Script.AlignSize(sizeof(int));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Roll);
            Script.AlignSize(sizeof(int));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"rotroll({PropertyDisplay.FormatLiteral(Roll)})";
        }
    }
}