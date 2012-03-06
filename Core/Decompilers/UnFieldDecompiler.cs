#if DECOMPILE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib;
using UELib.Core;

namespace UELib.Core
{
	public partial class UField : UObject
	{
		protected string DecompileMeta()
		{
			return Meta != null ? Meta.Decompile() : String.Empty; 
		}

		// Introduction of the change from intrinsic to native.
		protected const uint NativeVersion = 100;
		// Introduction of the change from expands to extends.
		protected const uint ExtendsVersion = 100;

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