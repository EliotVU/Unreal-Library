using System.Diagnostics;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    /// Implements UPalette/Engine.Palette
    /// </summary>
    [UnrealRegisterClass]
    public class UPalette : UObject, IUnrealViewable
    {
        /// <summary>
        /// No alpha was serialized for packages of version 65 or less.
        /// </summary>
        public UArray<UColor> Colors;

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
            _Buffer.ReadMarshalArray(out Colors, count);
        }
    }
}