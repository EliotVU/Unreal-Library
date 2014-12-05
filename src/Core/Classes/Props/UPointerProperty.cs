using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Pointer Property
    ///
    /// UE2 Only (UStructProperty in UE3)
    /// </summary>
    [UnrealRegisterClass]
    public class UPointerProperty : UProperty
    {
        /// <summary>
        /// Creates a new instance of the UELib.Core.UPointerProperty class.
        /// </summary>
        public UPointerProperty()
        {
            Type = PropertyType.PointerProperty;
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            return "pointer";
        }
    }
}