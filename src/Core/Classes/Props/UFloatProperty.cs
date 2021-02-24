using UELib.JsonDecompiler.Types;

namespace UELib.JsonDecompiler.Core
{
    /// <summary>
    /// Float Property
    /// </summary>
    [UnrealRegisterClass]
    public class UFloatProperty : UProperty
    {
        /// <summary>
        ///	Creates a new instance of the UELib.Core.UFloatProperty class.
        /// </summary>
        public UFloatProperty()
        {
            Type = PropertyType.FloatProperty;
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "float";
        }
    }
}