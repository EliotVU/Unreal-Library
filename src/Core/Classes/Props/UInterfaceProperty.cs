using UELib.Branch;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UInterfaceProperty/Core.InterfaceProperty
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
    public class UInterfaceProperty : UProperty
    {
        #region Serialized Members

        public UClass InterfaceClass { get; set; }

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UInterfaceProperty class.
        /// </summary>
        public UInterfaceProperty()
        {
            Type = PropertyType.InterfaceProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            InterfaceClass = _Buffer.ReadObject<UClass>();
            Record(nameof(InterfaceClass), InterfaceClass);
#if ROCKETLEAGUE
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                _Buffer.LicenseeVersion >= 32)
            {
                var vd0 = _Buffer.ReadName();
                Record(nameof(vd0), vd0);
            }
#endif
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return InterfaceClass != null ? InterfaceClass.GetFriendlyType() : "@NULL";
        }
    }
}
