using UELib.Branch;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UShadowMapTexture2D/Engine.ShadowMapTexture2D
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UShadowMapTexture2D : UTexture2D;
}
