using System;
using UELib.Branch.UE3.GIGANTIC.Core.Classes;
using UELib.Branch.UE3.GIGANTIC.Tokens;
using UELib.Core.Tokens;

namespace UELib.Branch.UE3.GIGANTIC
{
    public class EngineBranchGigantic : DefaultEngineBranch
    {
        [Flags]
        public enum PropertyFlags : ulong
        {
            JsonTransient = 0x80000000000U,
        }

        [Flags]
        public enum ClassFlags : ulong
        {
            JsonImport = 0x00008000U,
        }

        public EngineBranchGigantic(BuildGeneration generation) : base(BuildGeneration.UE3)
        {
        }

        public override void PostDeserializePackage(UnrealPackage package, IUnrealStream stream)
        {
            base.PostDeserializePackage(package, stream);

            package.AddClassType("JsonNodeRoot", typeof(UJsonNodeRoot));
        }

        protected override TokenMap BuildTokenMap(UnrealPackage package)
        {
            var tokenMap = base.BuildTokenMap(package);
            tokenMap[0x4D] = typeof(JsonRefVariableToken);

            return tokenMap;
        }
    }
}
