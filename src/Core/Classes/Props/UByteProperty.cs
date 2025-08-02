using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UByteProperty/Core.ByteProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UByteProperty : UProperty
    {
        #region Serialized Members

        /// <summary>
        ///     The enum associated with this byte property, if any.
        /// </summary>
        [StreamRecord]
        public UEnum? Enum { get; set; }

        #endregion

        public UByteProperty()
        {
            Type = PropertyType.ByteProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Enum = stream.ReadObject<UEnum>();
            stream.Record(nameof(Enum), Enum);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.Write(Enum);
        }

        public override string GetFriendlyType()
        {
            if (Enum != null)
            {
                // The compiler doesn't understand any non-UClass qualified identifiers.
                return Enum.Outer is UClass
                    ? $"{Enum.Outer.Name}.{Enum.Name}"
                    : Enum.Name;
            }

            return "byte";
        }
    }
}
