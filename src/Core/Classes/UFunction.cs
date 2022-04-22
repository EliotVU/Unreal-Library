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
        private const uint VFriendlyName = 189;

        #region Serialized Members

        public ushort NativeToken { get; private set; }

        public byte OperPrecedence { get; private set; }

        /// <value>
        /// 32bit in UE2
        /// 64bit in UE3
        /// </value>
        private ulong FunctionFlags { get; set; }

        public ushort RepOffset { get; private set; }

        public bool RepReliable => HasFunctionFlag(Flags.FunctionFlags.NetReliable);

        public uint RepKey => RepOffset | ((uint)Convert.ToByte(RepReliable) << 16);

        #endregion

        #region Script Members

        public List<UProperty> Params { get; private set; }
        public UProperty ReturnProperty { get; private set; }

        #endregion

        #region Constructors

        protected override void Deserialize()
        {
#if BORDERLANDS2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2)
            {
                ushort size = _Buffer.ReadUShort();
                Record("??size_BL2", size);
                _Buffer.Skip(size * 2);
            }
#endif

            base.Deserialize();

            if (Package.Version <= 63)
            {
                ushort parmsSize = _Buffer.ReadUShort();
                Record("Unknown", parmsSize);
            }

            NativeToken = _Buffer.ReadUShort();
            Record("NativeToken", NativeToken);

            if (Package.Version <= 63)
            {
                byte numParms = _Buffer.ReadByte();
                Record("Unknown", numParms);
            }

            OperPrecedence = _Buffer.ReadByte();
            Record("OperPrecedence", OperPrecedence);

            if (Package.Version <= 63)
            {
                ushort returnValueOffset = _Buffer.ReadUShort();
                Record("Unknown", returnValueOffset);
            }

            if (Package.Version <= 63)
            {
                ushort unknown = _Buffer.ReadUShort();
                Record("Unknown", unknown);
            }

#if TRANSFORMERS
            // TODO: Version?
            FunctionFlags = Package.Build == UnrealPackage.GameBuild.BuildName.Transformers
                ? _Buffer.ReadUInt64()
                : _Buffer.ReadUInt32();
#else
            FunctionFlags = _Buffer.ReadUInt32();
#endif
            Record("FunctionFlags", (FunctionFlags)FunctionFlags);
            if (HasFunctionFlag(Flags.FunctionFlags.Net))
            {
                RepOffset = _Buffer.ReadUShort();
                Record("RepOffset", RepOffset);
            }

            // TODO: Data-strip version?
            if (Package.Version >= VFriendlyName && !Package.IsConsoleCooked()
#if TRANSFORMERS
                // Cooked, but not stripped, However FriendlyName got stripped or deprecated.
                && Package.Build != UnrealPackage.GameBuild.BuildName.Transformers
#endif
#if MKKE
                // Cooked and stripped, but FriendlyName still remains
                || Package.Build == UnrealPackage.GameBuild.BuildName.MKKE
#endif
               )
            {
                FriendlyName = _Buffer.ReadNameReference();
                Record("FriendlyName", FriendlyName);
            }
            else
            {
                // HACK: Workaround for packages that have stripped FriendlyName data.
                // FIXME: Operator names need to be translated.
                FriendlyName = Table.ObjectName;
            }
        }

        protected override void FindChildren()
        {
            base.FindChildren();
            Params = new List<UProperty>();
            foreach (var property in Variables)
            {
                if (property.HasPropertyFlag(PropertyFlagsLO.ReturnParm)) ReturnProperty = property;

                if (property.IsParm()) Params.Add(property);
            }
        }

        #endregion

        #region Methods

        public bool HasFunctionFlag(FunctionFlags flag)
        {
            return ((uint)FunctionFlags & (uint)flag) != 0;
        }

        public bool IsOperator()
        {
            return HasFunctionFlag(Flags.FunctionFlags.Operator);
        }

        public bool IsPost()
        {
            return IsOperator() && OperPrecedence == 0;
        }

        public bool IsPre()
        {
            return IsOperator() && HasFunctionFlag(Flags.FunctionFlags.PreOperator);
        }

        #endregion
    }
}