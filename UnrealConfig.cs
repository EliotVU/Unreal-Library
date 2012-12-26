using System;
using System.Collections.Generic;
using UELib.Types;

namespace UELib
{
	public static class UnrealConfig
	{
		#region Config
		public static bool SuppressComments;
		public static bool SuppressSignature;

		public static string PreBeginBracket = UnrealSyntax.NewLine + "{0}";
		public static string PreEndBracket = UnrealSyntax.NewLine + "{0}";
		public static string Indention = "\t";

		public enum CookedPlatform
		{
			PC,
			Console
		}
		public static CookedPlatform Platform;
		public static Dictionary<string, Tuple<string, PropertyType>> VariableTypes;
		#endregion

		public static string PrintBeginBracket()
		{
			return String.Format( PreBeginBracket, UDecompilingState.Tabs ) + UnrealSyntax.BeginBracket;
		}

		public static string PrintEndBracket()
		{
			return String.Format( PreEndBracket, UDecompilingState.Tabs ) + UnrealSyntax.EndBracket;
		}

		public static string ToUFloat( this float value )
		{		
			return value.ToString( "0.0000000000" ).TrimEnd( '0' ).Replace( ',', '.' ) + '0';		
		}
	}

	public static class UnrealSyntax
	{
		public const string NewLine = "\r\n";	
		public const string BeginBracket = "{";
		public const string EndBracket = "}";
		public const string BeginParentheses = "(";
		public const string EndParentheses = ")";
	}

	public static class UDecompilingState
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible" )]
		public static string Tabs = String.Empty;

		public static void AddTabs( int count )
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

		public static void RemoveSpaces( int count )
		{
			if( Tabs.Length < count )
			{
				Tabs = String.Empty;
				return;
			}
			Tabs = Tabs.Substring( 0, Tabs.Length - count );	
		}

		public static void ResetTabs()
		{
			Tabs = String.Empty;
		}
	}
}
