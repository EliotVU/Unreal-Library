using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.SFX.Tokens
{
    [ExprToken(ExprToken.VirtualFunction)]
    public class NamedFunctionToken : UStruct.UByteCodeDecompiler.VirtualFunctionToken
    {
        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            decompiler.MarkSemicolon();

            return DecompileCall($"'{FunctionName}'", decompiler);
        }
    }
}
