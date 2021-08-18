using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UELib
{
    public enum BuildGeneration
    {
        Undefined,

        // Batman2+ use the same Engine spinoff, but it's still necessary to distinguish the builds by name.
        Batman2,
        Batman3,
        Batman3MP,
        Batman4
    }

    [Flags]
    public enum BuildFlags : byte
    {
        /// <summary>
        /// Is cooked for consoles.
        /// </summary>
        ConsoleCooked = 0x01,

        /// <summary>
        /// Is cooked for Xenon(Xbox 360). Could be true on PC games.
        /// </summary>
        XenonCooked = 0x02,

    }
}
