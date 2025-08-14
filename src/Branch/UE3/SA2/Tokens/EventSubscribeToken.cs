using UELib.Core;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.SA2.Tokens
{
    public class EventSubscribeToken : Token
    {
        public UName EventName;

        public override void Deserialize(IUnrealStream stream)
        {
            DeserializeNext();

            EventName = DeserializeNext<InstanceDelegateToken>().DelegateName;
        }

        public override string Decompile()
        {
            Decompiler.MarkSemicolon();
            return $"{DecompileNext()} += {EventName}";
        }
    }
}