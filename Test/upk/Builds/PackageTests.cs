using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;
using UELib.Services;
using static UELib.UnrealPackage.GameBuild;

namespace Eliot.UELib.Test.Builds
{
    [TestClass]
    public class PackageTests
    {
        // .upk packages are assumed to be decompressed.
        [DataTestMethod]
        [DataRow(@"X-COM-Alliance\",
            BuildName.Unreal1,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"TheWheelOfTime\",
            BuildName.Default,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Devastation\",
            BuildName.Devastation,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"America's Army 2\2_5\",
            BuildName.AA2_2_5,
            BuildPlatform.Undetermined,
            BuildName.AA2_2_5,
            BuildGeneration.AGP
        )]
        [DataRow(@"America's Army 2\2_6\", // encrypted
            BuildName.AA2_2_6,
            BuildPlatform.Undetermined,
            BuildName.AA2_2_6,
            BuildGeneration.AGP
        )]
        [DataRow(@"America's Army (Arcade)\2_6", // encrypted
            BuildName.AA2_2_6,
            BuildPlatform.Undetermined,
            BuildName.AA2_2_6,
            BuildGeneration.AGP
        )]
        [DataRow(@"America's Army (Arcade)\2_8", // encrypted
            BuildName.AA2_2_8,
            BuildPlatform.Undetermined,
            BuildName.AA2_2_8,
            BuildGeneration.AGP
        )]
        [DataRow(@"Stargate_SG-1_-_The_Alliance-2005-12-15\",
            BuildName.SG1_TA,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Advent Rising\",
            BuildName.Advent,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Star Wars - Republic Commando\",
            BuildName.SWRepublicCommando,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Huxley\",
            BuildName.Huxley,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Borderlands\",
            BuildName.Borderlands,
            BuildPlatform.PC
        )]
        [DataRow(@"Borderlands2\",
            BuildName.Borderlands2,
            BuildPlatform.PC
        )]
        [DataRow(@"Borderlands 2 VR\",
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
            BuildName.Bulletstorm,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Bulletstorm Full Clip Edition\",
            BuildName.Bulletstorm_FCE,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Clive Barker's Undying\",
            BuildName.Undying,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"DCUO\",
            BuildName.DCUO,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"DNF\",
            BuildName.DNF,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"EndWar\",
            BuildName.EndWar,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Gears of War\", // G4WLive
            BuildName.GoW1,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Gears of War 2\",
            BuildName.GoW2,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Gears of War 3\",
            BuildName.GoW3,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Gears of War Ultimate Edition\", // Microsoft Store / WinUAP
            BuildName.GoWUE,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Gears of War Reloaded Beta\", // Steam
            BuildName.GoWUE,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Mirrors Edge\",
            BuildName.MirrorsEdge,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"MKKE\",
            BuildName.MKKE,
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
        [DataRow(@"rocketleague\",
            BuildName.RocketLeague,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Spellborn\",
            BuildName.Spellborn,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Stranglehold\StrangleholdGame\",
            BuildName.Stranglehold,
            BuildPlatform.PC
        )]
        [DataRow(@"Tera\",
            BuildName.Tera,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Tom Clancys Rainbow Six 3 Raven Shield\",
            BuildName.R6RS,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Tom Clancys Rainbow Six Vegas Collection\",
            BuildName.R6Vegas,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Tom Clancy's Splinter Cell 3 - Chaos Theory\", // Demo version
            BuildName.SCCT_Offline,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Tom Clancy's Splinter Cell Chaos Theory\", // Full Offline version
            BuildName.SCCT_Offline,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Warmonger\",
            BuildName.Default,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UT3\",
            BuildName.UT3,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"SuddenAttack2\",
            BuildName.SA2,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"A Hat in Time\",
            BuildName.AHIT,
            BuildPlatform.Undetermined
        )]
        public async Task TestPackages(
            string packagesPath,
            // The expected build name of the package.
            BuildName packagesBuild = BuildName.Unset,
            // The expected cooker platform of the package.
            BuildPlatform packagesPlatform = BuildPlatform.Undetermined,
            // The forced build name for the package.
            BuildName forcedBuild = BuildName.Unset,
            BuildGeneration forcedGeneration = BuildGeneration.Undefined)
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

            var versions = new SortedSet<uint>();

#if DEBUG
            const int maxTasks = 1;
#else
            const int maxTasks = 3;
#endif
            for (int i = 0; i < filePaths.Count; i += maxTasks)
            {
                var tasks = filePaths[i..Math.Min(filePaths.Count, i + maxTasks)]
                    .Select(filePath => Task.Factory.StartNew(() =>
                    {
                        Console.WriteLine($@"Validating '{filePath}'");

                        using var stream = new UPackageStream(filePath, FileMode.Open, FileAccess.Read);
                        using var linker = new UnrealPackage(stream)
                        {
                            BuildTarget = forcedBuild
                        };
                        linker.Deserialize(stream);

                        versions.Add((linker.Summary.Version << 16) | linker.Summary.LicenseeVersion);

                        Console.WriteLine($@"Detected build: {linker.Build} and expected build: {packagesBuild}");

                        if (forcedBuild != BuildName.Unset)
                        {
                            Assert.AreEqual(forcedBuild, linker.Build.Name);
                        }

                        // Commented out, because, UE2 games often contain a mix of UE1 packages.
                        //Assert.AreEqual(packagesBuild, linker.Build.Name);

                        if (linker.CookerPlatform != BuildPlatform.Undetermined)
                        {
                            Console.WriteLine($@"Detected cooker platform: {linker.CookerPlatform}");
                        }

                        if (packagesPlatform != BuildPlatform.Undetermined)
                        {
                            Assert.AreEqual(packagesPlatform, linker.CookerPlatform);
                        }

                        AssertPackage(linker, exceptions);
                    }, TaskCreationOptions.LongRunning))
                    //.Select(task => task.WaitAsync(TimeSpan.FromSeconds(20)))
                    .ToList();

                await Task.WhenAll(tasks);
            }

            Console.WriteLine($@"Unique package versions: [{string.Join(',', versions.Select(v => $"{(ushort)(v >> 16)}/{(ushort)v}"))}]");

            Assert.IsFalse(exceptions.Any(), $"{exceptions.Count} exceptions: {string.Join('\n', exceptions)}");
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

                //try
                //{
                obj.Load();
                //}
                //catch (Exception exception)
                //{
                //    // Catch, because we don't want to stop the test, not until all objects have been loaded.
                //    LibServices.LogService.Log(exception.ToString());
                //    obj.ThrownException = exception;
                //}

                // Lazy deserialization tests
                try
                {
                    switch (obj)
                    {
                        case UStruct { ByteCodeManager.DeserializedTokens: null } uStruct:
                            uStruct.ByteCodeManager.Deserialize();
                            break;
                    }
                }
                catch (Exception exception)
                {
                    exceptions.Add(new DeserializationException($"Script deserialization exception in {obj.GetReferencePath()}", exception));

                    // Catch, because we don't want to stop the test, not until all objects have been loaded.
                    LibServices.LogService.Log($"Script deserialization exception {exception} for {obj.GetReferencePath()}");
                }
            }

            void AssertObjects(IEnumerable<UObject> objects)
            {
                var exportObjects = objects.ToList();
                var compatibleExports = exportObjects
                    .Where(exp => exp is not UnknownObject)
                    .ToList();

                compatibleExports.ForEach(TryLoadObject);

                var incompatibleExports = exportObjects
                    .Where(exp => exp is UnknownObject)
                    .ToList();

                if (incompatibleExports.Count == 0)
                {
                    return;
                }

                Console.WriteLine($@"Found {incompatibleExports.Count} unrecognized exports in '{linker.FullPackageName}'");

                var incompatibleClasses =
                    incompatibleExports
                        .Where(exp => !exp.IsTemplate())
                        .DistinctBy(exp => exp.Class)
                        .ToList();
                Console.WriteLine($@"Found {incompatibleClasses.Count} unrecognized classes [{string.Join(',', incompatibleClasses)}]");
                // Commented out, because it gets too long.
                //Console.Write($@"{string.Join(';', incompatibleExports.Select(exp => exp.GetReferencePath()))}");
            }
        }
    }
}
