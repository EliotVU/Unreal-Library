using UELib.Branch;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UPointerProperty/Core.PointerProperty
    ///
    ///     Exclusive to UE2(and UE1 OldUnreal), replaced by a UStructProperty with UE3.
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE2)]
    public class UPointerProperty : UProperty
    {
        #region Serialized Members

#if DNF
        [StreamRecord, Build(UnrealPackage.GameBuild.BuildName.DNF)]
        public UName PointerType { get; set; }
#endif

        #endregion

        public UPointerProperty()
        {
            Type = PropertyType.PointerProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                PointerType = stream.ReadName();
                stream.Record(nameof(PointerType), PointerType);
            }
#endif
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                stream.Write(PointerType);
            }
#endif
        }

        public override string GetFriendlyType()
        {
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                return "pointer(" + PointerType.Name + ")";
#endif
            return "pointer";
        }
    }
}
