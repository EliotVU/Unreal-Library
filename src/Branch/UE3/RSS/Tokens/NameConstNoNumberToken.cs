using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.RSS.Tokens
{
    [ExprToken(ExprToken.NameConst)]
    public class NameConstNoNumberToken : UStruct.UByteCodeDecompiler.NameConstToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            Name = ReadNameNoNumber(stream);
        }
    }
}
