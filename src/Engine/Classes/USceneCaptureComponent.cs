using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements USceneCaptureComponent/Engine.SceneCaptureComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USceneCaptureComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements USceneCapture2DComponent/Engine.SceneCapture2DComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USceneCapture2DComponent : USceneCaptureComponent
    {
    }

    /// <summary>
    ///     Implements USceneCapture2DHitMaskComponent/Engine.SceneCapture2DHitMaskComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USceneCapture2DHitMaskComponent : USceneCaptureComponent
    {
    }

    /// <summary>
    ///     Implements USceneCaptureCubeMapComponent/Engine.SceneCaptureCubeMapComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USceneCaptureCubeMapComponent : USceneCaptureComponent
    {
    }

    /// <summary>
    ///     Implements USceneCapturePortalComponent/Engine.SceneCapturePortalComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USceneCapturePortalComponent : USceneCaptureComponent
    {
    }

    /// <summary>
    ///     Implements USceneCaptureReflectComponent/Engine.SceneCaptureReflectComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USceneCaptureReflectComponent : USceneCaptureComponent
    {
    }
}
