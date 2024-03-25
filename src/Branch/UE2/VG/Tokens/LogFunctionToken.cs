using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE2.VG.Tokens
{
    [ExprToken(ExprToken.NativeFunction)]
    public class LogFunctionToken : UStruct.UByteCodeDecompiler.FunctionToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // NothingToken(0x0B) twice
            DeserializeCall(stream);
        }

        public override string Decompile()
        {
            Decompiler.MarkSemicolon();
            // FIXME: Reverse-order of params?
            return DecompileCall("log");
        }
    }
}
