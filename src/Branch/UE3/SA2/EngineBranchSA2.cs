using UELib.Branch.UE3.SA2.Tokens;
using UELib.Core.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.SA2
{
    public class EngineBranchSA2 : DefaultEngineBranch
    {
        public EngineBranchSA2(BuildGeneration generation) : base(generation)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage package)
        {
            var tokenMap = base.BuildTokenMap(package);

            tokenMap[0x2B] = typeof(Int64ConstToken);
            tokenMap[0x4C] = typeof(Tokens.DelegateFunctionToken);
            tokenMap[0x4D] = typeof(EventSubscribeToken);
            tokenMap[0x4F] = typeof(EventUnsubscribeToken);

            return tokenMap;
        }
    }
}