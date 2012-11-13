#if DECOMPILE
using System;
using System.Linq;

namespace UELib.Core
{
	public partial class UEnum
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
			string output = "\r\n";
			UDecompilingState.AddTabs( 1 );
			foreach( int index in _NamesIndex )
			{
				output += UDecompilingState.Tabs + Package.NameTableList[index].Name + (index != _NamesIndex.Last() ? ",\r\n" : String.Empty);
			}
			UDecompilingState.RemoveTabs( 1 );
			return output;
		}
	}
}
#endif