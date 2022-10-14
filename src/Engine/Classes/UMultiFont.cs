using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    /// Represents the UMultiFont:Engine.MultiFont
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UMultiFont : UFont
    {
        public UArray<float> ResolutionTestTable;

        protected override void Deserialize()
        {
            base.Deserialize();

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                _Buffer.ReadArray(out ResolutionTestTable);
            }
        }
    }
}