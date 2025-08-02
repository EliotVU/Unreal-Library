using UELib.Branch;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UComponentProperty/Core.ComponentProperty
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
    public class UComponentProperty : UObjectProperty
    {
        public UComponentProperty()
        {
            Type = PropertyType.ComponentProperty;
        }
    }
}