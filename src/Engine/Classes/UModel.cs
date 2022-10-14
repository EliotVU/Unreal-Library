namespace UELib.Core
{
    /// <summary>
    /// Implements UModel/Engine.Model
    /// </summary>
    [UnrealRegisterClass]
    public class UModel : UObject
    {
        public UModel()
        {
            ShouldDeserializeOnDemand = true;
        }
    }
}