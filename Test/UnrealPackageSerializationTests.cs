using System.Diagnostics.Contracts;
using Eliot.UELib.Test.Builds;
using UELib;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;
using UELib.IO;
using UELib.ObjectModel.Annotations;

namespace Eliot.UELib.Test;

[TestClass]
public class UnrealPackageSerializationTests
{
    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.LowestVersion)]
    [DataRow(PackageObjectLegacyVersion.Release64)]
    [DataRow(PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)]
    [DataRow(PackageObjectLegacyVersion.HighestVersion)]
    public void TestPackageNamesSerialization(PackageObjectLegacyVersion version)
    {
        using var sourcePackage = PackageTestsUDK.GetScriptPackage();

        using var outPackage = UnrealPackageUtilities.CreateTempPackage(version);
        var outStream = outPackage.Stream;
        // write the script packages content to the temp stream
        outPackage.Names.AddRange(sourcePackage.Names);
        // Rebuild the name indices (name hash to package index).
        for (int i = 0; i < outPackage.Names.Count; i++)
        {
            outPackage.Archive.NameIndices.Add(((IndexName)outPackage.Names[i]).Index, i);
        }

        var sizes = new long[sourcePackage.Names.Count];

        outStream.Position = 0;
        sourcePackage.Names.ForEach(name =>
        {
            long p = outStream.Position;
            name.Serialize(outStream);
            sizes[name.Index] = outStream.Position - p;
        });
        long endPosition = outStream.Position;
        outStream.Position = 0;
        sourcePackage.Names.ForEach(name =>
        {
            long p = outStream.Position;
            name.Deserialize(outStream);
            Assert.AreEqual(sizes[name.Index], outStream.Position - p);
        });
        Assert.AreEqual(endPosition, outStream.Position);
    }

    [TestMethod]
    // No direct changes, only the serialization of UName and UIndex have changed but this is not the test for that.
    [DataRow(PackageObjectLegacyVersion.UE3)]
    [DataRow(PackageObjectLegacyVersion.HighestVersion)]
    public void TestPackageImportsSerialization(PackageObjectLegacyVersion version)
    {
        using var sourcePackage = PackageTestsUDK.GetScriptPackage();

        using var outPackage = UnrealPackageUtilities.CreateTempPackage(version);
        var outStream = outPackage.Stream;
        // write the script packages content to the temp stream
        outPackage.Names.AddRange(sourcePackage.Names);
        // Rebuild the name indices (name hash to package index).
        for (int i = 0; i < outPackage.Names.Count; i++)
        {
            outPackage.Archive.NameIndices.Add(((IndexName)outPackage.Names[i]).Index, i);
        }
        outPackage.Imports.AddRange(sourcePackage.Imports);

        var sizes = new long[sourcePackage.Imports.Count];

        outStream.Position = 0;
        sourcePackage.Imports.ForEach(imp =>
        {
            long p = outStream.Position;
            imp.Serialize(outStream);
            sizes[imp.Index] = outStream.Position - p;
        });
        long endPosition = outStream.Position;
        outStream.Position = 0;
        sourcePackage.Imports.ForEach(imp =>
        {
            long p = outStream.Position;
            imp.Deserialize(outStream);
            Assert.AreEqual(sizes[imp.Index], outStream.Position - p);
        });
        Assert.AreEqual(endPosition, outStream.Position);
    }

    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.LowestVersion)]
    [DataRow(PackageObjectLegacyVersion.CompactIndexDeprecated)]
    [DataRow(PackageObjectLegacyVersion.NumberAddedToName)]
    [DataRow(PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)]
    [DataRow(PackageObjectLegacyVersion.ArchetypeAddedToExports)]
    [DataRow(PackageObjectLegacyVersion.ExportFlagsAddedToExports)]
    [DataRow(PackageObjectLegacyVersion.SerialSizeConditionRemoved)]
    [DataRow(PackageObjectLegacyVersion.NetObjectCountAdded)]
    [DataRow(PackageObjectLegacyVersion.PackageFlagsAddedToExports)]
    [DataRow(PackageObjectLegacyVersion.ComponentMapDeprecated)]
    [DataRow(PackageObjectLegacyVersion.HighestVersion)]
    public void TestPackageExportsSerialization(PackageObjectLegacyVersion version)
    {
        using var sourcePackage = PackageTestsUDK.GetScriptPackage();

        using var outArchive = UnrealPackageUtilities.CreateTempPackage(version);
        var outPackage = outArchive;
        var outStream = outArchive.Stream;
        // write the script packages content to the temp stream
        outPackage.Names.AddRange(sourcePackage.Names);
        // Rebuild the name indices (name hash to package index).
        for (int i = 0; i < outPackage.Names.Count; i++)
        {
            outPackage.Archive.NameIndices.Add(((IndexName)outPackage.Names[i]).Index, i);
        }
        outPackage.Exports.AddRange(sourcePackage.Exports);

        var sizes = new long[sourcePackage.Exports.Count];
        var flags = new ulong[sourcePackage.Exports.Count];

        outStream.Position = 0;
        sourcePackage.Exports.ForEach(exp =>
        {
            long p = outStream.Position;
            exp.Serialize(outStream);
            sizes[exp.Index] = outStream.Position - p;
            flags[exp.Index] = exp.ObjectFlags;
        });
        long endPosition = outStream.Position;
        outStream.Position = 0;
        sourcePackage.Exports.ForEach(exp =>
        {
            long p = outStream.Position;
            exp.Deserialize(outStream);
            Assert.AreEqual(sizes[exp.Index], outStream.Position - p);
            if (outStream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                Assert.AreEqual(flags[exp.Index], exp.ObjectFlags);
            }
            else
            {
                Assert.AreEqual((uint)flags[exp.Index], (uint)exp.ObjectFlags);
            }
        });
        Assert.AreEqual(endPosition, outStream.Position);
    }

    [TestMethod]
    [DataRow(PackageObjectLegacyVersion.LowestVersion)] // Heritages test
    [DataRow(PackageObjectLegacyVersion.HeritageTableDeprecated)] // Generations test
    [DataRow(PackageObjectLegacyVersion.AddedDependsTable)]
    [DataRow(PackageObjectLegacyVersion.AddedAdditionalPackagesToCook)]
    [DataRow(PackageObjectLegacyVersion.AddedThumbnailTable)]
    [DataRow(PackageObjectLegacyVersion.AddedImportExportGuidsTable)]
    [DataRow(PackageObjectLegacyVersion.AddedTextureAllocations)]
    [DataRow(PackageObjectLegacyVersion.HighestVersion)]
    public void TestPackageSerialization(PackageObjectLegacyVersion version)
    {
        using var sourcePackage = PackageTestsUDK.GetScriptPackage();

        using var outArchive = UnrealPackageUtilities.CreateTempPackage(version);
        var outPackage = outArchive;
        var outStream = outArchive.Stream;

        outPackage.Names.AddRange(sourcePackage.Names);
        // Rebuild the name indices (name hash to package index).
        for (int i = 0; i < outPackage.Names.Count; i++)
        {
            outPackage.Archive.NameIndices.Add(((IndexName)outPackage.Names[i]).Index, i);
        }

        // Version is serialized from the summary instead of the stream version.
        sourcePackage.Summary.Version = outPackage.Version;
        sourcePackage.Summary.LicenseeVersion = outPackage.LicenseeVersion;

        // We expect the offset to CHANGE
        // - so we must set them to 0 to indicate that we don't want to serialize the table data at the last position.
        sourcePackage.Summary.HeritageOffset = 0;
        sourcePackage.Summary.NameOffset = 0;
        sourcePackage.Summary.ImportOffset = 0;
        sourcePackage.Summary.ExportOffset = 0;
        sourcePackage.Summary.DependsOffset = 0;
        sourcePackage.Summary.ImportExportGuidsOffset = 0;
        sourcePackage.Summary.ThumbnailTableOffset = 0;

        // Copy the entire package to the temp stream (minus objects)
        outStream.Position = 0;
        sourcePackage.Serialize(outStream);
        long endPosition = outStream.Position;

        // Now that we know the correct table offsets, re-write it with the correct offsets.
        outStream.Position = 0;
        sourcePackage.Summary.Serialize(outStream);

        // re-deserialize from our temp copy to see if it all aligns.
        outStream.Position = 0;
        outPackage.Deserialize(outStream);
        Assert.AreEqual(endPosition, outStream.Position);
    }

    [TestMethod]
    public void TestPackageSaving()
    {
        using var sourcePackage = PackageTestsUDK.GetScriptPackage();
        sourcePackage.InitializePackage(null, InternalClassFlags.Default);

        // Create a new temporary package to contain a copy of the test package.
        using var outPackage = UnrealPackageUtilities.CreateTempPackage(
            (PackageObjectLegacyVersion)sourcePackage.Version,
            sourcePackage.LicenseeVersion,
            sourcePackage.Linker.PackageEnvironment
        );
        var outStream = outPackage.Stream;

        outPackage.Names.AddRange(sourcePackage.Names);
        // Rebuild the name indices (name hash to package index).
        for (int i = 0; i < outPackage.Names.Count; i++)
        {
            outPackage.Archive.NameIndices.Add(((IndexName)outPackage.Names[i]).Index, i);
        }
        // just for fun, let's double the function sizes
        // FIXME: Not possible yet, we need to preserve the old bulk data for which we need to preserve the SerialOffset :/

        //foreach (var function in sourcePackage.Objects
        //             .Where(obj => (int)obj > 0)
        //             .OfType<UFunction>())
        //{
        //    function.ExportTable.SerialOffset = 0;
        //    function.ExportTable.SerialSize *= 2;
        //}

        using var packageSaver = new UnrealPackageSaver();
        using var saveStream = packageSaver.Save(sourcePackage, outStream);

        outPackage.Deserialize(outStream);
        outPackage.InitializePackage(null, InternalClassFlags.Default);

        //tempStream.Flush();
    }

    private class UnrealPackageSaver : IDisposable
    {
        public void Dispose()
        {
            // TODO release managed resources here
        }

        public UnrealSaveStream Save(UnrealPackage sourcePackage, IUnrealStream baseStream)
        {
            // Version is serialized from the summary instead of the stream version.
            // This is because the stream version may technically be incorrect due that some games' deserialized versions may be overriden.
            sourcePackage.Summary.Version = baseStream.Package.Version;
            sourcePackage.Summary.LicenseeVersion = baseStream.Package.LicenseeVersion;

            // We expect the offset to CHANGE
            // - so we must set them to 0 to indicate that we don't want to serialize the table data at the last position.
            sourcePackage.Summary.HeritageOffset = 0;
            sourcePackage.Summary.NameOffset = 0;
            sourcePackage.Summary.ImportOffset = 0;
            sourcePackage.Summary.ExportOffset = 0;
            sourcePackage.Summary.DependsOffset = 0;
            sourcePackage.Summary.ImportExportGuidsOffset = 0;
            sourcePackage.Summary.ThumbnailTableOffset = 0;

            sourcePackage.Summary.Guid = (UGuid)Guid.NewGuid();

            int originalHeaderSize = sourcePackage.Summary.HeaderSize;
            sourcePackage.Summary.HeaderSize = 0;

            // To be used to save objects (to track the export indexes etc.)
            var stream = new UnrealSaveStream(this, sourcePackage.Archive, baseStream);

            // This will also re-set the table offsets.
            stream.Position = 0;
            sourcePackage.Serialize(baseStream);
            long endPosition = stream.Position;

            // If the tables go beyond the first object offset, then we should adapt accordingly.
            int newHeaderSize = sourcePackage.Summary.HeaderSize;
            bool hasIncreasedHeaderSize = newHeaderSize > originalHeaderSize;

            // Now that we know the correct table offsets, re-write it with the correct offsets.
            stream.Position = 0;
            sourcePackage.Summary.Serialize(baseStream);
            Contract.Assert(endPosition == stream.Position);

            // Objects position
            stream.Seek(newHeaderSize, SeekOrigin.Begin);

            var exports = sourcePackage
                .Linker
                .PackageEnvironment
                .ObjectContainer
                .Enumerate(sourcePackage)
                .Where(IsExportableObject)
                .ToList();

            bool canSerializeAllObjects = exports.All(CanSerializeObject);
            if (canSerializeAllObjects)
            {
                // We can serialize all object types.

                throw new NotSupportedException("Object serialization is not yet implemented.");
            }

            // When we cannot serialize all objects we should serialize the objects in the last position.

            // We cannot move the unsupported objects so abort.
            if (hasIncreasedHeaderSize)
            {
                throw new NotSupportedException(
                    "The package header has increased in size, and the package contains objects that are not supported.");
            }

            //exports.ForEach(obj => obj.BeginDeserializing());

            // The header is equal or smaller than before, we can serialize in place.

            // Objects that must be serialized at the last known offset.
            var objectsInPlace = exports
                .Where(
                    obj => obj.ExportResource.SerialOffset > 0 /*&& obj.ExportTable.SerialSize <= obj.DeserializedSize*/);

            long longestPosition = stream.Position;

            foreach (var obj in objectsInPlace)
            {
                int desiredOffset = obj.ExportResource.SerialOffset;
                int desiredSize = obj.ExportResource.SerialSize;

                // Re-write in bulk, no modifications are persisted.
                byte[] buffer = obj.CopyBuffer();

                bool hasBeenCutdown = desiredSize < buffer.Length;
                int newLength = hasBeenCutdown ? desiredSize : buffer.Length;

                if (hasBeenCutdown && CanSerializeObject(obj) == false)
                    throw new NotSupportedException("Cannot cutdown objects of an unknown type.");

                stream.Seek(desiredOffset, SeekOrigin.Begin);
                stream.Write(buffer, 0, newLength);

                if (hasBeenCutdown)
                {
                    int skipLength = buffer.Length - desiredSize;
                    stream.Write(new byte[skipLength], 0, skipLength);
                }

                Contract.Assert(desiredSize == (int)(stream.Position - desiredOffset));

                if (stream.Position > longestPosition)
                {
                    longestPosition = stream.Position;
                }
            }

            // Objects that must be serialized at the end of the package file.
            // Which are new objects or objects that have increased in size (programmatically)
            var objectsOutOfPlace = exports
                .Where(obj => obj.ExportResource.SerialOffset == 0
                    /*|| obj.ExportTable.SerialSize > obj.DeserializedSize*/);

            // Serialize at the end of the stream.
            stream.Position = longestPosition;

            foreach (var obj in objectsOutOfPlace)
            {
                if (IsSerializableObject(obj) == false)
                {
                    throw new NotSupportedException("Cannot move objects of an unknown type.");
                }

                // Re-write in bulk, no modifications are persisted.
                byte[] buffer = obj.CopyBuffer();

                long newSerializedOffset = stream.Position;
                stream.Write(buffer, 0, buffer.Length);
                long newSerializedSize = stream.Position - newSerializedOffset;

                obj.ExportResource.SerialOffset = (int)newSerializedOffset;
                obj.ExportResource.SerialSize = (int)newSerializedSize;
            }

            // TODO update generations

            // Serialize the exports again to write the new SerialOffset.
            stream.Position = 0;
            // TODO: Serialize the export table directly?
            sourcePackage.Serialize(baseStream);
            Contract.Assert(sourcePackage.Summary.HeaderSize == newHeaderSize);

            return stream;
        }

        private bool IsExportableObject(UObject obj)
        {
            return (int)obj > 0;
        }

        private bool IsSerializableObject(UObject obj)
        {
            // We cannot re-serialize objects of an unknown type
            return obj is not UnknownObject;
        }

        private bool CanSerializeObject(UObject obj)
        {
            return false;
            //return obj.DeserializationState == UObject.ObjectState.Deserialized;
        }

        internal class UnrealSaveStream(UnrealPackageSaver saver, UnrealPackageArchive archive, IUnrealStream baseStream) : IUnrealStream
        {
            public UnrealPackage Package => archive.Package;

            UnrealBinaryReader IUnrealStream.UR => Reader;
            UnrealBinaryWriter IUnrealStream.UW => Writer;

            private UnrealBinaryReader Reader => baseStream.UR;
            private UnrealBinaryWriter Writer => baseStream.UW;

            public readonly Dictionary<int, UName> HashToNameMap = new(archive.Package.Names.Count);

            public readonly Dictionary<UImportTableItem, int> ImportToIndexMap = new(archive.Package.Imports.Count);
            public readonly Dictionary<UExportTableItem, int> ExportToIndexMap = new(archive.Package.Exports.Count);

            [Obsolete]
            public IBufferDecoder? Decoder
            {
                get => archive.Decoder;
                set => archive.Decoder = value;
            }

            public IPackageSerializer Serializer
            {
                get => baseStream.Serializer;
                set => baseStream.Serializer = value;
            }

            public long Position
            {
                get => baseStream.Position;
                set => baseStream.Position = value;
            }

            public long Length
            {
                get => baseStream.Length;
            }

            public long AbsolutePosition
            {
                get => baseStream.AbsolutePosition;
                set => baseStream.AbsolutePosition = value;
            }

            public T? ReadObject<T>() where T : UObject
            {
                throw new NotImplementedException();
            }

            public void WriteObject<T>(T? value) where T : UObject
            {
                int index;

                switch ((int)value)
                {
                    case > 0:
                        if (ExportToIndexMap.TryGetValue(value!.ExportResource!, out index) == false)
                        {
                            index = ExportToIndexMap.Count + 1;
                            ExportToIndexMap.Add(value.ExportResource!, index);
                        }

                        break;

                    case < 0:
                        if (ImportToIndexMap.TryGetValue(value!.ImportResource!, out index) == false)
                        {
                            index = -(ImportToIndexMap.Count + 1);
                            ImportToIndexMap.Add(value.ImportResource!, index);
                        }

                        break;

                    default:
                        index = 0;
                        break;
                }

                Writer.WriteIndex(index);
            }

            public UName ReadName()
            {
                throw new NotImplementedException();
            }

            public void WriteName(in UName value)
            {
                if (HashToNameMap.TryGetValue(value.GetHashCode(), out var name))
                {
                    Writer.WriteName(name);
                }
                else
                {
                    int index = HashToNameMap.Count;
                    // temp copy to work around a limitation.
                    // FIXME: Adapt accordingly when merging UELib 2.0 UName's code.
                    name = new UName(index, value.Number);
                    HashToNameMap.Add(value.GetHashCode(), name);

                    Writer.WriteName(name);
                }
            }

            public void Skip(int bytes)
            {
                baseStream.Skip(bytes);
            }

            public int Read(byte[] buffer, int index, int count)
            {
                return baseStream.Read(buffer, index, count);
            }

            public void Write(byte[] buffer, int index, int count)
            {
                throw new NotImplementedException();
            }

            public long Seek(long offset, SeekOrigin origin)
            {
                return baseStream.Seek(offset, origin);
            }

            public IUnrealStream Record(string name, object? value)
            {
                throw new NotSupportedException("Cannot record data when writing data.");
            }

            public void ConformRecordPosition()
            {
                throw new NotSupportedException("Cannot record data when writing data.");
            }

            public uint Version => baseStream.Version;
            public uint LicenseeVersion => baseStream.LicenseeVersion;
            public uint UE4Version => baseStream.UE4Version;
            public UnrealPackage.GameBuild Build => baseStream.Build;
            public bool BigEndianCode => Flags.HasFlag(UnrealArchiveFlags.BigEndian);
            public UnrealArchiveFlags Flags { get; set; }

            public void Dispose()
            {
                // Don't close the stream, it should be closed by the handler.
                //baseStream.Dispose();
            }
        }
    }
}
