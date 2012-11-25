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
		private const uint NativeVersion = 69;
		// Introduction of the change from expands to extends.
		private const uint ExtendsVersion = 69;
		protected const uint PlaceableVersion = 69;

		protected string FormatNative()
		{
			return Package.Version >= NativeVersion ? "native" : "intrinsic";
		}

		protected string FormatExtends()
		{
			return Package.Version >= ExtendsVersion ? "extends" : "expands";
		}
	}
}
#endif