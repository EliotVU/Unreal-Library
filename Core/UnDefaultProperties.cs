using System;
using System.Globalization;
using UELib.Types;

namespace UELib.Core
{
	/// <summary>
	/// [Default]Properties values deserializer.
	/// </summary>
	public sealed class UPropertyTag
	{
		private readonly UObject _Owner;
		private readonly UObjectStream _Buffer;

		/// <summary>
		/// Name of the property
		/// </summary>
		public string Name;
		public int NameIndex;

		/// <summary>
		/// Name of the struct not UStructProperty!, if IsA UStructProperty only
		/// </summary>
		public string ItemName;

		/// <summary>
		/// A packed byte containing the size, type and arrayindex.
		/// </summary>
		private byte _Info;

		/// <summary>
		/// See PropertysType enum in UnrealFlags.cs
		/// </summary>
		public PropertyType Type
		{
			get;
			private set;
		}

		/// <summary>
		/// The stream size of this DefaultProperty.
		/// </summary>
		public int Size
	   	{
			get;
			private set;
		}

		/// <summary>
		/// Whether this property is part of an array, and the index into it
		/// </summary>
	   	public int ArrayIndex = -1;

		private long _PropertyOffset;
		public long PropertyOffset
		{
			get{ return _PropertyOffset; }
		}

		private long _ValueOffset;
		public long ValueOffset
		{
			get{ return _ValueOffset; }
		}

		internal byte TempFlags;

		public bool IsComplex
		{
			get{ return Type == PropertyType.StructProperty || Type == PropertyType.ArrayProperty; }
		}

		public UPropertyTag( UObject owner )
		{
			_Owner = owner;
			_Buffer = _Owner.Buffer;
		}

		private int DeserializeSize( byte sizePack )
		{
			int size = 0;
			switch( sizePack )
			{
				case 0x00:
					size = 1;
					break;

				case 0x10:
					size = 2;
					break;

				case 0x20:
					size = 4;
					break;

				case 0x30:
					size = 12;
					break;

				case 0x40:
					size = 16;
					break;

				case 0x50:
					size = _Buffer.ReadByte();
					break;

				case 0x60:
					size = _Buffer.ReadUShort();
					break;

				case 0x70:
					size = _Buffer.ReadInt32();
					break;
			}
			return size;
		}

		private int DeserializeArrayIndex()
		{
			int arrayIndex;
			byte b = _Buffer.ReadByte();
			if( (b & 0x80) == 0 )
			{
				arrayIndex = b;
			}
			else if( (b & 0xC0) == 0x80 )
			{
				arrayIndex = ((int)(b & 0x7F) << 8) + ((int)_Buffer.ReadByte());
			}
			else
			{
				arrayIndex = ((int)(b & 0x3F) << 24) 
					+ ((int)_Buffer.ReadByte() << 16)
					+ ((int)_Buffer.ReadByte() << 8) 
					+ ((int)_Buffer.ReadByte());
			}
			return arrayIndex;
		}

		public bool Deserialize()
		{
			_PropertyOffset = _Buffer.Position;

			int num;
			NameIndex = _Buffer.ReadNameIndex( out num );
			_Owner.TestNoteRead( "NameIndex", NameIndex );
			Name = _Owner.GetIndexName( NameIndex, num );
			if( Name.Equals( "None", StringComparison.OrdinalIgnoreCase ) )
			{
				return false;
			}

			// Unreal Engine 1 and 2
			if( _Buffer.Version < 220 )
			{
				// Packed byte
				_Info = _Buffer.ReadByte();
				Type = (PropertyType)((byte)(_Info & 0x0F));

				// Read the ItemName(StructName) if this is a struct
				if( Type == PropertyType.StructProperty )
				{
					ItemName = _Owner.Package.GetIndexName( _Buffer.ReadNameIndex() );
				}

				Size = DeserializeSize( (byte)(_Info & 0x70) );
				if( (_Info & 0x80) > 0 && Type != PropertyType.BoolProperty )
				{
					ArrayIndex = DeserializeArrayIndex();
				}
			}
			// Unreal Engine 3
			else
			{
				string typeName = _Owner.Package.GetIndexName( _Buffer.ReadNameIndex() );
				_Owner.TestNoteRead( "typeName", typeName );
				try
				{
					Type = (PropertyType)Enum.Parse( typeof(PropertyType), typeName );
				}
				catch
				{
					Type = PropertyType.ByteProperty;
				}				

				Size = _Buffer.ReadInt32();
				_Owner.TestNoteRead( "Size", Size );
				ArrayIndex = _Buffer.ReadInt32();
				_Owner.TestNoteRead( "ArrayIndex", ArrayIndex );

				if( Type == PropertyType.StructProperty )
				{
					ItemName = _Owner.GetIndexName( _Buffer.ReadNameIndex( out num ), num );
					_Owner.TestNoteRead( "ItemName", ItemName );
				}
			}

			_ValueOffset = _Buffer.Position;
			try
			{
				DeserializeValue();	
			}
			finally
			{
				// Size is only accurate before 220
				if( _Buffer.Version < 220 )
				{
					_Buffer.Position = _ValueOffset + Size;
				}
			}
			return true;
		}

		[Flags]
		public enum DeserializeFlags : byte
		{
			None					= 0x00,
			WithinStruct			= 0x01,
			WithinArray				= 0x02,
			SkipHardCodedStructs	= 0x04,	// Fix for multi version support for hardcoded structs so that they can recall the struct deserializing(without causing an infinite loop) if a version is not supported.
		}

		/// <summary>
		/// Deserialize the value of this UPropertyTag instance.
		/// 
		/// Note:
		/// 	Only call after the whole package has been deserialized!
		/// </summary>
		/// <returns>The deserialized value if any.</returns>
		public string DeserializeValue( DeserializeFlags deserializeFlags = DeserializeFlags.None )
		{
			string output;

			string bakItemName = ItemName;
			string bakName = Name;

			_Buffer.Seek( _ValueOffset, System.IO.SeekOrigin.Begin );
			try
			{	
				output = DeserializeDefaultPropertyValue( Type, ref deserializeFlags );	
			}
			catch( SerializationException e )
			{
				output = e.Output;
			}
			finally
			{
				// Reset everything.
				Name = bakName;
				ItemName = bakItemName;
			}
			return output;
		}

		/// <summary>
		/// Deserialize a default property value of a specified type.
		/// </summary>
		/// <param name="type">Kind of type to try deserialize.</param>
		/// <returns>The deserialized value if any.</returns>
		private string DeserializeDefaultPropertyValue( PropertyType type, ref DeserializeFlags deserializeFlags )
		{
			if( (_Buffer.Position - _ValueOffset) > Size ) 
			{
				throw new SerializationException( "end of DefaultProperty reached..." );
			}

			string propertyValue = String.Empty;
			try
			{
				// Deserialize Value
				switch( type )
				{
					case PropertyType.BoolProperty:
						if( IsComplex || _Buffer.Version >= 222 )	// i.e. within a struct or array we need to deserialize the value.
						{
							bool val = _Buffer.Version < 673 ? _Buffer.ReadInt32() > 0 : _Buffer.ReadByte() > 0;
							propertyValue = val.ToString( CultureInfo.InvariantCulture ).ToLower();
						}
						else
						{
							propertyValue = ((ArrayIndex != 0) ? "true" : "false");
						}
						break;

					case PropertyType.StrProperty:
						propertyValue = "\"" + _Buffer.ReadName() + "\"";
						break;

					case PropertyType.NameProperty:
					{
						int nameindex = _Buffer.ReadNameIndex();
						propertyValue = _Owner.Package.NameTableList[nameindex].Name;
						break;
					}

					case PropertyType.IntProperty:
						propertyValue = _Buffer.ReadInt32().ToString( CultureInfo.InvariantCulture );
						break;

					case PropertyType.FloatProperty:
						propertyValue = _Buffer.ReadUFloat();
						break;

					case PropertyType.ByteProperty:
						switch( Size )
						{
							case 8:				
								int enumType = _Buffer.ReadNameIndex();
								if( _Buffer.Version >= 633 )
								{
									int enumValue = _Buffer.ReadNameIndex();
									propertyValue = _Owner.Package.GetIndexName( enumType ) + "." 
										+ _Owner.Package.GetIndexName( enumValue );
								}
								else
								{
									propertyValue = _Owner.Package.GetIndexName( enumType );
								}
								break;

							case 1:
								if( _Buffer.Version >= 633 )
									_Buffer.ReadNameIndex();

								propertyValue = _Buffer.ReadByte().ToString( CultureInfo.InvariantCulture );
								break;

							default:
								propertyValue = _Buffer.ReadByte().ToString( CultureInfo.InvariantCulture );
								break;
						}
						break;

					case PropertyType.InterfaceProperty:
					case PropertyType.ComponentProperty:
					case PropertyType.ObjectProperty:
					{
						int index = _Buffer.ReadObjectIndex();
						var obj = _Owner.Package.GetIndexObject( index );
						if( obj != null )
						{
							// SubObject??, see if the subobject owner is this defaultproperties owner.
							if( obj.GetOuterName() == _Owner.Name )
							{
								TempFlags |= 0x01; // Don't automatically add a name.
								if( obj.ObjectIndex > 0 )
								{
									// This is probably an unknown object which is by default never deserialized, so force!
									obj.BeginDeserializing();										
									if( obj.Properties != null && obj.Properties.Count > 0 )
									{
										propertyValue += obj.Decompile() + "\r\n" + UDecompiler.Tabs;

										/*propertyValue = "begin object class=" + obj.GetClassName() + " name=" + obj.Name + "\r\n";
											UDecompiler.AddTabs( 1 );
											propertyValue += obj.DecompileProperties();
											UDecompiler.RemoveTabs( 1 );
										propertyValue += UDecompiler.Tabs + "end object\r\n" + UDecompiler.Tabs;*/
									}
								}

								if( (deserializeFlags & DeserializeFlags.WithinArray) != 0 ) // fake actually an array in this case xD.
								{
									TempFlags |= 0x02;	// Notify the array that it needs to replace %x%.
									propertyValue += "%ARRAYNAME%=" + obj.Name;
								}
								else
								{
									propertyValue += Name + "=" + obj.Name;	
								}
							}
							else
							{
								string classname = obj.GetClassName();
								propertyValue = (String.IsNullOrEmpty( classname ) ? "class" : classname) 
									+ "\'" + obj.GetOuterGroup() + "\'";
							}
						}
						else
						{
							propertyValue = "none";
						}
						break;
					}

					case PropertyType.ClassProperty:
					{
						int index = _Buffer.ReadObjectIndex();
						var obj = _Owner.Package.GetIndexObject( index );
						propertyValue = (obj != null ? "class\'" + obj.Name + "\'" : "none");
						break;
					}

					case PropertyType.DelegateProperty:
					{
						TempFlags |= 0x01;
						int outerindex = _Buffer.ReadObjectIndex(); // Where the assigned delegate property exists.
						int delegateindex = _Buffer.ReadNameIndex();
						string delegatename = Name.Substring( 2, Name.Length - 12 );
						propertyValue = delegatename + "=" + _Owner.Package.NameTableList[delegateindex].Name;
						break;
					}

					#region HardCoded Struct Types
					case PropertyType.Color:
					{
						string B = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
						string G = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
						string R = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
						string A = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
								  												  
						propertyValue += "R=" + R +
							",G=" + G +
							",B=" + B +
							",A=" + A;
						break;
					}

					case PropertyType.LinearColor:
					{
						string B = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
						string G = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
						string R = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
						string A = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
								  												  
						propertyValue += "R=" + R +
							",G=" + G +
							",B=" + B +
							",A=" + A;
						break;
					}

					case PropertyType.Vector:
					{
						string X = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
						string Y = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
						string Z = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
								  												  
						propertyValue += "X=" + X +
							",Y=" + Y +
							",Z=" + Z;
						break;
					}

					case PropertyType.TwoVectors:
					{											  
						string V1 = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
						string V2 = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );

						propertyValue += "v1=(" + V1 + "),v2=(" + V2 + ")";
						break;
					}

					case PropertyType.Vector4:
					{											  
						string Plane = DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );

						propertyValue += Plane;
						break;
					}

					case PropertyType.Vector2D:
					{
						string X = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
						string Y = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
								  												  
						propertyValue += "X=" + X +
							",Y=" + Y;
						break;
					}

					case PropertyType.Rotator:
					{
						string Pitch = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
						string Yaw = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
						string Roll = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
								  												  
						propertyValue += "Pitch=" + Pitch +
							",Yaw=" + Yaw +
							",Roll=" + Roll;
						break;
					}

					case PropertyType.Guid:
					{
						string A = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
						string B = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
						string C = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
						string D = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
								  												  
						propertyValue += "A=" + A +
							",B=" + B +
							",C=" + C +
							",D=" + D;
						break;
					}

					case PropertyType.Sphere:
					case PropertyType.Plane:
					{
						string V = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
						string W = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
								  												  
						propertyValue += V + ",W=" + W;
						break;
					}

					case PropertyType.Scale:
					{
						string Scale = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
						string SheerRate = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
						string SheerAxis = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
								  												  
						propertyValue += "Scale=(" + Scale + ")" +
							",SheerRate=" + SheerRate +
							",SheerAxis=" + SheerAxis;
						break;
					}

					case PropertyType.Box:
					{
						string Min = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
						string Max = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
						string IsValid = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
								  
						propertyValue += "Min=(" + Min + ")" +
							",Max=(" + Max + ")" +
							",IsValid=" + IsValid;
						break;
					}

					/*case PropertyType.InterpCurve:
					{	
						// HACK:
						UPropertyTag tag = new UPropertyTag( _Owner );
						tag.Serialize();
						buffer.Seek( tag.ValueOffset, System.IO.SeekOrigin.Begin );

						int curvescount = buffer.ReadIndex();
						if( curvescount <= 0 )
						{
							break;
						}
						propertyvalue += tag.Name + "=(";						
						for( int i = 0; i < curvescount; ++ i )
						{
							propertyvalue += "(" + SerializeDefaultPropertyValue( PropertyType.InterpCurvePoint, buffer, ref serializeFlags ) + ")";
							if( i != curvescount - 1 )
							{
								propertyvalue += ",";
							}
						}
						propertyvalue += ")";
						break;
					}*/

					/*case PropertyType.InterpCurvePoint:
					{
						string InVal = SerializeDefaultPropertyValue( PropertyType.Float, buffer, ref serializeFlags );
						string OutVal = SerializeDefaultPropertyValue( PropertyType.Float, buffer, ref serializeFlags );
								  												  
						propertyvalue += "InVal=" + InVal +
							",OutVal=" + OutVal;
						break;
					}*/

					case PropertyType.Quat:
					{											  
						propertyValue += DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
						break;
					}

					case PropertyType.Matrix:
					{			
						string XPlane = DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
		  				string YPlane =	DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
						string ZPlane =	DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
						string WPlane =	DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
						propertyValue += "XPlane=(" + XPlane + ")" +
							",YPlane=(" + YPlane + ")" +
							",ZPlane=(" + ZPlane + ")" +
							",WPlane=(" + WPlane + ")";
						break;
					}

					case PropertyType.IntPoint:
					{											  
						string X = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
						string Y = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );

						propertyValue += "X=" + X + ",Y=" + Y;
						break;
					}
					#endregion

					case PropertyType.PointerProperty:
					case PropertyType.StructProperty:	
					{
						deserializeFlags |= DeserializeFlags.WithinStruct;
						bool bWasHardcoded = false;
						var hardcodedstructs = (PropertyType[])Enum.GetValues( typeof(PropertyType) );
						for( var i = (byte)PropertyType.StructOffset; i < hardcodedstructs.Length; ++ i )
						{
							if( String.Compare( ItemName, 
								Enum.GetName( typeof(PropertyType), (byte)hardcodedstructs[i] ), 
								StringComparison.OrdinalIgnoreCase ) == 0 
								)
							{
								bWasHardcoded = true;
								propertyValue += DeserializeDefaultPropertyValue( hardcodedstructs[i], ref deserializeFlags );
								break;
							}
						}

						if( !bWasHardcoded )
						{
							while( true )
							{
								var tag = new UPropertyTag( _Owner );
								if( tag.Deserialize() )
								{
									propertyValue += tag.Name + 
										(tag.ArrayIndex > 0 && tag.Type != PropertyType.BoolProperty ? "[" + tag.ArrayIndex + "]" : String.Empty) + 
										"=" + tag.DeserializeValue( deserializeFlags ) + ",";
								}
								else
								{
									if( propertyValue.EndsWith( "," ) )
									{
										propertyValue = propertyValue.Remove( propertyValue.Length - 1, 1 );
									}
									break;
								}						
							}					
						}
						propertyValue = propertyValue.Length != 0? "(" + propertyValue + ")" : "none";
						break;
					}

					case PropertyType.ArrayProperty:
					{		
						deserializeFlags |= DeserializeFlags.WithinArray;
						PropertyType arrayType = PropertyType.None;
						var arrayObject = _Owner.Package.ObjectsList.Find
						(
							obj => _Buffer.Version < 513	// UT3 and older 
								? obj.Name == Name && obj.IsClassType( "ArrayProperty" ) 
								: obj.Name == Name && obj.IsClassType( "ArrayProperty" ) && obj.Outer == _Owner.Class 
						) as UArrayProperty;

						// TODO:FIXME
						/*foreach( var varType in UnrealConfig.VariableTypes )
						{
							if( varType.VName.Equals( Name, StringComparison.OrdinalIgnoreCase ) )
							{
								arrayType = (PropertyType)Enum.Parse( typeof(PropertyType), varType.VType );
							}
						} */
						// This is hardcoded for testing purpose.
						if( String.Compare( Name, "Controls", StringComparison.OrdinalIgnoreCase ) == 0 
							|| String.Compare( Name, "Components", StringComparison.OrdinalIgnoreCase ) == 0 )
						{
							arrayType = PropertyType.ObjectProperty;
						}

						int arraySize = _Buffer.ReadIndex();
						if( arraySize <= 0 )
						{
							propertyValue += "none";
							break;
						}

						bool fallback = false;
						if( arrayType == PropertyType.None )
						{
							if( arrayObject != null )
							{			
								if( arrayObject.InnerProperty != null )
								{
									arrayType = arrayObject.InnerProperty.Type;
									switch( arrayType )
									{
										case PropertyType.PointerProperty:
										case PropertyType.StructProperty:
										{
											var structObject = (UStructProperty)arrayObject.InnerProperty;
											ItemName = structObject.StructObject.Name;
											break;
										}

										default:
											ItemName = arrayObject.InnerProperty.Class.Name;
											break;
									}
								}
								else
								{
									fallback = true;
									propertyValue = "/* Cannot find this imported array's type.";
								}
							}
							else
							{
								fallback = true;
								propertyValue = "/* Cannot find this array's type.";
							}
						}

						if( fallback )
						{
							int innerSize = Size - (int)(_Buffer.Position - _ValueOffset);
							propertyValue += "\r\n" + UDecompiler.Tabs + "\tDataSize:" + innerSize + " */";
							break;
						}

#if DEBUG
						//propertyValue = "Type:" + arraytype + " Array.Name:" + AProp.Name + " Inner.Name:" + AProp.InnerProperty.Name + " Inner.Type:" + AProp.InnerProperty.Type + "\r\n" + UDecompiler.Tabs;
#endif

						string orgName = Name;
						string orgItemName = ItemName;

						if( (deserializeFlags & DeserializeFlags.WithinStruct) != 0 )
						{
							// Hardcoded fix for InterpCurve and InterpCurvePoint.
							if( String.Compare( Name, "Points", StringComparison.OrdinalIgnoreCase ) == 0 )
							{
								// Remove commentary.
								propertyValue = String.Empty;
								arrayType = PropertyType.StructProperty;
							}

							propertyValue += "(";
							for( int i = 0; i < arraySize; ++ i )
							{
								propertyValue += DeserializeDefaultPropertyValue( arrayType, ref deserializeFlags ) + 
									(i != arraySize - 1 
										? ","
										: String.Empty);
							}
							propertyValue += ")";
						}
						else
						{
							for( int i = 0; i < arraySize; ++ i )
							{
								string elementvalue = DeserializeDefaultPropertyValue( arrayType, ref deserializeFlags );
								if( (TempFlags & 0x02) != 0 )	// Must replace %x%
								{
									propertyValue = elementvalue.Replace( "%ARRAYNAME%", Name + "(" + i + ")" );
									TempFlags = 0x00;
								}
								else
								{
									propertyValue += Name + "(" + i + ")=" + elementvalue + 
										(i != arraySize - 1 
											? "\r\n" + UDecompiler.Tabs
											: String.Empty);
								}
								// Restore, in case it changed between this loop.
								// e.g. a array of a struct which contains many variants could easily screw it up for the next element which are going to do the same obviously :D.
								Name = orgName;
								ItemName = orgItemName;
							}
						}
						TempFlags |= 0x01; // Don't automatically add a name.
						break;
					}

					default:
						propertyValue = "/*unknown " + ItemName + " (" + Type + "_" + type + ")*/";
						break;
				}
			}
			catch( Exception e )
			{             
                return propertyValue + "// " + e.Message + "\r\n" + UDecompiler.Tabs;                          
			}
			return propertyValue;
		}
	}

	/// <summary>
	/// Represents a Decompileable UPropertyTag.
	/// </summary>
	public sealed class UDefaultProperty : IUnrealDecompilable
	{
		/// <summary>
		/// Serialized Info
		/// </summary>
		public UPropertyTag Tag;

		public string Decompile()
		{
			Tag.TempFlags = 0x00;
			string value;
			try
			{
				value = Tag.DeserializeValue();
			}
			catch( Exception e )
			{
				value = "//" + e.Message;
			}

			// Array or Inlined object
			if( (Tag.TempFlags & 0x01) != 0 )
			{		
				// The tag handles the name etc on its own.
				return value;
			}
			string arrayindex = String.Empty; 
			if( Tag.ArrayIndex > 0 && Tag.Type != PropertyType.BoolProperty )
			{
				arrayindex += "[" + Tag.ArrayIndex + "]";
			}
			return Tag.Name + arrayindex + "=" + value;
		}
	}
}
