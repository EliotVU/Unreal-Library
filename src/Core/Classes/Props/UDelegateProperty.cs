using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Delegate Property
    ///
    /// UE2+
    /// </summary>
    [UnrealRegisterClass]
    public class UDelegateProperty : UProperty
    {
        #region Serialized Members

        public UObject FunctionObject;
        public UObject DelegateObject;

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UDelegateProperty class.
        /// </summary>
        public UDelegateProperty()
        {
            Type = PropertyType.DelegateProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            FunctionObject = GetIndexObject(_Buffer.ReadObjectIndex());
            if (Package.Version > 184)
            {
                DelegateObject = GetIndexObject(_Buffer.ReadObjectIndex());
            }
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return $"delegate<{GetFriendlyInnerType()}>";
        }

        public override string GetFriendlyInnerType()
        {
            return FunctionObject != null ? FunctionObject.GetFriendlyType() : "@NULL";
        }
    }
}