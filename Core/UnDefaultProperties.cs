using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// [Default]Properties values deserializer.
    /// </summary>
    public sealed class UDefaultProperty : IUnrealDecompilable
    {
        [Flags]
        public enum DeserializeFlags : byte
        {
            None					= 0x00,
            WithinStruct			= 0x01,
            WithinArray				= 0x02,
            Complex					= WithinStruct | WithinArray,
        }

        private const byte			DoNotAppendName		= 0x01;
        private const byte			ReplaceNameMarker	= 0x02;

        private IUnrealStream		_Buffer{ get{ return _Owner.Buffer; } }
        private readonly UObject	_Owner;
        private UStruct				_Outer;

        internal long				_PropertyOffset{ get; private set; }
        private long				_ValueOffset{ get; set; }
        private byte				_TempFlags{ get; set; }

        #region Serialized Members
        /// <summary>
        /// Name of the property
        /// </summary>
        public string				Name{ get; private set; }
        public int					NameIndex;

        /// <summary>
        /// Name of the struct not UStructProperty!, if IsA UStructProperty only
        /// </summary>
        private string				ItemName{ get; set; }

        /// <summary>
        /// See PropertysType enum in UnrealFlags.cs
        /// </summary>
        private PropertyType		Type{ get; set; }

        /// <summary>
        /// The stream size of this DefaultProperty.
        /// </summary>
        private int					Size{ get; set; }

        /// <summary>
        /// Whether this property is part of an array, and the index into it
        /// </summary>
        public int					ArrayIndex = -1;
        #endregion

        #region Constructors
        public UDefaultProperty( UObject owner, UStruct outer = null )
        {
            _Owner = owner;
            _Outer = (outer ?? _Owner as UStruct) ?? _Owner.Outer as UStruct;
        }

        private int DeserializeSize( byte sizePack )
        {
            switch( sizePack )
            {
                case 0x00:
                    return 1;
                                                      
                case 0x10:
                    return 2;

                case 0x20:
                    return 4;

                case 0x30:
                    return 12;

                case 0x40:
                    return 16;

                case 0x50:
                    return _Buffer.ReadByte();

                case 0x60:
                    return _Buffer.ReadInt16();

                case 0x70:
                    return _Buffer.ReadInt32();

                default:
                    throw new NotImplementedException( String.Format( "Unknown sizePack {0}", sizePack ) );
            }
        }

        const byte ArrayIndexMask = 0x80;

        private int DeserializeArrayIndex()
        {
            int arrayIndex;
            byte b = _Buffer.ReadByte();
            if( (b & ArrayIndexMask) == 0 )
            {
                arrayIndex = b;
            }
            else if( (b & 0xC0) == 0x80 )
            {
                arrayIndex = ((b & 0x7F) << 8) + _Buffer.ReadByte();
            }
            else
            {
                arrayIndex = ((b & 0x3F) << 24) 
                    + (_Buffer.ReadByte() << 16)
                    + (_Buffer.ReadByte() << 8) 
                    + _Buffer.ReadByte();
            }
            return arrayIndex;
        }

        public bool Deserialize()
        {
            _PropertyOffset = _Buffer.Position;

            int num;
            NameIndex = _Buffer.ReadNameIndex( out num );
            Name = _Owner.GetIndexName( NameIndex, num );
            _Owner.NoteRead( "NameIndex", Name );
            if( Name.Equals( "None", StringComparison.OrdinalIgnoreCase ) )
            {
                return false;
            }

            // Unreal Engine 1 and 2
            if( _Buffer.Version < 220 )
            {
                const byte typeMask = 0x0F;
                const byte sizeMask = 0x70;

                // Packed byte
                var info = _Buffer.ReadByte();

                Type = (PropertyType)(byte)(info & typeMask);
                _Owner.NoteRead( "Type", Type );

                // Read the ItemName(StructName) if this is a struct
                if( Type == PropertyType.StructProperty )
                {
                    ItemName = _Owner.Package.GetIndexName( _Buffer.ReadNameIndex() );
                    _Owner.NoteRead( "ItemName", ItemName );
                }

                Size = DeserializeSize( (byte)(info & sizeMask) );
                _Owner.NoteRead( "Size", Size );
                
                if( (info & ArrayIndexMask) > 0 && Type != PropertyType.BoolProperty )
                {
                    ArrayIndex = DeserializeArrayIndex();
                    _Owner.NoteRead( "ArrayIndex", ArrayIndex );
                }
            }
            // Unreal Engine 3
            else
            {
                string typeName = _Owner.Package.GetIndexName( _Buffer.ReadNameIndex() );
                _Owner.NoteRead( "typeName", typeName );
                Type = (PropertyType)Enum.Parse( typeof(PropertyType), typeName );				

                Size = _Buffer.ReadInt32();
                _Owner.NoteRead( "Size", Size );
                ArrayIndex = _Buffer.ReadInt32();
                _Owner.NoteRead( "ArrayIndex", ArrayIndex );

                if( Type == PropertyType.StructProperty )
                {
                    ItemName = _Owner.GetIndexName( _Buffer.ReadNameIndex( out num ), num );
                    _Owner.NoteRead( "ItemName", ItemName );
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

        /// <summary>
        /// Deserialize the value of this UPropertyTag instance.
        /// 
        /// Note:
        /// 	Only call after the whole package has been deserialized!
        /// </summary>
        /// <returns>The deserialized value if any.</returns>
        public string DeserializeValue( DeserializeFlags deserializeFlags = DeserializeFlags.None )
        {
            _Buffer.Seek( _ValueOffset, System.IO.SeekOrigin.Begin );
            try
            {	
                return DeserializeDefaultPropertyValue( Type, ref deserializeFlags );	
            }
            catch( DeserializationException e )
            {
                return e.ToString();
            }
        }

        /// <summary>
        /// Deserialize a default property value of a specified type.
        /// </summary>
        /// <param name="type">Kind of type to try deserialize.</param>
        /// <returns>The deserialized value if any.</returns>
        private string DeserializeDefaultPropertyValue( PropertyType type, ref DeserializeFlags deserializeFlags )
        {
            if( _Buffer.Position - _ValueOffset > Size ) 
            {
                throw new DeserializationException( "End of defaultpropertyies stream reached..." );
            }

            var orgOuter = _Outer;
            string propertyValue = String.Empty;
            try
            {
                // Deserialize Value
                switch( type )
                {
                    case PropertyType.BoolProperty:
                        { 
                            bool value;
                            // i.e. within a struct or array we need to deserialize the value.
                            if( (deserializeFlags & DeserializeFlags.Complex) != 0 || _Buffer.Version >= 222 )
                            {
                                value = _Buffer.Version >= 673
                                    ? _Buffer.ReadByte() > 0
                                    : _Buffer.ReadInt32() > 0;
                            }
                            else
                            {
                                value = ArrayIndex != 0;
                            }
                            propertyValue = value ? "true" : "false";
                            break;
                        }

                    case PropertyType.StrProperty:
                        propertyValue = "\"" + _Buffer.ReadString().Replace( "\"", "\\\"" )
                            .Replace( "\\", "\\\\" )
                            .Replace( "\n", "\\n" )
                            .Replace( "\r", "\\r" )
                            + "\"";
                        break;

                    case PropertyType.NameProperty:
                        {
                            int nameNum;
                            int nameIndex = _Buffer.ReadNameIndex( out nameNum );
                            propertyValue = _Owner.GetIndexName( nameIndex, nameNum );
                            break;
                        }

                    case PropertyType.IntProperty:
                        propertyValue = _Buffer.ReadInt32().ToString( CultureInfo.InvariantCulture );
                        break;

                    case PropertyType.FloatProperty:
                        propertyValue = _Buffer.ReadFloat().ToUFloat();
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
                            int objectIndex = _Buffer.ReadObjectIndex();
                            var obj = _Owner.Package.GetIndexObject( objectIndex );
                            if( obj != null )
                            {
                                // SubObject??, see if the subobject owner is this defaultproperties owner.
                                if( obj.GetOuterName() == _Owner.Name )
                                {
                                    _TempFlags |= DoNotAppendName;
                                    if( (int)obj > 0 )
                                    {
                                        // This is probably an unknown object which is by default never deserialized, so force!
                                        obj.BeginDeserializing();
                                        if( obj.Properties != null && obj.Properties.Count > 0 )
                                        {
                                            propertyValue = obj.Decompile() + "\r\n" + UDecompilingState.Tabs;
                                        }
                                    }

                                    if( (deserializeFlags & DeserializeFlags.WithinArray) != 0 )
                                    {
                                        _TempFlags |= ReplaceNameMarker;
                                        propertyValue += "%ARRAYNAME%=" + obj.Name;
                                    }
                                    else
                                    {
                                        propertyValue += Name + "=" + obj.Name;
                                    }
                                }
                                else
                                {
                                    string className = obj.GetClassName();
                                    propertyValue = (String.IsNullOrEmpty( className ) ? "class" : className)
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
                            _TempFlags |= DoNotAppendName;
                            int outerIndex = _Buffer.ReadObjectIndex(); // Where the assigned delegate property exists.
                            int delegateIndex = _Buffer.ReadNameIndex();
                            string delegateName = Name.Substring( 2, Name.Length - 12 );
                            propertyValue = delegateName + "=" + _Owner.Package.Names[delegateIndex].Name;
                            break;
                        }

                    #region HardCoded Struct Types
                    case PropertyType.Color:
                        {
                            string b = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
                            string g = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
                            string r = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );
                            string a = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );

                            propertyValue += "R=" + r +
                                ",G=" + g +
                                ",B=" + b +
                                ",A=" + a;
                            break;
                        }

                    case PropertyType.LinearColor:
                        {
                            string b = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
                            string g = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
                            string r = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
                            string a = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );

                            propertyValue += "R=" + r +
                                ",G=" + g +
                                ",B=" + b +
                                ",A=" + a;
                            break;
                        }

                    case PropertyType.Vector:
                        {
                            string x = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
                            string y = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
                            string z = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );

                            propertyValue += "X=" + x +
                                ",Y=" + y +
                                ",Z=" + z;
                            break;
                        }

                    case PropertyType.TwoVectors:
                        {
                            string v1 = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
                            string v2 = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );

                            propertyValue += "v1=(" + v1 + "),v2=(" + v2 + ")";
                            break;
                        }

                    case PropertyType.Vector4:
                        {
                            string plane = DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );

                            propertyValue += plane;
                            break;
                        }

                    case PropertyType.Vector2D:
                        {
                            string x = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
                            string y = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );

                            propertyValue += "X=" + x +
                                ",Y=" + y;
                            break;
                        }

                    case PropertyType.Rotator:
                        {
                            string pitch = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
                            string yaw = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
                            string roll = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );

                            propertyValue += "Pitch=" + pitch +
                                ",Yaw=" + yaw +
                                ",Roll=" + roll;
                            break;
                        }

                    case PropertyType.Guid:
                        {
                            string a = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
                            string b = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
                            string c = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
                            string d = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );

                            propertyValue += "A=" + a +
                                ",B=" + b +
                                ",C=" + c +
                                ",D=" + d;
                            break;
                        }

                    case PropertyType.Sphere:
                    case PropertyType.Plane:
                        {
                            string v = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
                            string w = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );

                            propertyValue += v + ",W=" + w;
                            break;
                        }

                    case PropertyType.Scale:
                        {
                            string scale = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
                            string sheerRate = DeserializeDefaultPropertyValue( PropertyType.FloatProperty, ref deserializeFlags );
                            string sheerAxis = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );

                            propertyValue += "Scale=(" + scale + ")" +
                                ",SheerRate=" + sheerRate +
                                ",SheerAxis=" + sheerAxis;
                            break;
                        }

                    case PropertyType.Box:
                        {
                            string min = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
                            string max = DeserializeDefaultPropertyValue( PropertyType.Vector, ref deserializeFlags );
                            string isValid = DeserializeDefaultPropertyValue( PropertyType.ByteProperty, ref deserializeFlags );

                            propertyValue += "Min=(" + min + ")" +
                                ",Max=(" + max + ")" +
                                ",IsValid=" + isValid;
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
                            string xPlane = DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
                            string yPlane =	DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
                            string zPlane =	DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
                            string wPlane =	DeserializeDefaultPropertyValue( PropertyType.Plane, ref deserializeFlags );
                            propertyValue += "XPlane=(" + xPlane + ")" +
                                ",YPlane=(" + yPlane + ")" +
                                ",ZPlane=(" + zPlane + ")" +
                                ",WPlane=(" + wPlane + ")";
                            break;
                        }

                    case PropertyType.IntPoint:
                        {
                            string x = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );
                            string y = DeserializeDefaultPropertyValue( PropertyType.IntProperty, ref deserializeFlags );

                            propertyValue += "X=" + x + ",Y=" + y;
                            break;
                        }
                    #endregion

                    case PropertyType.PointerProperty:
                    case PropertyType.StructProperty:
                        {
                            deserializeFlags |= DeserializeFlags.WithinStruct;
                            bool isHardCoded = false;
                            var hardcodedStructs = (PropertyType[])Enum.GetValues( typeof( PropertyType ) );
                            for( var i = (byte)PropertyType.StructOffset; i < hardcodedStructs.Length; ++ i )
                            {
                                if( String.Compare( ItemName, 
                                    Enum.GetName( typeof( PropertyType ), (byte)hardcodedStructs[i] ),
                                    StringComparison.OrdinalIgnoreCase ) != 0 
                                ) 
                                    continue;

                                isHardCoded = true;
                                propertyValue += DeserializeDefaultPropertyValue( hardcodedStructs[i], ref deserializeFlags );
                                break;
                            }

                            if( !isHardCoded )
                            {
                                // We have to modify the outer so that dynamic arrays within this struct 
                                // will be able to find its variables to determine the array type.
                                FindProperty( out _Outer );
                                while( true )
                                {
                                    var tag = new UDefaultProperty( _Owner, _Outer );
                                    if( tag.Deserialize() )
                                    {
                                        propertyValue += tag.Name +
                                            (tag.ArrayIndex > 0 && tag.Type != PropertyType.BoolProperty
                                            ? "[" + tag.ArrayIndex + "]" : String.Empty) +
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
                            propertyValue = propertyValue.Length != 0 ? "(" + propertyValue + ")" : "none";
                            break;
                        }

                    case PropertyType.ArrayProperty:
                        {
                            int arraySize = _Buffer.ReadIndex();
                            if( arraySize == 0 )
                            {
                                propertyValue = "none";
                                break;
                            }

                            // Find the property within the outer/owner or its inheritances. 
                            // If found it has to modify the outer so structs within this array can find their array variables.
                            // Additionally we need to know the property to determine the array's type.
                            var arrayType = PropertyType.None;
                            var property = FindProperty( out _Outer ) as UArrayProperty;
                            if( property != null && property.InnerProperty != null )
                            {
                                arrayType = property.InnerProperty.Type;
                            }
                            // If we did not find a reference to the associated property(because of imports)
                            // then try to determine the array's type by scanning the definined array types.
                            else if( UnrealConfig.VariableTypes != null && UnrealConfig.VariableTypes.ContainsKey( Name ) )
                            {
                                var varTuple = UnrealConfig.VariableTypes[Name];
                                if( varTuple != null )
                                {
                                    arrayType = varTuple.Item2;
                                }
                            }

                            if( arrayType == PropertyType.None )
                            {
                                propertyValue = "/* Array type was not detected. */";
                                break;
                            }

                            deserializeFlags |= DeserializeFlags.WithinArray;
                            if( (deserializeFlags & DeserializeFlags.WithinStruct) != 0 )
                            {
                                // Hardcoded fix for InterpCurve and InterpCurvePoint.
                                if( String.Compare( Name, "Points", StringComparison.OrdinalIgnoreCase ) == 0 )
                                {
                                    arrayType = PropertyType.StructProperty;
                                }

                                for( int i = 0; i < arraySize; ++ i )
                                {
                                    propertyValue += DeserializeDefaultPropertyValue( arrayType, ref deserializeFlags )
                                        + (i != arraySize - 1 ? "," : String.Empty);
                                }
                                propertyValue = "(" + propertyValue + ")";
                            }
                            else
                            {
                                for( int i = 0; i < arraySize; ++ i )
                                {
                                    string elementValue = DeserializeDefaultPropertyValue( arrayType, ref deserializeFlags );
                                    if( (_TempFlags & ReplaceNameMarker) != 0 )
                                    {
                                        propertyValue = elementValue.Replace( "%ARRAYNAME%", Name + "(" + i + ")" );
                                        _TempFlags = 0x00;
                                    }
                                    else
                                    {
                                        propertyValue += Name + "(" + i + ")=" + elementValue
                                            + (i != arraySize - 1 ? "\r\n" + UDecompilingState.Tabs : String.Empty);
                                    }
                                }
                            }

                            _TempFlags |= DoNotAppendName; 
                            break;
                        }

                    default:
                        propertyValue = "/* Unknown default property type! */";
                        break;
                }
            }
            catch( Exception e )
            {
                return propertyValue + "\r\n" + UDecompilingState.Tabs 
                    + "// Exception thrown while deserializing " + Name + "\t" + e;
            }
            finally
            {
                _Outer = orgOuter;
            }
            return propertyValue;
        }
        #endregion

        #region Decompilation
        public string Decompile()
        {
            _TempFlags = 0x00;
            string value;
            try
            {
                value = DeserializeValue();
            }
            catch( Exception e )
            {
                value = "//" + e;
            }

            // Array or Inlined object
            if( (_TempFlags & DoNotAppendName) != 0 )
            {		
                // The tag handles the name etc on its own.
                return value;
            }
            string arrayindex = String.Empty; 
            if( ArrayIndex > 0 && Type != PropertyType.BoolProperty )
            {
                arrayindex += "[" + ArrayIndex + "]";
            }
            return Name + arrayindex + "=" + value;
        }
        #endregion

        #region Methods
        private UProperty FindProperty( out UStruct outer )
        {
            UProperty property = null;
            outer = _Outer;
            for( var structField = _Outer; structField != null; structField = structField.Super as UStruct )
            {
                if( structField.Variables == null || !structField.Variables.Any() )
                    continue;

                property = structField.Variables.Find( i => i.Name == Name );
                if( property == null )
                    continue;

                switch( property.Type )
                {
                    case PropertyType.StructProperty:
                        outer = ((UStructProperty)property).StructObject;
                        break;

                    case PropertyType.ArrayProperty:
                        var arrayField = property as UArrayProperty;
                        Debug.Assert( arrayField != null, "arrayField != null" );
                        var arrayInnerField = arrayField.InnerProperty;
                        if( arrayInnerField.Type == PropertyType.StructProperty )
                        {
                            _Outer = ((UStructProperty)arrayInnerField).StructObject;
                        }
                        break;

                    default:
                        outer = structField;
                        break;
                }
                break;
            }
            return property;
        }
        #endregion
    }
}
