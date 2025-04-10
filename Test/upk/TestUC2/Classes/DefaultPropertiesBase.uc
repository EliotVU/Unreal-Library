class DefaultPropertiesBase extends Object;

// Primitives
var bool BoolTrue, BoolFalse;

var array<byte> BaseByteArray;

defaultproperties
{
    // Reversed to test inheritance.
    BoolTrue=false
    BoolFalse=true

    BaseByteArray(0)=1
}
