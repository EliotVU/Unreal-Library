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
        StringProperty		= 7,	// (Fixed String UE1)
        DelegateProperty	= 7,	// (Delegate UE2+)
        ClassProperty		= 8, 	// Deprecated???
        ArrayProperty		= 9,	// Dynamic Array
        StructProperty		= 10, 	// Struct, Pointer
        // 11, 12 moved to hardcoded structs.
        StrProperty			= 13,	// Dynamic string(UE2+)
        MapProperty			= 14,
        FixedArrayProperty	= 15,	// Fixed Array

#if BIOSHOCK
        QwordProperty,              // (UE3, Bioshock Infinite)
        XWeakReferenceProperty,
#endif

        // Temp
        PointerProperty,	        // (UE2)
        InterfaceProperty,	        // (UE3)
        ComponentProperty,	        // (UE3)

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
    }
}