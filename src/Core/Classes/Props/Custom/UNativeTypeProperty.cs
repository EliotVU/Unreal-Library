#if MKKE
namespace UELib.Core
{
    /// <summary>
    /// Represents a property that can refer to native defined classes whom don't register themselves as an Unreal class, e.g. FMYCLASSNAME.
    /// </summary>
    [UnrealRegisterClass]
    public class UNativeTypeProperty : UProperty
    {
        public UName NativeTypeName;

        protected override void Deserialize()
        {
            base.Deserialize();

            NativeTypeName = _Buffer.ReadNameReference();
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return NativeTypeName;
        }
    }
}
#endif