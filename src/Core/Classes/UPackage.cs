namespace UELib.Core
{
    /// <summary>
    ///     Implements UPackage/Core.Package
    /// 
    ///     Generally acts a group within an actual package.
    ///     e.g. Core.MyGroupName.ObjectName; or "Core" itself when used a root-package.
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