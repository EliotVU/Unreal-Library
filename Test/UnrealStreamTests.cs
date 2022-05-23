using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Text;
using UELib.Core.Types;

namespace UELib.Test
{
    [TestClass]
    public class UnrealStreamTests
    {
        // HACK: Ugly workaround the issues with UPackageStream
        private static UPackageStream CreateTempStream()
        {
            string tempFilePath = Path.Join(Assembly.GetExecutingAssembly().Location, "../test.u");
            File.WriteAllBytes(tempFilePath, BitConverter.GetBytes(UnrealPackage.Signature));

            var stream = new UPackageStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite);
            return stream;
        }
        
        [TestMethod]
        public void ReadString()
        {
            using var stream = CreateTempStream();
            using var linker = new UnrealPackage(stream);
            // The easiest version to test against.
            linker.Version = 300;
            using var writer = new BinaryWriter(stream);
            // Skip past the signature
            writer.Seek(sizeof(int), SeekOrigin.Begin);

            const string rawUtf8String = "String";
            byte[] utf8StringBytes = Encoding.UTF8.GetBytes(rawUtf8String);
            writer.Write(rawUtf8String.Length + 1);
            writer.Write(utf8StringBytes);
            writer.Write((byte)'\0');

            const string rawUnicodeString = "语言处理";
            byte[] unicodeStringBytes = Encoding.Unicode.GetBytes(rawUnicodeString);
            writer.Write(-(rawUnicodeString.Length + 1));
            writer.Write(unicodeStringBytes);
            writer.Write((short)'\0');

            // Test our stream implementation
            // Skip past the signature
            stream.Seek(sizeof(int), SeekOrigin.Begin);

            string readString = stream.ReadText();
            Assert.AreEqual(rawUtf8String, readString);

            readString = stream.ReadText();
            Assert.AreEqual(rawUnicodeString, readString);
        }

        [TestMethod]
        public void ReadAtomicStruct()
        {
            using var stream = CreateTempStream();
            using var linker = new UnrealPackage(stream);
            // The easiest version to test against.
            linker.Version = 300;
            using var writer = new BinaryWriter(stream);
            // Skip past the signature
            writer.Seek(sizeof(int), SeekOrigin.Begin);
            
            // B, G, R, A;
            var structBuffer = new byte[] { 255, 128, 64, 80 };
            writer.Write(structBuffer);

            stream.Seek(sizeof(int), SeekOrigin.Begin);
            stream.ReadAtomicStruct<UColor>(out var color);
            Assert.AreEqual(255, color.B);
            Assert.AreEqual(128, color.G);
            Assert.AreEqual(64, color.R);
            Assert.AreEqual(80, color.A);
        }
    }
}
