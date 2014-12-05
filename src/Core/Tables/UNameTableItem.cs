using System;

namespace UELib
{
    /// <summary>
    /// Represents a unreal name table with serialized data from a unreal package header.
    /// </summary>
    public sealed class UNameTableItem : UTableItem, IUnrealSerializableClass
    {
        #region Serialized Members
        /// <summary>
        /// Object Name
        /// </summary>
        public string Name = String.Empty;

        /// <summary>
        /// Object Flags, such as LoadForEdit, LoadForServer, LoadForClient
        /// </summary>
        /// <value>
        /// 32bit in UE2
        /// 64bit in UE3
        /// </value>
        public ulong Flags;
        #endregion

        private const int QWORDVersion = 141;

        public void Deserialize( IUnrealStream stream )
        {
            Name = stream.ReadText();
            Flags = stream.Version >= QWORDVersion ? stream.ReadUInt64() : stream.ReadUInt32();
#if DEOBFUSCATE
    // De-obfuscate names that contain unprintable characters!
            foreach( char c in Name )
            {
                if( !char.IsLetterOrDigit( c ) )
                {
                    Name = "N" + TableIndex + "_OBF";
                    break;
                }
            }
#endif
        }

        public void Serialize( IUnrealStream stream )
        {
            stream.WriteString( Name );

            if( stream.Version < QWORDVersion )
            {
                // Writing UINT
                stream.UW.Write( (uint)Flags );
            }
            else
            {
                // Writing ULONG
                stream.UW.Write( Flags );
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==( UNameTableItem a, string b )
        {
            return String.Equals( a, b );
        }

        public static bool operator !=( UNameTableItem a, string b )
        {
            return !String.Equals( a, b );
        }

        public static implicit operator string( UNameTableItem a )
        {
            return a.Name;
        }

        public static implicit operator int( UNameTableItem a )
        {
            return a.Index;
        }
    }
}