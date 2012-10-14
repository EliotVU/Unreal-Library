/***********************************************************************
 *	Author - Eliot Van Uytfanghe
 *	Copyright - (C) Eliot Van Uytfanghe 2009 - 2010 
 **********************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib;
using UELib.Core;

namespace UELib.Core
{
	public partial class UObject : IUnrealDecompilable
	{
		/// <summary>
		/// Decompiles this Object into human-readable code
		/// </summary>
		public virtual string Decompile()
		{
			if( ShouldDeserializeOnDemand )
			{
				BeginDeserializing();
			}

			string output = "begin object name=" + Name + " class=" + Class.Name + " \r\n";
			UDecompiler.AddTabs( 1 );
			try
			{
				output += DecompileProperties();	
			}
			finally
			{
				UDecompiler.RemoveTabs( 1 );
			}
			return output + UDecompiler.Tabs + "object end";		
		}

		// Ment to be overriden!
		protected virtual string FormatHeader()
		{
			// Note:Dangerous recursive call!
			return Decompile();
		}

		public string DecompileProperties()
		{
			if( _Properties == null || _Properties.Count == 0 )
				return UDecompiler.Tabs + "// This object has no properties!\r\n";

			string output = String.Empty;
			for( int i = 0; i < _Properties.Count; ++ i )
			{
				string propOutput = _Properties[i].Decompile();

				// This is the first element of a static array
				if( i+1 < _Properties.Count 
					&& _Properties[i+1].Tag.Name == _Properties[i].Tag.Name 
					&& _Properties[i].Tag.ArrayIndex <= 0
					&& _Properties[i+1].Tag.ArrayIndex > 0 )
				{
					propOutput = propOutput.Insert( _Properties[i].Tag.Name.Length, "[0]" );
				}

				// FORMAT: 'DEBUG[TAB /* 0xPOSITION */] TABS propertyOutput + NEWLINE
				output += UDecompiler.Tabs +
#if DEBUG
				"/*" + UnrealMethods.FlagToString( (uint)_Properties[i].Tag.PropertyOffset ) + "*/\t" +
#endif
				propOutput + "\r\n";
			}

			return output;
		}
	}
}
