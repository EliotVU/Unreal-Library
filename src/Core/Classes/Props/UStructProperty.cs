using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Struct Property
    /// </summary>
    [UnrealRegisterClass]
    public class UStructProperty : UProperty
    {
        #region Serialized Members

        public UStruct Struct;

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UStructProperty class.
        /// </summary>
        public UStructProperty()
        {
            Type = PropertyType.StructProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            Struct = _Buffer.ReadObject<UStruct>();
            Record(nameof(Struct), Struct);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return Struct != null ? Struct.GetFriendlyType() : "@NULL";
        }
    }
}
