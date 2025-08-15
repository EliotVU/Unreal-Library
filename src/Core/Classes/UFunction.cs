using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using UELib.Branch;
using UELib.Flags;
using UELib.IO;
using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UFunction/Core.Function
    /// </summary>
    [UnrealRegisterClass]
    public partial class UFunction : UStruct, IUnrealNetObject
    {
        #region Serialized Members

        /// <summary>
        ///     The native toke index for this function.
        ///     A matching token index is serialized for any <see cref="UStruct.UByteCodeDecompiler.NativeFunctionToken" />
        /// </summary>
        [StreamRecord]
        public ushort NativeToken { get; set; }

        /// <summary>
        ///     The operation precedence for this operator.
        /// </summary>
        [StreamRecord]
        public byte OperPrecedence { get; set; }

        /// <summary>
        ///     The function flags for this function.
        /// </summary>
        [StreamRecord]
        public UnrealFlags<FunctionFlag> FunctionFlags { get; set; }

        /// <summary>
        ///     The offset to the conditional in the replication script of the outer-class.
        /// </summary>
        [StreamRecord]
        public ushort RepOffset { get; set; }

        /// <summary>
        ///     Whether this function is marked with the 'reliable' modifier in the replication block or function declaration.
        /// </summary>
        public bool RepReliable => FunctionFlags.HasFlag(FunctionFlag.NetReliable);

        public uint RepKey => RepOffset | ((uint)Convert.ToByte(RepReliable) << 16);

        #endregion

        public UProperty? ReturnProperty => EnumerateFields<UProperty>()
            .FirstOrDefault(prop => prop.PropertyFlags.HasFlag(PropertyFlag.ReturnParm));

        [Obsolete("Use EnumerateFields")]
        public IEnumerable<UProperty> Params => EnumerateFields<UProperty>().Where(prop => prop.IsParm());

        public override void Deserialize(IUnrealStream stream)
        {
#if BORDERLANDS2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
            {
                ushort size = stream.ReadUShort();
                stream.Skip(size * 2);
                stream.Record("Unknown:Borderlands2", size);
            }
#endif
            base.Deserialize(stream);
#if UE4
            if (stream.IsUE4())
            {
                FunctionFlags = stream.ReadFlags32<FunctionFlag>();
                stream.Record(nameof(FunctionFlags), FunctionFlags);

                if (FunctionFlags.HasFlag(FunctionFlag.Net))
                {
                    RepOffset = stream.ReadUShort();
                    stream.Record(nameof(RepOffset), RepOffset);
                }

                FriendlyName = ExportResource.ObjectName;

                return;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                ushort paramsSize = stream.ReadUShort();
                stream.Record(nameof(paramsSize), paramsSize);
            }

            NativeToken = stream.ReadUShort();
            stream.Record(nameof(NativeToken), NativeToken);

            if (stream.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                byte paramsCount = stream.ReadByte();
                stream.Record(nameof(paramsCount), paramsCount);
            }

            OperPrecedence = stream.ReadByte();
            stream.Record(nameof(OperPrecedence), OperPrecedence);

            if (stream.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                ushort returnValueOffset = stream.ReadUShort();
                stream.Record(nameof(returnValueOffset), returnValueOffset);
            }

#if TRANSFORMERS
            // FIXME: version
            if (stream.Build == BuildGeneration.HMS)
            {
                FunctionFlags = stream.ReadFlags64<FunctionFlag>();
                stream.Record(nameof(FunctionFlags), FunctionFlags);

                goto skipFunctionFlags;
            }
#endif
            FunctionFlags = stream.ReadFlags32<FunctionFlag>();
            stream.Record(nameof(FunctionFlags), FunctionFlags);
#if ROCKETLEAGUE
            // Disassembled code shows two calls to ByteOrderSerialize, might be a different variable not sure.
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 24)
            {
                // HO:0x04 = Constructor
                uint v134 = stream.ReadUInt32();
                FunctionFlags = new UnrealFlags<FunctionFlag>(FunctionFlags | (ulong)v134 << 32, FunctionFlags.FlagsMap);
            }
#endif
#if SA2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SA2 &&
                stream.Version >= 869)
            {
                uint v4d = stream.ReadUInt32();
                stream.Record(nameof(v4d), v4d);
                FunctionFlags = new UnrealFlags<FunctionFlag>(FunctionFlags | (ulong)v4d << 32, FunctionFlags.FlagsMap);
            }
#endif
        skipFunctionFlags:
            if (FunctionFlags.HasFlag(FunctionFlag.Net))
            {
                RepOffset = stream.ReadUShort();
                stream.Record(nameof(RepOffset), RepOffset);
            }
#if SPELLBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn
                && 133 < stream.Version)
            {
                // Always 0xAC6975C
                uint unknownFlags1 = stream.ReadUInt32();
                stream.Record(nameof(unknownFlags1), unknownFlags1);
                uint replicationFlags = stream.ReadUInt32();
                stream.Record(nameof(replicationFlags), replicationFlags);
            }
#endif
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent &&
                stream.Version >= 133)
            {
                FriendlyName = stream.ReadName();
                stream.Record(nameof(FriendlyName), FriendlyName);

                // Debug.Assert here, because we can work without a FriendlyName, but it is not expected.
                Debug.Assert(FriendlyName.IsNone() == false, "FriendlyName should not be 'None'");

                return;
            }
#endif

            if ((stream.Version >= (uint)PackageObjectLegacyVersion.MovedFriendlyNameToUFunction &&
                 stream.ContainsEditorOnlyData()
#if TRANSFORMERS
                 // Cooked, but not stripped, However FriendlyName got stripped or deprecated.
                 && stream.Build != BuildGeneration.HMS)
#endif
#if MKKE
                // Cooked and stripped, but FriendlyName still remains
                || stream.Build == UnrealPackage.GameBuild.BuildName.MKKE
#endif
#if MASS_EFFECT
                // Cooked and stripped, but FriendlyName still remains
                || stream.Build == BuildGeneration.SFX
#endif
               )
            {
                FriendlyName = stream.ReadName();
                stream.Record(nameof(FriendlyName), FriendlyName);

                // Debug.Assert here, because we can work without a FriendlyName, but it is not expected.
                Debug.Assert(FriendlyName.IsNone() == false, "FriendlyName should not be 'None'");
            }
            else
            {
                // HACK: Workaround for packages that have stripped FriendlyName data.
                // FIXME: Operator names need to be translated.
                if (FriendlyName.IsNone()) FriendlyName = Name;
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if UE4
            if (stream.IsUE4())
            {
                stream.Write((uint)FunctionFlags);
                if (FunctionFlags.HasFlag(FunctionFlag.Net))
                {
                    stream.Write(RepOffset);
                }

                // FriendlyName is not written here, as it's set from ExportTable.ObjectName
                return;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                // not verified.
                ushort paramsSize = EnumerateFields<UProperty>()
                    .Where(prop => prop.IsParm())
                    .Aggregate<UProperty, ushort>(0, (size, prop) => (ushort)(size + prop.ArrayDim * prop.ElementSize));
                stream.Write(paramsSize);
            }

            stream.Write(NativeToken);

            if (stream.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                // not verified.
                byte paramsCount = (byte)EnumerateFields<UProperty>().Count(prop => prop.IsParm());
                stream.Write(paramsCount);
            }

            stream.Write(OperPrecedence);

            if (stream.Version < (uint)PackageObjectLegacyVersion.Release64)
            {
                // not verified.
                ushort returnValueOffset = EnumerateFields<UProperty>()
                   .Where(prop => prop.IsParm())
                   .TakeWhile(prop => prop.HasPropertyFlag(PropertyFlag.ReturnParm) == false)
                   .Aggregate<UProperty, ushort>(0, (size, prop) => (ushort)(size + prop.ArrayDim * prop.ElementSize));
                stream.Write(returnValueOffset);
            }

#if TRANSFORMERS
            if (stream.Build == BuildGeneration.HMS)
            {
                stream.Write((ulong)FunctionFlags);

                goto skipFunctionFlags;
            }
#endif
            stream.Write((uint)FunctionFlags);
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 24)
            {
                // v134
                stream.Write((uint)((ulong)FunctionFlags >> 32));
            }
#endif
#if SA2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SA2 &&
                stream.Version >= 869)
            {
                // v4d
                stream.Write((uint)((ulong)FunctionFlags >> 32));
            }
#endif
        skipFunctionFlags:
            if (FunctionFlags.HasFlag(FunctionFlag.Net))
            {
                stream.Write(RepOffset);
            }
#if SPELLBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn
                && 133 < stream.Version)
            {
                stream.Write((uint)0); // unknownFlags1
                stream.Write((uint)0); // replicationFlags
            }
#endif
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent &&
                stream.Version >= 133)
            {
                Contract.Assert(FriendlyName.IsNone() == false, "FriendlyName should not be 'None'");
                stream.Write(FriendlyName);

                return;
            }
#endif

            if ((stream.Version >= (uint)PackageObjectLegacyVersion.MovedFriendlyNameToUFunction &&
                 stream.ContainsEditorOnlyData()
#if TRANSFORMERS
                 && stream.Build != BuildGeneration.HMS)
#endif
#if MKKE
                || stream.Build == UnrealPackage.GameBuild.BuildName.MKKE
#endif
#if MASS_EFFECT
                || stream.Build == BuildGeneration.SFX
#endif
               )
            {
                Contract.Assert(FriendlyName.IsNone() == false, "FriendlyName should not be 'None'");
                stream.Write(FriendlyName);
            }
        }

        [Obsolete("Use HasAnyFunctionFlags")]
        public bool HasFunctionFlag(uint flag)
        {
            return (FunctionFlags & flag) != 0;
        }

        [Obsolete("Use FunctionFlags directly")]
        public bool HasFunctionFlag(FunctionFlags flag)
        {
            return (FunctionFlags & (uint)flag) != 0;
        }

        internal bool HasFunctionFlag(FunctionFlag flagIndex)
        {
            return FunctionFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(FunctionFlag)], flagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnyFunctionFlags(ulong flag) => (FunctionFlags & flag) != 0;

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
                   && Script != null
                   && EnumerateFields<UProperty>()
                       .Any(prop => prop.HasPropertyFlag(PropertyFlag.Parm))
                // Not available for older packages.
                // && HasFunctionFlag(Flags.FunctionFlags.OptionalParameters);
                ;
        }
    }
}
