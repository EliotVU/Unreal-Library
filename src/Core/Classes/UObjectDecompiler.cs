using System.Collections.Concurrent;
using UELib.Flags;
using UELib.UnrealScript;

namespace UELib.Core
{
    public partial class UObject : IUnrealDecompilable
    {
        /// <summary>
        /// Decompiles this Object into human-readable code
        /// </summary>
        public virtual string Decompile()
        {
            string output;

            if (!UnrealConfig.SuppressComments)
            {
                output = $"// Reference: {GetReferencePath()}\r\n";
            }
            else
            {
                output = "";
            }

            if (!UnrealConfig.SuppressComments && Archetype != null)
            {
                output += $"{UDecompilingState.Tabs}// " +
                          $"Archetype: {PropertyDisplay.FormatLiteral(Archetype)}" +
                          $"\r\n";

                //output += $"{UDecompilingState.Tabs}// " +
                //          $"Class: {PropertyDisplay.FormatLiteral(Class)}" +
                //          $"\r\n";
            }

            // FIXME: Won't be detected, an UnknownObject might be a UComponent.
            if (!UnrealConfig.SuppressComments && this is UComponent uComponent)
            {
                output += $"{UDecompilingState.Tabs}// " +
                          $"TemplateOwnerClass: {PropertyDisplay.FormatLiteral(uComponent.TemplateOwnerClass)} " +
                          $"TemplateOwnerName: {PropertyDisplay.FormatLiteral(uComponent.TemplateName)}" +
                          $"\r\n";
            }

            if (output.EndsWith("\r\n"))
            {
                output += UDecompilingState.Tabs;
            }

            output += $"begin object name=\"{Name}\"";
            // If null then we have a new sub-object (not an override)
            if (Archetype == null)
            {
                // ! UE2 compiler does not properly parse a typed reference path, so instead output the qualified identifier loosely.
                output += $" class={Class.GetPath()}";
            }

            output += "\r\n";

            UDecompilingState.AddTabs(1);
            try
            {
                output += DecompileProperties();
            }
            finally
            {
                UDecompilingState.RemoveTabs(1);
            }

            return $"{output}" +
                   $"{UDecompilingState.Tabs}" +
                   $"end object";
        }

        public virtual string FormatHeader()
        {
            return GetReferencePath();
        }

        protected string DecompileProperties()
        {
            // Ensure that the script properties have been loaded (in most cases these are lazy-loaded)
            if (DeserializationState == 0)
            {
                Load();
            }

            // Re-direct to the default
            if (Default != null && Default != this)
            {
                return Default.DecompileProperties();
            }

            if (Properties.Count == 0)
            {
                return $"{UDecompilingState.Tabs}// No script properties available.\r\n";
            }

            string output = string.Empty;

            // HACK: Start with a fresh scope.
            var oldState = UDecompilingState.s_inlinedSubObjects;
            try
            {
                var inlinedObjectsState = new ConcurrentDictionary<UObject, bool>(new Dictionary<UObject, bool>());
                UDecompilingState.s_inlinedSubObjects = inlinedObjectsState;

                for (int i = 0; i < Properties.Count; ++i)
                {
                    //output += $"{UDecompilingState.Tabs}// {Properties[i].Type}\r\n";
                    string propertyText = Properties[i].Decompile();

                    // This is the first element of an array
                    if (i + 1 < Properties.Count
                        && Properties[i + 1].Name == Properties[i].Name
                        && Properties[i].ArrayIndex <= 0
                        && Properties[i + 1].ArrayIndex > 0)
                    {
                        propertyText = propertyText.Insert(
                            Properties[i].Name.Length,
                            PropertyDisplay.FormatT3DElementAccess("0", Package.Version)
                        );
                    }

                    // HACK: Inline sub-objects that we could not inline directly, such as in an array or struct.
                    // This will still miss sub-objects that have no reference.
                    var objectsToInline = inlinedObjectsState
                        .Where((k, _) => !k.Value)
                        .Select(k => k.Key)
                        .ToList();

                    // Mark pending objects as inlined.
                    foreach (var pendingObject in objectsToInline)
                    {
                        inlinedObjectsState[pendingObject] = true;
                    }

                    // Append the inlined text before the properties that assigned them.
                    string inlinedObjectsText = objectsToInline
                        .Select(obj => obj.Decompile())
                        .Aggregate("", (current, objectText) => current + $"{UDecompilingState.Tabs}{objectText}\r\n");

                    output += inlinedObjectsText;
                    output += $"{UDecompilingState.Tabs}{propertyText}\r\n";
                }
            }
            finally
            {
                UDecompilingState.s_inlinedSubObjects = oldState;
            }

            return output;
        }

        protected static bool TryGetUnknownFlags<T>(ulong remainingFlags, UnrealFlags<T> mappedFlags, out string? output) where T : Enum
        {
            ulong undescribedFlags = mappedFlags
                .EnumerateFlags()
                .Aggregate(remainingFlags, (flags, flagIndex) => flags & ~mappedFlags.FlagsMap[flagIndex]);

            if (undescribedFlags != 0)
            {
                // Get all the undescribed flags.
                output = $"/*0x{undescribedFlags:X}*/ ";

                return true;
            }

            output = null;
            return false;
        }
    }
}
