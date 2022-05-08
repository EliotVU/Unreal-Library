using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Byte Property
    /// </summary>
    [UnrealRegisterClass]
    public class UByteProperty : UProperty
    {
        #region Serialized Members

        public UEnum EnumObject;

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UByteProperty class.
        /// </summary>
        public UByteProperty()
        {
            Type = PropertyType.ByteProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            EnumObject = _Buffer.ReadObject<UEnum>();
        }

        public override string GetFriendlyType()
        {
            return EnumObject != null
                ? EnumObject.GetOuterGroup()
                : "byte";
        }
    }
}