using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.SFX.Tokens
{
    [ExprToken(ExprToken.VirtualFunction)]
    public class NamedFunctionToken : UStruct.UByteCodeDecompiler.VirtualFunctionToken
    {
        public override string Decompile()
        {
            Decompiler.MarkSemicolon();
            
            return DecompileCall($"'{FunctionName}'");
        }
    }
}
