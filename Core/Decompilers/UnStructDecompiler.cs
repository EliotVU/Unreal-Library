#if DECOMPILE
using System;
using System.Linq;

namespace UELib.Core
{
	public partial class UStruct : UField
	{
		/// <summary>
		/// Decompiles this object into a text format of:
		/// 
		///	struct [FLAGS] NAME [extends NAME]
		///	{
		///		[CONSTS]
		///		
		///		[ENUMS]
		///		
		///		[STRUCTS]
		///		
		///		[VARIABLES]
		///		
		///		[STRUCTDEFAULTPROPERTIES]
		/// };
		/// </summary>
		/// <returns></returns>
		public override string Decompile()
		{
			string Content = UDecompiler.Tabs + FormatHeader() +
			"\r\n" + UDecompiler.Tabs + "{";
			UDecompiler.AddTabs( 1 );
			string Props = FormatProperties();

			string defprops = FormatDefaultProperties();
			if( defprops.Length != 0 )
			{
				defprops += "\r\n";
			}
			UDecompiler.RemoveTabs( 1 );
			return Content + Props + defprops + 
			UDecompiler.Tabs + "};";
		}

		protected override string FormatHeader()
		{
			return "struct " + FormatFlags() + Name + (Super != null ? " " + FormatExtends() + " " + Super.Name : String.Empty);
		}

		private string FormatFlags()
		{
			string Output = String.Empty;
			if( StructFlags == 0 )
			{
				return String.Empty;
			}

			if( (StructFlags & (uint)Flags.StructFlags.Native) != 0 )
			{
				Output += "native ";
			}

			if( (StructFlags & (uint)Flags.StructFlags.Export) != 0 )
			{
				Output += "export ";
			}

			if( Package.Version <= 128 )
			{
				if( (StructFlags & (uint)Flags.StructFlags.Long) != 0 )
				{
					Output += "long ";
				}
			}

			if( (StructFlags & (uint)Flags.StructFlags.Init) != 0 && Package.Version < 222 )
			{
				Output += "init ";
			}
			else if( HasStructFlag( Flags.StructFlags.Transient ) )
			{
				Output += "transient ";
			}

			if( HasStructFlag( Flags.StructFlags.Atomic ) )
			{
				Output += "atomic ";
			}

			if( HasStructFlag( Flags.StructFlags.AtomicWhenCooked ) )
			{
				Output += "atomicwhencooked ";
			}

			if( HasStructFlag( Flags.StructFlags.Immutable ) )
			{
				Output += "immutable ";
			}

			if( HasStructFlag( Flags.StructFlags.ImmutableWhenCooked ) )
			{
				Output += "immutablewhencooked ";
			}

			if( HasStructFlag( Flags.StructFlags.StrictConfig ) )
			{
				Output += "strictconfig ";
			}
			return Output;
		}

		protected string FormatConstants()
		{
			string Output = String.Empty;
			foreach( UConst C in _ChildConstants )
			{
				try
				{
					Output += "\r\n" + UDecompiler.Tabs + C.Decompile();
				}
				catch
				{
					Output += "\r\nFailed at decompiling const: " + C.Name; 
				}
			}
			return Output + (Output.Length != 0 ? "\r\n" : String.Empty);
		}

		protected string FormatEnums()
		{
			string Output = String.Empty;
			foreach( UEnum En in _ChildEnums )
			{
				try
				{
					// And add a empty line between all enums!
					Output += "\r\n" + En.Decompile() + (En != Enumerable.Last<UEnum>( _ChildEnums ) ? "\r\n" : String.Empty);
				}
				catch
				{
					Output += "\r\nFailed at decompiling enum: " + En.Name; 
				}
			}
			return Output + (Output.Length != 0 ? "\r\n" : String.Empty);
		}

		protected string FormatStructs()
		{
			string Output = String.Empty;
			foreach( UStruct Str in _ChildStructs )
			{
				// And add a empty line between all structs!
				try
				{
					Output += "\r\n" + Str.Decompile() + (Str != Enumerable.Last<UStruct>( _ChildStructs ) ? "\r\n" : String.Empty);
				}
				catch
				{
					Output += "\r\nFailed at decompiling struct: " + Str.Name; 
				}
			}
			return Output + (Output.Length != 0 ? "\r\n" : String.Empty);;
		}

		protected string FormatProperties()
		{
			string Output = String.Empty;
			// Only for pure UStructs because UClass handles this on its own
			if( (IsClassType( "Struct" ) || IsClassType( "ScriptStruct" )) )
			{
				Output += FormatConstants() + FormatEnums() + FormatStructs();
			}

			// Don't use foreach, screws up order.
			for( int i = 0; i < _ChildProperties.Count; ++ i )
			{
				try
				{
					// Fix for properties within structs				   
					Output += "\r\n" + _ChildProperties[i].PreDecompile() + UDecompiler.Tabs + "var"; 
					try
					{
						if( String.Compare( _ChildProperties[i].CategoryName, "None", 
							true, System.Globalization.CultureInfo.InvariantCulture ) != 0 )
						{
							if( _ChildProperties[i].CategoryName != Name )
							{
								Output += "(" + _ChildProperties[i].CategoryName + ")";
							}
							else
							{
								Output += "()";
							}
						}
					}
					catch( ArgumentOutOfRangeException )
					{
						Output += "/* INDEX:" + _ChildProperties[i].CategoryIndex + " */";
					}

					Output += " " + _ChildProperties[i].Decompile() + ";";
				}
				catch( Exception e )
				{
					Output += " /* Property:" + _ChildProperties[i].Name + " throwed the following exception:" + e.Message + " */";
				}
			}
			return Output + (Output.Length != 0 ? "\r\n" : String.Empty);
		}

		public string FormatDefaultProperties()
		{
			string output = String.Empty;
			string innerOutput = String.Empty;

			if( (_Properties != null && _Properties.Count > 0) )
			{
				if( IsClassType( "Class" ) )
				{
					output += "\r\ndefaultproperties\r\n{\r\n";
 				}
				else
				{
					output += "\r\n" + UDecompiler.Tabs + "structdefaultproperties\r\n" + UDecompiler.Tabs + "{\r\n";
				}

				UDecompiler.AddTabs( 1 );
				try
				{
					innerOutput = DecompileProperties();
				}
				catch( Exception e )
				{
					innerOutput = UDecompiler.Tabs + "// " + e.GetType().Name + " occurred while decompiling properties!\r\n";
				}
				finally
				{
					UDecompiler.RemoveTabs( 1 );
				}
				output += innerOutput + UDecompiler.Tabs + "}";
			}
			return innerOutput.Length != 0 ? output : String.Empty;
		}
	}
}
#endif