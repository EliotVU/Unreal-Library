using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UNxForceFieldComponent/Engine.NxForceFieldComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UNxForceFieldComponent : UPrimitiveComponent
    {
    }

    /// <summary>
    ///     Implements UNxForceFieldCylindricalComponent/Engine.NxForceFieldCylindricalComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UNxForceFieldCylindricalComponent : UNxForceFieldComponent
    {
    }

    /// <summary>
    ///     Implements UNxForceFieldGenericComponent/Engine.NxForceFieldGenericComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UNxForceFieldGenericComponent : UNxForceFieldComponent
    {
    }

    /// <summary>
    ///     Implements UNxForceFieldRadialComponent/Engine.NxForceFieldRadialComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UNxForceFieldRadialComponent : UNxForceFieldComponent
    {
    }

    /// <summary>
    ///     Implements UNxForceFieldTornadoComponent/Engine.NxForceFieldTornadoComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UNxForceFieldTornadoComponent : UNxForceFieldComponent
    {
    }
}
