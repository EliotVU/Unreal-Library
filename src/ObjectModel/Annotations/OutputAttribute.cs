using System;
using System.Diagnostics;

namespace UELib.ObjectModel.Annotations
{
    public enum OutputSlot
    {
        Parameter,
        Property
    }

    [Flags]
    public enum OutputFlags
    {
        Default = 0x00,
        ShorthandProperty = 1 << 0
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public class OutputAttribute : Attribute
    {
        public readonly string Identifier;
        public readonly OutputSlot Slot;
        public readonly OutputFlags Flags;

        public OutputAttribute()
        {
            Slot = OutputSlot.Property;
            Flags = OutputFlags.Default;
        }

        public OutputAttribute(string identifier)
        {
            Identifier = identifier;
            Slot = OutputSlot.Property;
            Flags = OutputFlags.Default;
        }
        
        public OutputAttribute(OutputFlags flags)
        {
            Slot = OutputSlot.Property;
            Flags = flags;
        }
        
        public OutputAttribute(string identifier, OutputFlags flags)
        {
            Identifier = identifier;
            Slot = OutputSlot.Property;
            Flags = flags;
        }
        
        public OutputAttribute(string identifier, OutputSlot slot, OutputFlags flags = OutputFlags.Default)
        {
            Debug.Assert(slot != OutputSlot.Parameter || !flags.HasFlag(OutputFlags.ShorthandProperty));

            Identifier = identifier;
            Slot = slot;
            Flags = flags;
        }

        public OutputAttribute(OutputSlot slot, OutputFlags flags = OutputFlags.Default)
        {
            Debug.Assert(slot != OutputSlot.Parameter || !flags.HasFlag(OutputFlags.ShorthandProperty));

            Slot = slot;
            Flags = flags;
        }
    }
}