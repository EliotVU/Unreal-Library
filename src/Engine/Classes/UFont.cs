using System;
using System.Linq;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UFont/Engine.Font
    /// </summary>
    [UnrealRegisterClass]
    public class UFont : UObject
    {
        #region Script Properties

        [StreamRecord, UnrealProperty]
        public UArray<FontCharacter> Characters { get; set; } = [];

        [StreamRecord, UnrealProperty]
        public UArray<UObject /*UTexture or UTexture2D*/> Textures { get; set; } = [];

        /// <summary>
        ///     The default spacing between characters in this font.
        /// </summary>
        [StreamRecord, UnrealProperty]
        public int Kerning { get; set; }

        [StreamRecord, UnrealProperty]
        [BuildGeneration(BuildGeneration.UE3)]
        public bool IsRemapped { get; set; }

        #endregion

        #region Serialized Members

        [StreamRecord]
        public UMap<ushort, ushort>? CharRemap { get; set; }

        #endregion

        public UFont()
        {
            ShouldDeserializeOnDemand = true;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            // UT2004(v128) < LicenseeVersion 29, but the correct version to test against is 122
            if (stream.Version < (uint)PackageObjectLegacyVersion.FontPagesDisplaced)
            {
                stream.Read(out UArray<FontPage> pages);
                stream.Record(nameof(pages), pages);

                stream.Read(out int charactersPerPage);
                stream.Record(nameof(charactersPerPage), charactersPerPage);

                Characters = [];
                Textures = [];
                for (var i = 0; i < pages.Count; i++)
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
                    stream.Read(out string fontName);
                    stream.Record(nameof(fontName), fontName);

                    stream.Read(out int fontHeight);
                    stream.Record(nameof(fontHeight), fontHeight);
                }
            }
            else if (stream.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                Characters = stream.ReadArray<FontCharacter>();
                stream.Record(nameof(Characters), Characters);

                Textures = stream.ReadObjectArray<UObject>();
                stream.Record(nameof(Textures), Textures);
            }
#if MKKE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.MKKE) return; // End of object.
#endif
            // No version check in UT2003 (v120) and UT2004 (v128)
            if (stream.Version >= (uint)PackageObjectLegacyVersion.KerningAddedToUFont &&
                stream.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                Kerning = stream.ReadInt32();
                stream.Record(nameof(Kerning), Kerning);
            }
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF) return; // Not yet supported.
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.CharRemapAddedToUFont)
            {
                stream.Read(out UMap<ushort, ushort> charRemap);
                CharRemap = charRemap;
                stream.Record(nameof(CharRemap), CharRemap);

                if (stream.Version >= (uint)PackageObjectLegacyVersion.CleanupFonts)
                {
                    return;
                }

                IsRemapped = stream.ReadBool();
                stream.Record(nameof(IsRemapped), IsRemapped);

                //if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
                //{
                //    stream.Read(out int xPad);
                //    stream.Read(out int yPad);
                //}
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            // UT2004(v128) < LicenseeVersion 29, but the correct version to test against is 122
            if (stream.Version < (uint)PackageObjectLegacyVersion.FontPagesDisplaced)
            {
                // Reconstruct FontPages and CharactersPerPage from Textures and Characters
                var pages = new UArray<FontPage>(Textures.Select(texture => new FontPage
                {
                    Texture = texture as UTexture,
                    Characters = Characters
                }));

                stream.Write(pages);

                int charactersPerPage = Characters.Count;
                stream.Write(charactersPerPage);

                if (pages.Count == 0 && charactersPerPage == 0)
                {
                    // Write dummy fontName and fontHeight
                    stream.Write(string.Empty);
                    stream.Write(0);
                }
            }
            else if (stream.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                stream.Write(Characters);
                stream.Write(Textures);
            }
#if MKKE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.MKKE) return; // End of object.
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.KerningAddedToUFont &&
                stream.Version < (uint)PackageObjectLegacyVersion.CleanupFonts)
            {
                stream.Write(Kerning);
            }
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF) return; // Not yet supported.
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.CharRemapAddedToUFont)
            {
                stream.WriteMap(CharRemap);
                if (stream.Version >= (uint)PackageObjectLegacyVersion.CleanupFonts)
                {
                    return;
                }

                stream.Write(IsRemapped);
#if UNREAL2
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
                {
                    throw new NotSupportedException("This package version is not supported!");
                }
#endif
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
                stream.Write(Characters);
            }
        }

        /// <summary>
        ///     Implements FFontCharacter/Engine.Font.FontCharacter (See Engine/Classes/UFont.uc)
        /// </summary>
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
