namespace UELib
{
    /// <summary>
    /// An abstract class for any table entry to inherit from.
    /// </summary>
    public abstract class UTableItem
    {
        /// <summary>
        /// Index into the table's enumerable.
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public int Index { get; internal set; }

        /// <summary>
        /// Table offset in bytes.
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public int Offset { get; internal set; }

        /// <summary>
        /// Table size in bytes.
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public int Size { get; internal set; }
    }
}