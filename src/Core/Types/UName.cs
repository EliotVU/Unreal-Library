using System;
using System.Diagnostics;

namespace UELib.JsonDecompiler
{
    /// <summary>
    /// Represents a data type that represents a string, possibly acquired from a names table.
    /// </summary>
    public sealed class UName : IUnrealSerializableClass
    {
        private const string    None = "None";
        public const int        Numeric = 0;
        internal const int      VNameNumbered = 343;

        private UNameTableItem  _NameItem;

        /// <summary>
        /// Represents the number in a name, e.g. "Component_1"
        /// </summary>
        private int             _Number;

        public string           Name => _NameItem.Name;
        private string          Text => _Number > Numeric ? _NameItem.Name + "_" + _Number : _NameItem.Name;

        private int             Index => _NameItem.Index;

        public int              Length => Text.Length;

        public UName( IUnrealStream stream )
        {
            Deserialize( stream );
        }

        public UName( UNameTableItem nameItem, int num )
        {
            _NameItem = nameItem;
            _Number = num;
        }

        public bool IsNone()
        {
            return _NameItem.Name.Equals( None, StringComparison.OrdinalIgnoreCase );
        }

        public void Deserialize( IUnrealStream stream )
        {
            int index = stream.ReadNameIndex( out _Number );
            _NameItem = stream.Package.Names[index];

            Debug.Assert( _NameItem != null, "_NameItem cannot be null! " + index );
            Debug.Assert( _Number >= -1, "Invalid _Number value! " + _Number );
        }

        public void Serialize( IUnrealStream stream )
        {
            stream.WriteIndex( Index );

            if (stream.Version < VNameNumbered) 
                return;

            Console.WriteLine( _Number + " " + Text );
            stream.Write( (uint)_Number + 1 );
        }

        public override string ToString()
        {
            return Text;
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public static bool operator ==( UName a, object b )
        {
            return Equals( a, b );
        }

        public static bool operator !=( UName a, object b )
        {
            return !Equals( a, b );
        }

        public static bool operator ==( UName a, string b )
        {
            return string.Equals( a, b );
        }

        public static bool operator !=( UName a, string b )
        {
            return !string.Equals( a, b );
        }

        public static implicit operator string( UName a )
        {
            return a?.Text;
        }

        public static explicit operator int( UName a )
        {
            return a.Index;
        }
    }
}