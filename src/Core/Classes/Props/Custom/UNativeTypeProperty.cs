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
        
        public UName NativeTypeName { get; set; }
        
        #endregion

        protected override void Deserialize()
        {
            base.Deserialize();

            NativeTypeName = _Buffer.ReadName();
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return NativeTypeName;
        }
    }
}
#endif
