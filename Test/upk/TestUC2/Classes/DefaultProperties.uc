class DefaultProperties extends DefaultPropertiesBase;

// Primitives
var byte Byte;
var int Int;
// var bool Bool;
var float Float;
var name NameProperty;
// var string[255] String;
var string String;

// var delegate<OnDelegate> Delegate;
var Object Object;
var Class<DefaultProperties> MetaClass;

// FixedArray
var byte ByteFixedArray[2];
var int IntFixedArray[2];

// Structs
var Guid Guid;
var Vector Vector;
var Plane Plane;
var Rotator Rotator;
var Coords Coords;
var Quat Quat;
var Range Range;
var Scale Scale;
var Color Color;
var Box Box;

var Matrix Matrix;
var pointer Pointer;

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

    ByteFixedArray[0]=1
    IntFixedArray[0]=1
    IntFixedArray[1]=2

	Vector=(X=1.0,Y=2.0,Z=3.0)
	Plane=(W=0.0,X=1.0,Y=2.0,Z=3.0)
	Rotator=(Pitch=180,Yaw=90,Roll=45)
	Coords=(Origin=(X=0.2,Y=0.4,Z=1.0),XAxis=(X=1.0,Y=0.0,Z=0.0),YAxis=(X=0.0,Y=1.0,Z=0.0),ZAxis=(X=0.0,Y=0.0,Z=1.0))
	Quat=(X=1.0,Y=2.0,Z=3.0,W=4.0)
	Range=(Min=80.0,Max=40.0)
	Scale=(Scale=(X=1.0,Y=2.0,Z=3.0),SheerRate=5.0,SheerAxis=SHEER_ZY)
	Color=(B=20,G=40,R=80,A=160)
	Box=(Min=(X=0,Y=1,Z=2),Max=(X=0,Y=2,Z=1),IsValid=1)
	Matrix=(XPlane=(W=0,X=1,Y=2,Z=3),YPlane=(W=4,X=5,Y=6,Z=7),ZPlane=(W=8,X=9,Y=10,Z=11),WPlane=(W=12,X=13,Y=14,Z=15))

    Pointer=+1

//    begin object name=__DefaultProperties class=DefaultProperties
//        OnDelegate=none
//    end object
    OnDelegate=DefaultProperties.InternalOnDelegate
    //Delegate=__DefaultProperties.InternalOnDelegate
}
