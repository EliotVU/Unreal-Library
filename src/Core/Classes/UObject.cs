using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UELib.Annotations;
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

        [Output(OutputSlot.Parameter)]
        public string Name => Table.ObjectName;

        #region Serialized Members

        protected UObjectStream _Buffer;

        /// <summary>
        /// Copy of the Object bytes
        /// </summary>
        public UObjectStream Buffer => _Buffer;

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
                BinaryMetaData = new BinaryMetaData();
#endif
                DeserializationState |= ObjectState.Deserializing;
                Deserialize();
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

                Console.WriteLine(e.Source + ":" + Name + ":" + e.GetType().Name + " occurred while deserializing;"
                                  + "\r\n" + e.StackTrace
                                  + "\r\n" + e.Message
                );
            }
            finally
            {
                DeserializationState &= ~ObjectState.Deserializing;
                MaybeDisposeBuffer();
            }
        }

        private void InitBuffer()
        {
            //Console.WriteLine( "Init buffer for {0}", (string)this );
            var buff = new byte[ExportTable.SerialSize];
            Package.Stream.Seek(ExportTable.SerialOffset, SeekOrigin.Begin);
            Package.Stream.Read(buff, 0, ExportTable.SerialSize);
            if (Package.Stream.BigEndianCode)
            {
                Array.Reverse(buff);
            }

            _Buffer = new UObjectStream(Package.Stream, buff);
        }

        internal void EnsureBuffer()
        {
            //Console.WriteLine( "Ensure buffer for {0}", (string)this );
            InitBuffer();
        }

        internal void MaybeDisposeBuffer()
        {
            //Console.WriteLine( "Disposing buffer for {0}", (string)this );

            // Do not dispose while deserializing!
            // For example DecompileDefaultProperties or DecompileScript, may dispose the buffer in certain situations!
            if (_Buffer == null || (DeserializationState & ObjectState.Deserializing) != 0)
                return;

            _Buffer.DisposeBuffer();
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
                    }
                }
            }
#endif
            // This appears to be serialized for templates of classes like AmbientSoundNonLoop
            if (HasObjectFlag(ObjectFlagsLO.HasStack))
            {
                _Buffer.ReadClass(out StateFrame);
            }
#if MKKE || BATMAN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.MKKE ||
                Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                goto skipNetIndex;
            }
#endif
            if (_Buffer.Version >= UExportTableItem.VNetObjects &&
                _Buffer.UE4Version < 196)
            {
                int netIndex = _Buffer.ReadInt32();
                Record(nameof(netIndex), netIndex);
            }
            skipNetIndex:

            // TODO: Serialize component data here
            //if( _Buffer.Version > 400
            //    && HasObjectFlag( Flags.ObjectFlagsHO.PropertiesObject )
            //    && HasObjectFlag( Flags.ObjectFlagsHO.ArchetypeObject ) )
            //{
            //    var componentClass = _Buffer.ReadObjectIndex();
            //    var componentName = _Buffer.ReadNameIndex();
            //}
#if THIEF_DS || DEUSEX_IW
            // FIXME: Not present in all objects, even some classes?
            if (Package.Build == BuildGeneration.Flesh && GetType() != typeof(UnknownObject))
            {
                // var native private const int ObjectInternalPropertyHash[1];
                int thiefLinkDataObjectCount = _Buffer.ReadInt32();
                Record(nameof(thiefLinkDataObjectCount), thiefLinkDataObjectCount);
                for (var i = 0; i < thiefLinkDataObjectCount; i++)
                {
                    // These probably contain the missing UFields.
                    var thiefLinkDataObject = _Buffer.ReadObject();
                    Record(nameof(thiefLinkDataObject), thiefLinkDataObject);
                }

                if (ExportTable.ClassIndex != 0)
                {
                    _Buffer.Skip(4);
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
        public bool HasObjectFlag(ObjectFlagsLO flag)
        {
            return ((uint)_ObjectFlags & (uint)flag) != 0;
        }

        /// <summary>
        /// Checks if the object contains the specified @flag or one of the specified flags.
        ///
        /// Checks the higher bits of ObjectFlags.
        /// </summary>
        /// <param name="flag">The flag(s) to compare to.</param>
        /// <returns>Whether it contained one of the specified flags.</returns>
        public bool HasObjectFlag(ObjectFlagsHO flag)
        {
            return ((_ObjectFlags >> 32) & (uint)flag) != 0;
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
                ? Package.PackageName + "." + GetOuterGroup() + "." + GetClassName()
                : GetOuterGroup() + "." + GetClassName();
        }

#endregion

        /// <summary>
        /// TODO: Move this feature into a stream.
        /// Outputs the present position and the value of the parsed object.
        ///
        /// Only called in the DEBUGBUILD!
        /// </summary>
        /// <param name="varName">The struct that was read from the previous buffer position.</param>
        /// <param name="varObject">The struct's value that was read.</param>
        [Conditional("BINARYMETADATA")]
        internal void Record(string varName, object varObject = null)
        {
            long size = _Buffer.Position - _Buffer.LastPosition;
            BinaryMetaData.AddField(varName, varObject, _Buffer.LastPosition, size);
#if LOG_RECORDS
            if( varObject == null )
            {
                Console.WriteLine( varName );
                return;
            }

            var propertyType = varObject.GetType();
            Console.WriteLine(
                "0x" + _Buffer.LastPosition.ToString("x8").ToUpper()
                + " : ".PadLeft( 2, ' ' )
                + varName.PadRight( 32, ' ' ) + ":" + propertyType.Name.PadRight( 32, ' ' )
                + " => " + varObject
            );
#endif
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

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                MaybeDisposeBuffer();
            }
        }

        ~UObject()
        {
            Dispose(false);
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

        [Obsolete]
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

        protected override void Deserialize()
        {
            if (Package.Version > 400 && _Buffer.Length >= 12)
            {
                // componentClassIndex
                _Buffer.Position += sizeof(int);
                int componentNameIndex = _Buffer.ReadNameIndex();
                if (componentNameIndex == (int)Table.ObjectName)
                {
                    base.Deserialize();
                    return;
                }

                _Buffer.Position -= 12;
            }

            base.Deserialize();
        }

        protected override bool CanDisposeBuffer()
        {
            return false;
        }
    }
}