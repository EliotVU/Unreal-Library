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
        private static readonly string PackagesPath = Packages.Packages_Path;

        [TestMethod]
        public void TestPackagesNoExceptions()
        {
            // Skip test if the dev is not in possess of this game.
            if (!Directory.Exists(PackagesPath))
            {
                Console.Error.Write($"Couldn't find packages path '{PackagesPath}'");
                return;
            }

            UnrealConfig.SuppressSignature = true;
            var files = Enumerable.Concat(
                Directory.GetFiles(PackagesPath, "*.u"),
                Directory.GetFiles(PackagesPath, "*.upk")
            );
            var exceptions = new List<Exception>();
            foreach (string file in files)
            {
                Debug.WriteLine(file);
                try
                {
                    using var linker = UnrealLoader.LoadPackage(file);
                    switch (linker.Build.Name)
                    {
                        // Not yet error free
                        case UnrealPackage.GameBuild.BuildName.BioShock:
                            continue;
                    }
                    
                    linker.InitializePackage();
                    var objWithError = linker.Objects.Find(obj =>
                        (obj.DeserializationState & UObject.ObjectState.Errorlized) != 0);
                    Assert.IsNull(objWithError, objWithError?.ThrownException.Message);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new NotSupportedException(file, ex));
                }
            }

            Assert.IsTrue(exceptions.Count == 0, string.Join('\n', exceptions));
            Debug.WriteLine($"Successfully tested {files.Count()} packages");
        }
    }
}