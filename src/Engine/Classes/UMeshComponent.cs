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
    public class UStaticMeshComponent : UMeshComponent
    {
    }

    /// <summary>
    ///     Implements UFracturedBaseComponent/Engine.FracturedBaseComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFracturedBaseComponent : UStaticMeshComponent
    {
    }

    /// <summary>
    ///     Implements UFracturedSkinnedMeshComponent/Engine.FracturedSkinnedMeshComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFracturedSkinnedMeshComponent : UFracturedBaseComponent
    {
    }

    /// <summary>
    ///     Implements UFracturedStaticMeshComponent/Engine.FracturedStaticMeshComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFracturedStaticMeshComponent : UFracturedBaseComponent
    {
    }

    /// <summary>
    ///     Implements UImageBasedReflectionComponent/Engine.ImageBasedReflectionComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UImageBasedReflectionComponent : UStaticMeshComponent
    {
    }

    /// <summary>
    ///     Implements UInstancedStaticMeshComponent/Engine.InstancedStaticMeshComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UInstancedStaticMeshComponent : UStaticMeshComponent
    {
    }


    /// <summary>
    ///     Implements UInteractiveFoliageComponent/Engine.InteractiveFoliageComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UInteractiveFoliageComponent : UStaticMeshComponent
    {
    }

    /// <summary>
    ///     Implements USplineMeshComponent/Engine.SplineMeshComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USplineMeshComponent : UStaticMeshComponent
    {
    }
}
