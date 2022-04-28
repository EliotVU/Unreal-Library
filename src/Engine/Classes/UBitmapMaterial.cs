using System;
using UELib.Core;

namespace UELib.Engine
{
    [UnrealRegisterClass]
    public class UBitmapMaterial : UObject
    {
        // UE2 implementation
        public enum TextureFormat
        {
            P8,
            RGBA7,
            RGB16,
            DXT1,
            RGB8,
            RGBA8,
            NODATA,
            DXT3,
            DXT5,
            L8,
            G16,
            RRRGGGBBB
        };

        public TextureFormat Format;

        public UBitmapMaterial()
        {
            ShouldDeserializeOnDemand = true;
        }
        
        protected override void Deserialize()
        {
            base.Deserialize();

            var formatProperty = Properties.Find("Format");
            if (formatProperty != null)
            {
                Enum.TryParse(formatProperty.Value, out Format);
            }
        }
    }
}