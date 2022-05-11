namespace UELib.Core
{
    [UnrealRegisterClass]
    public class UComponent : UObject
    {
        public UComponent()
        {
            ShouldDeserializeOnDemand = true;
        }
    }
}