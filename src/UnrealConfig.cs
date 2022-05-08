using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UELib.Annotations;
using UELib.Types;
using UELib.UnrealScript;

namespace UELib
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnassignedField.Global")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    [SuppressMessage("ReSharper", "ConvertToConstant.Global")]
    [PublicAPI("UE Explorer")]
    public static class UnrealConfig
    {
        #region Config

        public static bool SuppressComments;
        public static bool SuppressSignature;

        public static string PreBeginBracket = "\r\n{0}";
        public static string PreEndBracket = "\r\n{0}";
        public static string Indention = "\t";

        public enum CookedPlatform
        {
            PC,
            Console
        }

        public static CookedPlatform Platform;
        public static Dictionary<string, Tuple<string, PropertyType>> VariableTypes;

        #endregion

        public static string PrintBeginBracket()
        {
            return $"{string.Format(PreBeginBracket, UDecompilingState.Tabs)}{ScriptConstants.BeginBracket}";
        }

        public static string PrintEndBracket()
        {
            return $"{string.Format(PreEndBracket, UDecompilingState.Tabs)}{ScriptConstants.EndBracket}";
        }
    }
}