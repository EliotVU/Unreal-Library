using System;
using System.Collections.Generic;
using UELib.Branch;
using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UField/Core.Field
    /// </summary>
    public partial class UField : UObject
    {
        /// <summary>
        ///     Reference to the metadata for this field, if any.
        /// </summary>
        public UMetaData.ObjectTags? MetaData { get; internal set; }

        #region Serialized Members

        /// <summary>
        ///     The super-struct of this struct, if any.
        ///     i.e. the parent struct that this struct(or state/class) extends.
        /// </summary>
        [StreamRecord]
        public UStruct? Super { get; set; }

        /// <summary>
        ///     The next field in the chain of fields.
        ///     The chain starts at <seealso cref="UStruct.get_Children" />
        /// </summary>
        [StreamRecord]
        public UField? NextField { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                return;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.SuperReferenceMovedToUStruct)
            {
                Super = stream.ReadObject<UStruct?>();
                stream.Record(nameof(Super), Super);
            }

            NextField = stream.ReadObject<UField?>();
            stream.Record(nameof(NextField), NextField);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                return;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.SuperReferenceMovedToUStruct)
            {
                stream.WriteObject(Super);
            }

            stream.WriteObject(NextField);
        }

        /// <summary>
        ///     Enumerates all super-structs of this struct.
        /// </summary>
        /// <returns>the enumerated super-struct.</returns>
        public IEnumerable<UStruct> EnumerateSuper()
        {
            for (var super = Super; super != null; super = super.Super)
            {
                yield return super;
            }
        }

        /// <summary>
        ///     Enumerates all super-structs of this struct, casting them to the specified type.
        /// </summary>
        /// <typeparam name="T">the struct type to cast the super-struct to.</typeparam>
        /// <returns>the enumerated super-struct.</returns>
        public IEnumerable<T> EnumerateSuper<T>() where T : UStruct
        {
            for (var super = Super; super != null; super = super.Super)
            {
                yield return (T)super;
            }
        }

        /// <summary>
        ///     Enumerates all super-structs of the specified struct.
        /// </summary>
        /// <returns>the enumerated super-struct.</returns>
        public static IEnumerable<UStruct> EnumerateSuper(UStruct super)
        {
            for (; super != null; super = super.Super)
            {
                yield return super;
            }
        }

        public IEnumerable<UField> EnumerateNext()
        {
            for (var next = NextField; next != null; next = next.NextField)
            {
                yield return next;
            }
        }

        public bool Extends(string superName)
        {
            return Extends(new UName(superName));
        }

        public bool Extends(in UName superName)
        {
            for (var field = Super; field != null; field = field.Super)
            {
                if (field.Name == superName)
                {
                    return true;
                }
            }

            return false;
        }

        [Obsolete]
        public string GetSuperGroup()
        {
            var group = string.Empty;
            for (var field = Super; field != null; field = field.Super)
            {
                group = $"{field.Name}.{group}";
            }

            return group + Name;
        }
    }
}
