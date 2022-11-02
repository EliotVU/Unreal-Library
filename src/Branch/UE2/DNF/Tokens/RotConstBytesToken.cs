using UELib.Core;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class RotConstBytesToken : UStruct.UByteCodeDecompiler.RotationConstToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // FIXME: These must have been compressed right?
            Rotation.Pitch = stream.ReadByte(); 
            Decompiler.AlignSize(sizeof(byte));
            Rotation.Roll = stream.ReadByte(); 
            Decompiler.AlignSize(sizeof(byte));
            Rotation.Yaw = stream.ReadByte(); 
            Decompiler.AlignSize(sizeof(byte));
        }
    }
}