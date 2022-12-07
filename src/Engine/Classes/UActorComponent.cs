using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UActorComponent/Engine.ActorComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UActorComponent : UComponent
    {
    }

    /// <summary>
    ///     Implements UAudioComponent/Engine.AudioComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UAudioComponent : UActorComponent
    {
    }
}
