using System;
using System.Diagnostics;
using Eliot.UELib.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Text;

namespace UELib.Test
{
    [TestClass]
    public class UnrealStreamTests
    {
        [TestMethod]
        public void ReadString()
        {
            string tempFilePath = Path.Join(Assembly.GetExecutingAssembly().Location, "../test.u");
            File.WriteAllBytes(tempFilePath, BitConverter.GetBytes(UnrealPackage.Signature));

            using var stream = new UPackageStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite);
            var linker = new UnrealPackage(stream);
            // The easiest version to test against.
            linker.Version = 300;
            
            var writer = new BinaryWriter(stream);
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
    }
}
