#if AA2
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Branch.UE2.AA2;

namespace Eliot.UELib.Test.Builds
{
    [TestClass]
    public class PackageTestsAA2
    {
        [TestMethod("AA2 Decryption of string 'None'")]
        public void TestDecryptionAAA2_6()
        {
            var decoder = new CryptoDecoderWithKeyAA2
            {
                Key = 0x9F
            };

            // "None" when bits are scrambled (As serialized in Core.u).
            var scrambledNone = new byte[] { 0x94, 0x3E, 0xBF, 0xB2 };
            decoder.DecodeRead(0x45, scrambledNone, 0, scrambledNone.Length);
            string decodedString = Encoding.ASCII.GetString(scrambledNone);
            Assert.AreEqual("None", decodedString);

            var i = (char)decoder.DecryptByte(0x44, 0xDE);
            Assert.AreEqual(5, i);
            var c = (char)decoder.DecryptByte(0x45, 0x94);
            Assert.AreEqual('N', c);
            var c2 = (char)decoder.DecryptByte(0x46, 0x3E);
            Assert.AreEqual('o', c2);
            var c3 = (char)decoder.DecryptByte(0x47, 0xBF);
            Assert.AreEqual('n', c3);
            var c4 = (char)decoder.DecryptByte(0x48, 0xB2);
            Assert.AreEqual('e', c4);
        }
    }
}
#endif
