using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Object Reference Property
    /// </summary>
    [UnrealRegisterClass]
    public class UObjectProperty : UProperty
    {
        #region Serialized Members

        public UObject Object { get; private set; }

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UObjectProperty class.
        /// </summary>
        public UObjectProperty()
        {
            Type = PropertyType.ObjectProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            int objectIndex = _Buffer.ReadObjectIndex();
            Object = GetIndexObject(objectIndex);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return Object != null ? Object.GetFriendlyType() : "@NULL";
        }
    }
}