class ExprTokens extends Object;

var int InstanceInt;

// var pointer instancePointer;

static final preoperator bool \ ( string v ) { return true; }

function AllCasts()
{
	local byte localByte;
	local int localInt;
	local bool localBool;
	local float localFloat;
	local string localString;
	local name localName;
	local Object localObject;
	local Class localClass;
	local Vector localVector;
	local Rotator localRotator;

	// ByteToX
	assert (\"ByteToInt" && int(localByte) == 0); 
	assert (\"ByteToBool" && bool(localByte) == false); 
	assert (\"ByteToFloat" && float(localByte) == 0.0); 
	assert (\"ByteToString" && string(localByte) == ""); 

	// IntToX
	assert (\"IntToByte" && byte(localInt) == 0); 
	assert (\"IntToBool" && bool(localInt) == false); 
	assert (\"IntToFloat" && float(localInt) == 0.0); 
	assert (\"IntToString" && string(localInt) == ""); 

	// BoolToX
	assert (\"BoolToByte" && byte(localBool) == 0); 
	assert (\"BoolToInt" && int(localBool) == 0); 
	assert (\"BoolToFloat" && float(localBool) == 0.0); 
	assert (\"BoolToString" && string(localBool) == "false"); 
	assert (\"BoolToButton" && button(localBool) == ""); // Alias 

	// FloatToX
	assert (\"FloatToByte" && byte(localFloat) == 0); 
	assert (\"FloatToInt" && int(localFloat) == 0); 
	assert (\"FloatToBool" && bool(localFloat) == false); 
	assert (\"FloatToString" && string(localFloat) == ""); 

	// StringToX
	assert (\"StringToByte" && byte(localString) == 0); 
	assert (\"StringToInt" && int(localString) == 0); 
	assert (\"StringToBool" && bool(localString) == false); 
	assert (\"StringToFloat" && float(localString) == 0.0); 
	assert (\"StringToVector" && Vector(localString) == vect(0,0,0)); 
	assert (\"StringToRotator" && Rotator(localString) == rot(0,0,0)); 

	// NameToX
	assert (\"NameToBool" && bool(localName) == true); 
	assert (\"NameToString" && string(localName) == ""); 

	// ObjectToX
	assert (\"DynamicCast" && Casts(localObject) == none); 
	assert (\"ObjectToString" && string(localObject) == "None"); 
	assert (\"ObjectToBool" && bool(localObject) == false); 

	// ClassToX
	assert (\"MetaCast" && Class<Casts>(localClass) == none); 

	// VectorToX
	assert (\"VectorToBool" && bool(localVector) == false); 
	assert (\"VectorToRotator" && Rotator(localVector) == rot(0,0,0)); 
	assert (\"VectorToString" && string(localVector) == "(X=0.0,Y=0.0,Z=0.0)"); 

	// RotatorToX
	assert (\"RotatorToBool" && bool(localRotator) == false); 
	assert (\"RotatorToVector" && Vector(localRotator) == vect(0,0,0)); 
	assert (\"RotatorToString" && string(localRotator) == "(Pitch=0,Yaw=0,Roll=0)");

    // This emits EX_PointerConst, however this crashes the compiler because the byte code is not serialized.
    // instancePointer = 1;

    // assert (int(instancePointer) == 0);
}

function VarTokens()
{
    local int localInt;

	assert (\"LocalVariable" && localInt == 0);
	assert (\"InstanceVariable" && InstanceInt == 0);
	assert (\"DefaultVariable" && default.InstanceInt == 0);
}

delegate OnDelegate();
private function InternalOnDelegate();

// OnDelegate gets internally redirected to property __OnDelegate__Delegate
function DelegateTokens()
{
    OnDelegate();
    OnDelegate = InternalOnDelegate;
    OnDelegate = none;
}
