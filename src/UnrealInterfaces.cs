﻿using System;
using System.Collections.Generic;
using System.IO;
using UELib.Core;

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

#if Forms
    /// <summary>
    /// This class has a reference to an object and are both decompilable.
    /// </summary>
    [Obsolete]
    public interface IDecompilableObject : IUnrealDecompilable
    {
        /// <summary>
        /// The decompileable object that will be decompiled when this object's Decompile() function is called.
        /// </summary>
        IUnrealDecompilable Object { get; }
    }
#endif

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

        IUnrealStream GetBuffer();

        int GetBufferPosition();

        int GetBufferSize();

        string GetBufferId(bool fullName = false);
    }

    /// <summary>
    /// This class contains binary meta data.
    /// </summary>
    public interface IBinaryData : IBuffered
    {
        BinaryMetaData? BinaryMetaData { get; }
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

    public interface IVisitor
    {
        void Visit(IAcceptable visitable);
        void Visit(UStruct.UByteCodeDecompiler.Token token);
    }

    public interface IVisitor<out TResult>
    {
        TResult Visit(IAcceptable visitable);
        TResult Visit(UStruct.UByteCodeDecompiler.Token token);
    }

    public interface IAcceptable
    {
        void Accept(IVisitor visitor);
        TResult Accept<TResult>(IVisitor<TResult> visitor);
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
    /// See <see cref="UnrealStreamImplementations.ReadStructMarshal{T}"/>
    /// </summary>
    public interface IUnrealAtomicStruct
    {
    }

    /// <summary>
    /// This class is capable of exporting data to a non-unreal format.
    /// e.g. <see cref="USound.Data" /> can be serialized to a stream and in turn be flushed to a .wav file.
    /// </summary>
    public interface IUnrealExportable
    {
        IEnumerable<string> ExportableExtensions { get; }

        /// <summary>
        /// Whether this object is exportable, usually called before any deserialization has occurred.
        /// </summary>
        bool CanExport();

        void SerializeExport(string desiredExportExtension, Stream exportStream);
    }

    public static class IUnrealExportableImplementation
    {
        [Obsolete("Use CanExport()")]
        public static bool CompatableExport(this IUnrealExportable exportable)
        {
            return exportable.CanExport();
        }
    }

    /// <summary>
    /// This class is replicable.
    /// </summary>
    public interface IUnrealNetObject
    {
        UName Name { get; set; }
        ushort RepOffset { get; set; }
        bool RepReliable { get; }
        uint RepKey { get; }
    }
}
