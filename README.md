What is UE Library
==============

This provides you an API to parse/deserialize package files from the Unreal Engine such as .UDK, .UPK, etc.
The API grants access to every object that resides within such packages. 

At the moment these are all the object classes that are supported by this API:

    UObject, UField, UConst, UEnum, UProperty, UStruct, UFunction, UState,
    UClass, UTextBuffer, UMetaData, UFont, USound, UPackage


Installing
==============

Fork this project and grab the Eliot.UELib.dll from your forked repository, or grab this repository from the Releases tab.

Using the Library
==============

Assuming the library is referenced by your project, you should now be able to use the API by starting with the static class named UnrealLoader.
Start by importing the library for example: "using UELib;"

Loading a Unreal Package
==============
  
    var package = UnrealLoader.LoadFullPackage( <PATH_TO_PACKAGE>, System.IO.FileAccess.Read );
    
Iterating its objects
==============    

After having loaded a package you can do the following:

    foreach( UObject obj in package.Objects )
    {
      Console.WriteLine( "Name: {0}, Class: {1}, Outer: {2}", obj.Name, obj.Class.Name, obj.Outer.Name );
    }
    
Loading a Unreal Package with custom classes
==============  
  
This code will first load a package and deserialize its summary, then bind every occurrence of class "UTexture" to your "UMyTexture" class, and then deserialize the whole package.
  
    var package = UnrealLoader.LoadPackage( <PATH_TO_PACKAGE>, System.IO.FileAccess.Read );
    if( package != null )
    {
        package.RegisterClass( "UTexture", typeof(UMyTexture) );
        package.InitializePackage();
    }
    
    ...
    
    public class UMyTexture : UObject
    {
    }
    
    
Decompiling an Object
==============

You can get the programming-friendly contents of any object by calling its "Decompile()" method:

    // public UObject FindObject( string objectName, Type type, bool checkForSubclass = false )
    var obj = package.FindObject( <OBJECT_NAME>, typeof(<OBJECT_CLASS>) );
    if( obj != null )
    {
      Console.WriteLine( obj.Decompile() );
    }
    
    
Extending the output of "Decompile()":

    public class UMyTexture : UObject
    {
        public override string Decompile()
        {
            var output = base.Decompile();
            return output + "\r\n\tUMyTexture has its own decompile output!";       
        }
    }
    
Teaching UMyTexture its binary structure:

    public class UMyTexture : UObject
    {
        private int _MipMapCount;
        
        protected override void Deserialize()
        {
            base.Deserialize();
            
            _MipMapCount = _Buffer.ReadIndex();
        }
        
        public override string Decompile()
        {
            return "Mip Maps: " + _MipMapCount;   
        }
    }
    
Note: The above UTexture implementation assumes the binary structure of Unreal Engine 2 games. (It is quite different for UDK and not official supported by the library, but you can do so using the example given.)


Contribute
==============

Feel free to fill in an issue to request documentation for a specific feature. Or contribute missing documentation by editing this file.
