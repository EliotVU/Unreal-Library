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
#if RM
            if (_Buffer.LicenseeVersion >= 3)
            {
                _Buffer.Read(out byte v34);
                Record(nameof(v34), v34);
            }
#endif
        }
    }
}
