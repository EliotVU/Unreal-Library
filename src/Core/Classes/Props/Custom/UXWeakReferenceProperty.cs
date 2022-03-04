#if BIOSHOCK
namespace UELib.Core
{
    /// <summary>
    /// WeakReference Property
    /// </summary>
    [UnrealRegisterClass]
    public class UXWeakReferenceProperty : UObjectProperty
    {
        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return $"{base.GetFriendlyType()}&";
        }
    }
}
#endif