An extension is a compiled .dll file that may be placed in the folder "/UE Explorer/Extensions/"

An extension can use attributes to register themselves to UE Explorer. Upon launching UE Explorer, the program will load any .dll found in the extensions folder and activate the attributed classes. 
The attributed classes can then use Eliot.UELib.dll to parse packages and do things UE Explorer doesn't.