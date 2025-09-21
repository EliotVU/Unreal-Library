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
        [TestMethod]
        [DataRow(@"Unreal\",
            BuildName.Unreal1,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UnrealTournament\",
            BuildName.UT,
            BuildPlatform.Undetermined
        )]
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
        [DataRow(@"UT2004\",
            BuildName.UT2004,
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
        [DataRow(@"ArcticCombat\",
            BuildName.ArcticCombat,
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
        [DataRow(@"Unreal 2\",
            BuildName.Unreal2,
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
        [DataRow(@"MedalOfHonorAirborne\",
            BuildName.MoHA,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"MedalOfHonor2010\",
            BuildName.MoH,
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
        [DataRow(@"Tom Clancy's Splinter Cell Double Agent\SCDA-Offline\",
            BuildName.SCDA_Offline,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Tom Clancy's Splinter Cell Double Agent\SCDA-Online\",
            BuildName.SCDA_Online,
            BuildPlatform.Undetermined
        )]
        //[DataRow(@"Tom Clancy's Splinter Cell Conviction\",
        //    BuildName.SCBL,
        //    BuildPlatform.Undetermined
        //)]
        [DataRow(@"Tom Clancy's Splinter Cell® Blacklist\",
            BuildName.SCBL,
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
        [DataRow(@"CrimeCraft\",
            BuildName.CrimeCraft,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Batman Asylum\",
            BuildName.Batman1,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Batman Knight\",
            BuildName.Batman4,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Singularity\",
            BuildName.Singularity,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"BioShockInfinite\",
            BuildName.Bioshock_Infinite,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Dishonored\",
            BuildName.Dishonored,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Remember Me\",
            BuildName.RememberMe,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"Hawken\",
            BuildName.Hawken,
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
            BuildGeneration forcedGeneration = BuildGeneration.Undefined,
            bool shouldTestSerialization = false)
        {
            string? gamesPath = Environment.GetEnvironmentVariable("UEGamesTestDirectory");
            packagesPath = Path.Join(gamesPath, packagesPath);
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

                        if (linker.Summary.CompressedChunks?.Count > 0)
                        {
                            Console.WriteLine($@"Skipping analysis for compressed package '{filePath}'");

                            return;
                        }

                        AssertPackage(linker, exceptions);
                    }, TaskCreationOptions.LongRunning))
                    //.Select(task => task.WaitAsync(TimeSpan.FromSeconds(20)))
                    .ToList();

                await Task.WhenAll(tasks);
            }

            Console.WriteLine($@"Unique package versions: [{string.Join(',', versions.Select(v => $"{(ushort)(v >> 16)}/{(ushort)v}"))}]");

            Assert.AreEqual(0, exceptions.Count, $"{exceptions.Count} exceptions: {string.Join('\n', exceptions)}");
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

        [TestMethod]
        // All (non-cooked) UDK builds that differ in package format.
        [DataRow(@"UDK-2009-11\", // Also includes The Ball (Game)
            BuildName.Default, // v648
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2009-12\",
            BuildName.Default, // v664
            BuildPlatform.Undetermined
        )]
        // 01-04 ??
        [DataRow(@"UDK-2010-05\", // Also includes (Updated) The Ball (Game)
            BuildName.Default, // v706
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2010-06\", // BioShock Infinite (but, heavily modified)
            BuildName.Default, // v727
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2010-07\",
            BuildName.Default, // v737
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2010-08\",
            BuildName.Default, // v756
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2010-09\",
            BuildName.Default, // v765
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2010-10\",
            BuildName.Default, // v787
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2010-11\",
            BuildName.Default, // v799
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2010-12\",
            BuildName.Default, // v803
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2011-01\", // Rock of Ages, Tribes (Modified), Batman Asylum (Heavily modified)
            BuildName.Default, // v805
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2011-02\", // Sanctum
            BuildName.Default, // v810
            BuildPlatform.Undetermined
        )]
        // 03 no difference in package format.
        [DataRow(@"UDK-2011-04\",
            BuildName.Default, // v813
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2011-06\", // Remember Me (Modified), Borderlands 2 (Heavily modified), Quantum (Modified)
            BuildName.Default, // v832
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2011-07\",
            BuildName.Default, // v840
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2011-08\", // FoxGame
            BuildName.Default, // v841
            BuildPlatform.Undetermined
        )]
        // 2011/09 - 2012/04 no difference in package format.
        [DataRow(@"UDK-2012-05\", // PainKillerHD
            BuildName.Default, // v860
            BuildPlatform.Undetermined
        )]
        // 06 no difference in package format.
        [DataRow(@"UDK-2012-07\", // AOC (Chivalry: Medieval Warfare), Hawken (Modified)
            BuildName.Default, // v860
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2012-11\",
            BuildName.Default,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2013-02\",
            BuildName.Default,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2013-07\",
            BuildName.Default,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2014-02\",
            BuildName.Default,
            BuildPlatform.Undetermined
        )]
        [DataRow(@"UDK-2015-02\", // 29 january 2015
            BuildName.Default, // v868
            BuildPlatform.Undetermined
        )]
        public async Task TestUDKPackages(
            string packagesPath,
            BuildName packagesBuild = BuildName.Unset,
            BuildPlatform packagesPlatform = BuildPlatform.Undetermined,
            BuildName forcedBuild = BuildName.Unset,
            BuildGeneration forcedGeneration = BuildGeneration.Undefined,
            bool shouldTestSerialization = true)
        {
            await TestPackages(packagesPath, packagesBuild, packagesPlatform, forcedBuild, forcedGeneration,
                shouldTestSerialization);
        }
    }
}
