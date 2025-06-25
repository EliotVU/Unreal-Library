using UELib.Branch;
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
#if DNF
        [Build(UnrealPackage.GameBuild.BuildName.DNF)]
        public UName PointerType { get; set; }
#endif

        /// <summary>
        /// Creates a new instance of the UELib.Core.UPointerProperty class.
        /// </summary>
        public UPointerProperty()
        {
            Type = PropertyType.PointerProperty;
        }

        /// <inheritdoc/>
        protected override void Deserialize()
        {
            base.Deserialize();
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                PointerType = _Buffer.ReadName();
            }
#endif
        }

        /// <inheritdoc/>
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
