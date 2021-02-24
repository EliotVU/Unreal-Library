﻿using UELib.JsonDecompiler.Types;

namespace UELib.JsonDecompiler.Core
{
    /// <summary>
    /// Component Property
    ///
    /// UE3 Only
    /// </summary>
    [UnrealRegisterClass]
    public class UComponentProperty : UObjectProperty
    {
        /// <summary>
        /// Creates a new instance of the UELib.Core.UComponentProperty class.
        /// </summary>
        public UComponentProperty()
        {
            Type = PropertyType.ComponentProperty;
        }
    }
}