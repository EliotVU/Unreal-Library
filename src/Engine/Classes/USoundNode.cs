using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements USoundNode/Engine.SoundNode
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USoundNode : UObject
    {
        #region Script Properties

        [UnrealProperty]
        public UArray<USoundNode> ChildNodes { get; set; } = [];

        #endregion

        public USoundNode()
        {
            ShouldDeserializeOnDemand = true;
        }
    }
}
