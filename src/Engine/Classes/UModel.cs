using UELib.Engine;
using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UModel/Engine.Model
    /// </summary>
    [Output("Brush")]
    [UnrealRegisterClass]
    public class UModel : UObject
    {
        [Output]
        public UPolys Polys;
        
        public UModel()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();
        }
    }
}