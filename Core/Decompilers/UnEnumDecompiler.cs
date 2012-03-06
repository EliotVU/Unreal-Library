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
			return UDecompiler.Tabs + FormatHeader() +
				"\r\n" + 
				UDecompiler.Tabs + "{" +
				FormatNames() +
				UDecompiler.Tabs + "};";
		}

		protected override string FormatHeader()
		{
			return "enum " + Name + DecompileMeta();
		}

		private string FormatNames()
		{
			string Output = "\r\n";
			UDecompiler.AddTabs( 1 );
			foreach( int Index in _NamesIndex )
			{
				Output += UDecompiler.Tabs + Package.NameTableList[Index].Name + (Index != _NamesIndex.Last() ? ",\r\n" : "\r\n");
			}
			UDecompiler.RemoveTabs( 1 );
			return Output;
		}
	}
}
#endif