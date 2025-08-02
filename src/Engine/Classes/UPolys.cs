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
        #region Serialized Members

        /// <summary>
        ///     The object (like a <see cref="UBrush"/>) this polygon list belongs to.
        /// </summary>
        [StreamRecord]
        public UObject? ElementOwner { get; set; }

        /// <summary>
        ///     The polygons for this polygon list.
        /// </summary>
        [StreamRecord, Output]
        public UArray<Poly> Element { get; set; } = [];

        #endregion

        public UPolys()
        {
            ShouldDeserializeOnDemand = true;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            // version >= 40
            int elementCount = stream.ReadInt32();
            stream.Record(nameof(elementCount), elementCount);

            // version >= 40
            int elementCapacity = stream.ReadInt32();
            stream.Record(nameof(elementCapacity), elementCapacity);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.ElementOwnerAddedToUPolys)
            {
                ElementOwner = stream.ReadObject();
                stream.Record(nameof(ElementOwner), ElementOwner);
            }
#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                // Not supported
                return;
            }
#endif
            Element = stream.ReadArray<Poly>(elementCount);
            if (elementCount > 0)
            {
                stream.Record(nameof(Element), Element);
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            // version >= 40
            int elementCount = Element.Count;
            stream.Write(elementCount);

            // version >= 40
            int elementCapacity = Element.Capacity;
            stream.Write(elementCapacity);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.ElementOwnerAddedToUPolys)
            {
                stream.WriteObject(ElementOwner);
            }
#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                // Not supported
                return;
            }
#endif
            foreach (var polygon in Element)
            {
                polygon.Serialize(stream);
            }
        }
    }
}
