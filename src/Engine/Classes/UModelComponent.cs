using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UModelComponent/Engine.ModelComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UModelComponent : UPrimitiveComponent
    {
        public UObject Model;
        public int ZoneIndex;
        
        protected override void Deserialize()
        {
            base.Deserialize();

            _Buffer.Read(out Model);
            Record(nameof(Model), Model);
            
            _Buffer.Read(out ZoneIndex);
            Record(nameof(ZoneIndex), ZoneIndex);
            
            // TODO: Elements (structure not implemented), ComponentIndex, Nodes
        }
    }
}
