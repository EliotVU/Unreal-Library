using UELib.Core;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorConstZeroToken : UStruct.UByteCodeDecompiler.Token
    {
        public override string Decompile()
        {
            return "vect(0, 0, 0)";
        }
    }
}