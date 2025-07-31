using UELib.Core;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class RotConstBytesToken : UStruct.UByteCodeDecompiler.RotationConstToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // FIXME: These must have been compressed right?
            Rotation.Pitch = stream.ReadByte(); 
            Rotation.Roll = stream.ReadByte(); 
            Rotation.Yaw = stream.ReadByte(); 
            Script.AlignSize(sizeof(byte)*3);
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write((byte)Rotation.Pitch);
            stream.Write((byte)Rotation.Roll);
            stream.Write((byte)Rotation.Yaw);
            Script.AlignSize(sizeof(byte)*3);
        }
    }
}