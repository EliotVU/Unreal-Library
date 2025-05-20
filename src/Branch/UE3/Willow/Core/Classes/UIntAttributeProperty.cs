using UELib.Core;

namespace UELib.Branch.UE3.Willow.Core.Classes
{
    public class UIntAttributeProperty : UIntProperty
    {
        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            stream.Read(out UProperty v80);
            stream.Record(nameof(v80), v80);

            stream.Read(out UProperty v84);
            stream.Record(nameof(v84), v84);
        }
    }
}
