using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UNameProperty/Core.NameProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UNameProperty : UProperty
    {
        /// <summary>
        ///	Creates a new instance of the UELib.Core.UNameProperty class.
        /// </summary>
        public UNameProperty()
        {
            Type = PropertyType.NameProperty;
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "name";
        }
    }
}
