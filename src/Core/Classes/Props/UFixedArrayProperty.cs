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

        public UProperty InnerObject;

        public int Count { get; private set; }

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

            int innerIndex = _Buffer.ReadObjectIndex();
            InnerObject = (UProperty)GetIndexObject(innerIndex);
            Count = _Buffer.ReadIndex();
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            // Just move to decompiling?
            return $"{base.GetFriendlyType()}[{Count}]";
        }
    }
}