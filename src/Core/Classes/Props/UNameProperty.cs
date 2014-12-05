using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Name Property
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