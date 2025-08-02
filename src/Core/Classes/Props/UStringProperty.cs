using System.Diagnostics;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UStringProperty/Core.StringProperty
    ///     
    ///     A fixed-length string property that is used to store strings in Unreal Engine 1.
    /// </summary>
    [UnrealRegisterClass]
    public class UStringProperty : UProperty
    {
        #region Serialized Members

        /// <summary>
        ///     The fixed-length of the string in bytes.
        /// </summary>
        [StreamRecord]
        public int Size { get; set; }

        #endregion

        public UStringProperty()
        {
            Type = PropertyType.StringProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Size = stream.ReadInt32();
            stream.Record(nameof(Size), Size);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            Debug.Assert(Size > 0);
            stream.Write(Size);
        }

        public override string GetFriendlyType()
        {
            return $"string[{Size}]";
        }
    }
}
