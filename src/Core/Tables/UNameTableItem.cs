using System;
using System.Diagnostics;
using UELib.Branch;

namespace UELib
{
    /// <summary>
    /// A unique name in a package or unreal environment, usually referenced in a package by index, or by hash in other cases.
    /// </summary>
    public sealed class UNameTableItem : UTableItem, IUnrealSerializableClass
    {
        /// <summary>
        /// The unique name excluding the terminal null character.
        /// </summary>
        public string Name
        {
            get => _Name;
            set => _Name = value;
        }

        internal string _Name;

        /// <summary>
        /// The flags to define loading rules, see <see cref="ObjectFlagsLO"/>
        /// </summary>
        [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE3)]
        public ulong Flags
        {
            get => _Flags;
            set => _Flags = value;
        }

        internal ulong _Flags;

        [BuildGeneration(BuildGeneration.UE4)]
        public ushort NonCasePreservingHash;

        [BuildGeneration(BuildGeneration.UE4)]
        public ushort CasePreservingHash;

        public UNameTableItem()
        {
        }

        public UNameTableItem(string text)
        {
            Name = text;
        }

        /// <summary>
        /// Serializes the name to a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Serialize(IUnrealStream, UNameTableItem)"/>
        /// </summary>
        /// <param name="stream">The output stream</param>
        public void Serialize(IUnrealStream stream)
        {
            Debug.Assert(_Name.Length <= 1024,
                "Maximum name length exceeded! Possible corrupt or unsupported package.");

            if (stream.Version >= (uint)PackageObjectLegacyVersion.Release64)
            {
                stream.Write(_Name);
            }
            else
            {
                stream.WriteAnsiNullString(_Name);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                stream.Write(_Flags);
            }
            else
            {
                stream.Write((uint)_Flags);
            }
        }

        /// <summary>
        /// Deserializes the name from a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Deserialize(IUnrealStream, UNameTableItem)"/>
        /// </summary>
        /// <param name="stream">The input stream</param>
        public void Deserialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.Release64)
            {
                stream.Read(out _Name);
            }
            else
            {
                _Name = stream.ReadAnsiNullString();
            }

            Debug.Assert(_Name.Length <= 1024,
                "Maximum name length exceeded! Possible corrupt or unsupported package.");
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock)
            {
                stream.Read(out _Flags);

                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                stream.Read(out _Flags);
            }
            else
            {
                stream.Read(out uint flags);
                _Flags = flags;
            }
        }

        public override string ToString()
        {
            return _Name;
        }

        public static implicit operator string(UNameTableItem a)
        {
            return a._Name;
        }

        public static implicit operator int(UNameTableItem a)
        {
            return a.Index;
        }
    }
}