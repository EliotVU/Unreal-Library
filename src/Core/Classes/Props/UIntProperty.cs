using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Int Property
    /// </summary>
    [UnrealRegisterClass]
    public class UIntProperty : UProperty
    {
        /// <summary>
        /// Creates a new instance of the UELib.Core.UIntProperty class.
        /// </summary>
        public UIntProperty()
        {
            Type = PropertyType.IntProperty;
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "int";
        }
    }
}