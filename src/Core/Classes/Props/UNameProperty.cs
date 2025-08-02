using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UNameProperty/Core.NameProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UNameProperty : UProperty
    {
        public UNameProperty()
        {
            Type = PropertyType.NameProperty;
        }

        public override string GetFriendlyType()
        {
            return "name";
        }
    }
}
