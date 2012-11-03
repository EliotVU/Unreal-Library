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

			if( ScriptText.Length != 0 )
			{
				// Only ScriptTexts should merge defaultproperties.
				if( Name == "ScriptText" )
				{
					var outerStruct = Outer as UStruct;
					if( outerStruct != null && outerStruct.Properties != null && outerStruct.Properties.Count > 0 )
					{
						try
						{
							return ScriptText + "// Decompiled with UE Explorer." + outerStruct.FormatDefaultProperties();
						}
						catch
						{
							return ScriptText + "\r\n// Failed to decompile defaultproperties for this object.";
						}	
					}
				}
				return ScriptText;
			}
			return "TextBuffer is empty!";
		}
	}	
}