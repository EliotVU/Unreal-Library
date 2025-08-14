using System;
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

        /// <summary>
        /// The function flags.
        /// </summary>
        public UnrealFlags<FunctionFlag> FunctionFlags;

        public ushort RepOffset { get; set; }

        public bool RepReliable => FunctionFlags.HasFlag(FunctionFlag.NetReliable);

        public uint RepKey => RepOffset | ((uint)Convert.ToByte(RepReliable) << 16);

        #endregion

        #region Script Members

        [Obsolete]
        public IEnumerable<UProperty> Params => EnumerateFields<UProperty>().Where(prop => prop.IsParm());

        public UProperty ReturnProperty => EnumerateFields<UProperty>()
            .FirstOrDefault(prop => prop.PropertyFlags.HasFlag(PropertyFlag.ReturnParm));

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
                _Buffer.Read(out FunctionFlags);
                Record(nameof(FunctionFlags), FunctionFlags);
                if (FunctionFlags.HasFlag(FunctionFlag.Net))
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
                FunctionFlags = _Buffer.ReadFlags64<FunctionFlag>();

                goto skipFunctionFlags;
            }
#endif
            _Buffer.Read(out FunctionFlags);
#if ROCKETLEAGUE
            // Disassembled code shows two calls to ByteOrderSerialize, might be a different variable not sure.
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                _Buffer.LicenseeVersion >= 24)
            {
                // HO:0x04 = Constructor
                uint v134 = _Buffer.ReadUInt32();
                FunctionFlags = new UnrealFlags<FunctionFlag>(FunctionFlags | (ulong)v134 << 32);
            }
#endif
#if SA2
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.SA2 &&
                _Buffer.Version >= 869)
            {
                uint v4d = _Buffer.ReadUInt32();
                Record(nameof(v4d), v4d); // merging this with FunctionFlags causes an overlap with FunctionFlag.Net
            }
#endif
        skipFunctionFlags:
            Record(nameof(FunctionFlags), FunctionFlags);
            if (FunctionFlags.HasFlag(FunctionFlag.Net))
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
#if ADVENT
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Advent &&
                _Buffer.Version >= 133)
            {
                FriendlyName = _Buffer.ReadNameReference();
                Record(nameof(FriendlyName), FriendlyName);

                return;
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
                if (FriendlyName == null) FriendlyName = Name;
            }
        }

        #endregion

        #region Methods

        [Obsolete("Use FunctionFlags directly")]
        public bool HasFunctionFlag(uint flag)
        {
            return ((uint)FunctionFlags & flag) != 0;
        }

        [Obsolete("Use FunctionFlags directly")]
        public bool HasFunctionFlag(FunctionFlags flag)
        {
            return ((uint)FunctionFlags & (uint)flag) != 0;
        }

        internal bool HasFunctionFlag(FunctionFlag flagIndex)
        {
            return FunctionFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(FunctionFlag)], flagIndex);
        }

        public bool IsOperator()
        {
            return FunctionFlags.HasFlag(FunctionFlag.Operator);
        }

        public bool IsPost()
        {
            return IsOperator() && OperPrecedence == 0;
        }

        public bool IsPre()
        {
            return IsOperator() && FunctionFlags.HasFlag(FunctionFlag.PreOperator);
        }

        public bool IsDelegate()
        {
            return FunctionFlags.HasFlag(FunctionFlag.Delegate);
        }

        public bool HasOptionalParamData()
        {
            // FIXME: Deprecate version check, and re-map the function flags using the EngineBranch class approach.
            return Package.Version > 300
                   && ByteCodeManager != null
                   && EnumerateFields<UProperty>()
                       ?.Where(prop => prop.PropertyFlags.HasFlag(PropertyFlag.Parm))
                       .Any() == true
                // Not available for older packages.
                // && HasFunctionFlag(Flags.FunctionFlags.OptionalParameters);
                ;
        }

        #endregion
    }
}
