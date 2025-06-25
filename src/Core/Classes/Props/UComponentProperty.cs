using UELib.Branch;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Component Property
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
    public class UComponentProperty : UObjectProperty
    {
        /// <summary>
        /// Creates a new instance of the UELib.Core.UComponentProperty class.
        /// </summary>
        public UComponentProperty()
        {
            Type = PropertyType.ComponentProperty;
        }
    }
}
