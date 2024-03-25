using System.Diagnostics;
using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UPalette/Engine.Palette
    /// </summary>
    [UnrealRegisterClass]
    public class UPalette : UObject, IUnrealViewable
    {
        /// <summary>
        /// No alpha was serialized for packages of version 65 or less.
        /// </summary>
        public UArray<UColor> Colors;

        [Build(UnrealPackage.GameBuild.BuildName.Undying)]
        public bool HasAlphaChannel;

        public UPalette()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            // This could be a lot faster with a fixed array, but it's not a significant class of interest.
            int count = _Buffer.ReadIndex();
            Debug.Assert(count == 256);
            _Buffer.ReadArray(out Colors, count);
            Record(nameof(Colors), Colors);
#if UNDYING
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                _Buffer.Version >= 75)
            {
                _Buffer.Read(out HasAlphaChannel); // v28
                Record(nameof(HasAlphaChannel), HasAlphaChannel);
            }
#endif
        }
    }
}
