using System;

namespace UELib.Core
{
    using Types;

    /// <summary>
    /// Represents a unreal property.
    /// </summary>
    public partial class UProperty : UField, IUnrealNetObject
    {
        #region PreInitialized Members
        public PropertyType Type
        {
            get;
            protected set;
        }
        #endregion

        #region Serialized Members
        public ushort 	ArrayDim
        {
            get;
            private set;
        }

        public ushort 	ElementSize
        {
            get;
            private set;
        }

        public ulong 	PropertyFlags
        {
            get;
            private set;
        }

#if XCOM2
        public UName    ConfigName
        {
            get;
            private set;
        }
#endif

        public int 		CategoryIndex
        {
            get;
            private set;
        }

        public UEnum	ArrayEnum{ get; private set; }

        public ushort 	RepOffset
        {
            get;
            private set;
        }

        public bool		RepReliable
        {
            get{ return HasPropertyFlag( Flags.PropertyFlagsLO.Net ); }
        }

        public uint		RepKey
        {
            get{ return RepOffset | ((uint)Convert.ToByte( RepReliable ) << 16); }
        }
        #endregion

        #region General Members
        private bool _IsArray
        {
            get{ return ArrayDim > 1; }
        }

        public string CategoryName
        {
            get{ return CategoryIndex != -1 ? Package.Names[CategoryIndex].Name : "@Null"; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of the UELib.Core.UProperty class.
        /// </summary>
        public UProperty()
        {
            Type = PropertyType.None;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

#if XIII
            if( Package.Build == UnrealPackage.GameBuild.BuildName.XIII )
            {
                ArrayDim = _Buffer.ReadUShort();
                Record( "ArrayDim", ArrayDim );
                goto skipInfo;
            }
#endif

            var info = _Buffer.ReadUInt32();
            ArrayDim = (ushort)(info & 0x0000FFFFU);
            Record( "ArrayDim", ArrayDim );
            ElementSize = (ushort)(info >> 16);
            Record( "ElementSize", ElementSize );
            skipInfo:

            PropertyFlags = Package.Version >= 220 ? _Buffer.ReadUInt64() : _Buffer.ReadUInt32();
            Record( "PropertyFlags", PropertyFlags );
            if( !Package.IsConsoleCooked() )
            {

#if XCOM2
                if( Package.Build == UnrealPackage.GameBuild.BuildName.XCOM2WotC )
                {
                    ConfigName = _Buffer.ReadNameReference();
                    Record( "ConfigName", ConfigName );
                }
#endif

                CategoryIndex = _Buffer.ReadNameIndex();
                Record( "CategoryIndex", CategoryIndex );

                if( Package.Version > 400 )
                {
                    ArrayEnum = GetIndexObject( _Buffer.ReadObjectIndex() ) as UEnum;
                    Record( "ArrayEnum", ArrayEnum );
                }
            }
            else CategoryIndex = -1;

            if( HasPropertyFlag( Flags.PropertyFlagsLO.Net ) )
            {
                RepOffset = _Buffer.ReadUShort();
                Record( "RepOffset", RepOffset );
            }

            if( HasPropertyFlag( Flags.PropertyFlagsLO.New ) && Package.Version <= 128 )
            {
                string unknown = _Buffer.ReadText();
                Console.WriteLine( "Found a property flagged with New:" + unknown );
            }

#if SWAT4
            if( Package.Build == UnrealPackage.GameBuild.BuildName.Swat4 )
            {
                // Contains meta data such as a ToolTip.
                _Buffer.Skip( 3 );
            }
#endif
        }

        protected override bool CanDisposeBuffer()
        {
            return true;
        }
        #endregion

        #region Methods
        public bool HasPropertyFlag( Flags.PropertyFlagsLO flag )
        {
            return ((uint)(PropertyFlags & 0x00000000FFFFFFFFU) & (uint)flag) != 0;
        }

        public bool HasPropertyFlag( Flags.PropertyFlagsHO flag )
        {
            return ((PropertyFlags >> 32) & (uint)flag) != 0;
        }

        public bool IsParm()
        {
            return HasPropertyFlag( Flags.PropertyFlagsLO.Parm );
        }

        public virtual string GetFriendlyInnerType()
        {
            return String.Empty;
        }
        #endregion
    }
}