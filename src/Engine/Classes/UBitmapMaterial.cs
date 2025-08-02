using UELib.Branch;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    /// Implements UBitmapMaterial/Engine.BitmapMaterial
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE2_5)]
    public class UBitmapMaterial : URenderedMaterial
    {
        #region Script Members

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

        [UnrealProperty]
        public TextureFormat Format { get; set; }

        [UnrealProperty]
        public UPalette? Palette { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            // HACK: This will do until we have a proper property linking setup.

            var formatProperty = Properties.Find("Format");
            if (formatProperty != null)
            {
                using (stream.Peek(formatProperty._PropertyValuePosition))
                {
                    stream.Read(out byte index);
                    Format = (TextureFormat)index;
                }
            }

            var paletteProperty = Properties.Find("Palette");
            if (paletteProperty != null)
            {
                using (stream.Peek(paletteProperty._PropertyValuePosition))
                {
                    Palette = stream.ReadObject<UPalette>();
                }
            }
        }
    }
}
