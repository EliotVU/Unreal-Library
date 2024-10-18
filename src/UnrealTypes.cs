namespace UELib.Types
{
    /// <summary>
    /// Types describing a default property type.
    /// </summary>
    public enum PropertyType : byte
    {
        None 				= 0,
        ByteProperty		= 1,
        IntProperty			= 2,
        BoolProperty		= 3,
        FloatProperty		= 4,
        ObjectProperty		= 5,	// Object, Component, Interface
        NameProperty		= 6,
        StringProperty		= 7,	// <= UE1, fixed string
        DelegateProperty	= StringProperty,	// >= UE2, displaced StringProperty
        ClassProperty		= 8,
        ArrayProperty		= 9,	// >= UT, dynamic array
        StructProperty		= 10, 	// Struct, Pointer
        // 11, 12 moved to hardcoded structs.
        StrProperty			= 13,	// >= UT, dynamic string
        MapProperty			= 14,   // >= UT
        FixedArrayProperty	= 15,	// >= UT, fixed array, < UE3
        PointerProperty     = 16,	// >= UE2.5 (UT2004), < UE3

#if BIOSHOCK
        QwordProperty,              // (UE3, Bioshock Infinite)
        XWeakReferenceProperty,
#endif

#if GIGANTIC
        JsonRefProperty,
#endif
#if MASS_EFFECT
        StringRefProperty,
        BioMask4Property,
#endif

        InterfaceProperty,  // >= UE3, displaced FixedArrayProperty, actual value 15, but we don't need the value for UE3 types.
        ComponentProperty,	// >= UE3

        StructOffset		= (1 + ComponentProperty),

        // Helpers for serializing hardcoded structs.
        Vector				= (2 + StructOffset),
        Rotator				= (3 + StructOffset),
        Color				= (4 + StructOffset),
        Vector2D			= (5 + StructOffset),
        Vector4				= (6 + StructOffset),
        Guid				= (7 + StructOffset),
        Plane				= (8 + StructOffset),
        Sphere				= (9 + StructOffset),
        Scale				= (10 + StructOffset),
        Box					= (11 + StructOffset),
        //BoxSphereBound		= (12 + StructOffset),
        Quat				= (12 + StructOffset),
        Matrix				= (13 + StructOffset),
        LinearColor			= (14 + StructOffset),
        IntPoint			= (15 + StructOffset),
        TwoVectors			= (16 + StructOffset),
        //InterpCurve		= (17 + PropertyType.StructOffset)
        //InterpCurvePoint	= (18 + PropertyType.StructOffset)
        PointRegion         = (19 + StructOffset),

        // Auto-conversions for old (<= 61) "StructName"s
        Rotation = Rotator,
        Region = PointRegion,
    }
}
