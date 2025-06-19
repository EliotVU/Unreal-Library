using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using UELib.Branch;
using UELib.Flags;
using UELib.ObjectModel.Annotations;
using UELib.Services;

namespace UELib.Core
{
    /// <summary>
    /// Represents the Unreal class Core.UObject.
    /// Instances of this class are deserialized from the exports table entries.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UObject : IUnrealSerializableClass, IAcceptable, IContainsTable, IBinaryData, IDisposable, IComparable
    {
        /// <summary>
        /// The object name.
        /// </summary>
        [Output(OutputSlot.Parameter)]
        public UName Name { get; set; }

        /// <summary>
        /// The object flags.
        /// </summary>
        public UnrealFlags<ObjectFlag> ObjectFlags { get; set; }

        /// <summary>
        /// The object package.
        /// </summary>
        public UnrealPackage Package { get; internal set; }

        /// <summary>
        /// The object resource index in the <see cref="Package"/>.
        /// </summary>
        public UPackageIndex PackageIndex { get; internal set; }

        public UObjectTableItem? Table { get; internal set; }

        public UExportTableItem? ExportTable => Table as UExportTableItem;

        public UImportTableItem? ImportTable => Table as UImportTableItem;

        [Obsolete("Pending deprecation")]
        public UNameTableItem? NameTable => Table.ObjectTable;

        /// <summary>
        /// The object class, as represented internally in UnrealScript.
        ///
        /// Null for imports and UClass objects.
        /// </summary>
        [Output(OutputSlot.Parameter)]
        public UClass? Class { get; set; }

        /// <summary>
        /// The object outer.
        ///
        /// e.g. <example>`Core.Object.Vector.X` where `Vector` is the outer of property `X`</example>
        ///
        /// Null for imports with no outer.
        /// </summary>
        public UObject? Outer { get; set; }

        /// <summary>
        /// The object archetype.
        /// </summary>
        public UObject? Archetype => ExportTable != null
            ? Package.IndexToObject(ExportTable.ArchetypeIndex)
            : null;

        [Obsolete("Displaced by PackageIndex")]
        private int _ObjectIndex => PackageIndex;

        #region Serialized Members

        protected UObjectStream _Buffer;

        public UObjectStream? Buffer => _Buffer;

        /// <summary>
        /// Serialized if version is lower than <see cref="PackageObjectLegacyVersion.NetObjectCountAdded"/> or UE4Version is equal or greater than 196
        /// </summary>
        public int NetIndex = -1;

        public UObject? Default { get; protected set; }

        /// <summary>
        /// Object Properties e.g. SubObjects or/and DefaultProperties
        /// </summary>
        public DefaultPropertiesCollection Properties { get; protected set; }

        /// <summary>
        /// Serialized if the object is marked with <see cref="ObjectFlag.HasStack" />.
        /// </summary>
        public UStateFrame? StateFrame;

        [BuildGeneration(BuildGeneration.UE4)]
        public UGuid ObjectGuid;

        #endregion

        [Flags]
        public enum ObjectState : byte
        {
            [Obsolete("Use Deserialized")]
            Deserialied = Deserialized,

            Deserialized = 0x01,
            Errorlized = 0x02,
            Deserializing = 0x04
        }

        public ObjectState DeserializationState;
        public Exception? ThrownException;
        public long ExceptionPosition;

        public UObject()
        {

        }

        public UObject(UnrealPackage package, UPackageIndex packageIndex) : this()
        {
            Package = package;
            PackageIndex = packageIndex;

            if (packageIndex)
            {
                Table = package.IndexToObjectResource(packageIndex);
            }
        }

        /// <summary>
        /// Object will not be deserialized by UnrealPackage, Can only be deserialized by calling the methods yourself.
        /// </summary>
        public bool ShouldDeserializeOnDemand { get; protected set; }

        public BinaryMetaData BinaryMetaData { get; private set; }

        #region Constructors

        [Obsolete("Use Load<UObjectRecordStream>() instead.")]
        public void BeginDeserializing()
        {
            Load<UObjectRecordStream>();
        }

        public void Load()
        {
            Load<UObjectStream>();
        }

        /// <summary>
        /// Loads the object data from the object's package stream using a particular object stream.
        ///
        /// Disposes the buffer after deserialization, unless the object has script properties.
        /// </summary>
        public void Load<T>()
            where T : UObjectStream
        {
            // non-export objects cannot be deserialized!
            if ((int)this <= 0)
            {
                LibServices.Debug("Attempted to load non-export {0}", GetReferencePath());

                return;
            }

            // e.g. None.
            if (ExportTable == null || ExportTable.SerialSize == 0)
            {
                DeserializationState |= ObjectState.Deserialized;

                return;
            }

            if (DeserializationState.HasFlag(ObjectState.Deserializing))
            {
                LibServices.LogService.SilentException(new DeserializationException("The object is already being deserialized."));

                return;
            }

            LibServices.Debug("Loading {0}", GetReferencePath());

            try
            {
                // Load the parent first, if it exists.
                // We need this to properly link up tagged properties.
                if (this is UStruct && Package.IndexToObject(ExportTable.SuperIndex) != null)
                {
                    LibServices.Debug("Loaded super struct {0}", ExportTable.Super);
                }
            }
            catch (DeserializationException exception)
            {
                LibServices.LogService.SilentException(new DeserializationException("Couldn't deserialize dependencies", exception));
            }

            _Buffer?.Close();
            _Buffer = LoadStream<T>(Package.Stream);
#if BINARYMETADATA
            if (_Buffer is UObjectRecordStream recordStream)
            {
                BinaryMetaData = recordStream.BinaryMetaData;
            }
#endif
            try
            {
                DeserializationState |= ObjectState.Deserializing;
                Load(_Buffer);
                DeserializationState |= ObjectState.Deserialized;
            }
            catch (DeserializationException exception) // Only catch a deserialization error, not a stream loading error etc.
            {
                DeserializationState |= ObjectState.Errorlized;

                ThrownException = exception;
                ExceptionPosition = _Buffer.Position;

                LibServices.LogService.SilentException(ThrownException);
            }
            finally
            {

                DeserializationState &= ~ObjectState.Deserializing;
                MaybeDisposeBuffer();
            }

            LibServices.Debug("Loaded {0}", GetReferencePath());
        }

        /// <summary>
        /// Loads the object data using a particular object stream.
        /// </summary>
        /// <param name="stream">the input object stream.</param>
        /// <exception cref="DeserializationException">Thrown if any exception occurs during the deserialization process.</exception>
        public void Load(UObjectStream stream)
        {
            // Necessary to keep the "Deserialize" implementations working.
            var buffer = _Buffer;
            _Buffer = stream;

            try
            {
                if (ObjectFlags.HasFlag(ObjectFlag.ClassDefaultObject))
                {
                    DeserializeClassDefault(stream);
                }
                else
                {
                    Deserialize(stream);
                }

                LibServices.LogService.SilentAssert(_Buffer.Position == _Buffer.Length, $"Trailing data for object {GetReferencePath()}");
            }
            catch (Exception exception)
            {
                throw new DeserializationException(
                    $"Couldn't load object {GetReferencePath()} as type {GetType()} due thrown exception {exception}",
                    exception
                );
            }
            finally
            {
                _Buffer = buffer;
            }
        }

        /// <summary>
        /// Loads the object data from the package stream.
        /// </summary>
        /// <param name="packageStream">the input package stream.</param>
        /// <returns>the loaded object stream.</returns>
        public T Load<T>(UPackageStream packageStream)
            where T : UObjectStream
        {
            var objectStream = LoadStream<T>(packageStream);
            Load(objectStream);

            return objectStream;
        }

        public T LoadStream<T>(UPackageStream stream)
            where T : UObjectStream
        {
            T objectStream;
            long objectOffset = ExportTable.SerialOffset;
            int objectSize = ExportTable.SerialSize;

            byte[] buffer = new byte[objectSize];
            int byteCount;

            // Make an object stream with a decoder as the base stream.
            if (stream.Decoder != null)
            {
                var decoder = stream.Decoder;
                // Read without decoding, because the encryption may be affected by the read count. e.g. "Huxley"
                stream.Decoder = null;
                // Bypass the terrible and slow endian reverse call
                stream.Seek(objectOffset, SeekOrigin.Begin);
                byteCount = stream.EndianAgnosticRead(buffer, 0, objectSize);
                stream.Decoder = decoder;

                var baseStream = new MemoryDecoderStream(stream.Decoder, buffer, objectOffset);
                objectStream = (T)Activator.CreateInstance(typeof(T), stream, baseStream, objectOffset);
            }
            else
            {
                // Bypass the terrible and slow endian reverse call
                stream.Seek(objectOffset, SeekOrigin.Begin);
                byteCount = stream.EndianAgnosticRead(buffer, 0, objectSize);

                objectStream = (T)Activator.CreateInstance(typeof(T), stream, buffer, objectOffset);
            }

            Contract.Assert(byteCount == objectSize,
                $"Incomplete read; expected a total bytes of {objectSize} but got {byteCount}");

            return objectStream;
        }

        internal UObjectStream LoadBuffer()
        {
            return _Buffer ??= LoadStream<UObjectStream>(Package.Stream);
        }

        internal void MaybeDisposeBuffer()
        {
            // Do not dispose while deserializing!
            // For example DecompileDefaultProperties or DecompileScript, may dispose the buffer in certain situations!
            if (_Buffer == null || (DeserializationState & ObjectState.Deserializing) != 0)
                return;

            _Buffer.Dispose();
            _Buffer = null;
        }

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
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.MKKE ||
                stream.Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                return;
            }
#endif
            stream.Read(out NetIndex);
            stream.Record(nameof(NetIndex), NetIndex);
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

        private void DeserializeTemplate(IUnrealStream stream, UComponent component)
        {
            stream.Read(out component.TemplateOwnerClass);
            stream.Record(nameof(component.TemplateOwnerClass), component.TemplateOwnerClass);

            if (stream.Version < (uint)PackageObjectLegacyVersion.ClassDefaultCheckAddedToTemplateName || IsTemplate(ObjectFlag.ClassDefaultObject))
            {
                stream.Read(out component.TemplateName);
                stream.Record(nameof(component.TemplateName), component.TemplateName);
            }
        }

        /// <summary>
        /// Checks if the object is a template.
        /// An object is considered a template if either it is an object containing the defaults of a <seealso cref="UClass"/> or an archetype.
        /// </summary>
        /// <param name="templateFlags"></param>
        /// <returns>true if the object is a template.</returns>
        public bool IsTemplate(ObjectFlag templateFlag = ObjectFlag.TemplateObject)
        {
            return ObjectFlags.HasFlag(templateFlag) || EnumerateOuter().Any(obj => obj.ObjectFlags.HasFlag(templateFlag));
        }

        public virtual void Deserialize(IUnrealStream stream)
        {
            // Temp hack for backwards compatibility.
            _Buffer = (UObjectStream)stream;
            Deserialize();
        }

        public virtual void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserialize this object's structure from the _Buffer stream.
        /// </summary>
        protected virtual void Deserialize()
        {
#if VENGEANCE
            if (Package.Build == BuildGeneration.Vengeance)
            {
                if (_Buffer.LicenseeVersion >= 25)
                {
                    var header = (3, 0);
                    VengeanceDeserializeHeader(_Buffer, ref header);
                    if (header.Item2 == 2)
                    {
                        _Buffer.ReadInt32();
                        _Buffer.ConformRecordPosition();
                    }
                }
            }
#endif
            // This appears to be serialized for templates of classes like AmbientSoundNonLoop
            if (ObjectFlags.HasFlag(ObjectFlag.HasStack))
            {
                _Buffer.ReadClass(out StateFrame);
                _Buffer.Record(nameof(StateFrame), StateFrame);
            }

            // No version check found in the GoW PC client
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.TemplateDataAddedToUComponent)
            {
                switch (this)
                {
                    case UComponent component:
                        DeserializeTemplate(_Buffer, component);
                        break;

                    // HACK: Ugly work around for unregistered component classes...
                    // Simply for checking for the parent's class is not reliable without importing objects.
                    case UnknownObject _ when _Buffer.Length >= 12
                                              && IsTemplate()
                                              && ((string)Class!.Name).EndsWith("Component"):
                        {
                            long backupPosition = _Buffer.Position;

                            var fakeComponent = new UComponent();
                            try
                            {
                                DeserializeTemplate(_Buffer, fakeComponent);
                            }
                            catch (Exception exception)
                            {
                                LibServices.LogService.SilentException(
                                    new Exception("Failed attempt to interpret object as a template {0}", exception));


                                _Buffer.Position = backupPosition;
                                _Buffer.ConformRecordPosition();

                                // ISSUE: If the above recorded any data, the data will not be undone.
                            }

                            break;
                        }
                }
            }

            DeserializeNetIndex(_Buffer);
#if THIEF_DS || DEUSEX_IW
            // FIXME: Not present in all objects, even some classes?
            if (Package.Build == BuildGeneration.Flesh && GetType() != typeof(UnknownObject))
            {
                // var native private const int ObjectInternalPropertyHash[1];
                int thiefLinkDataObjectCount = _Buffer.ReadInt32();
                _Buffer.Record(nameof(thiefLinkDataObjectCount), thiefLinkDataObjectCount);
                for (int i = 0; i < thiefLinkDataObjectCount; i++)
                {
                    // These probably contain the missing UFields.
                    var thiefLinkDataObject = _Buffer.ReadObject();
                    _Buffer.Record(nameof(thiefLinkDataObject), thiefLinkDataObject);
                }

                if (ExportTable.ClassIndex != 0)
                {
                    _Buffer.Skip(4);
                    _Buffer.ConformRecordPosition();
                }
            }
#endif
            if (ExportTable.ClassIndex == 0)
            {
                return;
            }

            DeserializeProperties(_Buffer);
#if UE4
            if (_Buffer.UE4Version > 0 && !ObjectFlags.HasFlag(ObjectFlag.ClassDefaultObject))
            {
                _Buffer.Read(out bool shouldSerializeGuid);
                _Buffer.Record(nameof(shouldSerializeGuid), shouldSerializeGuid);

                if (shouldSerializeGuid)
                {
                    _Buffer.ReadStruct(out ObjectGuid);
                    _Buffer.Record(nameof(ObjectGuid), ObjectGuid);
                }
            }
#endif
        }

        /// <summary>
        /// Tries to read all properties that resides in this object instance.
        /// </summary>
        /// <param name="stream"></param>
        protected void DeserializeProperties(IUnrealStream stream)
        {
            Default = this;
            Properties = new DefaultPropertiesCollection();
            while (true)
            {
                var tag = new UDefaultProperty(Default);
                if (!tag.Deserialize())
                {
                    break;
                }

                Properties.Add(tag);
            }
        }

        #endregion

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

        public string GetReferencePath()
        {
            if (ImportTable != null)
            {
                return $"{ImportTable.ClassName}'{GetPath()}'";
            }

            return Class != null
                ? $"{Class.Name}'{GetPath()}'"
                : $"Class'{GetPath()}'";
        }

        /// <summary>
        /// Gets the name of this object instance class.
        /// </summary>
        /// <returns>The class name of this object instance.</returns>
        [Obsolete("To be deprecated")]
        public string GetClassName()
        {
            return ImportTable != null
                ? ImportTable.ClassName
                : Class?.Name ?? "Class";
        }

        /// <summary>
        /// Use this over 'typeof' to support UELib modifications such as replacing classes with the 'RegisterClass' function.
        /// </summary>
        /// <param name="className">The class name to compare to.</param>
        /// <returns>TRUE if this object instance class name is equal className, FALSE otherwise.</returns>
        public bool IsClassType(string className)
        {
            return string.Compare(GetClassName(), className, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Macro for getting a object instance by index.
        /// </summary>
        [Obsolete("To be deprecated", true)]
        protected UObject GetIndexObject(int index)
        {
            return Package.IndexToObject(index);
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
            // FIXME:
            if (Package.Stream.BigEndianCode)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public IUnrealStream GetBuffer()
        {
            return Package?.Stream;
        }

        public int GetBufferPosition()
        {
            return ExportTable?.SerialOffset ?? -1;
        }

        public int GetBufferSize()
        {
            return ExportTable?.SerialSize ?? 0;
        }

        public string GetBufferId(bool fullName = false)
        {
            return fullName
                ? $"{Package.PackageName}.{GetPath()}.{GetClassName()}"
                : $"{GetPath()}.{GetClassName()}";
        }

        #endregion

        [Conditional("BINARYMETADATA")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Record(string varName, object varObject = null)
        {
            _Buffer.Record(varName, varObject);
        }

        [Obsolete]
        protected void AssertEOS(int size, string testSubject = "")
        {
            if (size > _Buffer.Length - _Buffer.Position)
            {
                throw new DeserializationException(Name + ": Allocation past end of stream detected! Size:" + size +
                                                   " Subject:" + testSubject);
            }
            //System.Diagnostics.Debug.Assert( size <= (_Buffer.Length - _Buffer.Position), Name + ": Allocation past end of stream detected! " + size );
        }

        public int CompareTo(object obj)
        {
            return (int)Table.ObjectName - (int)((UObject)obj).Table.ObjectName;
        }

        public override string ToString()
        {
            return $"{Name}({(int)this})";
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return PackageIndex;
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

            _Buffer?.Close();
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

        public static implicit operator UPackageIndex(UObject? obj)
        {
            return obj?.PackageIndex ?? 0;
        }

        public static explicit operator int(UObject? obj)
        {
            return obj?.PackageIndex ?? 0;
        }

        public static explicit operator string(UObject? obj)
        {
            return obj?.Name ?? "none";
        }

        /// <summary>
        /// <see cref="UObject.Outer"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="GetPath()"/>
        /// </summary>
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
        /// <summary>
        /// Creates a new instance of the UELib.Core.UnknownObject class.
        /// </summary>
        public UnknownObject()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override bool CanDisposeBuffer()
        {
            return false;
        }
    }
}
