using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace UELib.Test
{
    public class L2PackageTests
    {
        /// <summary>
        /// This could be any unreal package file. In this particular scenario a decrypted LineageII map file is used.
        /// </summary>
        private const string MAP_PACKAGE_PATH = @"F:\lineage2\Lineage2Community\Assets\_export~\maps_info\20_18.unr";
        
        [TestCase(MAP_PACKAGE_PATH, "TerrainInfo")]
        [TestCase(MAP_PACKAGE_PATH, "Light")]
        [TestCase(MAP_PACKAGE_PATH, "StaticMeshActor")]
        [TestCase(MAP_PACKAGE_PATH, "ZoneInfo")]
        [TestCase(MAP_PACKAGE_PATH, "LevelInfo")]
        [TestCase(MAP_PACKAGE_PATH, "AmbientSoundObject")]
        [TestCase(MAP_PACKAGE_PATH, "Emitter")]
        [TestCase(MAP_PACKAGE_PATH, "PhysicsVolume")]
        public void LoadMapPackage_DecompileObjects_ShouldBeValidJson(string mapPath, string objectTypeName)
        {
            var package = UnrealLoader.LoadFullPackage(mapPath);

            for (var i = 0; i < package.Objects.Count; i++)
            {
                var obj = package.Objects[i];
                
                // obj.Name.Length > objectTypeName.Length is to include only instances, not bare class types
                if (!obj.Name.StartsWith(objectTypeName) && obj.Name.Length > objectTypeName.Length)
                    continue;
                
                try
                {
                    var json = obj.Decompile();
                
                    TestContext.Out.Write(json);
                
                    Assert.False(string.IsNullOrEmpty(json));
                    Assert.IsTrue(JsonConvert.DeserializeObject(json) != null);
                }
                catch (Exception e)
                {
                    // catch for a breakpoint
                    Console.WriteLine(e);
                }
            }
        }
    }
}