using System;
using System.Collections.Generic;
using UELib.Flags;

namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal function. 
    /// </summary>
    [UnrealRegisterClass]
    public partial class UFunction : UStruct, IUnrealNetObject
    {
        private const uint VDeprecatedData = 69;
        private const uint VFriendlyName = 189;

        #region Serialized Members
        public ushort	NativeToken
        {
            get;
            private set;
        }

        public byte		OperPrecedence
        {
            get;
            private set;
        }

        /// <value>
        /// 32bit in UE2
        /// 64bit in UE3
        /// </value>
        private ulong	FunctionFlags
        {
            get; set;
        }

        public ushort	RepOffset
        {
            get;
            private set;
        }

        public bool		RepReliable
        {
            get{ return HasFunctionFlag( Flags.FunctionFlags.NetReliable ); }
        }

        public uint		RepKey
        {
            get{ return RepOffset | ((uint)Convert.ToByte( RepReliable ) << 16); }
        }
        #endregion

        #region Script Members
        public IList<UProperty> Locals{ get; private set; }
        public IList<UProperty> Params{ get; private set; }
        public UProperty        ReturnProperty{ get; private set; }
        #endregion

        #region Constructors
        protected override void Deserialize()
        {
#if BORDERLANDS2
            if( Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 )
            {
                var size = _Buffer.ReadUShort();
                Record( "??size", size );
                _Buffer.Skip( size * 2 );
            }
#endif

            base.Deserialize();

            NativeToken = _Buffer.ReadUShort();
            Record( "NativeToken", NativeToken );
            OperPrecedence = _Buffer.ReadByte();
            Record( "OperPrecedence", OperPrecedence );
            if( Package.Version < VDeprecatedData )
            {
                // ParmsSize, NumParms, and ReturnValueOffse
                _Buffer.Skip( 5 );
            }

            FunctionFlags = _Buffer.ReadUInt32();
            Record( "FunctionFlags", (FunctionFlags)FunctionFlags );
            if( HasFunctionFlag( Flags.FunctionFlags.Net ) )
            {
                RepOffset = _Buffer.ReadUShort();
                Record( "RepOffset", RepOffset );
            }

            if( Package.Version >= VFriendlyName && !Package.IsConsoleCooked() )
            {
                FriendlyName = _Buffer.ReadNameReference();
                Record( "FriendlyName", FriendlyName );
            }
        }

        protected override void FindChildren()
        {
            base.FindChildren();
            Locals = new List<UProperty>();
            Params = new List<UProperty>();
            foreach( var property in Variables )
            {
                if( property.HasPropertyFlag( PropertyFlagsLO.ReturnParm ) )
                {
                    ReturnProperty = property;
                    continue;																																  
                }

                if( property.IsParm() )
                {
                    Params.Add( property );
                }
                else
                {
                    Locals.Add( property );
                }
            }
        }
        #endregion

        #region Methods
        public bool HasFunctionFlag( FunctionFlags flag )
        {
            return ((uint)FunctionFlags & (uint)flag) != 0; 
        }

        public bool IsOperator()
        {
            return HasFunctionFlag( Flags.FunctionFlags.Operator );
        }

        public bool IsPost()
        {
            return IsOperator() && OperPrecedence == 0;
        }

        public bool IsPre()
        {
            return IsOperator() && HasFunctionFlag( Flags.FunctionFlags.PreOperator );
        }
        #endregion
    }
}
