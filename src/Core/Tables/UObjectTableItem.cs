using System;
using System.ComponentModel;
using System.IO;
using UELib.Core;

namespace UELib
{
    /// <summary>
    /// An internal implementation for the Export and Import table classes.
    /// </summary>
    public abstract class UObjectTableItem : UTableItem, IBuffered
    {
        /// <summary>
        /// Reference to the UnrealPackage this object resists in
        /// </summary>
        public UnrealPackage Owner;

        /// <summary>
        /// Reference to the serialized object based on this table.
        ///
        /// Only valid if Owner != null and Owner is fully serialized or on demand.
        /// </summary>
        public UObject Object;

        #region Serialized Members

        protected UName _ObjectName;
        public UName ObjectName
        {
            get => _ObjectName;
            set => _ObjectName = value;
        }

        protected int _OuterIndex;
        public int OuterIndex
        {
            get => _OuterIndex;
            set => _OuterIndex = value;
        }


        [Obsolete, Browsable(false)] public UNameTableItem ObjectTable => Owner.Names[(int)_ObjectName];
        [Obsolete, Browsable(false)] public UObjectTableItem ClassTable => null;
        [Obsolete, Browsable(false)] public UObjectTableItem OuterTable => Owner.GetIndexTable(OuterIndex);
        public UObjectTableItem Outer => Owner.GetIndexTable(_OuterIndex);

        [Obsolete, Browsable(false)]
        public string OuterName
        {
            get
            {
                var table = OuterTable;
                return table != null ? table._ObjectName : string.Empty;
            }
        }

        #endregion

        #region IBuffered

        public virtual byte[] CopyBuffer()
        {
            var buff = new byte[Size];
            Owner.Stream.Seek(Offset, SeekOrigin.Begin);
            Owner.Stream.Read(buff, 0, Size);
            if (Owner.Stream.BigEndianCode)
            {
                Array.Reverse(buff);
            }

            return buff;
        }

        public IUnrealStream GetBuffer()
        {
            return Owner.Stream;
        }

        public int GetBufferPosition()
        {
            return Offset;
        }

        public int GetBufferSize()
        {
            return Size;
        }

        public string GetBufferId(bool fullName = false)
        {
            return fullName ? Owner.PackageName + "." + _ObjectName + ".table" : _ObjectName + ".table";
        }

        #endregion

        public static explicit operator int(UObjectTableItem item)
        {
            return item.Index;
        }
    }
}