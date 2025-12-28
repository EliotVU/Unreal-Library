using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UPrimitive/Engine.Primitive
    /// </summary>
    [UnrealRegisterClass]
    public class UPrimitive : UObject
    {
        #region Serialized Members

        [StreamRecord]
        public UBox BoundingBox { get; set; }

        [StreamRecord]
        public USphere BoundingSphere { get; set; }

        #endregion

        public UPrimitive()
        {
            ShouldDeserializeOnDemand = true;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            BoundingBox = stream.ReadStruct<UBox>();
            stream.Record(nameof(BoundingBox), BoundingBox);

            BoundingSphere = stream.ReadStruct<USphere>();
            stream.Record(nameof(BoundingSphere), BoundingSphere);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            stream.WriteStruct(BoundingBox);
            stream.WriteStruct(BoundingSphere);
        }
    }
}
