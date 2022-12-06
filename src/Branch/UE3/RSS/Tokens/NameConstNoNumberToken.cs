using UELib.Core;

namespace UELib.Branch.UE3.RSS.Tokens
{
    public class NameConstNoNumberToken : UStruct.UByteCodeDecompiler.NameConstToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            Name = ReadNameNoNumber(stream);
        }
    }
}