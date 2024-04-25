using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Object Reference Property
    /// </summary>
    [UnrealRegisterClass]
    public class UObjectProperty : UProperty
    {
        #region Serialized Members

        public UObject Object;

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UObjectProperty class.
        /// </summary>
        public UObjectProperty()
        {
            Type = PropertyType.ObjectProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            Object = _Buffer.ReadObject<UObject>();
            Record(nameof(Object), Object);
#if ROCKETLEAGUE
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                // version >= 17 for UComponentProperty?
                _Buffer.LicenseeVersion >= 32)
            {
                var vd0 = _Buffer.ReadNameReference();
                Record(nameof(vd0), vd0);
            }
#endif
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return Object != null ? Object.GetFriendlyType() : "@NULL";
        }
    }
}
