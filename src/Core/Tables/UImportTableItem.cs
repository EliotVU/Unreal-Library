using System;
using UELib.Core;

namespace UELib
{
    /// <summary>
    /// An imported resource to assist with the linking of any <see cref="UObject"/>
    /// It describes the name of the object, its class name, and the package containing that class.
    /// </summary>
    public sealed class UImportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
        private UName _ClassPackageName;

        /// <summary>
        /// The name of the package that contains the export for the <see cref="ClassName"/>.
        /// </summary>
        public UName ClassPackageName
        {
            get => _ClassPackageName;
            set => _ClassPackageName = value;
        }

        [Obsolete("Renamed to ClassPackageName")]
        public UName PackageName
        {
            get => _ClassPackageName;
            set => _ClassPackageName = value;
        }

        private UName _ClassName;

        /// <summary>
        /// The name of the class excluding the prefix "U"
        /// </summary>
        public UName ClassName
        {
            get => _ClassName;
            set => _ClassName = value;
        }

        [Obsolete] protected override string __ClassName => _ClassName;
        [Obsolete] protected override int __ClassIndex => (int)_ClassName;

        /// <summary>
        /// Serializes the import to a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Serialize(IUnrealStream, UImportTableItem)"/>
        /// </summary>
        /// <param name="stream">The output stream</param>
        public void Serialize(IUnrealStream stream)
        {
            stream.Write(_ClassPackageName);
            stream.Write(_ClassName);
            // version >= 50
            stream.Write((int)_OuterIndex); // Always an ordinary integer
            stream.Write(_ObjectName);
        }

        /// <summary>
        /// Deserializes the import from a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Deserialize(IUnrealStream, UImportTableItem)"/>
        /// </summary>
        /// <param name="stream">The input stream</param>
        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out _ClassPackageName);
            stream.Read(out _ClassName);
            // version >= 50
            stream.Read(out int outerIndex); // ObjectIndex, though always written as 32bits regardless of build.
            _OuterIndex = outerIndex;
            stream.Read(out _ObjectName);
        }

        public override string GetReferencePath()
        {
            return $"{_ClassName}'{GetPath()}'";
        }

        public override string ToString()
        {
            return $"{ObjectName}({-(Index + 1)})";
        }

        [Obsolete("Use ToString()")]
        public string ToString(bool v)
        {
            return ToString();
        }

        public static explicit operator int(UImportTableItem item)
        {
            return -(item.Index + 1);
        }
    }
}
