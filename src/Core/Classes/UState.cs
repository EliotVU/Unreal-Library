using System.Runtime.CompilerServices;
using UELib.Branch;
using UELib.Flags;
using UELib.IO;
using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    public struct ULabelEntry
    {
        /// <summary>
        /// The label name, may be set to a generated name.
        /// </summary>
        public UName Name;

        /// <summary>
        /// The label position in memory relative to the script.
        /// </summary>
        public int CodeOffset;
    }

    /// <summary>
    ///     Implements UState/Core.State
    /// </summary>
    [UnrealRegisterClass]
    public partial class UState : UStruct
    {
        [Obsolete] public const int VProbeMaskReducedAndIgnoreMaskRemoved = 691;

        #region Serialized Members

        /// <summary>
        ///     A mask of functions being probed by this state.
        /// </summary>
        [StreamRecord]
        public ulong ProbeMask { get; set; }

        /// <summary>
        ///     A mask of functions being ignored by this state.
        /// </summary>
        [StreamRecord]
        public ulong IgnoreMask { get; set; }

        /// <summary>
        ///     Offset in the script to get to the <see cref="UStruct.UByteCodeDecompiler.LabelTableToken" /> token.
        /// </summary>
        [StreamRecord]
        public ushort LabelTableOffset { get; private set; }

        /// <summary>
        ///     The state flags for this state.
        /// </summary>
        [StreamRecord]
        public UnrealFlags<StateFlag> StateFlags { get; set; }

        /// <summary>
        ///     A virtual-call function map for this state.
        ///     This maps the name of a <see cref="UStruct.UByteCodeDecompiler.VirtualFunctionToken" /> to the actual function.
        ///     Always null if version is lower than <see cref="PackageObjectLegacyVersion.AddedFuncMapToUState" />
        /// </summary>
        [StreamRecord]
        public UMap<UName, UFunction>? FuncMap { get; set; }

        #endregion

        [Obsolete("Use EnumerateFields")]
        public IEnumerable<UFunction> Functions => EnumerateFields<UFunction>();

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
#if UE4
            if (stream.IsUE4())
            {
                return;
            }
#endif
#if TRANSFORMERS
            if (stream.Build == BuildGeneration.HMS)
            {
                goto noMasks;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.ProbeMaskReducedAndIgnoreMaskRemoved)
            {
                ProbeMask = stream.ReadUInt64();
                stream.Record(nameof(ProbeMask), ProbeMask);

                IgnoreMask = stream.ReadUInt64();
                stream.Record(nameof(IgnoreMask), IgnoreMask);
            }
            else
            {
                ProbeMask = stream.ReadUInt32();
                stream.Record(nameof(ProbeMask), ProbeMask);
            }

        noMasks:
            LabelTableOffset = stream.ReadUInt16();
            stream.Record(nameof(LabelTableOffset), LabelTableOffset);

#if BORDERLANDS2 || TRANSFORMERS || BATMAN
            // FIXME:Temp fix
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                (stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn && stream.LicenseeVersion >= 18) ||
                stream.Build == BuildGeneration.HMS ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                ushort flags = stream.ReadUShort();
                StateFlags = new UnrealFlags<StateFlag>(flags, stream.Package.Branch.EnumFlagsMap[typeof(StateFlag)]);

                goto skipStateFlags;
            }
#endif
            StateFlags = stream.ReadFlags32<StateFlag>();
        skipStateFlags:
            stream.Record(nameof(StateFlags), StateFlags);
#if TRANSFORMERS
            if (stream.Build == BuildGeneration.HMS)
            {
                stream.Skip(4);
                stream.ConformRecordPosition();

                return;
            }
#endif
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent &&
                stream.Version >= 134)
            {
                FuncMap = stream.ReadMap(stream.ReadName, stream.ReadObject<UFunction>);
                stream.Record(nameof(FuncMap), FuncMap);

                return;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.AddedFuncMapToUState)
            {
                return;
            }

            FuncMap = stream.ReadMap(stream.ReadName, stream.ReadObject<UFunction>);
            stream.Record(nameof(FuncMap), FuncMap);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if UE4
            if (stream.IsUE4())
            {
                return;
            }
#endif
#if TRANSFORMERS
            if (stream.Build == BuildGeneration.HMS)
            {
                goto noMasks;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.ProbeMaskReducedAndIgnoreMaskRemoved)
            {
                stream.Write(ProbeMask);
                stream.Write(IgnoreMask);
            }
            else
            {
                stream.Write((uint)ProbeMask);
            }

        noMasks:
            stream.Write(LabelTableOffset);
#if BORDERLANDS2 || TRANSFORMERS || BATMAN
            // FIXME:Temp fix
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                (stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn && stream.LicenseeVersion >= 18) ||
                stream.Build == BuildGeneration.HMS ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                stream.Write(StateFlags);

                goto skipStateFlags;
            }
#endif
            stream.Write((uint)StateFlags);
        skipStateFlags:
#if TRANSFORMERS
            if (stream.Build == BuildGeneration.HMS)
            {
                stream.Skip(4);
                throw new NotSupportedException("This package version is not supported!");

                return;
            }
#endif
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent &&
                stream.Version >= 134)
            {
                stream.WriteMap(FuncMap);

                return;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.AddedFuncMapToUState)
            {
                return;
            }

            stream.WriteMap(FuncMap);
        }

        [Obsolete("Use StateFlags directly.")]
        public bool HasStateFlag(StateFlags flag)
        {
            return (StateFlags & (uint)flag) != 0;
        }

        [Obsolete("Use StateFlags directly.")]
        public bool HasStateFlag(uint flag)
        {
            return (StateFlags & flag) != 0;
        }

        internal bool HasStateFlag(StateFlag flagIndex)
        {
            return StateFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(StateFlag)], flagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnyStateFlags(ulong flag) => (StateFlags & flag) != 0;
    }
}
