using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UStringProperty/Core.StringProperty
    ///     
    ///     A fixed-length string property that is used to store strings in Unreal Engine 1.
    /// </summary>
    [UnrealRegisterClass]
    public class UStringProperty : UProperty
    {
        public int Size { get; set; }

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
            Record(nameof(Size), Size);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return $"string[{Size}]";
        }
    }
}
