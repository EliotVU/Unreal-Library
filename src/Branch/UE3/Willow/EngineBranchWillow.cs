using UELib.Branch.UE3.Willow.Core.Classes;
using UELib.Branch.UE3.Willow.Tokens;
using UELib.Core;
using UELib.Core.Tokens;
using UELib.Flags;

namespace UELib.Branch.UE3.Willow
{
    public class EngineBranchWillow : DefaultEngineBranch
    {
        public enum PropertyFlags : ulong
        {
            //Unknown = 0x40000000UL, // Always applied to the attribute derived properties, sometimes on any other, so probably not an attribute marker.
            AttributeVariable = 0x80000000UL, // Property variable that is marked with the 'attribute' specifier?
        }

        public EngineBranchWillow(BuildGeneration generation) : base(generation)
        {
        }

        // TODO: AttributeNotify flag?

        public override void PostDeserializePackage(UnrealPackage package, IUnrealStream stream)
        {
            base.PostDeserializePackage(package, stream);

            if (package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                package.Build == UnrealPackage.GameBuild.BuildName.Battleborn ||
                package.Build == UnrealPackage.GameBuild.BuildName.ACM)
            {
                package.AddClassType("ByteAttributeProperty", typeof(UByteAttributeProperty));
                package.AddClassType("FloatAttributeProperty", typeof(UFloatAttributeProperty));
                package.AddClassType("IntAttributeProperty", typeof(UIntAttributeProperty));
            }
        }

        protected override void SetupEnumFunctionFlags(UnrealPackage linker)
        {
            base.SetupEnumFunctionFlags(linker);

            if (linker.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                // Something else in Battleborn
                FunctionFlags[(int)FunctionFlag.K2Pure] = 0;
                FunctionFlags[(int)FunctionFlag.DLLImport] = 0;
            }
        }

        protected override TokenMap BuildTokenMap(UnrealPackage package)
        {
            var tokenMap = base.BuildTokenMap(package);

            if (package.Build == UnrealPackage.GameBuild.BuildName.Borderlands)
            {
                // Replaces DynamicArraySortToken
                tokenMap[0x59] = typeof(AttributeVariableToken);
                // Replaces FilterEditorOnlyToken
                tokenMap[0x5A] = typeof(LetAttributeToken);
            }
            else if (package.Build == UnrealPackage.GameBuild.BuildName.Borderlands_GOTYE)
            {
                tokenMap[0x5B] = typeof(AttributeVariableToken);
                tokenMap[0x5C] = typeof(LetAttributeToken);
            }
            else if (
                package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                package.Build == UnrealPackage.GameBuild.BuildName.Battleborn ||
                package.Build == UnrealPackage.GameBuild.BuildName.ACM)
            {
                tokenMap[0x4C] = typeof(LocalVariableToken<int>);
                tokenMap[0x4D] = typeof(LocalVariableToken<float>);
                tokenMap[0x4E] = typeof(LocalVariableToken<byte>);
                tokenMap[0x4F] = typeof(LocalVariableToken<bool>);
                tokenMap[0x50] = typeof(LocalVariableToken<UObject>);
                // FIXME: Wrong, is there really even a dynamic type???
                //tokenMap[0x51] = typeof(LocalVariableToken<dynamic>);

                // Same serialization route as 0x0, 0x1, 0x2, 0x20 and 0x48
                tokenMap[0x5E] = typeof(AttributeVariableToken);
                tokenMap[0x5F] = typeof(LetAttributeToken);
            }

            if (package.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                // 0x5A = FilterEditorOnlyToken
                tokenMap[0x5B] = typeof(ScriptConversionConstToken);
            }

            return tokenMap;
        }
    }
}
