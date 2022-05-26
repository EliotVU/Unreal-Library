using System.Collections.Generic;
using System.Diagnostics;
using UELib.Annotations;

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
            /// Name of this field.
            /// </summary>
            [PublicAPI] public string Name;

            /// <summary>
            /// Value of this field.
            /// </summary>
            [PublicAPI] public object Tag;

            /// <summary>
            /// The position in bytes where this field's value was read from.
            /// </summary>
            [PublicAPI] public long Position;

            /// <summary>
            /// The size in bytes of this field's value.
            /// </summary>
            [PublicAPI] public long Size;

            /// <summary>
            /// Decompiles and returns the output of @Tag.
            /// </summary>
            /// <returns>Output of @Tag or "NULL" if @Tag is null</returns>
            [PublicAPI]
            public string Decompile()
            {
                return Tag != null 
                    ? $"({Tag.GetType()}) : {Tag}"
                    : "NULL";
            }
        }

        /// <summary>
        /// Stack of all deserialized fields.
        /// </summary>
        [PublicAPI] public Stack<BinaryField> Fields = new Stack<BinaryField>(1);

        /// <summary>
        /// Adds a new field to the @Fields stack.
        /// </summary>
        /// <param name="name">Name of the field</param>
        /// <param name="tag">Value of the field</param>
        /// <param name="position">Position in bytes where the field is read from</param>
        /// <param name="size">Size in bytes of the field</param>
        [PublicAPI]
        public void AddField(string name, object tag, long position, long size)
        {
            Debug.Assert(size > 0);
            Fields.Push
            (
                new BinaryField
                {
                    Name = name,
                    Tag = tag,
                    Position = position,
                    Size = size
                }
            );
        }
    }
}