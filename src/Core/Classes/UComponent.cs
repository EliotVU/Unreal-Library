using UELib.Branch;
using UELib.Flags;
using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UComponent/Core.Component
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UComponent : UObject
    {
        #region Serialized Members

        [StreamRecord]
        public UClass? TemplateOwnerClass { get; set; }

        [StreamRecord]
        public UName TemplateName { get; set; } = UnrealName.None;

        #endregion

        public UComponent()
        {
            ShouldDeserializeOnDemand = true;
        }

        internal void DeserializeTemplate(IUnrealStream stream)
        {
            TemplateOwnerClass = stream.ReadObject<UClass?>();
            stream.Record(nameof(TemplateOwnerClass), TemplateOwnerClass);

            if (stream.Version < (uint)PackageObjectLegacyVersion.ClassDefaultCheckAddedToTemplateName
                || IsTemplate(ObjectFlag.ClassDefaultObject))
            {
                TemplateName = stream.ReadName();
                stream.Record(nameof(TemplateName), TemplateName);
            }
        }

        internal void SerializeTemplate(IUnrealStream stream)
        {
            stream.Write(TemplateOwnerClass);

            if (stream.Version < (uint)PackageObjectLegacyVersion.ClassDefaultCheckAddedToTemplateName
                || IsTemplate(ObjectFlag.ClassDefaultObject))
            {
                stream.Write(TemplateName);
            }
        }
    }

    /// <summary>
    ///     Implements UDistributionFloat/Core.DistributionFloat
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionFloat : UComponent;

    /// <summary>
    ///     Implements UDistributionVector/Core.DistributionVector
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDistributionVector : UComponent;
}
