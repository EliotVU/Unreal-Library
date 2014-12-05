using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Dynamic String
    /// </summary>
    [UnrealRegisterClass]
    public class UStrProperty : UProperty
    {
        /// <summary>
        /// Creates a new instance of the UELib.Core.UStrProperty class.
        /// </summary>
        public UStrProperty()
        {
            Type = PropertyType.StrProperty;
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "string";
        }
    }
}