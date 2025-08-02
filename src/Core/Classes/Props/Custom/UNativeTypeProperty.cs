using UELib.ObjectModel.Annotations;

#if MKKE
namespace UELib.Core
{
    /// <summary>
    /// Represents a property that can refer to native defined classes whom don't register themselves as an Unreal class, e.g. FMYCLASSNAME.
    /// </summary>
    [UnrealRegisterClass]
    public class UNativeTypeProperty : UProperty
    {
        #region Serialized Members

        [StreamRecord]
        public UName NativeTypeName { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            NativeTypeName = stream.ReadName();
            stream.Record(nameof(NativeTypeName), NativeTypeName);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.Write(NativeTypeName);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return NativeTypeName;
        }
    }
}
#endif
