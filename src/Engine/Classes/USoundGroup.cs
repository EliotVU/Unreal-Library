using UELib.Branch;

namespace UELib.Core
{
    /// <summary>
    ///     Implements USoundGroup/Engine.SoundGroup
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE2_5)]
    public class USoundGroup : USound
    {
        // p.s. It's not safe to cast to USound
        public UArray<UObject> Sounds;
        
        protected override void Deserialize()
        {
            base.Deserialize();
#if UT
            if ((Package.Build == UnrealPackage.GameBuild.BuildName.UT2004 ||
                 Package.Build == UnrealPackage.GameBuild.BuildName.UT2003)
                && _Buffer.LicenseeVersion < 27)
            {
                _Buffer.Read(out string package);
                Record(nameof(package), package);
            }
#endif
            _Buffer.ReadArray(out Sounds);
            Record(nameof(Sounds), Sounds);
        }
    }
}
