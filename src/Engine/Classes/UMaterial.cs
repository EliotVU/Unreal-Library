namespace UELib.Engine
{
    /// <summary>
    ///     Implements UMaterial/Engine.Material
    /// </summary>
    [UnrealRegisterClass]
    public class UMaterial : UMaterialInterface
    {
        protected override void Deserialize()
        {
            base.Deserialize();
#if UNREAL2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
            {
                _Buffer.Read(out byte textureType);
                Record(nameof(textureType), textureType);
            }
#endif
        }
    }
}