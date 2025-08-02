using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UIntProperty/Core.IntProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UIntProperty : UProperty
    {
        public UIntProperty()
        {
            Type = PropertyType.IntProperty;
        }

        public override string GetFriendlyType()
        {
            return "int";
        }
    }
}