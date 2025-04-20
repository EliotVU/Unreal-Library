using UELib.Core;

namespace UELib.Branch.UE3.Willow.Core.Classes
{
    public class UByteAttributeProperty : UByteProperty
    {
        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            stream.Read(out UProperty v84);
            stream.Record(nameof(v84), v84);

            stream.Read(out UProperty v88);
            stream.Record(nameof(v88), v88);
        }
    }
}
