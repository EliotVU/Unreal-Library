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
            if (linker.LicenseeVersion < 32) return base.BuildTokenMap(linker);

            // FIXME: Incomplete
            var tokenMap = new TokenMap
            {
                { 0x00, typeof(ReturnToken) },
                { 0x04, typeof(LocalVariableToken) },
                { 0x06, typeof(JumpIfNotToken) },
                { 0x07, typeof(JumpToken) },
                { 0x0A, typeof(NothingToken) },
                { 0x0B, typeof(CaseToken) }
            };
            return tokenMap;
        }
    }
}