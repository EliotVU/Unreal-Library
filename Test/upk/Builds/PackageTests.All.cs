using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Test.upk.Builds
{
    [TestClass]
    public class PackageTestsAll
    {
        private static readonly string s_packagesPath = Packages.Packages_Path;

        /// <summary>
        /// FIXME: Beware, we are experiencing a memory leak in this chain of events.
        /// </summary>
        [TestMethod]
        public void TestPackagesNoExceptions()
        {
            // Skip test if the dev is not in possess of this game.
            if (!Directory.Exists(s_packagesPath))
            {
                Console.Error.Write($"Couldn't find packages path '{s_packagesPath}'");
                return;
            }

            UnrealConfig.SuppressSignature = true;
            var files = Directory
                .EnumerateFiles(s_packagesPath, "*.*", SearchOption.AllDirectories)
                .Where(UnrealLoader.IsUnrealFileExtension);
            var exceptions = new List<Exception>();
            foreach (string filePath in files)
            {
                Debug.WriteLine($"Testing: {filePath}");
                try
                {
                    TestPackageFile(filePath, exceptions);
                }
                // Likely a loading error.
                catch (Exception ex)
                {
                    exceptions.Add(new NotSupportedException($"{filePath} loading exception: {ex}"));
                }
            }
            Assert.IsFalse(exceptions.Any(), string.Join('\n', exceptions));
        }

        //[DataTestMethod]
        //[DataRow("(V490_009,E3329,C046)GoWPC_Engine.u", UnrealPackage.GameBuild.BuildName.GoW1)]
        public void TestPackageNoExceptions(string fileName, UnrealPackage.GameBuild.BuildName buildName)
        {
            string filePath = Path.Combine(s_packagesPath, fileName);
            Debug.WriteLine($"Testing: {filePath}");

            var exceptions = new List<Exception>();
            TestPackageFile(filePath, exceptions);

            Assert.IsFalse(exceptions.Any(), string.Join('\n', exceptions));
            Debug.WriteLine($"Successfully tested package \"{filePath}\"");
        }

        private static void TestPackageFile(string filePath, List<Exception> exceptions)
        {
            UnrealConfig.SuppressSignature = true;
            using var linker = UnrealLoader.LoadPackage(filePath);

            try
            {
                // FIXME: RegisterClasses is wasteful
                linker.InitializePackage(UnrealPackage.InitFlags.Construct |
                                         UnrealPackage.InitFlags.RegisterClasses);
            }
            catch (Exception ex)
            {
                exceptions.Add(new NotSupportedException($"{filePath} initialization exception: {ex}"));
                return;
            }

            exceptions.AddRange(linker.Objects
                .Where(obj => !(obj is UnknownObject))
                .Select(obj =>
                {
                    if (obj.DeserializationState == 0)
                    {
                        obj.BeginDeserializing();
                    }

                    return obj;
                })
                .Where(obj => obj.ThrownException != null)
                .Select(obj => new NotSupportedException($"{filePath} object exception: {obj.ThrownException}")));
        }
    }
}
