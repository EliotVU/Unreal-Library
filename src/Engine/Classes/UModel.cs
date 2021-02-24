using UELib.JsonDecompiler.Core;

namespace UELib.JsonDecompiler.Engine
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