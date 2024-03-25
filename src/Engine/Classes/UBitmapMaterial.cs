using UELib.Annotations;
using UELib.Branch;
using UELib.Core;

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
        [CanBeNull] public UPalette Palette;

        public UBitmapMaterial()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            // HACK: This will do until we have a proper property linking setup.

            var formatProperty = Properties.Find("Format");
            if (formatProperty != null)
            {
                _Buffer.StartPeek(formatProperty._PropertyValuePosition);
                _Buffer.Read(out byte index);
                _Buffer.EndPeek();
                
                Format = (TextureFormat)index;
            }

            var paletteProperty = Properties.Find("Palette");
            if (paletteProperty != null)
            {
                _Buffer.StartPeek(paletteProperty._PropertyValuePosition);
                _Buffer.Read(out Palette);
                _Buffer.EndPeek();
            }
        }
    }
}