using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UELib
{
	public static class UnrealConfig
	{
		public const string NewLine = "\r\n";

		#region Config
		public static bool SuppressComments;
		public static bool SuppressSignature;
		public static string PreBeginBracket = NewLine + "{0}";
		public static string PreEndBracket = NewLine + "{0}";
		public static string Indention = "\t";
		public enum CookedPlatform
		{
			PC,
			Console
		}
		public static CookedPlatform Platform;

		public class VariableType
		{
			public string VFullName;
			public string Name
			{
				get{ return VFullName.Substring( VFullName.LastIndexOf( '.' ) + 1 ); }
			}
			public string VType;
		}

		public static List<VariableType> VariableTypes;
		#endregion

		public static string PrintBeginBracket()
		{
			return String.Format( PreBeginBracket, UDecompilingState.Tabs ) + "{";
		}

		public static string PrintEndBracket()
		{
			return String.Format( PreEndBracket, UDecompilingState.Tabs ) + "}";
		}

		public static string ToUFloat( this float value )
		{		
			return value.ToString( "0.000000" ).TrimEnd( '0' ).Replace( ',', '.' ) + '0';		
		}
	}

	public static class UDecompilingState
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible" )]
		public static string Tabs = String.Empty;

		public static void AddTabs( byte count )
		{
			for( int i = 0; i < count; ++ i )
			{
				Tabs += UnrealConfig.Indention;
			}
		}

		public static void AddTab()
		{
			Tabs += UnrealConfig.Indention;
		}

		public static void RemoveTabs( int count )
		{
			count *= UnrealConfig.Indention.Length;
			Tabs = count > Tabs.Length ? String.Empty : Tabs.Substring( 0, (Tabs.Length) - count );
		}

		public static void RemoveTab()
		{
			Tabs = Tabs.Substring( 0, Tabs.Length - UnrealConfig.Indention.Length );
		}

		public static void ResetTabs()
		{
			Tabs = String.Empty;
		}
	}
}
