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
        /// This state's flags mask e.g. Auto, Simulated.
        /// TODO: Retype to UStateFlags and deprecate HasStateFlag, among others
        /// </summary>
        private uint _StateFlags;

        /// <summary>
        /// Always null if version is lower than <see cref="PackageObjectLegacyVersion.AddedFuncMapToUState"/>
        /// </summary>
        public UMap<UName, UFunction> FuncMap;

        #endregion

        #region Script Members

        public IList<UFunction> Functions { get; private set; }

        #endregion

        #region Constructors

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

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedStateFlagsToUState)
            {
#if BORDERLANDS2 || TRANSFORMERS || BATMAN
                // FIXME:Temp fix
                if (Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                    Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn ||
                    Package.Build == BuildGeneration.HMS ||
                    Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
                {
                    _StateFlags = _Buffer.ReadUShort();
                    goto skipStateFlags;
                }
#endif
                _StateFlags = _Buffer.ReadUInt32();
            skipStateFlags:
                Record(nameof(_StateFlags), (StateFlags)_StateFlags);
            }
#if TRANSFORMERS
            if (Package.Build == BuildGeneration.HMS)
            {
                _Buffer.Skip(4);
                _Buffer.ConformRecordPosition();
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

        protected override void FindChildren()
        {
            base.FindChildren();
            Functions = new List<UFunction>();
            for (var child = Children; child != null; child = child.NextField)
            {
                if (child.IsClassType("Function"))
                {
                    Functions.Insert(0, (UFunction)child);
                }
            }
        }

        #endregion

        #region Methods

        public bool HasStateFlag(StateFlags flag) => (_StateFlags & (uint)flag) != 0;

        public bool HasStateFlag(uint flag) => (_StateFlags & flag) != 0;

        #endregion
    }
}
