using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UELib.Branch;
using UELib.Flags;
using UELib.IO;
using UELib.ObjectModel.Annotations;
using UELib.Services;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UObject/Core.Object
    /// 
    ///     Instances of this class are deserialized from the exports table entries.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UObject : IUnrealSerializableClass, IAcceptable, IContainsTable, IBinaryData, IDisposable,
                                   IComparable
    {
        internal InternalClassFlags InternalFlags { get; set; } = InternalClassFlags.LazyLoad;

        /// <summary>
        ///     The name for this object.
        /// </summary>
        [Output(OutputSlot.Parameter)]
        public UName Name { get; set; }

        /// <summary>
        ///     The object flags for this object.
        /// </summary>
        public UnrealFlags<ObjectFlag> ObjectFlags { get; set; }

        /// <summary>
        ///     The package for this object.
        /// </summary>
        public UnrealPackage Package { get; internal set; }

        /// <summary>
        ///     The object resource index in the <see cref="Package"/> to this object.
        /// </summary>
        public UPackageIndex PackageIndex { get; internal set; }

        /// <summary>
        ///     The object resource for this object in the <see cref="Package"/>.
        /// </summary>
        public UObjectTableItem? PackageResource { get; internal set; }

        /// <summary>
        ///     The object export resource for this object in the <see cref="Package"/>, if any.
        /// </summary>
        public UExportTableItem? ExportResource => PackageResource as UExportTableItem;

        /// <summary>
        ///     The object import resource for this object in the <see cref="Package"/>, if any.
        /// </summary>
        public UImportTableItem? ImportResource => PackageResource as UImportTableItem;

        /// <summary>
        ///     The class for this object, as represented internally in UnrealScript.
        ///
        ///     Null for imports and UClass objects.
        /// </summary>
        [Output(OutputSlot.Parameter)]
        public UClass Class { get; set; }

        /// <summary>
        ///     The outer for this object, if any.
        ///
        ///     e.g. <example>`Core.Object.Vector.X` where `Vector` is the outer of property `X`</example>
        ///
        ///     Null for imports with no outer.
        /// </summary>
        public UObject? Outer { get; set; }

        /// <summary>
        ///     The archetype for this object, if any.
        /// </summary>
        public UObject? Archetype { get; set; }

        /// <summary>
        ///     A reference to the default object for this object.
        ///     Usually refers to itself, but may in case of a <see cref="UClass"/> refer to a standalone default object.
        /// </summary>
        public UObject? Default { get; protected set; }

        /// <summary>
        ///     Collection of tagged properties for this object.
        ///
        ///     Tagged properties hold default values for properties of this object's class.
        /// </summary>
        public DefaultPropertiesCollection Properties { get; protected set; } = [];

        /// <summary>
        ///     The serialized applied script properties for this object, if any.
        /// </summary>
        private DefaultPropertiesCollection? ObjectProperties { get; set; }

        #region Serialized Members

        /// <summary>
        ///     The networking index for this object.
        /// 
        ///     Serialized if version is lower than <see cref="PackageObjectLegacyVersion.NetObjectCountAdded"/> or UE4Version is equal or greater than 196
        /// </summary>
        [StreamRecord]
        public int NetIndex { get; set; } = -1;

        /// <summary>
        ///     The state frame for this object.
        /// 
        ///     Serialized if the object is marked with <see cref="ObjectFlag.HasStack" />.
        /// </summary>
        [StreamRecord]
        public UStateFrame? StateFrame
        {
            get => _StateFrame;
            set
            {
                _StateFrame = value;

                ObjectFlags = value != null
                    ? new UnrealFlags<ObjectFlag>(ObjectFlags | ObjectFlags.GetFlag(ObjectFlag.HasStack),
                                                  ObjectFlags.FlagsMap)
                    : new UnrealFlags<ObjectFlag>(ObjectFlags & ~ObjectFlags.GetFlag(ObjectFlag.HasStack),
                                                  ObjectFlags.FlagsMap);
            }
        }

        private UStateFrame? _StateFrame;

        /// <summary>
        ///     The unique identifier for this object.
        /// </summary>
        [StreamRecord, BuildGeneration(BuildGeneration.UE4)]
        public UGuid ObjectGuid { get; set; }

        /// <summary>
        ///     Additional data for this object that is non-standard.
        /// </summary>
        public UObjectLicenseeAttachment? LicenseeAttachment { get; set; }

        #endregion

        public UObject() { }

        public UObject(UnrealPackage package, UPackageIndex packageIndex) : this()
        {
            Package = package;
            PackageIndex = packageIndex;

            if (packageIndex)
            {
                PackageResource = package.IndexToObjectResource(packageIndex);
            }
        }

        /// <summary>
        /// Object will not be deserialized by UnrealPackage.InitializePackage and must be explicitly requested by calling Load().
        /// </summary>
        [Obsolete("Pending deprecation")]
        public bool ShouldDeserializeOnDemand { get; protected set; }

        [Obsolete("Pending deprecation; instead load the object using a UObjectRecordStream.")]
        public BinaryMetaData BinaryMetaData { get; private set; }

        public IUnrealStream? Buffer => _Buffer;

        [Obsolete("Use the stream passed to Deserialize(IUnrealStream) instead.")]
        protected IUnrealStream? _Buffer;

        [Flags]
        public enum ObjectState : byte
        {
            Deserialized = 0x01,
            Errorlized = 0x02,
            Deserializing = 0x04,

            [Obsolete("Use Deserialized")]
            Deserialied = Deserialized,
        }

        public ObjectState DeserializationState { get; set; }
        public Exception? ThrownException { get; set; }
        public long ExceptionPosition { get; private set; }

        [Obsolete("Use Load<UObjectRecordStream>() instead.")]
        public void BeginDeserializing()
        {
            Load<UObjectRecordStream>();
        }

        /// <summary>
        /// Loads the object data from the object's associated package.
        ///
        /// The stream may be preserved if the object contains a script or script properties.
        /// That stream is accessible using <see cref="Buffer"/> and must be disposed of manually.
        /// </summary>
        public void Load()
        {
            Load<UObjectStream>();
        }

        /// <summary>
        /// Loads the object data from the object's package stream using a particular object stream.
        ///
        /// The stream may be preserved if the object contains a script or script properties.
        /// That stream is accessible using <see cref="Buffer"/> and must be disposed of manually.
        /// </summary>
        public void Load<T>()
            where T : UObjectStream
        {
            // non-export objects cannot be deserialized!
            if ((int)this <= 0)
            {
                LibServices.Debug("Attempted to load non-export {0}", this);

                return;
            }

            // e.g. None.
            if (ExportResource == null || ExportResource.SerialSize == 0)
            {
                DeserializationState |= ObjectState.Deserialized;

                return;
            }

            if (DeserializationState.HasFlag(ObjectState.Deserializing))
            {
                LibServices.LogService.SilentException(new DeserializationException("The object is already being deserialized."));

                return;
            }

            if (DeserializationState.HasFlag(ObjectState.Deserialized))
            {
                LibServices.Trace("Re-loading {0}", this);
            }

            LibServices.Trace("Loading {0}", this);

            try
            {
                // Load the parent first, if it exists.
                // We need this to properly link up tagged properties.
                if (this is UStruct && Package.Linker.IndexToObject<UObject?>(ExportResource.SuperIndex) != null)
                {
                    LibServices.Trace("Loaded super struct {0}", ExportResource.Super);
                }
            }
            catch (DeserializationException exception)
            {
                LibServices.LogService.SilentException(new DeserializationException("Couldn't deserialize dependencies", exception));
            }

            var stream = LoadStream<T>(Package.Stream);
#if BINARYMETADATA
            if (stream is UObjectRecordStream recordStream)
            {
                BinaryMetaData = recordStream.BinaryMetaData;
            }
#endif
            try
            {
                DeserializationState |= ObjectState.Deserializing;
                Load(stream);
                DeserializationState |= ObjectState.Deserialized;

                LibServices.LogService.SilentAssert(stream.Position == stream.Length,
                                                    $"Trailing data for object {this}");
            }
            catch (DeserializationException
                   exception) // Only catch a deserialization error, not a stream loading error etc.
            {
                DeserializationState |= ObjectState.Errorlized;

                ThrownException = exception;
                ExceptionPosition = stream.Position;

                LibServices.LogService.SilentException(ThrownException);
            }
            finally
            {
                DeserializationState &= ~ObjectState.Deserializing;

                // Dispose the buffer if it is not needed anymore.
                // This is usually the case for objects that do not have script properties or script code.
                if (CanDisposeBuffer())
                {
                    stream.Dispose();
                }
                else
                {
                    if (_Buffer != stream)
                    {
                        // If we already have one, ensure it's disposed of.
                        _Buffer?.Dispose();

                        // Preserve the buffer for backward compatibility with old deserialization and decompiling code.
                        // e.g. UDefaultProperty is still using a reference to the buffer.
                        _Buffer = stream;
                    }
                }
            }

            LibServices.Trace("Loaded {0}", this);
        }

        /// <summary>
        /// Loads the object data from a stream.
        /// </summary>
        /// <param name="stream">the input stream.</param>
        /// <exception cref="DeserializationException">Thrown if any exception occurs during the deserialization process.</exception>
        public void Load(IUnrealStream stream)
        {
            try
            {
                if (ObjectFlags.HasFlag(ObjectFlag.ClassDefaultObject))
                {
                    DeserializeClassDefault(stream);
                }
                else
                {
                    Deserialize(stream);

                    // Legacy support
                    if (_Buffer != null)
                    {
                        Deserialize();
                    }
                }
            }
            catch (Exception exception)
            {
                throw new DeserializationException(
                    $"Couldn't load object {this} as type {GetType()} due thrown exception {exception}",
                    exception
                );
            }
        }

        /// <summary>
        /// Saves the object data to any <see cref="IUnrealStream"/>.
        /// </summary>
        /// <param name="stream">the output stream.</param>
        public void Save(IUnrealStream stream)
        {
            try
            {
                if (ObjectFlags.HasFlag(ObjectFlag.ClassDefaultObject))
                {
                    SerializeClassDefault(stream);
                }
                else
                {
                    Serialize(stream);
                }
            }
            catch (Exception exception)
            {
                throw new SerializationException(
                    $"Couldn't save object {this} as type {GetType()} due thrown exception {exception}",
                    exception
                );
            }
        }

        /// <summary>
        /// Loads the object data from the package stream.
        /// </summary>
        /// <param name="packageStream">the input package stream.</param>
        /// <returns>the loaded object stream.</returns>
        public T Load<T>(UnrealPackageStream packageStream)
            where T : UObjectStream
        {
            var objectStream = LoadStream<T>(packageStream);
            Load(objectStream);

            return objectStream;
        }

        public T LoadStream<T>(UnrealPackageStream stream)
            where T : UObjectStream
        {
            long objectOffset = ExportResource.SerialOffset;
            int objectSize = ExportResource.SerialSize;

            return LoadStream<T>(stream, objectOffset, objectSize);
        }

        public T LoadStream<T>(UnrealPackageStream stream, long objectOffset, int objectSize)
            where T : UObjectStream
        {
            byte[] buffer = new byte[objectSize];
            stream.Seek(objectOffset, SeekOrigin.Begin);
            int byteCount = stream.Read(buffer, 0, objectSize);
            Stream baseStream = new MemoryStream(buffer, 0, objectSize, false, true);
            var packageArchive = stream.Package.Archive;
            // Make an object stream with a decoder as the base stream.
            if (stream.IsEncoded())
            {
                Contract.Assert(packageArchive.Decoder != null);
                baseStream = new EncodedStream(baseStream, packageArchive.Decoder, objectOffset);
            }

            var objectStream = (T)Activator.CreateInstance(typeof(T), packageArchive, baseStream, objectOffset);

            Contract.Assert(byteCount == objectSize,
                            $"Incomplete read; expected a total bytes of {objectSize} but got {byteCount}");

            return objectStream;
        }

        [Obsolete("Pending deprecation")]
        internal IUnrealStream LoadBuffer()
        {
            return _Buffer ??= LoadStream<UObjectStream>(Package.Stream);
        }

        [Obsolete("Pending deprecation")]
        internal void MaybeDisposeBuffer()
        {
            // Do not dispose while deserializing!
            // For example DecompileDefaultProperties or DecompileScript, may dispose the buffer in certain situations!
            if (_Buffer == null || (DeserializationState & ObjectState.Deserializing) != 0)
                return;

            _Buffer.Dispose();
            _Buffer = null;
        }

        [Obsolete("Pending deprecation")]
        protected virtual bool CanDisposeBuffer()
        {
            return Properties.Count == 0;
        }
#if VENGEANCE
        // FIXME: Incomplete
        // Some classes like Core.Object read A as 0x06, but I can't make any sense of the data that comes after it.
        // Also the data of classes like ShockGame.Item, and ShockGame.Holdable etc do not seem to contain familiar data.
        protected void VengeanceDeserializeHeader(IUnrealStream stream, ref (int a, int b) header)
        {
            header.a = stream.ReadInt32();
            stream.Record("A:Vengeance", header.a);
            header.b = stream.ReadInt32();
            stream.Record("B:Vengeance", header.b);
            switch (header.a)
            {
                case 2:
                    header.a = stream.ReadInt32();
                    stream.Record("C:Vengeance", header.a);
                    break;

                case 3:
                    int c = stream.ReadInt32();
                    stream.Record("C:Vengeance", c);
                    break;
            }
        }
#endif
        private void DeserializeNetIndex(IUnrealStream stream)
        {
            if (stream.Version < (uint)PackageObjectLegacyVersion.NetObjectCountAdded ||
                stream.UE4Version >= 196)
            {
                return;
            }
#if MKKE || BATMAN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.MKKE ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                return;
            }
#endif
            NetIndex = stream.ReadInt32();
            stream.Record(nameof(NetIndex), NetIndex);
        }

        private void SerializeNetIndex(IUnrealStream stream)
        {
            if (stream.Version < (uint)PackageObjectLegacyVersion.NetObjectCountAdded ||
                stream.UE4Version >= 196)
            {
                return;
            }
#if MKKE || BATMAN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.MKKE ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                return;
            }
#endif
            stream.Write(NetIndex);
        }

        /// <summary>
        /// Special route for objects that are acting as the ClassDefault for a class
        /// i.e. a class like PrimitiveComponent is accompanied by an instance DEFAULT_PrimitiveComponent of the same class.
        /// </summary>
        private void DeserializeClassDefault(IUnrealStream stream)
        {
            DeserializeNetIndex(stream);
            DeserializeProperties(stream);
        }

        private void SerializeClassDefault(IUnrealStream stream)
        {
            SerializeNetIndex(stream);
            SerializeProperties(stream);
        }

        /// <summary>
        /// Checks if the object is a template.
        /// An object is considered a template if either it is an object containing the defaults of a <seealso cref="UClass"/> or an archetype.
        /// </summary>
        /// <param name="templateFlags"></param>
        /// <returns>true if the object is a template.</returns>
        public bool IsTemplate(ObjectFlag templateFlag = ObjectFlag.TemplateObject)
        {
            return ObjectFlags.HasFlag(templateFlag) ||
                   EnumerateOuter().Any(obj => obj.ObjectFlags.HasFlag(templateFlag));
        }

        public virtual void Deserialize(IUnrealStream stream)
        {
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance)
            {
                if (stream.LicenseeVersion >= 25)
                {
                    var header = (3, 0);
                    VengeanceDeserializeHeader(stream, ref header);
                    if (header.Item2 == 2)
                    {
                        stream.ReadInt32();
                        stream.ConformRecordPosition();
                    }
                }
            }
#endif
            // This appears to be serialized for templates of classes like AmbientSoundNonLoop
            if (ObjectFlags.HasFlag(ObjectFlag.HasStack))
            {
                stream.ReadClass(out _StateFrame);
                stream.Record(nameof(StateFrame), StateFrame);
            }

            // No version check found in the GoW PC client
            if (stream.Version >= (uint)PackageObjectLegacyVersion.TemplateDataAddedToUComponent)
            {
                switch (this)
                {
                    case UComponent component:
                        component.DeserializeTemplate(stream);
#if BATMAN
                        if (stream.Build == UnrealPackage.GameBuild.BuildName.Batman2)
                        {
                            goto skipNetIndex;
                        }
#endif
                        break;
                }
            }

            DeserializeNetIndex(stream);
        skipNetIndex:
#if THIEF_DS || DEUSEX_IW
            // FIXME: Not present in all objects, even some classes?
            if (stream.Build == BuildGeneration.Flesh && GetType() != typeof(UnknownObject))
            {
                // var native private const int ObjectInternalPropertyHash[1];
                int thiefLinkDataObjectCount = stream.ReadInt32();
                stream.Record(nameof(thiefLinkDataObjectCount), thiefLinkDataObjectCount);
                for (int i = 0; i < thiefLinkDataObjectCount; i++)
                {
                    // These probably contain the missing UFields.
                    var thiefLinkDataObject = stream.ReadObject();
                    stream.Record(nameof(thiefLinkDataObject), thiefLinkDataObject);
                }

                if (ExportResource.ClassIndex)
                {
                    stream.Skip(4);
                    stream.ConformRecordPosition();
                }
            }
#endif
            if (ExportResource.ClassIndex.IsNull)
            {
                return;
            }

            DeserializeProperties(stream);
#if UE4
            if (stream.IsUE4() && !ObjectFlags.HasFlag(ObjectFlag.ClassDefaultObject))
            {
                stream.Read(out bool shouldSerializeGuid);
                stream.Record(nameof(shouldSerializeGuid), shouldSerializeGuid);

                if (shouldSerializeGuid)
                {
                    ObjectGuid = stream.ReadStruct<UGuid>();
                    stream.Record(nameof(ObjectGuid), ObjectGuid);
                }
            }
#endif
        }

        public virtual void Serialize(IUnrealStream stream)
        {
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance)
            {
                if (stream.LicenseeVersion >= 25)
                {
                    throw new NotSupportedException("Cannot serialize object headers for Vengeance");
                }
            }
#endif
            // This appears to be serialized for templates of classes like AmbientSoundNonLoop
            if (ObjectFlags.HasFlag(ObjectFlag.HasStack))
            {
                Contract.Assert(StateFrame != null, "Missing StateFrame for object with HasStack");
                stream.WriteClass(StateFrame);
            }
#if MKKE || BATMAN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.MKKE ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                goto skipNetIndex;
            }
#endif
            // No version check found in the GoW PC client
            if (stream.Version >= (uint)PackageObjectLegacyVersion.TemplateDataAddedToUComponent)
            {
                switch (this)
                {
                    case UComponent component:
                        component.SerializeTemplate(stream);
                        break;
                }
            }

        skipNetIndex:

            SerializeNetIndex(stream);
#if THIEF_DS || DEUSEX_IW
            if (stream.Build == BuildGeneration.Flesh)
            {
                throw new NotSupportedException("Cannot serialize LinkedData objects for Flesh");
            }
#endif
            if (ExportResource.ClassIndex.IsNull)
            {
                return;
            }

            SerializeProperties(stream);
#if UE4
            if (stream.IsUE4())
            {
                bool shouldSerializeGuid = (Guid)ObjectGuid != Guid.Empty;
                stream.Write(shouldSerializeGuid ? 1 : 0);

                if (shouldSerializeGuid)
                {
                    stream.WriteStruct(ObjectGuid);
                }
            }
#endif
        }

        [Obsolete("Use Overload")]
        protected void DeserializeProperties()
        {
            Debug.Assert(_Buffer != null);
            DeserializeProperties(_Buffer);
        }

        private void DeserializeProperties(IUnrealStream stream)
        {
            var propertySource = Class ?? (UStruct)this;
            DeserializeProperties(stream, propertySource);
        }

        private void DeserializeProperties(IUnrealStream stream, UStruct propertySource)
        {
            Default = this;
            ObjectProperties = propertySource.DeserializeScriptProperties(stream, this);
            Properties = ObjectProperties;
        }

        private void SerializeProperties(IUnrealStream stream)
        {
            var propertySource = Class ?? (UStruct)this;
            SerializeProperties(stream, propertySource);
        }

        private void SerializeProperties(IUnrealStream stream, UStruct propertySource)
        {
            propertySource.SerializeScriptProperties(stream, this, ObjectProperties);
            Properties = ObjectProperties;
        }

        [Obsolete]
        public bool HasObjectFlag(ObjectFlagsLO flag)
        {
            return (ObjectFlags & (ulong)flag) != 0;
        }

        [Obsolete]
        public bool HasObjectFlag(ObjectFlagsHO flag)
        {
            return (ObjectFlags & ((ulong)flag << 32)) != 0;
        }

        internal bool HasObjectFlag(ObjectFlag flagIndex)
        {
            return ObjectFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(ObjectFlag)], flagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnyObjectFlags(ulong flag) => (ObjectFlags & flag) != 0;

        public bool IsPublic()
        {
            return ObjectFlags.HasFlag(ObjectFlag.Public);
        }

        public bool IsProtected()
        {
            return ObjectFlags.HasFlag(ObjectFlag.Protected);
        }

        public bool IsPrivate()
        {
            return ObjectFlags.HasFlag(ObjectFlag.Final) || !ObjectFlags.HasFlag(ObjectFlag.Public);
        }

        /// <summary>
        /// Gets a human-readable name of this object instance.
        /// </summary>
        /// <returns>The human-readable name of this object instance.</returns>
        public virtual string GetFriendlyType()
        {
            return Name;
        }

        public IEnumerable<UObject> EnumerateOuter()
        {
            for (var outer = Outer; outer != null; outer = outer.Outer)
            {
                yield return outer;
            }
        }

        public T OuterMost<T>() where T : UObject
        {
            for (var outer = Outer; outer != null; outer = outer.Outer)
            {
                if (outer is T result)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds a full path string of the object
        /// </summary>
        /// <returns>Full path of object e.g. "Core.Object.Vector"</returns>
        public string GetPath()
        {
            string group = EnumerateOuter().Aggregate(string.Empty, (current, outer) => $"{outer.Name}.{current}");
            return $"{group}{Name}";
        }

        public void GetPath(out IList<UObject> chain)
        {
            chain = new List<UObject>(3) { this };
            foreach (var outer in EnumerateOuter())
            {
                chain.Add(outer);
            }
        }

        /// <summary>
        /// Builds a full path string of the object and its class.
        /// </summary>
        /// <returns>Full path of object e.g. "Struct'Core.Object.Vector'"</returns>
        public string GetReferencePath()
        {
            Debug.Assert(Class != null, "Class cannot be null");
            return $"{Class.Name}'{GetPath()}'";
        }

        public bool InheritsStaticClass(UClass superClass)
        {
#if NET5_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(superClass, nameof(superClass));
#endif
            Debug.Assert(Class != null);
            for (var @class = Class; @class != null; @class = (UClass)@class.Super)
            {
                if (ReferenceEquals(@class, superClass))
                {
                    return true;
                }
            }

            return false;
        }

        #region IBuffered

        public virtual byte[] CopyBuffer()
        {
            var stream = GetBuffer();
            if (stream == null)
                return null;

            int offset = GetBufferPosition();
            if (offset == -1)
                return null;

            int size = GetBufferSize();
            if (size == 0)
                return null;

            var bytes = new byte[size];
            long prePosition = stream.Position;
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Read(bytes, 0, size);
            stream.Position = prePosition;

            return bytes;
        }

        public IUnrealStream GetBuffer()
        {
            return Package.Stream;
        }

        public int GetBufferPosition()
        {
            return ExportResource?.SerialOffset ?? -1;
        }

        public int GetBufferSize()
        {
            return ExportResource?.SerialSize ?? 0;
        }

        public string GetBufferId(bool fullName = false)
        {
            return fullName
                ? $"{Package.PackageName}.{GetPath()}.{GetClassName()}"
                : $"{GetPath()}.{GetClassName()}";
        }

        #endregion

        public int CompareTo(object obj)
        {
            return (int)this - (int)(UObject)obj;
        }

        public override string ToString()
        {
            return $"{GetReferencePath()}({(int)this})";
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return Name.GetHashCode();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _Buffer?.Dispose();
            _Buffer = null;
        }

        ~UObject()
        {
            Dispose(true);
        }

        public virtual void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }

        public virtual TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }

#if NETCOREAPP2_1_OR_GREATER
        [param: NotNullWhen(true)]
#endif
        public static implicit operator UPackageIndex(UObject? obj)
        {
            return obj?.PackageIndex ?? UPackageIndex.Null;
        }

        public static explicit operator int(UObject? obj)
        {
            return obj?.PackageIndex ?? UPackageIndex.Null;
        }

        public static explicit operator string(UObject? obj)
        {
            return obj?.Name ?? "none";
        }

        [Obsolete("Use PackageResource instead")]
        public UObjectTableItem? Table => PackageResource;

        [Obsolete("Use ExportResource instead")]
        public UExportTableItem? ExportTable => ExportResource;

        [Obsolete("Use ImportResource instead")]
        public UImportTableItem? ImportTable => ImportResource;

        [Obsolete("Use Name instead")]
        public UNameTableItem? NameTable => PackageResource.ObjectTable;

        [Obsolete("Use Deserialize(IUnrealStream) instead.")]
        protected virtual void Deserialize()
        {
            Contract.Assert(_Buffer != null, "Cannot proceed to Deserialize without a buffer.");
        }

        [Obsolete("Use Class?.Name ?? \"Class\"")]
        public string GetClassName()
        {
            return Class.Name;
        }

        [Obsolete("Use Class?.Name ?? \"Class\"")]
        public bool IsClassType(string className)
        {
            return string.Compare(Class.Name, className, StringComparison.OrdinalIgnoreCase) == 0;
        }

        [Obsolete("Use Package.IndexToObject", true)]
        protected UObject GetIndexObject(int index)
        {
            return Package.Linker.IndexToObject<UObject>(index);
        }

        [Obsolete("Use stream.Record instead")]
        internal void Record(string varName, object varObject = null)
        {
            _Buffer.Record(varName, varObject);
        }

        [Obsolete("Use Outer?.Name")]
        public string GetOuterName()
        {
            return Outer?.Name;
        }

        [Obsolete("Pending deprecation")]
        public virtual void PostInitialize()
        {
        }

        [Obsolete("Deprecated", true)]
        public virtual void InitializeImports()
        {
            throw new NotImplementedException();
        }

        [Obsolete("Deprecated", true)]
        public bool ResistsInGroup()
        {
            throw new NotImplementedException();
        }

        [Obsolete("Deprecated", true)]
        public UObject GetHighestOuter(byte offset = 0)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Use GetPath instead")]
        public string GetOuterGroup() => GetPath();

        [Obsolete("Deprecated", true)]
        public bool IsClass(string className)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Deprecated", true)]
        public bool IsMember(UField membersClass)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Unknown Object
    ///
    /// Notes:
    ///     Instances of this class are created because of a class type that was not found within the 'RegisteredClasses' list.
    ///     Instances of this class will only be deserialized on demand.
    /// </summary>
    public sealed class UnknownObject : UObject
    {
        public UnknownObject()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override bool CanDisposeBuffer()
        {
            return false;
        }
    }

    public abstract class ObjectLicenseeAttachment
    {
        private Dictionary<string, object?> _Properties { get; } = new();

        public void SetProperty(string name, object? value)
        {
            _Properties[name] = value;
        }

        public object? GetProperty(string name)
        {
            return _Properties[name];
        }
    }

    public sealed class UObjectLicenseeAttachment : ObjectLicenseeAttachment;
}
