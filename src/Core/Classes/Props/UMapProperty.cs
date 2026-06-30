using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Dynamic Map Property
    /// </summary>
    [UnrealRegisterClass]
    public class UMapProperty : UProperty
    {
        #region Serialized Members

        public UProperty KeyProperty;
        public UProperty ValueProperty;

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

            KeyProperty = _Buffer.ReadObject<UProperty>();
            Record(nameof(KeyProperty), KeyProperty);
            if (KeyProperty != null && KeyProperty.DeserializationState == 0)
            {
                try { KeyProperty.Load(); }
                catch { }
            }

            ValueProperty = _Buffer.ReadObject<UProperty>();
            Record(nameof(ValueProperty), ValueProperty);
            if (ValueProperty != null && ValueProperty.DeserializationState == 0)
            {
                try { ValueProperty.Load(); }
                catch { }
            }
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
