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

        public UEnum Enum;

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

            Enum = _Buffer.ReadObject<UEnum>();
            Record(nameof(Enum), Enum);
        }

        public override string GetFriendlyType()
        {
            if (Enum != null)
            {
                // The compiler doesn't understand any non-UClass qualified identifiers.
                return Enum.Outer is UClass
                    ? $"{Enum.Outer.Name}.{Enum.Name}"
                    : Enum.Name;
            }
            return "byte";
        }
    }
}
