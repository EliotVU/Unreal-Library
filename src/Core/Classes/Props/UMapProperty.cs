using UELib.ObjectModel.Annotations;
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

        [StreamRecord]
        public UProperty? KeyProperty { get; set; }

        [StreamRecord]
        public UProperty? ValueProperty { get; set; }

        #endregion

        public UMapProperty()
        {
            Type = PropertyType.MapProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            KeyProperty = stream.ReadObject<UProperty?>();
            stream.Record(nameof(KeyProperty), KeyProperty);

            ValueProperty = stream.ReadObject<UProperty?>();
            stream.Record(nameof(ValueProperty), ValueProperty);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.Write(KeyProperty);
            stream.Write(ValueProperty);
        }

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
