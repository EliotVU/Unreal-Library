#if DECOMPILE
using System;
using System.Linq;

namespace UELib.Core
{
	public partial class UStruct
	{
		/// <summary>
		/// Decompiles this object into a text format of:
		/// 
		///	struct [FLAGS] NAME [extends NAME]
		///	{
		///		[STRUCTCPPTEXT]
		/// 
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
			string content = UDecompilingState.Tabs + FormatHeader() +
				UnrealConfig.PrintBeginBracket();
			UDecompilingState.AddTabs( 1 );
			string cpptext = FormatCPPText();
			string props = FormatProperties();

			string defProps = FormatDefaultProperties();
			if( defProps.Length != 0 )
			{
				defProps += "\r\n";
			}
			UDecompilingState.RemoveTabs( 1 );
			content += cpptext + props + defProps;
			if( content.EndsWith( "\r\n" ) )
			{
				content = content.TrimEnd( '\r', '\n' );
			}
			return content + UnrealConfig.PrintEndBracket() + ";";
		}

		protected override string FormatHeader()
		{
			return "struct " + FormatFlags() + Name + (Super != null ? " " + FormatExtends() + " " 
				+ Super.Name : String.Empty);
		}

		private string FormatFlags()
		{
			string output = String.Empty;
			if( StructFlags == 0 )
			{
				return String.Empty;
			}

			if( (StructFlags & (uint)Flags.StructFlags.Native) != 0 )
			{
				output += "native ";
			}

			if( (StructFlags & (uint)Flags.StructFlags.Export) != 0 )
			{
				output += "export ";
			}

			if( Package.Version <= 128 )
			{
				if( (StructFlags & (uint)Flags.StructFlags.Long) != 0 )
				{
					output += "long ";
				}
			}

			if( (StructFlags & (uint)Flags.StructFlags.Init) != 0 && Package.Version < 222 )
			{
				output += "init ";
			}
			else if( HasStructFlag( Flags.StructFlags.Transient ) )
			{
				output += "transient ";
			}

			if( HasStructFlag( Flags.StructFlags.Atomic ) )
			{
				output += "atomic ";
			}

			if( HasStructFlag( Flags.StructFlags.AtomicWhenCooked ) )
			{
				output += "atomicwhencooked ";
			}

			if( HasStructFlag( Flags.StructFlags.Immutable ) )
			{
				output += "immutable ";
			}

			if( HasStructFlag( Flags.StructFlags.ImmutableWhenCooked ) )
			{
				output += "immutablewhencooked ";
			}

			if( HasStructFlag( Flags.StructFlags.StrictConfig ) )
			{
				output += "strictconfig ";
			}
			return output;
		}

		protected virtual string CPPTextKeyword
		{
			get{ return Package.Version < VCppText ? "cppstruct" : "structcpptext"; }	
		}

		protected string FormatCPPText()
		{
			if( CppBuffer == null )
			{
				return String.Empty;
			}

			string output = String.Format( "\r\n{0}{1}{2}\r\n", 
				UDecompilingState.Tabs, 
				CPPTextKeyword,
				UnrealConfig.PrintBeginBracket() 
			);
			output += CppBuffer.Decompile() + UnrealConfig.PrintEndBracket() + "\r\n";
			return output;
		}

		protected string FormatConstants()
		{
			string output = String.Empty;
			foreach( UConst c in _ChildConstants )
			{
				try
				{
					output += "\r\n" + UDecompilingState.Tabs + c.Decompile();
				}
				catch
				{
					output += string.Format( "\r\nFailed at decompiling const: {0}", c.Name ); 
				}
			}
			return output + (output.Length != 0 ? "\r\n" : String.Empty);
		}

		protected string FormatEnums()
		{
			string output = String.Empty;
			foreach( UEnum en in _ChildEnums )
			{
				try
				{
					// And add a empty line between all enums!
					output += "\r\n" + en.Decompile() + (en != _ChildEnums.Last() ? "\r\n" : String.Empty);
				}
				catch
				{
					output += string.Format( "\r\nFailed at decompiling enum: {0}", en.Name ); 
				}
			}
			return output + (output.Length != 0 ? "\r\n" : String.Empty);
		}

		protected string FormatStructs()
		{
			string output = String.Empty;
			foreach( UStruct str in _ChildStructs )
			{
				// And add a empty line between all structs!
				try
				{
					output += "\r\n" + str.Decompile() + (str != _ChildStructs.Last() ? "\r\n" : String.Empty);
				}
				catch
				{
					output += string.Format( "\r\nFailed at decompiling struct: {0}", str.Name ); 
				}
			}
			return output + (output.Length != 0 ? "\r\n" : String.Empty);
		}

		protected string FormatProperties()
		{
			string output = String.Empty;
			// Only for pure UStructs because UClass handles this on its own
			if( IsPureStruct() )
			{
				output += FormatConstants() + FormatEnums() + FormatStructs();
			}

			// Don't use foreach, screws up order.
			foreach( UProperty property in _ChildProperties )
			{
				try
				{
					// Fix for properties within structs				   
					output += "\r\n" + property.PreDecompile() + UDecompilingState.Tabs + "var"; 
					try
					{
						if( property.CategoryIndex > -1 
						    && String.Compare( property.CategoryName, "None", 
						                       StringComparison.OrdinalIgnoreCase ) != 0 )
						{
							if( property.CategoryName != Name )
							{
								output += "(" + property.CategoryName + ")";
							}
							else
							{
								output += "()";
							}
						}
					}
					catch( ArgumentOutOfRangeException )
					{
						output += string.Format( "/* INDEX:{0} */", property.CategoryIndex );
					}

					output += " " + property.Decompile() + ";";
				}
				catch( Exception e )
				{
					output += string.Format( " /* Property:{0} threw the following exception:{1} */", 
					                         property.Name, e.Message 
						);
				}
			}
			return output + (output.Length != 0 ? "\r\n" : String.Empty);
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
					output += "\r\n" + UDecompilingState.Tabs + "structdefaultproperties\r\n" 
						+ UDecompilingState.Tabs + "{\r\n";
				}

				UDecompilingState.AddTabs( 1 );
				try
				{
					innerOutput = DecompileProperties();
				}
				catch( Exception e )
				{
					innerOutput = string.Format( "{0}// {1} occurred while decompiling properties!\r\n", 
						UDecompilingState.Tabs, e.GetType().Name 
					);
				}
				finally
				{
					UDecompilingState.RemoveTabs( 1 );
				}
				output += innerOutput + UDecompilingState.Tabs + "}";
			}
			return innerOutput.Length != 0 ? output : String.Empty;
		}
	}
}
#endif