using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            if (ShouldDeserializeOnDemand)
            {
                BeginDeserializing();
            }

            string output = $"// Reference: {GetReferencePath()}\r\n";

            if (ImportTable != null)
            {
                return output + $"\r\n{UDecompilingState.Tabs}// Cannot decompile an imported object";
            }

            // FIXME: Won't be detected, an UnknownObject might be a UComponent.
            if (this is UComponent uComponent)
            {
                output += $"{UDecompilingState.Tabs}// TemplateOwnerClass: {PropertyDisplay.FormatLiteral(uComponent.TemplateOwnerClass)}\r\n";
                output += $"{UDecompilingState.Tabs}// TemplateOwnerName: {PropertyDisplay.FormatLiteral(uComponent.TemplateName)}\r\n";
            }

            if (Archetype != null)
            {
                output += $"{UDecompilingState.Tabs}// Archetype: {PropertyDisplay.FormatLiteral(Archetype)}\r\n";
            }

            output += UDecompilingState.Tabs;
            output += $"begin object name=\"{Name}\"";
            // If null then we have a new sub-object (not an override)
            if (Archetype == null)
            {
                Debug.Assert(Class != null);
                output += $" class={Class.GetReferencePath()}";
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
                   $"{UDecompilingState.Tabs}end object";
        }

        public virtual string FormatHeader()
        {
            return GetReferencePath();
        }

        protected string DecompileProperties()
        {
            if (Properties == null || Properties.Count == 0)
                return string.Empty;

            string output = string.Empty;
            
            // HACK: Start with a fresh scope.
            var oldState = UDecompilingState.s_inlinedSubObjects;
            UDecompilingState.s_inlinedSubObjects = new Dictionary<UObject, bool>();

            try
            {
                string propertiesText = string.Empty;
                for (int i = 0; i < Properties.Count; ++i)
                {
                    //output += $"{UDecompilingState.Tabs}// {Properties[i].Type}\r\n";
                    string propertyText = Properties[i].Decompile();

                    // This is the first element of a static array
                    if (i + 1 < Properties.Count
                        && Properties[i + 1].Name == Properties[i].Name
                        && Properties[i].ArrayIndex <= 0
                        && Properties[i + 1].ArrayIndex > 0)
                    {
                        propertyText = propertyText.Insert(Properties[i].Name.Length, PropertyDisplay.FormatT3DElementAccess("0", Package.Version));
                    }

                    propertiesText += $"{UDecompilingState.Tabs}{propertyText}\r\n";
                }

                // HACK: Inline sub-objects that we could not inline directly, such as in an array or struct.
                // This will still miss sub-objects that have no reference.
                var missingSubObjects = UDecompilingState.s_inlinedSubObjects
                    .Where((k, v) => k.Value == false)
                    .Select(k => k.Key);
                foreach (var obj in missingSubObjects)
                {
                    obj.BeginDeserializing();

                    string objectText = obj.Decompile();
                    output += $"{UDecompilingState.Tabs}{objectText}\r\n";
                }

                output += propertiesText;
            }
            finally
            {
                UDecompilingState.s_inlinedSubObjects = oldState;
            }

            return output;
        }
    }
}
