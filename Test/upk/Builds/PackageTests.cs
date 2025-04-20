using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;
using UELib.Services;
using static UELib.UnrealPackage.GameBuild;

namespace Eliot.UELib.Test.UPK.Builds
{
    [TestClass]
    public class PackageTests
    {
        [DataTestMethod]
        [DataRow(@"Huxley\",
            BuildName.Huxley,
            BuildPlatform.PC
        )]
        [DataRow(@"Borderlands\",
            BuildName.Borderlands,
            BuildPlatform.PC
        )]
        [DataRow(@"Borderlands 2 VR\",
            BuildName.Borderlands2,
            BuildPlatform.PC
        )]
        [DataRow(@"Borderlands2\",
            BuildName.Borderlands2,
            BuildPlatform.PC
        )]
        [DataRow(@"Borderlands Legends\",
            BuildName.Default,
            BuildPlatform.Console
        )]
        [DataRow(@"BorderlandsGOTY\",
            BuildName.Borderlands_GOTYE,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Battleborn\",
            BuildName.Battleborn,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Bulletstorm\",
            BuildName.BulletStorm,
            BuildPlatform.PC
        )]
        [DataRow(@"EndWar\",
            BuildName.EndWar,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"RoboGame\",
            BuildName.RoboBlitz,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"RoboHordes\",
            BuildName.Default,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Stranglehold\StrangleholdGame\",
            BuildName.Stranglehold,
            BuildPlatform.PC
        )]
        [DataRow(@"UT3\",
            BuildName.UT3,
            BuildPlatform.PC
        )]
        [DataRow(@"Mirrors Edge\",
            BuildName.MirrorsEdge,
            BuildPlatform.PC
        )]
        [DataRow(@"Tera\",
            BuildName.Tera,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Spellborn\",
            BuildName.Spellborn,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"DNF\",
            BuildName.DNF,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"DCUO\",
            BuildName.DCUO,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Clive Barker's Undying\",
            BuildName.Undying,
            BuildPlatform.Undetermined
        )]
        public async Task TestPackages(string packagesPath, BuildName packagesBuild, BuildPlatform packagesPlatform)
        {
            packagesPath = Path.Join(Packages.UnrealEngineGamesPath, packagesPath);
            if (!Directory.Exists(packagesPath))
            {
                Assert.Inconclusive($"Couldn't find directory '{packagesPath}'");
            }

            var exceptions = new List<Exception>();
            UnrealConfig.SuppressSignature = true;

            var files = Directory
                .EnumerateFiles(packagesPath, "*.*", SearchOption.AllDirectories)
                .Where(UnrealLoader.IsUnrealFileExtension);

            var filePaths = files.ToList();
            Console.WriteLine($@"Validating {filePaths.Count} packages");

            if (filePaths.Count == 0)
            {
                Assert.Inconclusive($"Couldn't find any files in '{packagesPath}'");
            }

            var tasks = new List<Task>(filePaths.Count);
            foreach (string filePath in filePaths)
            {
                Console.WriteLine($@"Validating '{filePath}'");

                tasks.Add(Task
                    .Factory.StartNew(() =>
                    {
                        using var stream = new UPackageStream(filePath, FileMode.Open, FileAccess.Read);
                        using var linker = new UnrealPackage(stream);
                        //package.BuildTarget = packagesBuild;
                        linker.Deserialize(stream);

                        // Commented out, because, UE2 games often contain a mix of UE1 packages.
                        //Assert.AreEqual(packagesBuild, linker.Build.Name);

                        AssertPackage(linker, exceptions);
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                    .WaitAsync(TimeSpan.FromSeconds(15))
                );

                await Task.Delay(250);
            }

            await Task.WhenAll(tasks);

            Assert.IsFalse(exceptions.Any(), $"{exceptions.Count} {string.Join('\n', exceptions)}");
        }

        private static void AssertPackage(UnrealPackage linker, List<Exception> exceptions)
        {
            try
            {
                linker.InitializePackage(UnrealPackage.InitFlags.Construct |
                                         UnrealPackage.InitFlags.RegisterClasses);
            }
            catch (Exception exception)
            {
                exceptions.Add(new NotSupportedException($"'{linker.FullPackageName}' package exception", exception));
                return;
            }

            AssertObjects(linker.Objects.Where(obj => (int)obj > 0));
            exceptions.AddRange(linker.Objects
                .Where(obj => obj.ThrownException != null)
                .Select(obj => new NotSupportedException($"'{linker.FullPackageName}' object exception", obj.ThrownException)));

            return;

            void TryLoadObject(UObject obj)
            {
                if (obj.DeserializationState != 0)
                {
                    return;
                }

                try
                {
                    obj.Load();
                }
                catch (Exception exception)
                {
                    // Catch, because we don't want to stop the test, not until all objects have been loaded.
                    obj.ThrownException = exception;
                }
            }

            void AssertObjects(IEnumerable<UObject> objects)
            {
                // We want to load all objects, even if some of them throw exceptions.
                var lastService = LibServices.LogService;
                LibServices.LogService = new DefaultLogService();
                var compatibleExports = objects
                    .Where(exp => exp is not UnknownObject)
                    .ToList();

                compatibleExports.ForEach(TryLoadObject);
                LibServices.LogService = lastService;
            }
        }
    }
}
