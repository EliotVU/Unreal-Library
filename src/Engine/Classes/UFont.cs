using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UFont/Engine.Font
    /// </summary>
    [UnrealRegisterClass]
    public class UFont : UObject
    {
        public UArray<FontCharacter> Characters;
        public UArray<UObject> Textures;
        public int Kerning;
        public UMap<ushort, ushort> CharRemap;
        public bool IsRemapped;

        public UFont()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            // UT2004(v128) < LicenseeVersion 29, but the correct version to test against is 122
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.FontPagesDisplaced)
            {
                _Buffer.Read(out UArray<FontPage> pages);
                Record(nameof(pages), pages);

                _Buffer.Read(out int charactersPerPage);
                Record(nameof(charactersPerPage), charactersPerPage);

                Characters = new UArray<FontCharacter>();
                Textures = new UArray<UObject>();
                for (int i = 0; i < pages.Count; i++)
                {
                    Textures.Add(pages[i].Texture);
                    foreach (var c in pages[i].Characters)
                    {
                        var character = c;
                        character.TextureIndex = (byte)i;
                        Characters.Add(character);
                    }
                }

                if (pages.Count == 0 && charactersPerPage == 0)
                {
                    _Buffer.Read(out string fontName);
                    Record(nameof(fontName), fontName);

                    _Buffer.Read(out int fontHeight);
                    Record(nameof(fontHeight), fontHeight);
                }
            }
            else if (_Buffer.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                _Buffer.Read(out Characters);
                Record(nameof(Characters), Characters);

                _Buffer.Read(out Textures);
                Record(nameof(Textures), Textures);
            }

            // No version check in UT2003 (v120) and UT2004 (v128)
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.KerningAddedToUFont &&
                _Buffer.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                _Buffer.Read(out Kerning);
                Record(nameof(Kerning), Kerning);
            }

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.CharRemapAddedToUFont)
            {
                _Buffer.Read(out CharRemap);
                Record(nameof(CharRemap), CharRemap);

                if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.CleanupFonts)
                {
                    return;
                }

                _Buffer.Read(out IsRemapped);
                Record(nameof(IsRemapped), IsRemapped);

                //if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
                //{
                //    _Buffer.Read(out int xPad);
                //    _Buffer.Read(out int yPad);
                //}
            }
        }

        public struct FontPage : IUnrealSerializableClass
        {
            public UTexture Texture;
            public UArray<FontCharacter> Characters;

            public void Deserialize(IUnrealStream stream)
            {
                stream.Read(out Texture);
                stream.Read(out Characters);
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(Texture);
                stream.Write(ref Characters);
            }
        }

        public struct FontCharacter : IUnrealSerializableClass
        {
            public int StartU;
            public int StartV;
            public int USize;
            public int VSize;
            public byte TextureIndex;
            public int VerticalOffset;

            public void Deserialize(IUnrealStream stream)
            {
                stream.Read(out StartU);
                stream.Read(out StartV);
                stream.Read(out USize);
                stream.Read(out VSize);

                if (stream.Version >= (uint)PackageObjectLegacyVersion.FontPagesDisplaced)
                {
                    stream.Read(out TextureIndex);
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.VerticalOffsetAddedToUFont)
                {
                    stream.Read(out VerticalOffset);
                }
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(StartU);
                stream.Write(StartV);
                stream.Write(USize);
                stream.Write(VSize);

                if (stream.Version >= (uint)PackageObjectLegacyVersion.FontPagesDisplaced)
                {
                    stream.Write(TextureIndex);
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.VerticalOffsetAddedToUFont)
                {
                    stream.Write(VerticalOffset);
                }
            }

            public override int GetHashCode()
            {
                return StartU ^ StartV ^ USize ^ VSize;
            }
        }
    }
}
