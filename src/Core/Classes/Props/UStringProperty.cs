using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Fixed String
    ///
    /// UE1 Only
    /// </summary>
    [UnrealRegisterClass]
    public class UStringProperty : UProperty
    {
        public int Size;

        /// <summary>
        /// Creates a new instance of the UELib.Core.UStringProperty class.
        /// </summary>
        public UStringProperty()
        {
            Type = PropertyType.StringProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            Size = _Buffer.ReadInt32();
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "string[" + Size + "]";
        }
    }
}