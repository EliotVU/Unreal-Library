﻿using System;
using System.Collections.Generic;
using System.Linq;
using UELib.Branch;
using UELib.Flags;

namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal function.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UFunction : UStruct, IUnrealNetObject
    {
        #region Serialized Members

        public ushort NativeToken { get; private set; }

        public byte OperPrecedence { get; private set; }

        /// <value>
        /// 32bit in UE2
        /// 64bit in UE3
        /// </value>
        private ulong FunctionFlags { get; set; }

        public ushort RepOffset { get; set; }

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
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                ushort size = _Buffer.ReadUShort();
                _Buffer.Skip(size * 2);
                Record("Unknown:Borderlands2", size);
            }
#endif
            base.Deserialize();
#if UE4
            if (_Buffer.UE4Version > 0)
            {
                FunctionFlags = _Buffer.ReadUInt32();
                Record(nameof(FunctionFlags), (FunctionFlags)FunctionFlags);
                if (HasFunctionFlag(Flags.FunctionFlags.Net))
                {
                    RepOffset = _Buffer.ReadUShort();
                    Record(nameof(RepOffset), RepOffset);
                }

                FriendlyName = ExportTable.ObjectName;
                return;
            }
#endif
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                ushort paramsSize = _Buffer.ReadUShort();
                Record(nameof(paramsSize), paramsSize);
            }

            NativeToken = _Buffer.ReadUShort();
            Record(nameof(NativeToken), NativeToken);

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                byte paramsCount = _Buffer.ReadByte();
                Record(nameof(paramsCount), paramsCount);
            }

            OperPrecedence = _Buffer.ReadByte();
            Record(nameof(OperPrecedence), OperPrecedence);

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                ushort returnValueOffset = _Buffer.ReadUShort();
                Record(nameof(returnValueOffset), returnValueOffset);
            }

#if TRANSFORMERS
            // FIXME: version
            if (_Buffer.Package.Build == BuildGeneration.HMS)
            {
                FunctionFlags = _Buffer.ReadUInt64();
                Record(nameof(FunctionFlags), (FunctionFlags)FunctionFlags);

                goto skipFunctionFlags;
            }
#endif
            FunctionFlags = _Buffer.ReadUInt32();
            Record(nameof(FunctionFlags), (FunctionFlags)FunctionFlags);
#if ROCKETLEAGUE
            // Disassembled code shows two calls to ByteOrderSerialize, might be a different variable not sure.
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                _Buffer.LicenseeVersion >= 24)
            {
                // HO:0x04 = Constructor
                uint v134 = _Buffer.ReadUInt32();
                Record(nameof(v134), v134);

                FunctionFlags |= ((ulong)v134 << 32);
            }
#endif
        skipFunctionFlags:
            if (HasFunctionFlag(Flags.FunctionFlags.Net))
            {
                RepOffset = _Buffer.ReadUShort();
                Record(nameof(RepOffset), RepOffset);
            }
#if SPELLBORN
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn
                && 133 < _Buffer.Version)
            {
                // Always 0xAC6975C
                uint unknownFlags1 = _Buffer.ReadUInt32();
                Record(nameof(unknownFlags1), unknownFlags1);
                uint replicationFlags = _Buffer.ReadUInt32();
                Record(nameof(replicationFlags), replicationFlags);
            }
#endif

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.MovedFriendlyNameToUFunction &&
                !Package.IsConsoleCooked()
#if TRANSFORMERS
                // Cooked, but not stripped, However FriendlyName got stripped or deprecated.
                && Package.Build != BuildGeneration.HMS
#endif
#if MKKE
                // Cooked and stripped, but FriendlyName still remains
                || Package.Build == UnrealPackage.GameBuild.BuildName.MKKE
#endif
#if MASS_EFFECT
                // Cooked and stripped, but FriendlyName still remains
                || Package.Build == BuildGeneration.SFX
#endif
               )
            {
                FriendlyName = _Buffer.ReadNameReference();
                Record(nameof(FriendlyName), FriendlyName);
            }
            else
            {
                // HACK: Workaround for packages that have stripped FriendlyName data.
                // FIXME: Operator names need to be translated.
                if (FriendlyName == null) FriendlyName = Table.ObjectName;
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

        public bool HasFunctionFlag(uint flag)
        {
            return ((uint)FunctionFlags & flag) != 0;
        }

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

        public bool IsDelegate()
        {
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                return HasFunctionFlag(0x400000);
#endif
            return HasFunctionFlag(Flags.FunctionFlags.Delegate);
        }

        public bool HasOptionalParamData()
        {
            // FIXME: Deprecate version check, and re-map the function flags using the EngineBranch class approach.
            return Package.Version > 300
                   && ByteCodeManager != null
                   && Params?.Any() == true
                // Not available for older packages.
                // && HasFunctionFlag(Flags.FunctionFlags.OptionalParameters);
                ;
        }

        #endregion
    }
}
