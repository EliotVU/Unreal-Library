using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Test
{
    [TestClass]
    public class UnrealStreamTests
    {
        // HACK: Ugly workaround the issues with UPackageStream
        private static UPackageStream CreateTempStream(string name = "test.u")
        {
            string tempFilePath = Path.Join(Assembly.GetExecutingAssembly().Location, "../", name);
            File.WriteAllBytes(tempFilePath, BitConverter.GetBytes(UnrealPackage.Signature));

            var stream = new UPackageStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite);
            return stream;
        }
        
        [TestMethod]
        public void ReadString()
        {
            using var stream = CreateTempStream("string.u");
            using var linker = new UnrealPackage(stream);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Build = new UnrealPackage.GameBuild(linker),
                // The easiest version to test against.
                Version = 300
            };
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
            using var stream = CreateTempStream("atomicstruct.u");
            using var linker = new UnrealPackage(stream);
            linker.Summary = new UnrealPackage.PackageFileSummary
            {
                Build = new UnrealPackage.GameBuild(linker),
                // The easiest version to test against.
                Version = 300
            };
            using var writer = new BinaryWriter(stream);
            // Skip past the signature
            writer.Seek(sizeof(int), SeekOrigin.Begin);
            
            // B, G, R, A;
            var inColor = new UColor(255, 128, 64, 80);
            stream.WriteAtomicStruct(ref inColor);
            Assert.AreEqual(8, stream.Position);

            stream.Seek(sizeof(int), SeekOrigin.Begin);
            stream.ReadAtomicStruct(out UColor outColor);
            Assert.AreEqual(8, stream.Position);
            
            Assert.AreEqual(255, outColor.B);
            Assert.AreEqual(128, outColor.G);
            Assert.AreEqual(64, outColor.R);
            Assert.AreEqual(80, outColor.A);
        }
    }
}
