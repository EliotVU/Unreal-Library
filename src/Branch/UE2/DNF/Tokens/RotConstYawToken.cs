using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class RotConstYawToken : UStruct.UByteCodeDecompiler.Token
    {
        public int Yaw;

        public override void Deserialize(IUnrealStream stream)
        {
            Yaw = stream.ReadInt32();
            Decompiler.AlignSize(sizeof(int));
        }

        public override string Decompile()
        {
            return $"rotyaw({PropertyDisplay.FormatLiteral(Yaw)})";
        }
    }
}