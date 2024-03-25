namespace UELib.Core
{
    public partial class UTextBuffer
    {
        public override string Decompile()
        {
            if (ShouldDeserializeOnDemand)
            {
                BeginDeserializing();
            }

            var output = string.Empty;
            if (ScriptText.Length <= 2)
            {
                output += "// Stripped";
            }

            if (ScriptText.Length > 0)
            {
                output = ScriptText + output;
                // Only ScriptTexts should merge defaultproperties.
                if (Name == "ScriptText")
                {
                    var outerStruct = Outer as UStruct;
                    if (outerStruct?.Properties != null && outerStruct.Properties.Count > 0)
                    {
                        try
                        {
                            output += "\r\n// Decompiled with UE Explorer." + outerStruct.FormatDefaultProperties();
                        }
                        catch
                        {
                            output += "\r\n// Failed to decompile defaultproperties for this object.";
                        }
                    }
                }
            }

            return output;
        }
    }
}