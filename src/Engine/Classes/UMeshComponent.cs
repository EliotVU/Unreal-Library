using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UMeshComponent/Engine.MeshComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UMeshComponent : UPrimitiveComponent
    {
    }

    /// <summary>
    ///     Implements USkeletalMeshComponent/Engine.SkeletalMeshComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USkeletalMeshComponent : UMeshComponent
    {
    }

    /// <summary>
    ///     Implements UStaticMeshComponent/Engine.StaticMeshComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UStaticMeshComponent : UModelComponent
    {
    }
}
