using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements USoundGroup/Engine.SoundGroup
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE2_5)]
    public class USoundGroup : USound
    {
        #region Serialized Members

        [StreamRecord]
        public UArray<UObject /*USound*/> Sounds { get; set; } = [];

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
#if UT
            if ((stream.Build == UnrealPackage.GameBuild.BuildName.UT2004 ||
                 stream.Build == UnrealPackage.GameBuild.BuildName.UT2003)
                && stream.LicenseeVersion < 27)
            {
                stream.Read(out string package);
                stream.Record(nameof(package), package);
            }
#endif
            Sounds = stream.ReadObjectArray<UObject>();
            stream.Record(nameof(Sounds), Sounds);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if UT
            if ((stream.Build == UnrealPackage.GameBuild.BuildName.UT2004 ||
                 stream.Build == UnrealPackage.GameBuild.BuildName.UT2003)
                && stream.LicenseeVersion < 27)
            {
                stream.Write("None"); // package name
            }
#endif
            stream.WriteArray(Sounds);
        }
    }
}
