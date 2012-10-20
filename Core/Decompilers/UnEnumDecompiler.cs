#if DECOMPILE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib;
using UELib.Core;
using UELib.Flags;

namespace UELib.Core
{
	public partial class UEnum : UField
	{
		/// <summary>
		/// Decompiles this object into a text format of:
		/// 
		///	enum NAME
		///	{
		///		[ELEMENTS]
		/// };
		/// </summary>
		/// <returns></returns>
		public override string Decompile()
		{
			return UDecompilingState.Tabs + FormatHeader() +
				UnrealConfig.PrintBeginBracket() +
				FormatNames() +
				UnrealConfig.PrintEndBracket()  + ";";
		}

		protected override string FormatHeader()
		{
			return "enum " + Name + DecompileMeta();
		}

		private string FormatNames()
		{
			string Output = "\r\n";
			UDecompilingState.AddTabs( 1 );
			foreach( int Index in _NamesIndex )
			{
				Output += UDecompilingState.Tabs + Package.NameTableList[Index].Name + (Index != _NamesIndex.Last() ? ",\r\n" : String.Empty);
			}
			UDecompilingState.RemoveTabs( 1 );
			return Output;
		}
	}
}
#endif