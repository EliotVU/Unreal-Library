using System.Collections.Generic;

namespace UELib
{
    public class BinaryMetaData
    {
        public struct BinaryField : IUnrealDecompilable
        {
            public string Name;
            public object Tag;
            public long Position;
            public long Size;

            public string Decompile()
            {
                return Tag != null ? Tag.ToString() : "NULL";
            }
        }

        public Stack<BinaryField> Fields;

        [System.Diagnostics.Conditional( "DEBUG" )]
        public void AddField( string name, object tag, long position, long size )
        {
            if( Fields == null )
            {
                Fields = new Stack<BinaryField>();
            }

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
