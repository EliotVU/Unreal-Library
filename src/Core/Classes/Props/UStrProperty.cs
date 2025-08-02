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
        public UStrProperty()
        {
            Type = PropertyType.StrProperty;
        }

        public override string GetFriendlyType()
        {
            return "string";
        }
    }
}
