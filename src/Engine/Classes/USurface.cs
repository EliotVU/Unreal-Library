using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements USurface/Engine.Surface
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USurface : UObject, IUnrealViewable
    {
        public USurface()
        {
            ShouldDeserializeOnDemand = true;
            InternalFlags |= InternalClassFlags.LinkAttributedProperties;
        }
    }
}
