using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UBrushComponent/Engine.BrushComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UBrushComponent : UPrimitiveComponent
    {
        // TODO: CachedPhysBrushData
        protected override void Deserialize() => base.Deserialize();
    }
}
