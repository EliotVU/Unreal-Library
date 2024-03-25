#if AA2
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Branch.UE2.AA2;
using UELib.Core;

namespace Eliot.UELib.Test.upk.Builds
{
    [TestClass]
    public class PackageTestsAA2
    {
        // Testing the "Arcade" packages only
        private static readonly string PackagesPath = Packages.Packages_Path;
        private static readonly string NoEncryptionCorePackagePath = Path.Join(PackagesPath, "(V128_032,Encrypted)AAA_2_6_Core.u");
        private static readonly string EncryptedCorePackagePath = Path.Join(PackagesPath, "(V128_032,Encrypted)AAA_2_6_Core.u");

        [TestMethod]
        public void TestPackageAAA2_6()
        {
            // Skip test if the dev is not in possess of this game.
            if (!Directory.Exists(PackagesPath))
            {
                Console.Error.Write($"Couldn't find packages path '{PackagesPath}'");
                return;
            }

            using var linker = UnrealLoader.LoadPackage(NoEncryptionCorePackagePath, UnrealPackage.GameBuild.BuildName.AA2_2_6);
            Assert.IsNotNull(linker);
            Assert.AreEqual(UnrealPackage.GameBuild.BuildName.AA2_2_6, linker.Build.Name, "Incorrect package's build");
            Assert.AreEqual(typeof(EngineBranchAA2), linker.Branch.GetType(), "Incorrect package's branch");

            linker.InitializePackage(UnrealPackage.InitFlags.Construct | UnrealPackage.InitFlags.RegisterClasses);
            
            var funcGetItemName = linker.FindObject<UFunction>("GetItemName");
            Assert.IsNotNull(funcGetItemName);
            
            Debug.WriteLine($"Testing Object: {funcGetItemName.GetReferencePath()}");
            funcGetItemName.BeginDeserializing();
            Assert.IsNull(funcGetItemName.ThrownException, "Function 'GetItemName' had thrown an exception during its deserialization process");
            
            funcGetItemName.Decompile();
        }

        [TestMethod]
        public void TestPackageDecryptionAAA2_6()
        {
            // Skip test if the dev is not in possess of this game.
            if (!Directory.Exists(PackagesPath))
            {
                Console.Error.Write($"Couldn't find packages path '{PackagesPath}'");
                return;
            }

            using var linker = UnrealLoader.LoadPackage(EncryptedCorePackagePath, UnrealPackage.GameBuild.BuildName.AA2_2_6);
            Assert.IsNotNull(linker);
            Assert.AreEqual(UnrealPackage.GameBuild.BuildName.AA2_2_6, linker.Build.Name, "Incorrect package's build");
            Assert.AreEqual(typeof(EngineBranchAA2), linker.Branch.GetType(), "Incorrect package's branch");
        }

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