using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace UELib
{
    /// <summary>
    /// This class can be decompiled.
    /// </summary>
    public interface IUnrealDecompilable
    {
        /// <summary>
        /// Decompile this object.
        /// </summary>
        /// <returns>The decompiled ouput of text.</returns>
        string Decompile();
    }

    /// <summary>
    /// This class has a reference to an object and are both decompilable.
    /// </summary>
    public interface IDecompilableObject : IUnrealDecompilable
    {
        /// <summary>
        /// The decompileable object that will be decompiled when this object's Decompile() function is called.
        /// </summary>
        IUnrealDecompilable Object { get; }
    }

    /// <summary>
    /// This class has a stream reference.
    /// </summary>
    public interface IBuffered
    {
        /// <summary>
        /// Returns a copy of the buffer.
        /// </summary>
        /// <returns>The copied buffer.</returns>
        byte[] CopyBuffer();

        [Pure]
        IUnrealStream GetBuffer();

        [Pure]
        int GetBufferPosition();

        [Pure]
        int GetBufferSize();

        [Pure]
        string GetBufferId(bool fullName = false);
    }

    /// <summary>
    /// This class contains binary meta data.
    /// </summary>
    public interface IBinaryData : IBuffered
    {
        BinaryMetaData BinaryMetaData { get; }
    }

    public interface IContainsTable
    {
        UObjectTableItem Table { get; }
    }

    /// <summary>
    /// This class represents viewable information.
    /// </summary>
    public interface IUnrealViewable
    {
    }

    /// <summary>
    /// This class can be deserialized from a specified stream.
    /// </summary>
    public interface IUnrealDeserializableClass
    {
        void Deserialize(IUnrealStream stream);
    }

    public interface IUnrealSerializableClass : IUnrealDeserializableClass
    {
        void Serialize(IUnrealStream stream);
    }

    /// <summary>
    /// An atomic struct (e.g. UObject.Color, Vector, etc).
    /// See <see cref="UnrealStreamImplementations.ReadAtomicStruct"/>
    /// </summary>
    public interface IUnrealAtomicStruct
    {
    }

    /// <summary>
    /// This class is exportable into an non-unreal format
    /// </summary>
    public interface IUnrealExportable
    {
        IEnumerable<string> ExportableExtensions { get; }

        bool CompatableExport();
        void SerializeExport(string desiredExportExtension, Stream exportStream);
    }

    /// <summary>
    /// This class is replicable.
    /// </summary>
    public interface IUnrealNetObject
    {
        string Name { get; }
        ushort RepOffset { get; }
        bool RepReliable { get; }
        uint RepKey { get; }
    }
}