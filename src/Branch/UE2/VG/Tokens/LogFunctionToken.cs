using UELib.Core;

namespace UELib.Branch.UE2.VG.Tokens
{
    public class LogFunctionToken : UStruct.UByteCodeDecompiler.FunctionToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // NothingToken(0x0B) twice
            DeserializeCall();
        }

        public override string Decompile()
        {
            Decompiler.MarkSemicolon();
            // FIXME: Reverse-order of params?
            return DecompileCall("log");
        }
    }
}