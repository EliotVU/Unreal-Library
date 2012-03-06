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
			return (ScriptText.Length != 0 ? ScriptText : "TextBuffer is empty!");	
		}
	}	
}
