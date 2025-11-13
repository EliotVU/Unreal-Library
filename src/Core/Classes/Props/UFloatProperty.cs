using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UFloatProperty/Core.FloatProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UFloatProperty : UProperty
    {
        public UFloatProperty()
        {
            Type = PropertyType.FloatProperty;
        }

        public override string GetFriendlyType()
        {
            return "float";
        }
    }
}