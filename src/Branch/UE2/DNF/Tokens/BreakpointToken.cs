using UELib.Core;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class BreakpointToken : UStruct.UByteCodeDecompiler.Token
    {
        public override string Decompile()
        {
            // TODO:
            Decompiler.PreComment = "// Breakpoint";
            Decompiler.MarkSemicolon();
            return "@UnknownSyntax";
        }
    }
}