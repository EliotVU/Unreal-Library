#if BIOSHOCK
namespace UELib.Core
{
    /// <summary>
    /// QWord Property
    /// </summary>
    [UnrealRegisterClass]
    public class UQWordProperty : UIntProperty
    {
        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "Qword";
        }
    }
}
#endif