using System.Diagnostics;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UFixedArrayProperty/Core.FixedArrayProperty
    /// </summary>
    [UnrealRegisterClass]
    public partial class UFixedArrayProperty : UProperty
    {
        #region Serialized Members

        [StreamRecord]
        public UProperty InnerProperty { get; set; }

        [StreamRecord]
        public int Count { get; set; }

        #endregion

        public UFixedArrayProperty()
        {
            Type = PropertyType.FixedArrayProperty;
            Count = 0;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            InnerProperty = stream.ReadObject<UProperty>();
            stream.Record(nameof(InnerProperty), InnerProperty);

            Count = stream.ReadIndex();
            stream.Record(nameof(Count), Count);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            Debug.Assert(InnerProperty != null);
            stream.WriteObject(InnerProperty);

            Debug.Assert(Count > 1);
            stream.WriteIndex(Count);
        }

        public override string GetFriendlyType()
        {
            return $"{InnerProperty.GetFriendlyType()}[{Count}]";
        }
    }
}
