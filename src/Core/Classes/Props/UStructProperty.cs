using System.Diagnostics;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UStructProperty/Core.StructProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UStructProperty : UProperty
    {
        #region Serialized Members

        /// <summary>
        ///     The UStruct that this property represents.
        /// </summary>
        [StreamRecord]
        public UStruct Struct { get; set; }

        #endregion

        public UStructProperty()
        {
            Type = PropertyType.StructProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Struct = stream.ReadObject<UStruct>();
            stream.Record(nameof(Struct), Struct);

            Debug.Assert(Struct != null);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            Debug.Assert(Struct != null);
            stream.Write(Struct);
        }

        public override string GetFriendlyType()
        {
            return Struct != null ? Struct.GetFriendlyType() : "@NULL";
        }
    }
}
