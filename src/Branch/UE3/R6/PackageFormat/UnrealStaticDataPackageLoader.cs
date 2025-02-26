using System;
using System.Collections.Generic;
using System.IO;

namespace UELib.Branch.UE3.R6.PackageFormat
{
    public class UnrealStaticDataPackageLoader(UnrealStaticDataPackage package)
    {
        /// <summary>
        ///     UObject class name to IUnrealSerializableClass
        /// </summary>
        public static readonly Dictionary<string, Type> ClassTypeSerializers = new()
        {
            { "Texture2D", typeof(UStaticTexture2D) }
        };

        public UnrealStaticDataPackageStream Load(Stream baseStream)
        {
            var stream = new UnrealStaticDataPackageStream(baseStream);
            Load(stream);

            return stream;
        }

        /// <summary>
        ///     Deserializes a .usdx file from a stream.
        ///     Depending on the file name, a different header has to be deserialized before the package data.
        /// </summary>
        /// <param name="stream">the input stream.</param>
        public void Load(IUnrealStream stream)
        {
            package.Deserialize(stream);

            // Seek-free FStaticCLASSNAME data here...
        }
    }
}
