using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UELib.Branch;
using UELib.Engine;
using UELib.Flags;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UStructProperty/Core.StructProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UStructProperty : UProperty
    {
        #region Serialized Members

        /// <summary>
        ///     The UStruct that this property represents.
        /// </summary>
        [StreamRecord]
        public UStruct Struct { get; set; }

        #endregion

        public UStructProperty()
        {
            Type = PropertyType.StructProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Struct = stream.ReadObject<UStruct>();
            stream.Record(nameof(Struct), Struct);

            Debug.Assert(Struct != null);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            Debug.Assert(Struct != null);
            stream.Write(Struct);
        }

        public override string GetFriendlyType()
        {
            return Struct != null ? Struct.GetFriendlyType() : "@NULL";
        }

        // Backported code from another branch with a rewritten version of UDefaultProperty.
        public abstract unsafe class PropertyValueSerializer
        {
            /// <summary>
            /// List of struct types the library can serialize using binary.
            /// </summary>
            public enum BinaryStructType
            {
                /// <summary>
                /// Not a supported struct type.
                /// </summary>
                None,

                IntPoint,
                Box,
                Color,
                Coords,
                Guid,
                LinearColor,
                Matrix,
                Plane,
                Quat,
                Range,
                RangeVector,
                Rotator,
                Scale,
                Sphere,
                TwoVectors,
                Vector,
                Vector2D,
                Vector4,

                // < UE3
                PointRegion,

                // Auto-conversions for old (<= 61) "StructName"s
                Rotation = Rotator,
                Region = PointRegion,
            }

            /// <summary>
            /// All structs are serialized using binary prior to 118.
            ///
            /// After version 118, the structs 'Vector', 'Rotator', and 'Color' are still serialized using binary.
            /// In some cases (such as save files) any struct is serialized using binary.
            ///
            /// Starting with UE3, a struct should be serialized using binary if the struct is marked either as 'Atomic' or 'Immutable'.
            /// </summary>
            public static bool CanSerializeStructUsingTags(IUnrealStream stream)
            {
#if UT
                // Added with UT2004.
                if (stream.Build == BuildGeneration.UE2_5 &&
                    stream.LicenseeVersion >= 28) // Are there any packages with version < 118 but licensee version >= 28?
                {
                    return true;
                }
#endif
                return stream.Version >= (uint)PackageObjectLegacyVersion.SerializeStructTags;
            }

            public static bool CanSerializeStructUsingBinary(IUnrealStream stream)
            {
#if DNF
                // All structs are serialized using tags.
                if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
                {
                    return false;
                }
#endif
                // ALl structs are serialized using binary.
                if (CanSerializeStructUsingTags(stream) == false)
                {
                    return true;
                }

                // Inconclusive, only immutable structs should be serialized using binary.
                return true;
            }

            // FIXME: Should also check if the package was serialized using binary for all structs.
            // e.g. SaveGame packages or any compressed package.
            public static bool IsStructImmutable(IUnrealStream stream, UStruct? uStruct, BinaryStructType binaryStructType)
            {
                if (stream.Version >= (uint)PackageObjectLegacyVersion.StructsShouldNotInheritImmutable)
                {
                    if (uStruct is { PackageIndex.IsExport: true })
                    {
                        return uStruct.StructFlags.HasFlag(StructFlag.Immutable)
                            || (uStruct.StructFlags.HasFlag(StructFlag.ImmutableWhenCooked)
                                && stream.IsCooked())
                        ;
                    }

                    // Fallback to a hardcoded list of immutable structs if the struct was not found.
                    return binaryStructType != BinaryStructType.None;
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedImmutableStructs)
                {
                    if (uStruct is { PackageIndex.IsExport: true })
                    {
                        return uStruct.EnumerateSuper().Any(super => super.StructFlags.HasFlag(StructFlag.Immutable));
                    }

                    // Fallback to a hardcoded list of immutable structs if the struct was not found.
                    return binaryStructType != BinaryStructType.None;
                }

                // All structs are immutable if we cannot serialize using tags.
                if (CanSerializeStructUsingTags(stream) == false)
                {
                    return true;
                }

                // Older than 278, fallback to a limited list of immutable structs.
                return binaryStructType switch
                {
                    BinaryStructType.Vector => true,
                    BinaryStructType.Rotator => true,
                    BinaryStructType.Color => true,
                    // Not attested in the assembly (UT2004), but it is in fact serialized using binary.
                    BinaryStructType.PointRegion => true,
                    _ => false
                };
            }
        }
    }
}
