using System;
using System.Collections.Generic;
using UELib.Flags;
using UELib.Branch;

namespace UELib.Core
{
    public struct ULabelEntry
    {
        public string Name;
        public int Position;
    }

    /// <summary>
    /// Represents a unreal state.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UState : UStruct
    {
        [Obsolete] public const int VProbeMaskReducedAndIgnoreMaskRemoved = 691;

        #region Serialized Members

        /// <summary>
        /// Mask of current functions being probed by this class.
        /// </summary>
        public ulong ProbeMask;

        /// <summary>
        /// Mask of current functions being ignored by the present state node.
        /// </summary>
        public ulong IgnoreMask;

        /// <summary>
        /// Offset into the ScriptStack where the FLabelEntry persist.
        /// </summary>
        public ushort LabelTableOffset;

        /// <summary>
        /// The state flags.
        /// </summary>
        public UnrealFlags<StateFlag> StateFlags;

        /// <summary>
        /// Always null if version is lower than <see cref="PackageObjectLegacyVersion.AddedFuncMapToUState"/>
        /// </summary>
        public UMap<UName, UFunction> FuncMap;

        #endregion

        #region Script Members

        [Obsolete]
        public IEnumerable<UFunction> Functions => EnumerateFields<UFunction>();

        #endregion
        
        protected override void Deserialize()
        {
            base.Deserialize();
#if UE4
            if (_Buffer.UE4Version > 0)
            {
                return;
            }
#endif
#if TRANSFORMERS
            if (Package.Build == BuildGeneration.HMS)
            {
                goto noMasks;
            }
#endif

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.ProbeMaskReducedAndIgnoreMaskRemoved)
            {
                ProbeMask = _Buffer.ReadUInt64();
                Record(nameof(ProbeMask), ProbeMask);

                IgnoreMask = _Buffer.ReadUInt64();
                Record(nameof(IgnoreMask), IgnoreMask);
            }
            else
            {
                ProbeMask = _Buffer.ReadUInt32();
                Record(nameof(ProbeMask), ProbeMask);
            }

        noMasks:
            LabelTableOffset = _Buffer.ReadUInt16();
            Record(nameof(LabelTableOffset), LabelTableOffset);

#if BORDERLANDS2 || TRANSFORMERS || BATMAN
            // FIXME:Temp fix
            if ((Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                 Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn &&
                 _Buffer.LicenseeVersion >= 18) ||
                Package.Build == BuildGeneration.HMS ||
                Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                StateFlags = new UnrealFlags<StateFlag>(_Buffer.ReadUShort(), _Buffer.Package.Branch.EnumFlagsMap[typeof(StateFlag)]);
                goto skipStateFlags;
            }
#endif
            StateFlags = _Buffer.ReadFlags32<StateFlag>();
        skipStateFlags:
            Record(nameof(StateFlags), StateFlags);
#if TRANSFORMERS
            if (Package.Build == BuildGeneration.HMS)
            {
                _Buffer.Skip(4);
                _Buffer.ConformRecordPosition();
                return;
            }
#endif
#if ADVENT
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Advent &&
                _Buffer.Version >= 134)
            {
                _Buffer.ReadMap(out FuncMap);
                Record(nameof(FuncMap), FuncMap);

                return;
            }
#endif
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.AddedFuncMapToUState)
            {
                return;
            }

            _Buffer.ReadMap(out FuncMap);
            Record(nameof(FuncMap), FuncMap);
        }

        [Obsolete("Use StateFlags directly.")]
        public bool HasStateFlag(StateFlags flag) => (StateFlags & (uint)flag) != 0;

        [Obsolete("Use StateFlags directly.")]
        public bool HasStateFlag(uint flag) => (StateFlags & flag) != 0;

        internal bool HasStateFlag(StateFlag flagIndex)
        {
            return StateFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(StateFlag)], flagIndex);
        }
    }
}
