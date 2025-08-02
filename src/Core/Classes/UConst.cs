using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UEnum/Core.Enum
    /// </summary>
    [UnrealRegisterClass]
    public partial class UConst : UField
    {
        #region Serialized Members

        /// <summary>
        ///     The literal value of this const.
        /// </summary>
        [StreamRecord]
        public string Value { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Value = stream.ReadString();
            stream.Record(nameof(Value), Value);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.WriteString(Value);
        }
    }
}
