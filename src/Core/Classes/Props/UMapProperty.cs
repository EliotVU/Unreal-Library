using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UMapProperty/Core.MapProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UMapProperty : UProperty
    {
        #region Serialized Members

        public UProperty? KeyProperty { get; set; }
        public UProperty? ValueProperty { get; set; }

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

            KeyProperty = _Buffer.ReadObject<UProperty?>();
            Record(nameof(KeyProperty), KeyProperty);
            
            ValueProperty = _Buffer.ReadObject<UProperty?>();
            Record(nameof(ValueProperty), ValueProperty);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            if (KeyProperty == null || ValueProperty == null)
            {
                return "map{VOID,VOID}";
            }
            
            return $"map<{KeyProperty.GetFriendlyType()}, {ValueProperty.GetFriendlyType()}>";
        }
    }
}
