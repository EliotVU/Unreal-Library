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
			if( _bDeserializeOnDemand )
			{
				BeginDeserializing();
			}

			if( ScriptText.Length != 0 )
			{
				if( Outer is UStruct )
				{
					try
					{
						return ScriptText + ((UClass)Outer).FormatDefaultProperties();
					}
					catch
					{
						return ScriptText + "\r\n// Failed to decompile defaultproperties for this object.";
					}
				}
				return ScriptText;
			}
			return "TextBuffer is empty!";
		}
	}	
}
