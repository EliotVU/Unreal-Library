using System;

namespace UELib
{
    /// <summary>
    /// An abstract class for any table entry to inherit from.
    /// </summary>
    public abstract class UTableItem
    {
        #region PreInitialized Members

        /// <summary>
        /// Index into the table's enumerable.
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// Table offset in bytes.
        /// </summary>
        public int Offset { get; internal set; }

        /// <summary>
        /// Table size in bytes.
        /// </summary>
        public int Size { get; internal set; }

        #endregion

        #region Methods

        public string ToString(bool shouldPrintMembers)
        {
            return shouldPrintMembers
                ? String.Format("\r\nTable Index:{0}\r\nTable Offset:0x{1:X8}\r\nTable Size:0x{2:X8}\r\n", Index,
                    Offset, Size)
                : base.ToString();
        }

        #endregion
    }
}