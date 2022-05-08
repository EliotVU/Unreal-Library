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

        public UStruct StructObject;

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

            StructObject = (UStruct)GetIndexObject(_Buffer.ReadObjectIndex());
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return StructObject != null ? StructObject.GetFriendlyType() : "@NULL";
        }
    }
}