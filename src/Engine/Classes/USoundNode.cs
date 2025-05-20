using UELib.Branch;

namespace UELib.Core
{
    /// <summary>
    ///     Implements USoundNode/Engine.SoundNode
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USoundNode : UObject
    {
        // UnrealProperty
        public UArray<USoundNode> ChildNodes;

        public USoundNode()
        {
            ShouldDeserializeOnDemand = true;
        }
    }
}
