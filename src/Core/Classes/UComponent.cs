using UELib.Annotations;
using UELib.Branch;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UComponent/Core.Component
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UComponent : UObject
    {
        [CanBeNull] public UClass TemplateOwnerClass;
        public UName TemplateName;

        public UComponent()
        {
            ShouldDeserializeOnDemand = true;
        }
    }

    /// <summary>
    ///     Implements UDistributionFloat/Core.DistributionFloat
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloat : UComponent
    {
    }

    /// <summary>
    ///     Implements UDistributionVector/Core.DistributionVector
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVector : UComponent
    {
    }
}
