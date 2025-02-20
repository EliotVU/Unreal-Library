using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UAudioComponent/Engine.AudioComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UAudioComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements USplineAudioComponent/Engine.SplineAudioComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USplineAudioComponent : UAudioComponent
    {
    }

    /// <summary>
    ///     Implements UMultiCueSplineAudioComponent/Engine.MultiCueSplineAudioComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UMultiCueSplineAudioComponent : USplineAudioComponent
    {
    }

    /// <summary>
    ///     Implements USimpleSplineAudioComponent/Engine.SimpleSplineAudioComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USimpleSplineAudioComponent : USplineAudioComponent
    {
    }

    /// <summary>
    ///     Implements USimpleSplineNonLoopAudioComponent/Engine.SimpleSplineNonLoopAudioComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USimpleSplineNonLoopAudioComponent : USimpleSplineAudioComponent
    {
    }
}
