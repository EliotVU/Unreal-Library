using UELib.Core;

namespace UELib.Engine
{
    [UnrealRegisterClass]
    public class UModel : UObject
    {
        public UModel()
        {
            ShouldDeserializeOnDemand = true;
        }
    }
}