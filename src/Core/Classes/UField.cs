using System;
using System.Collections.Generic;
using UELib.Branch;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UField/Core.Field
    /// </summary>
    public partial class UField : UObject
    {
        #region Serialized Members

        public UStruct? Super { get; set; }
        public UField? NextField { get; set; }

        #endregion

        /// <summary>
        ///     Reference to the metadata for this field, if any.
        /// </summary>
        public UMetaData.UFieldData? MetaData { get; internal set; }

        #region Constructors

        protected override void Deserialize()
        {
            base.Deserialize();
#if SWRepublicCommando
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                return;
            }
#endif
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.SuperReferenceMovedToUStruct)
            {
                Super = _Buffer.ReadObject<UStruct?>();
                Record(nameof(Super), Super);
            }

            NextField = _Buffer.ReadObject<UField?>();
            Record(nameof(NextField), NextField);
        }

        #endregion

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

        public bool Extends(string superName) => Extends(new UName(superName));

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
                group = $"{field.Name}.{@group}";
            }

            return group + Name;
        }
    }
}
