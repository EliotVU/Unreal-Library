using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements AActor/Engine.Actor
    /// </summary>
    [Output("Actor")]
    [UnrealRegisterClass]
    public class AActor : UObject
    {
        public AActor()
        {
            ShouldDeserializeOnDemand = true;
        }
    }
}
