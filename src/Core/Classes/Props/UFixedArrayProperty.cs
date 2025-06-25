using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UFixedArrayProperty/Core.FixedArrayProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UFixedArrayProperty : UProperty
    {
        #region Serialized Members

        public UProperty InnerProperty { get; set; }
        public int Count { get; set; }

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UFixedArrayProperty class.
        /// </summary>
        public UFixedArrayProperty()
        {
            Type = PropertyType.FixedArrayProperty;
            Count = 0;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            InnerProperty = _Buffer.ReadObject<UProperty>();
            Record(nameof(InnerProperty), InnerProperty);
            
            Count = _Buffer.ReadIndex();
            Record(nameof(Count), Count);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return $"{InnerProperty.GetFriendlyType()}[{Count}]";
        }
    }
}
