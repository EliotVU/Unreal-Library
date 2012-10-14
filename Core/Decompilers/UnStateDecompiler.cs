#if DECOMPILE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib;
using UELib.Core;
using UELib.Tokens;

namespace UELib.Core
{
	public partial class UState : UStruct
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
			UDecompiler.AddTabs( 1 );
				content += FormatIgnores() +
					FormatConstants() +
					FormatFunctions() + (_ChildFunctions.Count == 0 ? "\r\n" : String.Empty) + 
					DecompileStateCode();
			UDecompiler.RemoveTabs( 1 );
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

			string Output = "\r\n" + UDecompiler.Tabs + "ignores ";
			List<string> ignores = new List<string>();
			//long ignoremask = _IgnoreMask;
			foreach( UFunction Func in _ChildFunctions )
			//foreach( UnrealNameTable N in Owner.NameTableList )
			{
				if( (Func.FunctionFlags & (uint)Flags.FunctionFlags.Defined) != 0 )
				{
					continue;
				}
				ignores.Add( Func.Name );
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
				Output += ignores[i] + ((ignores[i] != ignores.Last()) ? ((", " + 
					((i % 5 == 0 && i >= 5) ? "\r\n\t" + UDecompiler.Tabs : String.Empty))) : (";\r\n"));
			}
			return (ignores.Count > 0) ? Output : String.Empty;
		}

		protected string FormatFunctions()
		{
			string Output = String.Empty;

			// Remove functions from parent state, e.g. when overriding states.
			List<UFunction> functions = new List<UFunction>();
			foreach( UFunction Func in _ChildFunctions )
			{
				if( GetType() == typeof(UState) )
				{
					// Has a body?
					if( !Func.HasFunctionFlag( Flags.FunctionFlags.Defined ) )
					{
						continue;
					}
				}
				functions.Add( Func );
			}

			foreach( UFunction Func in functions )
			{
				string FuncOutput = String.Empty;
				FuncOutput = UDecompiler.Tabs + Func.Decompile();

				// And add a empty line between all functions, except empty functions!
				Output += (FuncOutput.EndsWith( ");" ) ? ("\r\n" + FuncOutput) : 
					("\r\n" + FuncOutput + (Func != _ChildFunctions.Last() ? "\r\n" : String.Empty)) );
			}
			return Output + (Output.Length != 0 ? "\r\n" : String.Empty);
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