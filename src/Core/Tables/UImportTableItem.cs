using System;
using System.Diagnostics.Contracts;

namespace UELib.JsonDecompiler
{
    /// <summary>
    /// An import table entry, representing a @UObject dependency in a package.
    /// This includes the name of the package that this dependency belongs to.
    /// </summary>
    public sealed class UImportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
        #region Serialized Members
        public UName PackageName;
        private UName _ClassName;

        [Pure]public override string ClassName{ get{ return _ClassName; } }
        #endregion

        public void Serialize( IUnrealStream stream )
        {
            Console.WriteLine( "Writing import " + ObjectName + " at " + stream.Position );
            stream.Write( PackageName );
            stream.Write( _ClassName );
            stream.Write( OuterTable != null ? (int)OuterTable.Object : 0 ); // Always an ordinary integer
            stream.Write( ObjectName );
        }

        public void Deserialize( IUnrealStream stream )
        {
            Console.WriteLine( "Reading import " + Index + " at " + stream.Position );
            PackageName         = stream.ReadNameReference();
            _ClassName          = stream.ReadNameReference();
            ClassIndex         = (int)_ClassName;
            OuterIndex          = stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
            ObjectName          = stream.ReadNameReference();
        }

        #region Methods
        public override string ToString()
        {
            return ObjectName + "(" + -(Index + 1) + ")";
        }
        #endregion
    }
}