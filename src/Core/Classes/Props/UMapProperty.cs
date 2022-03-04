using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Dynamic Map Property
    ///
    /// Obsolete
    /// </summary>
    [UnrealRegisterClass]
    public class UMapProperty : UProperty
    {
        #region Serialized Members

        private int _Key;
        private int _Value;

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UMapProperty class.
        /// </summary>
        public UMapProperty()
        {
            Type = PropertyType.MapProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            _Key = _Buffer.ReadObjectIndex();
            _Value = _Buffer.ReadObjectIndex();
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return $"map<{_Key}, {_Value}>";
        }
    }
}