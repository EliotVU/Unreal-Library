#if DECOMPILE
using System;

namespace UELib.Core
{
	public partial class UField
	{
		protected string DecompileMeta()
		{
			return Meta != null ? Meta.Decompile() : String.Empty; 
		}

		// Introduction of the change from intrinsic to native.
		private const uint NativeVersion = 100;
		// Introduction of the change from expands to extends.
		private const uint ExtendsVersion = 100;
		protected const uint PlaceableVersion = 100;

		protected string FormatNative()
		{
			if( Package.Version >= NativeVersion )
			{
				return "native";
			}
			return "intrinsic";
		}

		protected string FormatExtends()
		{
			if( Package.Version >= ExtendsVersion )
			{
				return "extends";
			}
			return "expands";
		}
	}
}
#endif