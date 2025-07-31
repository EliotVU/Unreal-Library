﻿using UELib.Core;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class BreakpointToken : UStruct.UByteCodeDecompiler.Token
    {
        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            // TODO:
            decompiler.PreComment = "// Breakpoint";
            decompiler.MarkSemicolon();

            return "@UnknownSyntax";
        }
    }
}