using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UELib.Annotations;
using UELib.Branch;
using UELib.Flags;
using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    public class ObjectEventArgs : EventArgs
    {
        public UObject ObjectRef { get; }

        public ObjectEventArgs(UObject objectRef)
        {
            ObjectRef = objectRef;
        }
    }

    /// <summary>
    /// Represents the Unreal class Core.UObject.
    /// Instances of this class are deserialized from the exports table entries.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UObject : object, IAcceptable, IContainsTable, IBinaryData, IDisposable, IComparable
    {
        /// <summary>
        /// The package this object resists in.
        /// </summary>
        public UnrealPackage Package { get; internal set; }

        public UObjectTableItem Table { get; internal set; }

        public UExportTableItem ExportTable => Table as UExportTableItem;

        public UImportTableItem ImportTable => Table as UImportTableItem;

        public UNameTableItem NameTable => Table.ObjectTable;

        /// <summary>
        /// The internal represented class in UnrealScript.
        /// </summary>
        [CanBeNull]
        [Output(OutputSlot.Parameter)]
        public UObject Class => ExportTable != null
            ? Package.GetIndexObject(ExportTable.ClassIndex)
            : null;

        /// <summary>
        /// [Package.Group:Outer].Object
        /// </summary>
        [CanBeNull]
        public UObject Outer => Package.GetIndexObject(Table.OuterIndex);

        [CanBeNull]
        public UObject Archetype => ExportTable != null
            ? Package.GetIndexObject(ExportTable.ArchetypeIndex)
            : null;

        /// <summary>
        /// The object's index represented as a table index.
        /// </summary>
        private int _ObjectIndex => Table is UExportTableItem
            ? Table.Index + 1
            : -(Table.Index + 1);

        /// <summary>
        /// The object's flags.
        /// </summary>
        private ulong _ObjectFlags => ExportTable?.ObjectFlags ?? 0;

        [Output(OutputSlot.Parameter)] public string Name => Table.ObjectName;

        #region Serialized Members

        protected UObjectRecordStream _Buffer;

        /// <summary>
        /// Copy of the Object bytes
        /// </summary>
        public UObjectStream Buffer => _Buffer;

        public int NetIndex = -1;

        [CanBeNull] public UObject Default { get; protected set; }

        /// <summary>
        /// Object Properties e.g. SubObjects or/and DefaultProperties
        /// </summary>
        public DefaultPropertiesCollection Properties { get; protected set; }

        /// <summary>
        /// Serialized if object is marked with <see cref="ObjectFlagsLO.HasStack" />.
        /// </summary>
        [CanBeNull] public UStateFrame StateFrame;

        #endregion

        #region General Members

        [Flags]
        public enum ObjectState : byte
        {
            Deserialied = 0x01,
            Errorlized = 0x02,
            Deserializing = 0x04
        }

        public ObjectState DeserializationState;
        public Exception ThrownException;
        public long ExceptionPosition;
        public UGuid ObjectGuid;

        /// <summary>
        /// Object will not be deserialized by UnrealPackage, Can only be deserialized by calling the methods yourself.
        /// </summary>
        public bool ShouldDeserializeOnDemand { get; protected set; }

        public BinaryMetaData BinaryMetaData { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Notifies this object instance to make a copy of this object's data from the Owner.Stream and then start deserializing this instance.
        /// </summary>
        public void BeginDeserializing()
        {
            // Imported objects cannot be deserialized!
            if (ImportTable != null)
            {
                return;
            }

            // e.g. None.
            if (ExportTable.SerialSize == 0)
            {
                DeserializationState |= ObjectState.Deserialied;
                return;
            }

            InitBuffer();
            try
            {
#if BINARYMETADATA
                BinaryMetaData = _Buffer.BinaryMetaData;
#endif
                DeserializationState |= ObjectState.Deserializing;
                if (HasObjectFlag(ObjectFlagsHO.PropertiesObject)
                    // Just in-case we have passed an overlapped object flag in UE2 or older packages.
                    && _Buffer.Version >= (uint)PackageObjectLegacyVersion.ClassDefaultCheckAddedToTemplateName)
                {
                    DeserializeClassDefault();
                }
                else
                {
                    Deserialize();
                }

                DeserializationState |= ObjectState.Deserialied;
#if STRICT
                Debug.Assert(Buffer.Position == Buffer.Length);
#endif
            }
            catch (Exception e)
            {
                ThrownException =
                    new UnrealException($"Couldn't deserialize object {GetReferencePath()}", e);
                ExceptionPosition = _Buffer?.Position ?? -1;
                DeserializationState |= ObjectState.Errorlized;

                Console.Error.WriteLine($"\r\n> Object deserialization error for {GetReferencePath()} as {GetType()}" +
                                        $"\r\n> Exception: {ThrownException}");
            }
            finally
            {
                DeserializationState &= ~ObjectState.Deserializing;
                MaybeDisposeBuffer();
            }
        }

        private void InitBuffer()
        {
            InitBuffer(Package.Stream);
        }

        private void InitBuffer(UPackageStream stream)
        {
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
                _Buffer = new UObjectRecordStream(stream, baseStream);
            }
            else
            {
                // Bypass the terrible and slow endian reverse call
                stream.Seek(objectOffset, SeekOrigin.Begin);
                byteCount = stream.EndianAgnosticRead(buffer, 0, objectSize);

                _Buffer = new UObjectRecordStream(stream, buffer);
            }

            Contract.Assert(byteCount == objectSize,
                $"Incomplete read; expected a total bytes of {objectSize} but got {byteCount}");
        }

        internal void EnsureBuffer()
        {
            //Console.WriteLine( "Ensure buffer for {0}", (string)this );
            if (_Buffer == null)
                InitBuffer();
        }

        internal void MaybeDisposeBuffer()
        {
            //Console.WriteLine( "Disposing buffer for {0}", (string)this );

            // Do not dispose while deserializing!
            // For example DecompileDefaultProperties or DecompileScript, may dispose the buffer in certain situations!
            if (_Buffer == null || (DeserializationState & ObjectState.Deserializing) != 0)
                return;

            _Buffer.Dispose();
            _Buffer = null;
            //Console.WriteLine( "Disposed" );
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
            Record("A:Vengeance", header.a);
            header.b = stream.ReadInt32();
            Record("B:Vengeance", header.b);
            switch (header.a)
            {
                case 2:
                    header.a = stream.ReadInt32();
                    Record("C:Vengeance", header.a);
                    break;

                case 3:
                    int c = stream.ReadInt32();
                    Record("C:Vengeance", c);
                    break;
            }
        }
#endif
        private void DeserializeNetIndex()
        {
#if MKKE || BATMAN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.MKKE ||
                Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                return;
            }
#endif
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.NetObjectCountAdded ||
                _Buffer.UE4Version >= 196)
            {
                return;
            }

            _Buffer.Read(out NetIndex);
            Record(nameof(NetIndex), NetIndex);
        }

        /// <summary>
        /// Special route for objects that are acting as the ClassDefault for a class
        /// i.e. a class like PrimitiveComponent is accompanied by an instance DEFAULT_PrimitiveComponent of the same class.
        /// </summary>
        private void DeserializeClassDefault()
        {
            DeserializeNetIndex();
            DeserializeProperties();
        }

        private void DeserializeTemplate(UComponent component)
        {
            _Buffer.Read(out component.TemplateOwnerClass);
            Record(nameof(component.TemplateOwnerClass), component.TemplateOwnerClass);

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.ClassDefaultCheckAddedToTemplateName || IsTemplate(ObjectFlagsHO.PropertiesObject))
            {
                _Buffer.Read(out component.TemplateName);
                Record(nameof(component.TemplateName), component.TemplateName);
            }
        }

        /// <summary>
        /// Checks if the object is a template.
        /// An object is considered a template if either it is an object containing the defaults of a <seealso cref="UClass"/> or an archetype.
        /// 
        /// FIXME: The <seealso cref="ObjectFlagsHO.ArchetypeObject"/> flag check was added later (Not checked for in GoW), no known version.
        /// </summary>
        /// <param name="templateFlags"></param>
        /// <returns>true if the object is a template.</returns>
        public bool IsTemplate(ObjectFlagsHO templateFlags = ObjectFlagsHO.PropertiesObject | ObjectFlagsHO.ArchetypeObject)
        {
            return HasObjectFlag(templateFlags) || EnumerateOuter().Any(obj => obj.HasObjectFlag(templateFlags));
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
            if (HasObjectFlag(ObjectFlagsLO.HasStack))
            {
                _Buffer.ReadClass(out StateFrame);
                Record(nameof(StateFrame), StateFrame);
            }
#if MKKE || BATMAN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.MKKE ||
                Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                goto skipNetIndex;
            }
#endif
            // No version check found in the GoW PC client
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.TemplateDataAddedToUComponent)
            {
                switch (this)
                {
                    case UComponent component:
                        DeserializeTemplate(component);
                        break;

                    // HACK: Ugly work around for unregistered component classes...
                    // Simply for checking for the parent's class is not reliable without importing objects.
                    case UnknownObject _ when _Buffer.Length >= 12
                                              && IsTemplate()
                                              && Class!.Name.EndsWith("Component"):
                        {
                            long backupPosition = _Buffer.Position;

                            var fakeComponent = new UComponent();
                            try
                            {
                                DeserializeTemplate(fakeComponent);
                            }
                            catch (Exception exception)
                            {
                                Console.Error.WriteLine("Failed attempt to interpret object as a template {0}",
                                    exception);

                                _Buffer.Position = backupPosition;
                                _Buffer.ConformRecordPosition();

                                // ISSUE: If the above recorded any data, the data will not be undone.
                            }

                            break;
                        }
                }
            }

        skipNetIndex:

            DeserializeNetIndex();
#if THIEF_DS || DEUSEX_IW
            // FIXME: Not present in all objects, even some classes?
            if (Package.Build == BuildGeneration.Flesh && GetType() != typeof(UnknownObject))
            {
                // var native private const int ObjectInternalPropertyHash[1];
                int thiefLinkDataObjectCount = _Buffer.ReadInt32();
                Record(nameof(thiefLinkDataObjectCount), thiefLinkDataObjectCount);
                for (int i = 0; i < thiefLinkDataObjectCount; i++)
                {
                    // These probably contain the missing UFields.
                    var thiefLinkDataObject = _Buffer.ReadObject();
                    Record(nameof(thiefLinkDataObject), thiefLinkDataObject);
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

            DeserializeProperties();
#if UE4
            if (_Buffer.UE4Version > 0)
            {
                bool shouldSerializeGuid = _Buffer.ReadInt32() > 0;
                Record(nameof(shouldSerializeGuid), shouldSerializeGuid);
                if (shouldSerializeGuid)
                {
                    _Buffer.ReadStruct(out ObjectGuid);
                    Record(nameof(ObjectGuid), ObjectGuid);
                }
            }
#endif
        }

        /// <summary>
        /// Tries to read all properties that resides in this object instance.
        /// </summary>
        protected void DeserializeProperties()
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

        /// <summary>
        /// Checks if the object contains the specified @flag or one of the specified flags.
        ///
        /// Checks the lower bits of ObjectFlags.
        /// </summary>
        /// <param name="flag">The flag(s) to compare to.</param>
        /// <returns>Whether it contained one of the specified flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasObjectFlag(ObjectFlagsLO flag)
        {
            return (_ObjectFlags & (ulong)flag) != 0;
        }

        /// <summary>
        /// Checks if the object contains the specified @flag or one of the specified flags.
        ///
        /// Checks the higher bits of ObjectFlags.
        /// </summary>
        /// <param name="flag">The flag(s) to compare to.</param>
        /// <returns>Whether it contained one of the specified flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasObjectFlag(ObjectFlagsHO flag)
        {
            return (_ObjectFlags & ((ulong)flag << 32)) != 0;
        }

        public bool IsPublic()
        {
            return (_ObjectFlags & (ulong)ObjectFlagsLO.Public) != 0;
        }

        public bool IsProtected()
        {
            // Protected was shifted to the higher order in later UE3 builds
            return HasObjectFlag(ObjectFlagsHO.Protected)
                   || (_ObjectFlags & (
                       (ulong)ObjectFlagsLO.Public | (ulong)ObjectFlagsLO.Protected))
                   == ((ulong)ObjectFlagsLO.Public | (ulong)ObjectFlagsLO.Protected);
        }

        public bool IsPrivate()
        {
            return (_ObjectFlags & ((ulong)ObjectFlagsLO.Public | (ulong)ObjectFlagsLO.Private)) !=
                   (ulong)ObjectFlagsLO.Public;
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
            return Package.GetIndexObject(index);
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
            return _ObjectIndex;
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

        public static explicit operator int(UObject obj)
        {
            return obj?._ObjectIndex ?? 0;
        }

        public static explicit operator string(UObject obj)
        {
            return obj?.Name;
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
