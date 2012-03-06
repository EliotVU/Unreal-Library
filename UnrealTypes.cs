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

		// Temp
		PointerProperty		= 16,	// (UE2)
		InterfaceProperty	= 17,	// (UE3)
		ComponentProperty	= 18,	// (UE3)
		StructOffset		= (1 + PropertyType.ComponentProperty),

		// Helpers for serializing hardcoded structs.
		Vector				= (PropertyType.StructOffset),
		Rotator				= (1 + PropertyType.StructOffset), 
		Color				= (2 + PropertyType.StructOffset),
		Vector2D			= (3 + PropertyType.StructOffset),
		Vector4				= (4 + PropertyType.StructOffset),		
		Guid				= (5 + PropertyType.StructOffset),
		Plane				= (6 + PropertyType.StructOffset),
		Sphere				= (7 + PropertyType.StructOffset),
		Scale				= (8 + PropertyType.StructOffset),
		Box					= (9 + PropertyType.StructOffset),
		BoxSphereBound		= (10 + PropertyType.StructOffset),
		Quat				= (11 + PropertyType.StructOffset),
		Matrix				= (12 + PropertyType.StructOffset),		
		LinearColor			= (13 + PropertyType.StructOffset),
		IntPoint			= (14 + PropertyType.StructOffset),
		TwoVectors			= (15 + PropertyType.StructOffset),
		//InterpCurve		= (16 + PropertyType.StructOffset)
		//InterpCurvePoint	= (4 + PropertyType.StructOffset),
	}
}
