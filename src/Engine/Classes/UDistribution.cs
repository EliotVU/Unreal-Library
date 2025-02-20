using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UDistributionFloatConstant/Engine.DistributionFloatConstant
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloatConstant : UDistributionFloat
    {
    }

    /// <summary>
    ///     Implements UDistributionFloatConstantCurve/Engine.DistributionFloatConstantCurve
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloatConstantCurve : UDistributionFloat
    {
    }

    /// <summary>
    ///     Implements UDistributionFloatParameterBase/Engine.DistributionFloatParameterBase
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloatParameterBase : UDistributionFloatConstant
    {
    }

    /// <summary>
    ///     Implements UDistributionFloatParticleParameter/Engine.DistributionFloatParticleParameter
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloatParticleParameter : UDistributionFloatParameterBase
    {
    }

    /// <summary>
    ///     Implements UDistributionFloatSoundParameter/Engine.DistributionFloatSoundParameter
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloatSoundParameter : UDistributionFloatParameterBase
    {
    }

    /// <summary>
    ///     Implements UDistributionFloatUniform/Engine.DistributionFloatUniform
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloatUniform : UDistributionFloat
    {
    }

    /// <summary>
    ///     Implements UDistributionFloatUniformCurve/Engine.DistributionFloatUniformCurve
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloatUniformCurve : UDistributionFloat
    {
    }

    /// <summary>
    ///     Implements UDistributionFloatUniformRange/Engine.DistributionFloatUniformRange
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloatUniformRange : UDistributionFloat
    {
    }

    /// <summary>
    ///     Implements UDistributionVectorConstant/Engine.DistributionVectorConstant
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVectorConstant : UDistributionVector
    {
    }

    /// <summary>
    ///     Implements UDistributionVectorConstantCurve/Engine.DistributionVectorConstantCurve
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVectorConstantCurve : UDistributionVector
    {
    }

    /// <summary>
    ///     Implements UDistributionVectorParameterBase/Engine.DistributionVectorParameterBase
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVectorParameterBase : UDistributionVectorConstant
    {
    }

    /// <summary>
    ///     Implements UDistributionVectorParticleParameter/Engine.DistributionVectorParticleParameter
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVectorParticleParameter : UDistributionVectorParameterBase
    {
    }

    /// <summary>
    ///     Implements UDistributionVectorUniform/Engine.DistributionVectorUniform
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVectorUniform : UDistributionVector
    {
    }

    /// <summary>
    ///     Implements UDistributionVectorUniformCurve/Engine.DistributionVectorUniformCurve
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVectorUniformCurve : UDistributionVector
    {
    }

    /// <summary>
    ///     Implements UDistributionVectorUniformRange/Engine.DistributionVectorUniformRange
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVectorUniformRange : UDistributionVector
    {
    }
}
