using System.Collections.Generic;
using System.Linq;

namespace UELib
{
    public static class UnrealExtensions
    {
        public const string UnrealCodeExt = ".uc";
        public const string UnrealFlagsExt = ".UPKG";

        public static readonly string[] Code = { ".d2u", ".t3u" };
        public static readonly string[] Map =
        {
            ".ut2", ".udk", ".unr",
            ".rom", ".un2", ".aao",
            ".run", ".sac", ".xcm",
            ".nrf", ".wot", ".scl",
            ".dvs", ".rsm", ".ut3",
            ".umap"
        };

        public static readonly string[] Other = { ".md5", ".usa", ".ums", ".rsa", ".sav" };
        public static readonly string[] Mod = { ".umod", ".ut2mod", ".ut4mod" };

        public static readonly string[] Common = { ".u", ".upk", ".xxx", ".umap", ".uasset" };
        public static readonly string[] Legacy =
        {
            ".utx", ".uax", ".umx",
            ".usx", ".upx", ".ugx",
            ".ukx", ".uxx", ".uvx"
        };

        public static string FormatUnrealExtensionsAsFilter()
        {
            string commonFilter = string.Empty;
            commonFilter = Common.Aggregate(commonFilter, (current, ext) => current + "*" + ext + ";");
            
            string mapFilter = string.Empty;
            mapFilter = Map.Aggregate(mapFilter, (current, ext) => current + "*" + ext + ";");
            
            string legacyFilter = string.Empty;
            legacyFilter = Legacy.Aggregate(legacyFilter, (current, ext) => current + "*" + ext + ";" );

            return "All Files (*.*)|*.*";
            //$"Unreal Files ()|{commonFilter}|" +
            //$"Unreal Legacy Files ()|{legacyFilter}|" +
            //$"Unreal Map Files ()|{mapFilter}";
        }

        public static List<string> FormatUnrealExtensionsAsList()
        {
            var extensionsList = new List<string>
            (
                Common.Length +
                Legacy.Length +
                Map.Length +
                Other.Length
            );

            extensionsList.AddRange(Common);
            extensionsList.AddRange(Legacy);
            extensionsList.AddRange(Map);
            extensionsList.AddRange(Other);
            return extensionsList;
        }
    }
}
