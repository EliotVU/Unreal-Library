﻿using System;
using System.Collections.Generic;
using UELib.JsonDecompiler.Flags;

namespace UELib.JsonDecompiler.Core
{
    /// <summary>
    /// Represents a unreal function.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UFunction : UStruct, IUnrealNetObject
    {
        // TODO: Corrigate version. Attested in version 61, but deprecated since at least version 68.
        private const uint VDeprecatedData = 68;
        private const uint VFriendlyName = 189;

        #region Serialized Members
        public ushort   NativeToken
        {
            get;
            private set;
        }

        public byte     OperPrecedence
        {
            get;
            private set;
        }

        /// <value>
        /// 32bit in UE2
        /// 64bit in UE3
        /// </value>
        private ulong   FunctionFlags
        {
            get; set;
        }

        public ushort   RepOffset
        {
            get;
            private set;
        }

        public bool     RepReliable
        {
            get{ return HasFunctionFlag( Flags.FunctionFlags.NetReliable ); }
        }

        public uint     RepKey
        {
            get{ return RepOffset | ((uint)Convert.ToByte( RepReliable ) << 16); }
        }
        #endregion

        #region Script Members
        public List<UProperty>  Params{ get; private set; }
        public UProperty        ReturnProperty{ get; private set; }
        #endregion

        #region Constructors
        protected override void Deserialize()
        {
#if BORDERLANDS2
            if( Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 )
            {
                var size = _Buffer.ReadUShort();
                Record( "??size_BL2", size );
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
                // ParmsSize, NumParms, and ReturnValueOffset
                _Buffer.Skip( 5 );
            }

#if TRANSFORMERS
            FunctionFlags = Package.Build == UnrealPackage.GameBuild.BuildName.Transformers 
                ? _Buffer.ReadUInt64() 
                : _Buffer.ReadUInt32();
#else
            FunctionFlags = _Buffer.ReadUInt32();
#endif
            Record( "FunctionFlags", (FunctionFlags)FunctionFlags );
            if( HasFunctionFlag( Flags.FunctionFlags.Net ) )
            {
                RepOffset = _Buffer.ReadUShort();
                Record( "RepOffset", RepOffset );
            }

#if TRANSFORMERS
            if( Package.Build == UnrealPackage.GameBuild.BuildName.Transformers )
            {
                FriendlyName = Table.ObjectName;
                return;
            }
#endif

            if( (Package.Version >= VFriendlyName && !Package.IsConsoleCooked())
#if MKKE
                || Package.Build == UnrealPackage.GameBuild.BuildName.MKKE
#endif
                )
            {
                FriendlyName = _Buffer.ReadNameReference();
                Record( "FriendlyName", FriendlyName );
            }
        }

        protected override void FindChildren()
        {
            base.FindChildren();
            Params = new List<UProperty>();
            foreach( var property in Variables )
            {
                if( property.HasPropertyFlag( PropertyFlagsLO.ReturnParm ) )
                {
                    ReturnProperty = property;
                }

                if( property.IsParm() )
                {
                    Params.Add( property );
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
