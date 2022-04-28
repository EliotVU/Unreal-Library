using System.Diagnostics;
using UELib.Core;
using UELib.Core.Types;

namespace UELib.Engine
{
    [UnrealRegisterClass]
    public class UPalette : UObject, IUnrealViewable
    {
        // This could be a lot faster with a fixed array, but it's not a significant class of interest.
        public UArray<UColor> Colors;

        public UPalette()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            int count = _Buffer.ReadIndex();
            Debug.Assert(count == 256);
            _Buffer.ReadMarshalArray(out Colors, count);
        }
    }
}