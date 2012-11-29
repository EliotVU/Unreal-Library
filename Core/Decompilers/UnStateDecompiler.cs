#if DECOMPILE
using System;
using System.Collections.Generic;
using System.Linq;

namespace UELib.Core
{
	public partial class UState
	{
		/// <summary>
		/// Decompiles this object into a text format of:
		/// 
		///	[FLAGS] state[()] NAME [extends NAME]
		///	{
		///		[ignores Name[,Name];]
		///		
		///		[FUNCTIONS]
		///		
		/// [STATE CODE]
		/// };
		/// </summary>
		/// <returns></returns>
		public override string Decompile()
		{	
			string content = FormatHeader() + UnrealConfig.PrintBeginBracket();
			UDecompilingState.AddTabs( 1 );
				content += FormatIgnores() +
					FormatConstants() +
					FormatFunctions() + (_ChildFunctions.Count == 0 ? "\r\n" : String.Empty) + 
					DecompileStateCode();
			UDecompilingState.RemoveTabs( 1 );
			content += UnrealConfig.PrintEndBracket();	
			return content;
		}

		private string GetAuto()
		{
			if( (StateFlags & (uint)Flags.StateFlags.Auto) != 0 )
			{
				return "auto ";
			}
			return String.Empty;
		}

		private string GetSimulated()
		{
			if( (StateFlags & (uint)Flags.StateFlags.Simulated) != 0 )
			{
				return "simulated ";
			}
			return String.Empty;
		}

		private string GetEdit()
		{
			if( (StateFlags & (uint)Flags.StateFlags.Editable) != 0 )
			{
				return "()";
			}
			return String.Empty;
		}  

		protected override string FormatHeader()
		{
			string output = GetAuto() + GetSimulated() + "state" + GetEdit() + " " + Name;
 			if( Super != null && Super.Name != Name
				/* Not the same because when overriding states it automatic extends the parent state */ )
			{
				output += " " + FormatExtends() + " " + Super.Name;
			}
			return output;
		}

		/// <summary>
		/// Currently broken...
		/// </summary>
		private string FormatIgnores()
		{
			if( _IgnoreMask == ulong.MaxValue )
			{
				return String.Empty;
			}

			string output = "\r\n" + UDecompilingState.Tabs + "ignores ";
			var ignores = new List<string>();
			//long ignoremask = _IgnoreMask;
			foreach( var func in _ChildFunctions )
			//foreach( UnrealNameTable N in Owner.NameTableList )
			{
				if( (func.FunctionFlags & (uint)Flags.FunctionFlags.Defined) != 0 )
				{
					continue;
				}
				ignores.Add( func.Name );
				/*if( Func.NameIndex >= ProbeMin && 
					Func.NameIndex < ProbeMax )
				{
					long ignored = (long)(ulong)1 << (Func.NameIndex - ProbeMin);
					if( (ignoremask & ignored) != 0 )
					{
						ignoremask &= ~ignored;
						ignores.Add( Func.GetName() );
					}
				}*/
			}		
			for( int i = 0; i < ignores.Count; ++ i )
			{
				output += ignores[i] + ((ignores[i] != ignores.Last()) ? ((", " + 
					((i % 5 == 0 && i >= 5) ? "\r\n\t" + UDecompilingState.Tabs : String.Empty))) : (";\r\n"));
			}
			return (ignores.Count > 0) ? output : String.Empty;
		}

		protected string FormatFunctions()
		{
			string output = String.Empty;

			// Remove functions from parent state, e.g. when overriding states.
			var functions = new List<UFunction>();
			foreach( var func in _ChildFunctions )
			{
				if( GetType() == typeof(UState) )
				{
					// Has a body?
					if( !func.HasFunctionFlag( Flags.FunctionFlags.Defined ) )
					{
						continue;
					}
				}
				functions.Add( func );
			}

			foreach( var func in functions )
			{
				try
				{
					string funcOutput = UDecompilingState.Tabs + func.Decompile();

					// And add a empty line between all functions, except empty functions!
					output += (funcOutput.EndsWith( ");" ) ? ("\r\n" + funcOutput) :
						("\r\n" + funcOutput + (func != _ChildFunctions.Last() ? "\r\n" : String.Empty)));
				}
				catch( Exception e )
				{
					output += "\r\n" + UDecompilingState.Tabs + "// F:" + func.Name + " E:" + e;	
				}
			}
			return output + (output.Length != 0 ? "\r\n" : String.Empty);
		}

		private string DecompileStateCode()
		{
			if( ScriptSize <= 0 )
			{
				return String.Empty;
			}
			return ByteCodeManager.Decompile();
		}
	}
}
#endif