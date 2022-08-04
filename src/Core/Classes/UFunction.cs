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
                Record("Unknown:Borderlands2", size);
                _Buffer.Skip(size * 2);
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
            if (_Buffer.Version < 64)
            {
                ushort paramsSize = _Buffer.ReadUShort();
                Record(nameof(paramsSize), paramsSize);
            }

            NativeToken = _Buffer.ReadUShort();
            Record(nameof(NativeToken), NativeToken);

            if (_Buffer.Version < 64)
            {
                byte paramsCount = _Buffer.ReadByte();
                Record(nameof(paramsCount), paramsCount);
            }

            OperPrecedence = _Buffer.ReadByte();
            Record(nameof(OperPrecedence), OperPrecedence);

            if (_Buffer.Version < 64)
            {
                ushort returnValueOffset = _Buffer.ReadUShort();
                Record(nameof(returnValueOffset), returnValueOffset);
            }

#if TRANSFORMERS
            // TODO: Version?
            FunctionFlags = Package.Build == BuildGeneration.HMS
                ? _Buffer.ReadUInt64()
                : _Buffer.ReadUInt32();
#else
            FunctionFlags = _Buffer.ReadUInt32();
#endif
            Record(nameof(FunctionFlags), (FunctionFlags)FunctionFlags);
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

            // TODO: Data-strip version?
            if (_Buffer.Version >= VFriendlyName && !Package.IsConsoleCooked()
#if TRANSFORMERS
                                                 // Cooked, but not stripped, However FriendlyName got stripped or deprecated.
                                                 && Package.Build != BuildGeneration.HMS
#endif
#if MKKE
                // Cooked and stripped, but FriendlyName still remains
                || Package.Build == UnrealPackage.GameBuild.BuildName.MKKE
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

        public bool HasOptionalParamData()
        {
            // FIXME: Deprecate version check, and re-map the function flags using the EngineBranch class approach.
            return Package.Version > 300 
                   && ByteCodeManager != null 
                   && HasFunctionFlag(Flags.FunctionFlags.OptionalParameters);
        }

        #endregion
    }
}