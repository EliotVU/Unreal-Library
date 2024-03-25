using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Branch;
using UELib.Core;
using UELib.Engine;

namespace Eliot.UELib.Test
{
    [TestClass]
    public class UnrealStreamTests
    {
        // HACK: Ugly workaround the issues with UPackageStream
        private static UPackageStream CreateTempStream()
        {
            string tempFilePath = Path.Join(Path.GetTempFileName());
            File.WriteAllBytes(tempFilePath, BitConverter.GetBytes(UnrealPackage.Signature));

            var stream = new UPackageStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite);
            return stream;
        }

        [DataTestMethod]
        [DataRow(PackageObjectLegacyVersion.Undefined, 1, +0b0000000000000000000000100001)]
        [DataRow(PackageObjectLegacyVersion.Undefined, 1, -0b0000000000000000000000100001)]
        [DataRow(PackageObjectLegacyVersion.Undefined, 2, +0b0000000000000001000001100001)]
        [DataRow(PackageObjectLegacyVersion.Undefined, 3, +0b0000000010000011000001100001)]
        [DataRow(PackageObjectLegacyVersion.Undefined, 4, +0b0100000110000011000001100001)]
        [DataRow(PackageObjectLegacyVersion.Undefined, 5, +0b1100000110000011000001100001)]
        [DataRow(PackageObjectLegacyVersion.CompactIndexDeprecated, 4, int.MaxValue)]
        public void SerializeCompactIndex(PackageObjectLegacyVersion version, int count, int compactIndex)
        {
            using var stream = CreateTempStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = (uint)version
            };

            using var writer = new UnrealWriter(stream, stream);
            // Skip past the signature
            writer.Seek(sizeof(int), SeekOrigin.Begin);

            writer.WriteIndex(compactIndex);

            long length = stream.Position - sizeof(int);
            Assert.AreEqual(count, length);

            using var reader = new UnrealReader(stream, stream);
            // Skip past the signature
            stream.Seek(sizeof(int), SeekOrigin.Begin);

            int readCompactIndex = reader.ReadIndex();
            Assert.AreEqual(compactIndex, readCompactIndex);

            long readLength = stream.Position - sizeof(int);
            Assert.AreEqual(length, readLength);
        }

        [DataTestMethod]
        [DataRow(PackageObjectLegacyVersion.Undefined, "String")]
        [DataRow(PackageObjectLegacyVersion.Undefined, "语言处理")]
        public void SerializeString(PackageObjectLegacyVersion version, string text)
        {
            using var stream = CreateTempStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = (uint)version
            };

            using var writer = new UnrealWriter(stream, stream);
            writer.Seek(sizeof(int), SeekOrigin.Begin);
            writer.WriteString(text);

            using var reader = new UnrealReader(stream, stream);
            stream.Seek(sizeof(int), SeekOrigin.Begin);
            string readString = reader.ReadString();
            Assert.AreEqual(text, readString);
        }

        [TestMethod]
        public void SerializeStruct()
        {
            using var stream = CreateTempStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary();
            
            using var writer = new BinaryWriter(stream);
            // Skip past the signature
            writer.Seek(sizeof(int), SeekOrigin.Begin);

            // B, G, R, A;
            var inColor = new UColor(255, 128, 64, 80);
            stream.WriteStruct(ref inColor);
            Assert.AreEqual(8, stream.Position);

            stream.Seek(sizeof(int), SeekOrigin.Begin);
            stream.ReadStruct(out UColor outColor);
            Assert.AreEqual(8, stream.Position);

            Assert.AreEqual(255, outColor.B);
            Assert.AreEqual(128, outColor.G);
            Assert.AreEqual(64, outColor.R);
            Assert.AreEqual(80, outColor.A);
        }

        [TestMethod]
        public void SerializeStructMarshal()
        {
            using var stream = CreateTempStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary();
            using var writer = new BinaryWriter(stream);
            // Skip past the signature
            writer.Seek(sizeof(int), SeekOrigin.Begin);

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

        [DataTestMethod]
        [DataRow(PackageObjectLegacyVersion.Undefined)]
        [DataRow(PackageObjectLegacyVersion.LazyArraySkipCountChangedToSkipOffset)]
        [DataRow(PackageObjectLegacyVersion.LazyLoaderFlagsAddedToLazyArray)]
        [DataRow(PackageObjectLegacyVersion.StorageSizeAddedToLazyArray)]
        //[DataRow(PackageObjectLegacyVersion.L8AddedToLazyArray)]
        [DataRow(PackageObjectLegacyVersion.LazyArrayReplacedWithBulkData)]
        public void SerializeBulkData(PackageObjectLegacyVersion version)
        {
            using var stream = CreateTempStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = (uint)version
            };

            using var writer = new BinaryWriter(stream);
            // Skip past the signature
            writer.Seek(sizeof(int), SeekOrigin.Begin);

            byte[] rawData = Encoding.ASCII.GetBytes("LET'S PRETEND THAT THIS IS BULK DATA!");
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
            Assert.AreEqual(bulkData.ElementData.Length, readBulkData.ElementData.Length);
        }

        [DataTestMethod]
        [DataRow(PackageObjectLegacyVersion.FontPagesDisplaced)]
        [DataRow(PackageObjectLegacyVersion.VerticalOffsetAddedToUFont)]
        public void SerializeDataTypes(PackageObjectLegacyVersion version)
        {
            using var stream = CreateTempStream();
            using var linker = new UnrealPackage(stream);
            linker.Build = new UnrealPackage.GameBuild(linker);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Version = (uint)version
            };
            using var writer = new BinaryWriter(stream);
            // Skip past the signature
            writer.Seek(sizeof(int), SeekOrigin.Begin);

            var fontPage = new UFont.FontPage
            {
                Characters = new UArray<UFont.FontCharacter>
                {
                    new UFont.FontCharacter
                    {
                        StartU = 1,
                        StartV = 2,
                        USize = 64,
                        VSize = 64,
                        TextureIndex = 0,
                        VerticalOffset = 0
                    }
                },
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
}
