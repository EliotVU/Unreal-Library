using UELib.Core;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class RotConstBytesToken : UStruct.UByteCodeDecompiler.RotationConstToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // FIXME: These must have been compressed right?
            Value.Pitch = stream.ReadByte(); 
            Decompiler.AlignSize(sizeof(byte));
            Value.Roll = stream.ReadByte(); 
            Decompiler.AlignSize(sizeof(byte));
            Value.Yaw = stream.ReadByte(); 
            Decompiler.AlignSize(sizeof(byte));
        }
    }
}