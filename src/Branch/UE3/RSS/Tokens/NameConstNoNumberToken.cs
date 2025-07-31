using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.RSS.Tokens
{
    [ExprToken(ExprToken.NameConst)]
    public class NameConstNoNumberToken : UStruct.UByteCodeDecompiler.NameConstToken
    {
        public UName Name;

        public override void Deserialize(IUnrealStream stream)
        {
            Name = Script.ReadNumberlessNameAligned(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            Script.WriteNumberlessNameAligned(stream, Name);
        }
    }
}
