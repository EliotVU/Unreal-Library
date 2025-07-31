﻿using UELib.Core;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class RotConstZeroToken : UStruct.UByteCodeDecompiler.Token
    {
        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return "rot(0, 0, 0)";
        }
    }
}