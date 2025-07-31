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
            Script.AlignSize(sizeof(int));
        }
        
        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Yaw);
            Script.AlignSize(sizeof(int));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"rotyaw({PropertyDisplay.FormatLiteral(Yaw)})";
        }
    }
}