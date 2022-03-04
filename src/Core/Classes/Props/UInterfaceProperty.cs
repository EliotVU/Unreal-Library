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

        public UClass InterfaceObject;
        //public UInterfaceProperty InterfaceType = null;

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

            int index = _Buffer.ReadObjectIndex();
            InterfaceObject = (UClass)GetIndexObject(index);

            //Index = _Buffer.ReadObjectIndex();
            //_InterfaceType = (UInterfaceProperty)GetIndexObject( Index );
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return InterfaceObject != null ? InterfaceObject.GetFriendlyType() : "@NULL";
        }
    }
}