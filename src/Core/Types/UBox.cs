﻿using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FBox/UObject.Box
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)] // Must pack as 1 for the marshaller to read the correct amount of storage bytes.
    public record struct UBox : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public UVector Min, Max;
        public byte IsValid;

        public UBox(UVector min, UVector max, byte isValid)
        {
            Min = min;
            Max = max;
            IsValid = isValid;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.ReadStruct(out Min);
            stream.ReadStruct(out Max);
            stream.Read(out IsValid);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.WriteStruct(ref Min);
            stream.WriteStruct(ref Max);
            stream.Write(IsValid);
        }
    }
}