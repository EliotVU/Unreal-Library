class DefaultProperties extends Object;

// Primitives
var string String;
var float Float;

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

defaultproperties
{
	// ASCII
	String="String_\"\\\0\a\b\f\n\r\t\v"
	Float=.0123456789
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
}