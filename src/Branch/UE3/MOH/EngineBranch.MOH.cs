using System.Linq;
using UELib.Core.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.MOH
{
    public class EngineBranchMOH : DefaultEngineBranch
    {
        public EngineBranchMOH(BuildGeneration generation) : base(generation)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);
            // FIXME: Incomplete
            var newTokenMap = new TokenMap
            {
                { 0x0C, typeof(EmptyParmToken) },
                { 0x1D, typeof(FinalFunctionToken) },
                { 0x16, typeof(EndOfScriptToken) },
                { 0x18, typeof(StringConstToken) },
                { 0x23, typeof(EndFunctionParmsToken) },
                { 0x28, typeof(NothingToken) },
                { 0x2C, typeof(LetToken) },
                { 0x31, typeof(LocalVariableToken) },
                { 0x34, typeof(JumpIfNotToken) },
                { 0x35, typeof(ReturnToken) },
                { 0x37, typeof(ReturnNothingToken) },
                { 0x3F, typeof(PrimitiveCastToken) },
                { 0x46, typeof(StructMemberToken) },
                { 0x4A, typeof(NativeParameterToken) }
            };
            return (TokenMap)tokenMap.Concat(newTokenMap);
        }
    }
}