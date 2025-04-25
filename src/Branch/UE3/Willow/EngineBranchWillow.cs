using UELib.Branch.UE3.Willow.Core.Classes;
using UELib.Branch.UE3.Willow.Tokens;
using UELib.Core;
using UELib.Core.Tokens;

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

        public override void PostDeserializePackage(UnrealPackage linker, IUnrealStream stream)
        {
            base.PostDeserializePackage(linker, stream);

            if (linker.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                linker.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                linker.AddClassType("ByteAttributeProperty", typeof(UByteAttributeProperty));
                linker.AddClassType("FloatAttributeProperty", typeof(UFloatAttributeProperty));
                linker.AddClassType("IntAttributeProperty", typeof(UIntAttributeProperty));
            }
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);

            if (linker.Build == UnrealPackage.GameBuild.BuildName.Borderlands)
            {
                // Replaces DynamicArraySortToken
                tokenMap[0x59] = typeof(AttributeVariableToken);
                // Replaces FilterEditorOnlyToken
                tokenMap[0x5A] = typeof(LetAttributeToken);
            }
            else if (linker.Build == UnrealPackage.GameBuild.BuildName.Borderlands_GOTYE)
            {
                tokenMap[0x5B] = typeof(AttributeVariableToken);
                tokenMap[0x5C] = typeof(LetAttributeToken);
            }
            else if (
                linker.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                linker.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
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

            return tokenMap;
        }
    }
}
