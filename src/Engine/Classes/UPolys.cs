using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UPolys/Engine.Polys
    /// </summary>
    [Output("PolyList")]
    [UnrealRegisterClass]
    public class UPolys : UObject
    {
        public UObject? ElementOwner;

        [Output] public UArray<Poly> Element;

        public UPolys()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            // version >= 40
            int num = _Buffer.ReadInt32();
            Record(nameof(num), num);

            // version >= 40
            int max = _Buffer.ReadInt32();
            Record(nameof(max), max);

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.ElementOwnerAddedToUPolys)
            {
                ElementOwner = _Buffer.ReadObject();
                Record(nameof(ElementOwner), ElementOwner);
            }

            Element = new UArray<Poly>(num);
            if (num > 0)
            {
                _Buffer.ReadArray(out Element, num);
                Record(nameof(Element), Element);
            }
        }
    }
}
