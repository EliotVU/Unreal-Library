using UELib.Core.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.APB
{
    public class EngineBranchAPB : DefaultEngineBranch
    {
        public EngineBranchAPB(BuildGeneration generation) : base(generation)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);
            if (linker.LicenseeVersion < 32) return tokenMap;

            // Verified against the GNatives table of APB 1.9.0 (547/032):
            // the layout is the regular UE3 one, except for three swapped pairs.
            tokenMap[0x00] = typeof(ReturnToken);
            tokenMap[0x04] = typeof(LocalVariableToken);

            tokenMap[0x06] = typeof(JumpIfNotToken);
            tokenMap[0x07] = typeof(JumpToken);

            tokenMap[0x0A] = typeof(NothingToken);
            tokenMap[0x0B] = typeof(CaseToken);

            return tokenMap;
        }
    }
}