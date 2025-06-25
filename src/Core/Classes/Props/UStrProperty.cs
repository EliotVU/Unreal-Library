using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UStrProperty/Core.StrProperty
    ///
    ///     A dynamic string property, unlike the fixed-length UStringProperty.
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
