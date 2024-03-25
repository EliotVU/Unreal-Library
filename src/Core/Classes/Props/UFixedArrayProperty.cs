using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Fixed Array Property
    /// </summary>
    [UnrealRegisterClass]
    public class UFixedArrayProperty : UProperty
    {
        #region Serialized Members

        public UProperty InnerProperty;
        public int Count;

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UFixedArrayProperty class.
        /// </summary>
        public UFixedArrayProperty()
        {
            Count = 0;
            Type = PropertyType.FixedArrayProperty;
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
