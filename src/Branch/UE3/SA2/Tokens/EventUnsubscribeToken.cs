using UELib.Core;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.SA2.Tokens
{
    public class EventUnsubscribeToken : UStruct.UByteCodeDecompiler.Token
    {
        public UName EventName;

        public override void Deserialize(IUnrealStream stream)
        {
            DeserializeNext<ContextToken>();

            EventName = DeserializeNext<InstanceDelegateToken>().DelegateName;

            DeserializeNext<EndFunctionParmsToken>();
        }

        public override string Decompile()
        {
            Decompiler.MarkSemicolon();
            return $"{DecompileNext()} -= {EventName}";
        }
    }
}