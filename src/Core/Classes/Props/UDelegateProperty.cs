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

            FunctionObject = _Buffer.ReadObject();
            // FIXME: Version 128-178
            if (_Buffer.Version <= 184)
            {
                return;
            }

            // FIXME: Version 374-491; Delegate source type changed from Name to Object
            if (_Buffer.Version <= 375)
            {
                _Buffer.ReadNameReference();
            }
            else
            {
                DelegateObject = _Buffer.ReadObject();
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
