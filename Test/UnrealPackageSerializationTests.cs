using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Eliot.UELib.Test.upk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;

namespace Eliot.UELib.Test;

[TestClass]
public class UnrealPackageSerializationTests
{
    [DataTestMethod]
    [DataRow(PackageObjectLegacyVersion.Release64 - 1)]
    [DataRow(PackageObjectLegacyVersion.Release64)]
    [DataRow(PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)]
    [DataRow(PackageObjectLegacyVersion.HighestVersion)]
    public void TestPackageNamesSerialization(PackageObjectLegacyVersion version)
    {
        using var sourcePackage = UE3PackageContentTests.GetScriptPackageLinker();
        Assert.IsNotNull(sourcePackage);

        using var stream = UnrealPackageUtilities.CreateTempPackageStream();
        using var linker = new UnrealPackage(stream);
        linker.Build = new UnrealPackage.GameBuild(linker);
        linker.Summary = new UnrealPackage.PackageFileSummary
        {
            Version = (uint)version
        };

        // write the script packages content to the temp stream

        linker.Names.AddRange(sourcePackage.Names);

        var sizes = new long[sourcePackage.Names.Count];

        stream.Position = 0;
        sourcePackage.Names.ForEach(name =>
        {
            long p = stream.Position;
            name.Serialize(stream);
            sizes[name.Index] = stream.Position - p;
        });
        long endPosition = stream.Position;
        stream.Position = 0;
        sourcePackage.Names.ForEach(name =>
        {
            long p = stream.Position;
            name.Deserialize(stream);
            Assert.AreEqual(sizes[name.Index], stream.Position - p);
        });
        Assert.AreEqual(endPosition, stream.Position);
    }

    [DataTestMethod]
    // No direct changes, only the serialization of UName and UIndex have changed but this is not the test for that.
    [DataRow(PackageObjectLegacyVersion.UE3)]
    [DataRow(PackageObjectLegacyVersion.HighestVersion)]
    public void TestPackageImportsSerialization(PackageObjectLegacyVersion version)
    {
        using var sourcePackage = UE3PackageContentTests.GetScriptPackageLinker();
        Assert.IsNotNull(sourcePackage);

        using var stream = UnrealPackageUtilities.CreateTempPackageStream();
        using var linker = new UnrealPackage(stream);
        linker.Build = new UnrealPackage.GameBuild(linker);
        linker.Summary = new UnrealPackage.PackageFileSummary
        {
            Version = (uint)version
        };

        // write the script packages content to the temp stream

        linker.Names.AddRange(sourcePackage.Names);
        linker.Imports.AddRange(sourcePackage.Imports);

        var sizes = new long[sourcePackage.Imports.Count];

        stream.Position = 0;
        sourcePackage.Imports.ForEach(imp =>
        {
            long p = stream.Position;
            imp.Serialize(stream);
            sizes[imp.Index] = stream.Position - p;
        });
        long endPosition = stream.Position;
        stream.Position = 0;
        sourcePackage.Imports.ForEach(imp =>
        {
            long p = stream.Position;
            imp.Deserialize(stream);
            Assert.AreEqual(sizes[imp.Index], stream.Position - p);
        });
        Assert.AreEqual(endPosition, stream.Position);
    }

    [DataTestMethod]
    //[DataRow(PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits - 1)]
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
        using var sourcePackage = UE3PackageContentTests.GetScriptPackageLinker();
        Assert.IsNotNull(sourcePackage);

        using var stream = UnrealPackageUtilities.CreateTempPackageStream();
        using var linker = new UnrealPackage(stream);
        linker.Build = new UnrealPackage.GameBuild(linker);
        linker.Summary = new UnrealPackage.PackageFileSummary
        {
            Version = (uint)version
        };

        // write the script packages content to the temp stream

        linker.Names.AddRange(sourcePackage.Names);
        linker.Exports.AddRange(sourcePackage.Exports);

        var sizes = new long[sourcePackage.Exports.Count];

        stream.Position = 0;
        sourcePackage.Exports.ForEach(exp =>
        {
            long p = stream.Position;
            exp.Serialize(stream);
            sizes[exp.Index] = stream.Position - p;
        });
        long endPosition = stream.Position;
        stream.Position = 0;
        sourcePackage.Exports.ForEach(exp =>
        {
            long p = stream.Position;
            exp.Deserialize(stream);
            Assert.AreEqual(sizes[exp.Index], stream.Position - p);
        });
        Assert.AreEqual(endPosition, stream.Position);
    }

    [DataTestMethod]
    [DataRow(PackageObjectLegacyVersion.AddedAdditionalPackagesToCook)]
    [DataRow(PackageObjectLegacyVersion.AddedThumbnailTable)]
    [DataRow(PackageObjectLegacyVersion.AddedImportExportGuidsTable)]
    [DataRow(PackageObjectLegacyVersion.AddedTextureAllocations)]
    [DataRow(PackageObjectLegacyVersion.HighestVersion)]
    public void TestPackageSerialization(PackageObjectLegacyVersion version)
    {
        using var sourcePackage = UE3PackageContentTests.GetScriptPackageLinker();
        Assert.IsNotNull(sourcePackage);

        // Create a new temporary package to contain a copy of the test package.
        using var tempStream = UnrealPackageUtilities.CreateTempPackageStream();
        using var tempPackage = new UnrealPackage(tempStream);
        tempPackage.Build = new UnrealPackage.GameBuild(tempPackage);
        tempPackage.Summary = new UnrealPackage.PackageFileSummary
        {
            Version = (uint)version
        };

        // Copy the entire package to the temp stream (minus objects)
        tempStream.Position = 0;
        // Version is serialized from the summary instead of the stream version.
        sourcePackage.Summary.Version = tempPackage.Version;
        sourcePackage.Summary.LicenseeVersion = tempPackage.LicenseeVersion;

        // We expect the offset to CHANGE
        // - so we must set them to 0 to indicate that we don't want to serialize the table data at the last position.
        sourcePackage.Summary.HeritageOffset = 0;
        sourcePackage.Summary.NameOffset = 0;
        sourcePackage.Summary.ImportOffset = 0;
        sourcePackage.Summary.ExportOffset = 0;
        sourcePackage.Summary.DependsOffset = 0;
        sourcePackage.Summary.ImportExportGuidsOffset = 0;
        sourcePackage.Summary.ThumbnailTableOffset = 0;

        sourcePackage.Serialize(tempStream);
        long endPosition = tempStream.Position;

        // Now that we know the correct table offsets, re-write it with the correct offsets.
        tempStream.Position = 0;
        sourcePackage.Summary.Serialize(tempStream);

        // re-deserialize from our temp copy to see if it all aligns.
        tempStream.Position = 0;
        tempPackage.Deserialize(tempStream);
        Assert.AreEqual(endPosition, tempStream.Position);
    }

    [TestMethod]
    public void TestPackageSaving()
    {
        using var sourcePackage = UE3PackageContentTests.GetScriptPackageLinker();
        Assert.IsNotNull(sourcePackage);

        // Create a new temporary package to contain a copy of the test package.
        using var tempStream = UnrealPackageUtilities.CreateTempPackageStream();
        using var tempPackage = new UnrealPackage(tempStream);
        tempPackage.Build = new UnrealPackage.GameBuild(tempPackage);
        tempPackage.Summary = new UnrealPackage.PackageFileSummary
        {
            Version = sourcePackage.Version,
            LicenseeVersion = sourcePackage.LicenseeVersion
        };

        sourcePackage.InitializePackage();

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
        using var saveStream = packageSaver.Save(sourcePackage, tempStream);

        tempPackage.Deserialize(tempStream);
        tempPackage.InitializePackage();

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
            var stream = new UnrealSaveStream(this, baseStream);

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

            var exports = sourcePackage.Objects
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
                    obj => obj.ExportTable.SerialOffset > 0 /*&& obj.ExportTable.SerialSize <= obj.DeserializedSize*/);

            long longestPosition = stream.Position;

            foreach (var obj in objectsInPlace)
            {
                int desiredOffset = obj.ExportTable.SerialOffset;
                int desiredSize = obj.ExportTable.SerialSize;

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
                .Where(obj => obj.ExportTable.SerialOffset == 0
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

                obj.ExportTable.SerialOffset = (int)newSerializedOffset;
                obj.ExportTable.SerialSize = (int)newSerializedSize;
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

        internal class UnrealSaveStream(UnrealPackageSaver saver, IUnrealStream baseStream) : IUnrealStream
        {
            public UnrealPackage Package => baseStream.Package;
            public UnrealReader UR => baseStream.UR;
            public UnrealWriter UW => baseStream.UW;

            public readonly Dictionary<int, UName> HashToNameMap = new(baseStream.Package.Names.Count);

            public readonly Dictionary<UImportTableItem, int> ImportToIndexMap = new(baseStream.Package.Imports.Count);
            public readonly Dictionary<UExportTableItem, int> ExportToIndexMap = new(baseStream.Package.Exports.Count);

            public IBufferDecoder Decoder
            {
                get => baseStream.Decoder;
                set => baseStream.Decoder = value;
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

            public long AbsolutePosition
            {
                get => baseStream.AbsolutePosition;
                set => baseStream.AbsolutePosition = value;
            }

            public T ReadObject<T>() where T : UObject
            {
                throw new NotImplementedException();
            }

            public void WriteObject<T>(T value) where T : UObject
            {
                int index;

                switch ((int)value)
                {
                    case > 0:
                        if (ExportToIndexMap.TryGetValue(value.ExportTable, out index) == false)
                        {
                            index = ExportToIndexMap.Count + 1;
                            ExportToIndexMap.Add(value.ExportTable, index);
                        }

                        break;

                    case < 0:
                        if (ImportToIndexMap.TryGetValue(value.ImportTable, out index) == false)
                        {
                            index = -(ImportToIndexMap.Count + 1);
                            ImportToIndexMap.Add(value.ImportTable, index);
                        }

                        break;

                    default:
                        index = 0;
                        break;
                }

                UW.WriteIndex(index);
            }

            public UName ReadName()
            {
                throw new NotImplementedException();
            }

            public void WriteName(in UName value)
            {
                if (HashToNameMap.TryGetValue(value.GetHashCode(), out var name))
                {
                    UW.WriteName(name);
                }
                else
                {
                    int index = HashToNameMap.Count;
                    // temp copy to work around a limitation.
                    // FIXME: Adapt accordingly when merging UELib 2.0 UName's code.
                    name = new UName(index, value.Number);
                    HashToNameMap.Add(value.GetHashCode(), name);

                    UW.WriteName(name);
                }
            }

            public int ReadObjectIndex()
            {
                throw new NotImplementedException();
            }

            public UObject ParseObject(int index)
            {
                throw new NotImplementedException();
            }

            public int ReadNameIndex()
            {
                throw new NotImplementedException();
            }

            public int ReadNameIndex(out int num)
            {
                throw new NotImplementedException();
            }

            public string ParseName(int index)
            {
                throw new NotImplementedException();
            }

            public void Skip(int bytes)
            {
                baseStream.Skip(bytes);
            }

            public int Read(byte[] buffer, int index, int count)
            {
                return baseStream.Read(buffer, index, count);
            }

            public long Seek(long offset, SeekOrigin origin)
            {
                return baseStream.Seek(offset, origin);
            }

            public IUnrealStream Record(string name, object value)
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
            public bool BigEndianCode => baseStream.BigEndianCode;

            public void Dispose()
            {
                // Don't close the stream, it should be closed by the handler.
                //baseStream.Dispose();
            }
        }
    }
}