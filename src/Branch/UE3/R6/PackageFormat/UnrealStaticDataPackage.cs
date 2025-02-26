using System;
using UELib.Core;

namespace UELib.Branch.UE3.R6.PackageFormat
{
    /// <summary>
    ///     A Keller's Unreal Static Data package i.e. a .usdx file.
    ///     p.s. the 's' may stand for streaming instead.
    /// </summary>
    public class UnrealStaticDataPackage(string packageName) : IUnrealSerializableClass
    {
        public readonly string PackageName = packageName;

        /// <summary>
        /// The class resources if the package is deserialized from a "weapons.usdx" file.
        ///
        /// The Resources field is likely a UBulkArray type instead.
        /// </summary>
        public UMap<string, UArray<UStaticObjectResource>> ClassResources;

        /// <summary>
        /// The resources if the package is deserialized from a "pec.usdx" file.
        /// 
        /// TODO: Unknown index... Used exclusively with the pec.usdx file.
        /// </summary>
        public UMap<int, UArray<UStaticObjectResource>> IndexedResources;

        /// <summary>
        /// The package guid if the package is deserialized from any ordinary .usdx file.
        /// </summary>
        public UGuid PackageGuid;

        public void Deserialize(IUnrealStream stream)
        {
            if (string.Compare(PackageName, "weapons", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                stream.Read(out int count);
                ClassResources = new UMap<string, UArray<UStaticObjectResource>>(count);
                for (int i = 0; i < count; ++i)
                {
                    stream.Read(out string className);
                    stream.Read(out int skipOffset);
                    stream.ReadArray(out UArray<UStaticObjectResource> resources);

                    ClassResources.Add(className, resources);
                }

                return;
            }

            if (string.Compare(PackageName, "pec", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                stream.Read(out int count);
                IndexedResources = new UMap<int, UArray<UStaticObjectResource>>(count);
                for (int i = 0; i < count; i++)
                {
                    // index into an array of 20 bytes per element, incrementing by 1 starting from 11000
                    // MaterialId? Similar numerals can be found in R6 script files.
                    stream.Read(out int key);
                    stream.Read(out int skipOffset);
                    // PackageName doesn't seem correct here
                    stream.Read(out UArray<UStaticObjectResource> resources); // BulkData

                    IndexedResources.Add(key, resources);
                }

                stream.Read(out int v1);
                stream.Read(out int v2);

                stream.Read(out int v3);
                for (int i = 0; i < v3; ++i)
                {
                    stream.Read(out int _);
                }

                // Compressed data?

                return;
            }

            stream.ReadStruct(out PackageGuid);
        }

        public void Serialize(IUnrealStream stream)
        {
            if (string.Compare(PackageName, "weapons", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                stream.Write(ClassResources.Count);
                foreach (var classResource in ClassResources)
                {
                    stream.Write(classResource.Key);
                    long p = stream.Position;
                    stream.Write(0);
                    stream.WriteArray(classResource.Value);
                    int skipOffset = (int)stream.Position;
                    stream.Position = p;
                    stream.Write(skipOffset);
                    stream.Position = skipOffset;
                }

                return;
            }

            if (string.Compare(PackageName, "pec", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // todo, incomplete deserialization a.t.m
                return;
            }

            stream.WriteStruct(ref PackageGuid);
        }
    }

    public class UStaticObjectResource : IUnrealSerializableClass
    {
        public string ObjectName;
        public string ObjectClassName;
        public string ObjectPackageName;

        /// <summary>
        ///     Absolute offset to data like a "FStaticTexture2D" if the <seealso cref="ObjectClassName" /> is "Texture2D"
        /// </summary>
        public int ObjectOffset;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out ObjectName);
            stream.Read(out ObjectClassName);
            stream.Read(out ObjectPackageName);
            stream.Read(out ObjectOffset);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(ObjectName);
            stream.Write(ObjectClassName);
            stream.Write(ObjectPackageName);
            stream.Write(ObjectOffset);
        }
    }

    public class UStaticTexture2D : IUnrealSerializableClass
    {
        public void Deserialize(IUnrealStream stream) => throw new NotImplementedException();

        public void Serialize(IUnrealStream stream) => throw new NotImplementedException();
    }
}
