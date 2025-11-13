using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UBoolProperty/Core.BoolProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UBoolProperty : UProperty
    {
        public UBoolProperty()
        {
            Type = PropertyType.BoolProperty;
        }

        public override string GetFriendlyType()
        {
            return "bool";
        }
    }
}
