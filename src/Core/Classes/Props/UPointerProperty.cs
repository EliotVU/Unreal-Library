using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Pointer Property
    ///
    /// UE2 Only (UStructProperty in UE3)
    /// </summary>
    [UnrealRegisterClass]
    public class UPointerProperty : UProperty
    {
#if DNF
        public UName PointerType;
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
                PointerType = _Buffer.ReadNameReference();
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