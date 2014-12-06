using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using UELib;
using UELib.Core;
namespace UELib
{
    public static class LineagePackageLoader
    {
        private static Dictionary<string, string> L2Paths;
        private static string L2RootDir;
        private static readonly List<UnrealPackage> CachedPackages = new List<UnrealPackage>();
        public static UnrealPackage LoadPackage(string packagePath, FileAccess fileAccess = FileAccess.Read)
        {
            var stream = new UPackageStream(packagePath, FileMode.Open, fileAccess);
            var package = new UnrealPackage(stream);
            package.Deserialize(stream);
            return package;
        }

        public static T[] RemoveAt<T>(this T[] source, int index)
        {
            T[] dest = new T[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }
        public static UObject FindObject(string fullname)
        {
            string[] words = fullname.Split('.');
            UnrealPackage p = LoadFullPackage(words[0]);
            words = words.RemoveAt(0);
            p.FindObjectByGroup(String.Join(".", words));
            return p.FindObjectByGroup(String.Join(".", words));

        }
        public static UnrealPackage LoadFullPackage(string name, FileAccess fileAccess = FileAccess.Read)
        {
            var package = CachedPackages.Find(pkg => pkg.PackageName == name);
            if (package == null)
            {
                if (L2RootDir == "")
                {
                    return null;
                }
                foreach (KeyValuePair<string, string> kvp in L2Paths)
                {
                    string path = Path.Combine(L2RootDir, kvp.Key);
                    string file = Path.Combine(path, name + "." + kvp.Value);
                    if (File.Exists(file))
                    {
                        package = LoadPackage(file, fileAccess);
                        if (package != null)
                        {
                            package.InitializePackage();
                            CachedPackages.Add(package);
                        }
                        break;
                    }
                }
            }
            return package;
        }
        public static bool Initialize(string root_path)
        {
            if (!Directory.Exists("Decrypt"))
                Directory.CreateDirectory("Decrypt");
            L2Paths = new Dictionary<string, string>();
            L2Paths.Add("Animations", "ukx");
            L2Paths.Add("MAPS", "unr");
            L2Paths.Add("Sounds", "uax");
            L2Paths.Add("StaticMeshes", "usx");
            L2Paths.Add("SysTextures", "utx");
            L2Paths.Add("Textures", "utx");
            // Verify that all dirs are under this root.
            foreach (KeyValuePair<string, string> kvp in L2Paths)
            {
                if (!Directory.Exists(Path.Combine(root_path, kvp.Key)))
                {
                    return false;
                }
            }
            L2RootDir = root_path;
            return true;
        }
    }
}
