using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Interface Property
    ///
    /// UE3 Only
    /// </summary>
    [UnrealRegisterClass]
    public class UInterfaceProperty : UProperty
    {
        #region Serialized Members

        public UClass InterfaceClass;

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UInterfaceProperty class.
        /// </summary>
        public UInterfaceProperty()
        {
            Type = PropertyType.InterfaceProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            InterfaceClass = _Buffer.ReadObject<UClass>();
            Record(nameof(InterfaceClass), InterfaceClass);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return InterfaceClass != null ? InterfaceClass.GetFriendlyType() : "@NULL";
        }
    }
}
