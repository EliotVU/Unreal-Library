using System;

namespace UELib
{
    public enum BuildGeneration
    {
        Undefined,
        
        Thief,
        UE2_5,
        UE2X,
        Vengeance,
        Lead,
        
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

        /// <summary>
        /// Some UDK games have disabled the DLLBind feature.
        /// </summary>
        NoDLLBind = 0x04
    }
}
