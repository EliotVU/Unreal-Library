using System;

namespace UELib.IO;

[Flags]
public enum UnrealArchiveFlags
{
    /// <summary>
    ///     The archive is serialized using big endian byte order.
    /// </summary>
    BigEndian = 1 << 0,

    /// <summary>
    ///     The archive is encoded and should pipe to an <see cref="EncodedStream"/>
    /// </summary>
    Encoded = 1 << 1,
}
