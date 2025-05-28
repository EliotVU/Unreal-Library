﻿using System;
using System.Collections.Generic;
using UELib.Branch;

namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal field.
    /// </summary>
    public partial class UField : UObject
    {
        #region Serialized Members

        public UStruct? Super { get; set; }
        public UField? NextField { get; set; }

        #endregion

        /// <summary>
        /// Initialized by the UMetaData object,
        /// This Meta contains comments and other meta related info that belongs to this instance.
        /// </summary>
        public UMetaData.UFieldData? MetaData;

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
                Super = _Buffer.ReadObject<UStruct>();
                Record(nameof(Super), Super);
            }

            NextField = _Buffer.ReadObject<UField>();
            Record(nameof(NextField), NextField);
        }

        #endregion
        
        public IEnumerable<UStruct> EnumerateSuper()
        {
            for (var super = Super; super != null; super = super.Super)
            {
                yield return super;
            }
        }

        public IEnumerable<T> EnumerateSuper<T>() where T : UStruct
        {
            for (var super = Super; super != null; super = super.Super)
            {
                yield return (T)super;
            }
        }


        public IEnumerable<UStruct> EnumerateSuper(UStruct super)
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

        public bool Extends(string classType)
        {
            for (var field = Super; field != null; field = field.Super)
            {
                if (string.Equals(field.Name, classType, StringComparison.OrdinalIgnoreCase))
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
