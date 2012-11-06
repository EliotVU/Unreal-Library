using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UELib;
using UELib.Core;

namespace UELib.Core
{
	public partial class UTextBuffer : UObject
	{
		public override string Decompile()
		{
			if( ShouldDeserializeOnDemand )
			{
				BeginDeserializing();
			}

			string output = String.Empty;
			if( ScriptText.Length <= 2 )
			{
				output += "// Stripped";
			}

			if( ScriptText.Length > 0 )
			{
				output = ScriptText + output;
				// Only ScriptTexts should merge defaultproperties.
				if( Name == "ScriptText" )
				{
					var outerStruct = Outer as UStruct;
					if( outerStruct != null && outerStruct.Properties != null && outerStruct.Properties.Count > 0 )
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