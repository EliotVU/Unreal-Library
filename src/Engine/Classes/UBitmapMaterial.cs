using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    /// Implements UBitmapMaterial/Engine.BitmapMaterial
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE2_5)]
    public class UBitmapMaterial : URenderedMaterial
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
        public UPalette? Palette;

        protected override void Deserialize()
        {
            base.Deserialize();

            // HACK: This will do until we have a proper property linking setup.

            var formatProperty = Properties.Find("Format");
            if (formatProperty != null)
            {
                using (_Buffer.Peek(formatProperty._PropertyValuePosition))
                {
                    _Buffer.Read(out byte index);
                    Format = (TextureFormat)index;
                }
            }

            var paletteProperty = Properties.Find("Palette");
            if (paletteProperty != null)
            {
                using (_Buffer.Peek(paletteProperty._PropertyValuePosition))
                {
                    Palette = _Buffer.ReadObject<UPalette>();
                }
            }
        }
    }
}
