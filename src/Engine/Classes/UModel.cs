using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UModel/Engine.Model
    /// </summary>
    [Output("Brush")]
    [UnrealRegisterClass]
    public class UModel : UPrimitive
    {
        #region Script Properties

        [UnrealProperty, Output] public UPolys Polys;

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
        }
    }
}
