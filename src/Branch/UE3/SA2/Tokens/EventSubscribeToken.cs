using UELib.Core;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.SA2.Tokens
{
    public class EventSubscribeToken : Token
    {
        public Token LeftExpression;
        public InstanceDelegateToken RightExpression;
        private Token _EndParms;

        public override void Deserialize(IUnrealStream stream)
        {
            LeftExpression = Script.DeserializeNextToken(stream);
            RightExpression = Script.DeserializeNextToken<InstanceDelegateToken>(stream);
            _EndParms = Script.DeserializeNextToken<EndFunctionParmsToken>(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            Script.SerializeToken(stream, LeftExpression);
            Script.SerializeToken(stream, RightExpression);
            Script.SerializeToken(stream, _EndParms);
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            decompiler.MarkSemicolon();

            var leftExpression = DecompileNext(decompiler);
            AssertSkipCurrentToken<InstanceDelegateToken>(decompiler);
            AssertSkipCurrentToken<EndFunctionParmsToken>(decompiler);

            return $"{leftExpression} += {RightExpression.DelegateName}";
        }
    }
}
