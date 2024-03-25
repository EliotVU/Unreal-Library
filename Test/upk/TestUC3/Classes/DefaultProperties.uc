class DefaultProperties extends DefaultPropertiesBase;

// Primitives
var byte Byte;
var int Int;
// var bool Bool;
var float Float;
var name NameProperty;
// var string[255] String;
var string String;

var Object Object;
var Class<DefaultProperties> MetaClass;
var delegate<OnDelegate> Delegate;

// FixedArray
var byte ByteFixedArray[2];
var int IntFixedArray[2];

// Structs
var Guid Guid;
var Vector Vector;
var Vector4 Vector4;
var Vector2D Vector2D;
var TwoVectors TwoVectors;
var Plane Plane;
var Rotator Rotator;
var Quat Quat;
var IntPoint IntPoint;
var Color Color;
var LinearColor LinearColor;
var Box Box;
var Matrix Matrix;

var array<bool> BoolArray;

delegate OnDelegate();
private function InternalOnDelegate();

defaultproperties
{
    BoolTrue=true
    BoolFalse=false

    Byte=255
    Int=1000
	Float=.0123456789
    NameProperty="Name"
	// ASCII
	String="String_\"\\\0\a\b\f\n\r\t\v"

    Object=Object'DefaultProperties'
    MetaClass=Class'DefaultProperties'
    Delegate=InternalOnDelegate

    ByteFixedArray[0]=1
    IntFixedArray[0]=1
    IntFixedArray[1]=2

	Vector=(X=1.0,Y=2.0,Z=3.0)
	Vector4=(X=1.0,Y=2.0,Z=3.0,W=4.0)
	Vector2D=(X=1.0,Y=2.0)
	Plane=(W=0,X=1,Y=2,Z=3)
	Rotator=(Pitch=180,Yaw=90,Roll=45)
	Quat=(X=1.0,Y=2.0,Z=3.0,W=4.0)
	Color=(B=20,G=40,R=80,A=160)
	LinearColor=(R=0.2,G=0.4,B=0.6,A=0.8)
	Box={(
		Min=(X=0,Y=1,Z=2),
		Max=(X=0,Y=2,Z=1),
		IsValid=1
	)}
	Matrix={(
		XPlane=(W=0,X=1,Y=2,Z=3),
		YPlane=(W=4,X=5,Y=6,Z=7),
		ZPlane=(W=8,X=9,Y=10,Z=11),
		WPlane=(W=12,X=13,Y=14,Z=15)
	)}

    BoolArray(0)=true
    BoolArray(1)=false

    OnDelegate=DefaultProperties.InternalOnDelegate
}
