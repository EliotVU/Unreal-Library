using UELib.Core;

namespace UELib.Branch.UE3.Willow.Core.Classes
{
    public class UFloatAttributeProperty : UFloatProperty
    {
        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            stream.Read(out UProperty v80); // UObjectProperty, AttributeStack that links to a UAttributeModifier
            stream.Record(nameof(v80), v80);

            stream.Read(out UProperty v84); // ValueBase if PropertyFlags & 0x80000000UL, AttributeVariable if PropertyFlags & 0x40000000UL
            stream.Record(nameof(v84), v84);
        }
    }
}
