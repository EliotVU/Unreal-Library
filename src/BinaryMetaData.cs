﻿using System;
using System.Collections.Generic;

namespace UELib
{
    /// <summary>
    /// Collects a log of deserialized fields.
    /// 
    /// This is the class initialized for every UObject instance to hold the deserialized data from the "Record" calls called in the "Deserialize" function.
    /// The data can then be used to show the structure of an UClass such as seen in "View Buffer" of UE Explorer.
    /// </summary>
    public class BinaryMetaData
    {
        /// <summary>
        /// Represents a deserialized field.
        /// </summary>
        public struct BinaryField : IUnrealDecompilable
        {
            /// <summary>
            /// The offset in bytes where this field's value was read.
            /// </summary>
            public long Offset { get; set; }
            
            /// <summary>
            /// Name of this field.
            /// </summary>
            public string Field { get; set; }

            /// <summary>
            /// Value of this field.
            /// </summary>
            public object? Value { get; set; }

            /// <summary>
            /// The size in bytes of this field's value.
            /// </summary>
            public long Size { get; set; }

            [Obsolete("Use Field")]
            public string Name => Field;

            [Obsolete("Use Value")]
            public object? Tag => Value;

            [Obsolete("Use Offset")] public int Position => (int)Offset;

            /// <summary>
            /// Decompiles and returns the output of @Tag.
            /// </summary>
            /// <returns>Output of @Tag or "NULL" if @Tag is null</returns>
            public string Decompile()
            {
                return Value != null 
                    ? $"({Value.GetType()}) : {Value}"
                    : "NULL";
            }
        }

        /// <summary>
        /// Stack of all deserialized fields.
        /// </summary>
        public Stack<BinaryField> Fields = new Stack<BinaryField>(1);

        /// <summary>
        /// Adds a new field to the @Fields stack.
        /// </summary>
        /// <param name="field">Name of the field</param>
        /// <param name="value">Value of the field</param>
        /// <param name="offset">Position in bytes where the field is read from</param>
        /// <param name="size">Size in bytes of the field</param>
        public void AddField(string field, object? value, long offset, long size)
        {
            //Debug.Assert(size > 0, $"Size of field {field} at {offset} cannot be less than 1");
            Fields.Push
            (
                new BinaryField
                {
                    Field = field,
                    Value = value,
                    Offset = offset,
                    Size = size
                }
            );
        }
    }
}
