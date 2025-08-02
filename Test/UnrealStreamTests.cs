using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Branch;
using UELib.Core;
using UELib.Engine;
using UELib.IO;

namespace Eliot.UELib.Test;

[TestClass]
public class UnrealStreamTests
{
    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.Undefined, 1, +0b0000000000000000000000100101)]
    [DataRow(PackageObjectLegacyVersion.Undefined, 1, -0b0000000000000000000000100101)]
    [DataRow(PackageObjectLegacyVersion.Undefined, 2, +0b0000000000000001000001100101)]
    [DataRow(PackageObjectLegacyVersion.Undefined, 3, +0b0000000010000011000001100101)]
    [DataRow(PackageObjectLegacyVersion.Undefined, 4, +0b0100000110000011000001100101)]
    [DataRow(PackageObjectLegacyVersion.Undefined, 5, +0b1100000110000011000001100101)]
    [DataRow(PackageObjectLegacyVersion.CompactIndexDeprecated, 4, int.MaxValue)]
    public void SerializeCompactIndex(PackageObjectLegacyVersion version, int count, int compactIndex)
    {
        using var stream = new UnrealDummyStream(
            new UnrealTestArchive(UnrealPackage.TransientPackage, (uint)version),
            UnrealPackageUtilities.CreateTempPackageStream()
        );

        stream.Seek(0, SeekOrigin.Begin);
        stream.WriteIndex(compactIndex);

        long length = stream.Position;
        Assert.AreEqual(count, length);

        stream.Seek(0, SeekOrigin.Begin);
        int readCompactIndex = stream.ReadIndex();
        Assert.AreEqual(length, stream.Position);
        Assert.AreEqual(compactIndex, readCompactIndex);
    }

    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.Undefined, "")] // Verify empty string
    [DataRow(PackageObjectLegacyVersion.Undefined, "String")] // Verify ASCII string
    [DataRow(PackageObjectLegacyVersion.Undefined, "Strîñg", false)] // Verify ANSI characters
    [DataRow(PackageObjectLegacyVersion.Undefined, "语言处理", true)] // Verify Unicode characters
    public void SerializeString(PackageObjectLegacyVersion version, string text, bool requiresUnicode = false)
    {
        using var stream = new UnrealDummyStream(
            new UnrealTestArchive(UnrealPackage.TransientPackage, (uint)version),
            UnrealPackageUtilities.CreateTempPackageStream()
        );

        bool isUnicode = UnrealBinaryWriter.IsUnicode(text);
        Assert.AreEqual(requiresUnicode, isUnicode);

        stream.Seek(0, SeekOrigin.Begin);
        stream.WriteString(text);

        if (isUnicode)
        {
            stream.WriteUnicodeNullString(text);
        }
        else
        {
            stream.WriteAnsiNullString(text);
        }
        long length = stream.Position;

        stream.Seek(0, SeekOrigin.Begin);
        string readString = stream.ReadString();
        Assert.AreEqual(text, readString);
        readString = isUnicode
            ? stream.ReadUnicodeNullString()
            : stream.ReadAnsiNullString();
        Assert.AreEqual(length, stream.Position, "Serialize cannot differ from deserialize");
        Assert.AreEqual(text, readString);
    }

    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.Undefined)]
    [DataRow(PackageObjectLegacyVersion.CompactIndexDeprecated)]
    [DataRow(PackageObjectLegacyVersion.NumberAddedToName)]
    public void SerializeName(PackageObjectLegacyVersion version)
    {
        using var archive = UnrealPackageUtilities.CreateTempArchive((uint)version);
        var package = archive.Package;
        var stream = archive.Stream;

        // Ensure the package knows about the name.
        package.Names.Add(new UNameTableItem("Name"));
        var writtenName = new UName(package.Names[0]);

        // Ensure the stream can retrieve the package name index.
        // Hash -> package name index.
        archive.NameIndices.Add(writtenName.Index, 0);

        stream.Seek(0, SeekOrigin.Begin);
        stream.WriteName(writtenName);
        long length = stream.Position;

        stream.Seek(0, SeekOrigin.Begin);
        var readName = stream.ReadName();
        Assert.AreEqual(length, stream.Position, "Serialize cannot differ from deserialize");
        Assert.AreEqual(writtenName, readName);
    }

    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.Undefined)]
    public void SerializeStruct(PackageObjectLegacyVersion version)
    {
        using var stream = new UnrealDummyStream(
            new UnrealTestArchive(UnrealPackage.TransientPackage, (uint)version),
            UnrealPackageUtilities.CreateTempPackageStream()
        );

        // B, G, R, A;
        var inColor = new UColor(255, 128, 64, 80);
        stream.WriteStruct(ref inColor);
        Assert.AreEqual(4, stream.Position);

        stream.Seek(0, SeekOrigin.Begin);
        stream.ReadStruct(out UColor outColor);
        Assert.AreEqual(4, stream.Position);

        Assert.AreEqual(255, outColor.B);
        Assert.AreEqual(128, outColor.G);
        Assert.AreEqual(64, outColor.R);
        Assert.AreEqual(80, outColor.A);
    }

    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.Undefined)]
    public void SerializeStructMarshal(PackageObjectLegacyVersion version)
    {
        using var stream = new UnrealDummyStream(
            new UnrealTestArchive(UnrealPackage.TransientPackage, (uint)version),
            UnrealPackageUtilities.CreateTempPackageStream()
        );

        long p1 = stream.Position;

        // B, G, R, A;
        var inColor = new UColor(255, 128, 64, 80);
        stream.WriteStructMarshal(ref inColor);
        var inColor2 = new UColor(128, 128, 64, 80);
        stream.WriteStructMarshal(ref inColor2);
        var inColor3 = new UColor(64, 128, 64, 80);
        stream.WriteStructMarshal(ref inColor3);

        stream.Seek(p1, SeekOrigin.Begin);
        stream.ReadStructMarshal(out UColor outColor);

        // Verify order
        Assert.AreEqual(255, outColor.B);
        Assert.AreEqual(128, outColor.G);
        Assert.AreEqual(64, outColor.R);
        Assert.AreEqual(80, outColor.A);

        stream.Seek(p1, SeekOrigin.Begin);
        stream.ReadArrayMarshal(out UArray<UColor> colors, 3);
        Assert.AreEqual(inColor, colors[0]);
        Assert.AreEqual(inColor2, colors[1]);
        Assert.AreEqual(inColor3, colors[2]);
    }

    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.LowestVersion)]
    [DataRow(PackageObjectLegacyVersion.LazyArraySkipCountChangedToSkipOffset)]
    [DataRow(PackageObjectLegacyVersion.LazyLoaderFlagsAddedToLazyArray)]
    [DataRow(PackageObjectLegacyVersion.StorageSizeAddedToLazyArray)]
    [DataRow(PackageObjectLegacyVersion.PackageNameAddedToLazyArray)]
    [DataRow(PackageObjectLegacyVersion.LazyArrayReplacedWithBulkData)]
    public void SerializeBulkData(PackageObjectLegacyVersion version)
    {
        using var archive = UnrealPackageUtilities.CreateTempArchive((uint)version);
        var package = archive.Package;
        var stream = archive.Stream;

        // To ensure that StoragePackageName can be serialized.
        package.Names.Add(new UNameTableItem(UnrealName.None));
        var nameInPackage = new UName(package.Names[0]);
        archive.NameIndices.Add(nameInPackage.Index, 0);

        byte[] rawData = "LET'S PRETEND THAT THIS IS BULK DATA!"u8.ToArray();
        var bulkData = new UBulkData<byte>(0, rawData);

        long bulkPosition = ((IUnrealStream)stream).Position;
        stream.Write(ref bulkData);
        Assert.AreEqual(rawData.Length, bulkData.StorageSize);

        stream.Position = bulkPosition;
        stream.Read(out UBulkData<byte> readBulkData);
        Assert.IsNull(readBulkData.ElementData);
        Assert.AreEqual(bulkData.StorageSize, readBulkData.StorageSize);
        Assert.AreEqual(bulkData.StorageOffset, readBulkData.StorageOffset);
        Assert.AreEqual(bulkData.ElementCount, readBulkData.ElementCount);

        readBulkData.LoadData(stream);
        Assert.IsNotNull(readBulkData.ElementData);
        Assert.AreEqual(bulkData.ElementData!.Length, readBulkData.ElementData.Length);
        CollectionAssert.AreEqual(bulkData.ElementData, readBulkData.ElementData);
    }

    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.LowestVersion)]
    //[DataRow(PackageObjectLegacyVersion.CharRemapAddedToUFont)]
    //[DataRow(PackageObjectLegacyVersion.KerningAddedToUFont)]
    [DataRow(PackageObjectLegacyVersion.FontPagesDisplaced)]
    [DataRow(PackageObjectLegacyVersion.VerticalOffsetAddedToUFont)]
    public void SerializeDataTypes(PackageObjectLegacyVersion version)
    {
        using var archive = UnrealPackageUtilities.CreateTempArchive((uint)version);
        var stream = archive.Stream;

        var fontPage = new UFont.FontPage
        {
            Characters =
            [
                new UFont.FontCharacter
                {
                    StartU = 1,
                    StartV = 2,
                    USize = 64,
                    VSize = 64,
                    TextureIndex = 0,
                    VerticalOffset = 0
                }
            ],
            Texture = null
        };

        long p1 = stream.Position;
        stream.WriteStruct(ref fontPage);
        stream.Seek(p1, SeekOrigin.Begin);
        stream.ReadStruct(out UFont.FontPage newFontPage);
        Assert.AreEqual(fontPage.Texture, newFontPage.Texture);
        Assert.AreEqual(fontPage.Characters[0].GetHashCode(), newFontPage.Characters[0].GetHashCode());
    }
}
