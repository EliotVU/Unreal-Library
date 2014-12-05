namespace UELib.Core
{
    /// <summary>
    /// Represents a group within an actual package.
    /// e.g. Core.FONTS.ObjectName.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UPackage : UObject
    {
        public UPackage()
        {
            ShouldDeserializeOnDemand = true;
        }
    }
}