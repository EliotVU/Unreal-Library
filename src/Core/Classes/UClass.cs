using System;
using System.Collections.Generic;
using System.Diagnostics;
using UELib.Annotations;
using UELib.Flags;

namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal class.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UClass : UState
    {
        public struct Dependency : IUnrealSerializableClass
        {
            public int Class { get; private set; }

            public void Serialize(IUnrealStream stream)
            {
                // TODO: Implement code
            }

            public void Deserialize(IUnrealStream stream)
            {
                Class = stream.ReadObjectIndex();

                // Deep
                stream.ReadInt32();

                // ScriptTextCRC
                stream.ReadUInt32();
            }
        }

        #region Serialized Members

        private ulong ClassFlags { get; set; }

        public Guid ClassGuid;
        public UClass Within { get; private set; }
        public UName ConfigName { get; private set; }
        [CanBeNull] public UName DLLBindName;
        public string NativeClassName = string.Empty;
        public bool ForceScriptOrder;

        /// <summary>
        /// A list of class dependencies that this class depends on. Includes Imports and Exports.
        ///
        /// Deprecated @ PackageVersion:186
        /// </summary>
        public UArray<Dependency> ClassDependencies;

        /// <summary>
        /// A list of objects imported from a package.
        /// </summary>
        public IList<int> PackageImports;

        /// <summary>
        /// Index of component names into the NameTableList.
        /// UE3
        /// </summary>
        public IList<int> Components = null;

        /// <summary>
        /// Index of unsorted categories names into the NameTableList.
        /// UE3
        /// </summary>
        public IList<int> DontSortCategories;

        /// <summary>
        /// Index of hidden categories names into the NameTableList.
        /// </summary>
        public IList<int> HideCategories;

        /// <summary>
        /// Index of auto expanded categories names into the NameTableList.
        /// UE3
        /// </summary>
        public IList<int> AutoExpandCategories;

        /// <summary>
        /// A list of class group.
        /// </summary>
        public IList<int> ClassGroups;

        /// <summary>
        /// Index of auto collapsed categories names into the NameTableList.
        /// UE3
        /// </summary>
        public IList<int> AutoCollapseCategories;

        /// <summary>
        /// Index of (Object/Name?)
        /// UE3
        /// </summary>
        public IList<int> ImplementedInterfaces;

        [CanBeNull] public UArray<UObject> Vengeance_Implements;

        #endregion

        #region Script Members

        public IList<UState> States { get; protected set; }

        #endregion

        #region Constructors

        // TODO: Clean this mess up...
        protected override void Deserialize()
        {
#if UNREAL2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
            {
                _Buffer.ReadArray(out UArray<UObject> u2NetProperties);
                Record(nameof(u2NetProperties), u2NetProperties);
            }
#endif
            base.Deserialize();
#if VENGEANCE
            if (Package.Build.Generation == BuildGeneration.Vengeance &&
                Package.LicenseeVersion >= 36)
            {
                var header = (2, 0);
                VengeanceDeserializeHeader(_Buffer, ref header);
            }
#endif
            if (Package.Version < 62)
            {
                int classRecordSize = _Buffer.ReadInt32();
                Record(nameof(classRecordSize), classRecordSize);
            }
#if AA2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.AA2)
            {
                uint unknownUInt32 = _Buffer.ReadUInt32();
                Record("Unknown:AA2", unknownUInt32);
            }
#endif
            ClassFlags = _Buffer.ReadUInt32();
            Record(nameof(ClassFlags), (ClassFlags)ClassFlags);
#if SPELLBORN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
            {
                _Buffer.ReadArray(out ClassDependencies);
                Record(nameof(ClassDependencies), ClassDependencies);
                PackageImports = DeserializeGroup(nameof(PackageImports));
                goto skipTo61Stuff;
            }
#endif
            if (Package.Version >= 276)
            {
                if (Package.Version < 547)
                {
                    byte unknownByte = _Buffer.ReadByte();
                    Record("ClassGuidReplacement???", unknownByte);
                }
            }
            else
            {
                ClassGuid = _Buffer.ReadGuid();
                Record(nameof(ClassGuid), ClassGuid);
            }

            if (Package.Version < 248)
            {
                _Buffer.ReadArray(out ClassDependencies);
                Record(nameof(ClassDependencies), ClassDependencies);
                PackageImports = DeserializeGroup(nameof(PackageImports));
            }

            skipTo61Stuff:
            if (Package.Version >= 62)
            {
                // Class Name Extends Super.Name Within _WithinIndex
                //      Config(_ConfigIndex);
                Within = _Buffer.ReadObject<UClass>();
                Record(nameof(Within), Within);
                ConfigName = _Buffer.ReadNameReference();
                Record(nameof(ConfigName), ConfigName);

                const int vHideCategoriesOldOrder = 539;
                bool isHideCategoriesOldOrder = Package.Version <= vHideCategoriesOldOrder
#if TERA
                                                || Package.Build == UnrealPackage.GameBuild.BuildName.Tera
#endif
                    ;

                // +HideCategories
                if (Package.Version >= 99)
                {
                    // TODO: Corrigate Version
                    if (Package.Version >= 220)
                    {
                        // TODO: Corrigate Version
                        if ((isHideCategoriesOldOrder && !Package.IsConsoleCooked() &&
                             !Package.Build.Flags.HasFlag(BuildFlags.XenonCooked))
#if TRANSFORMERS
                            || Package.Build == UnrealPackage.GameBuild.BuildName.Transformers
#endif
                           )
                            DeserializeHideCategories();

                        DeserializeComponentsMap();

                        // RoboBlitz(369)
                        // TODO: Corrigate Version
                        if (Package.Version >= 369) DeserializeInterfaces();
                    }

                    if (!Package.IsConsoleCooked() && !Package.Build.Flags.HasFlag(BuildFlags.XenonCooked))
                    {
                        if (Package.Version >= 603
#if TERA
                            && Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                           )
                            DontSortCategories = DeserializeGroup("DontSortCategories");

                        // FIXME: Added in v99, removed in ~220?
                        if (Package.Version < 220 || !isHideCategoriesOldOrder)
                        {
                            DeserializeHideCategories();
#if SPELLBORN
                            if (Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
                            {
                                uint replicationFlags = _Buffer.ReadUInt32();
                                Record(nameof(replicationFlags), replicationFlags);
                            }
#endif
                        }

                        // +AutoExpandCategories
                        if (Package.Version >= 185)
                        {
                            // 490:GoW1, 576:CrimeCraft
                            if (!HasClassFlag(Flags.ClassFlags.CollapseCategories)
                                || Package.Version <= vHideCategoriesOldOrder || Package.Version >= 576)
                                AutoExpandCategories = DeserializeGroup("AutoExpandCategories");

                            if (Package.Version > 670)
                            {
                                AutoCollapseCategories = DeserializeGroup("AutoCollapseCategories");

                                if (Package.Version >= 749
#if SPECIALFORCE2
                                    && Package.Build != UnrealPackage.GameBuild.BuildName.SpecialForce2
#endif
                                   )
                                {
                                    // bForceScriptOrder
                                    ForceScriptOrder = _Buffer.ReadInt32() > 0;
                                    Record(nameof(ForceScriptOrder), ForceScriptOrder);
#if DISHONORED
                                    if (Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                                    {
                                        var unknownName = _Buffer.ReadNameReference();
                                        Record("Unknown:Dishonored", unknownName);
                                    }
#endif
                                    if (Package.Version >= UnrealPackage.VCLASSGROUP)
                                    {
#if DISHONORED
                                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                                        {
                                            NativeClassName = _Buffer.ReadText();
                                            Record(nameof(NativeClassName), NativeClassName);
                                            goto skipClassGroups;
                                        }
#endif
                                        ClassGroups = DeserializeGroup("ClassGroups");
                                        if (Package.Version >= 813)
                                        {
                                            NativeClassName = _Buffer.ReadText();
                                            Record(nameof(NativeClassName), NativeClassName);
                                        }
                                    }
#if DISHONORED
                                    skipClassGroups: ;
#endif
                                }
                            }

                            // FIXME: Found first in(V:655), Definitely not in APB and GoW 2
                            // TODO: Corrigate Version
                            if (Package.Version > 575 && Package.Version < 673
#if TERA
                                                      && Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                               )
                            {
                                int unknownInt32 = _Buffer.ReadInt32();
                                Record("Unknown", unknownInt32);
#if SINGULARITY
                                if (Package.Build == UnrealPackage.GameBuild.BuildName.Singularity) _Buffer.Skip(8);
#endif
                            }
                        }
                    }
#if BATMAN
                    if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.BatmanUDK)
                    {
                        _Buffer.Skip(sizeof(int));
                    }
#endif
                    if (Package.Version >= UnrealPackage.VDLLBIND)
                    {
                        if (!Package.Build.Flags.HasFlag(BuildFlags.NoDLLBind))
                        {
                            DLLBindName = _Buffer.ReadNameReference();
                            Record(nameof(DLLBindName), DLLBindName);
                        }
#if REMEMBERME
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.RememberMe)
                        {
                            var unknownName = _Buffer.ReadNameReference();
                            Record("Unknown:RememberMe", unknownName);
                        }
#endif
#if DISHONORED
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                            ClassGroups = DeserializeGroup("ClassGroups");
#endif
#if BORDERLANDS2
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2)
                        {
                            byte unknownByte = _Buffer.ReadByte();
                            Record("Unknown:Borderlands2", unknownByte);
                        }
#endif
                    }
                }
            }
#if THIEF_DS || DeusEx_IW
            if (Package.Build.Generation == BuildGeneration.Thief)
            {
                string thiefClassVisibleName = _Buffer.ReadText();
                Record(nameof(thiefClassVisibleName), thiefClassVisibleName);

                // Restore the human-readable name if possible
                if (!string.IsNullOrEmpty(thiefClassVisibleName) 
                    && Package.Build == UnrealPackage.GameBuild.BuildName.Thief_DS)
                {
                    var nameEntry = new UNameTableItem()
                    {
                        Name = thiefClassVisibleName
                    };
                    NameTable.Name = nameEntry;
                }
            }
#endif
#if VENGEANCE
            if (Package.Build.Generation == BuildGeneration.Vengeance)
            {
                if (Package.LicenseeVersion >= 2)
                {
                    ulong unkInt64 = _Buffer.ReadUInt64();
                    Record("Unknown:Vengeance", unkInt64);
                }

                if (Package.LicenseeVersion >= 3)
                {
                    ulong unkInt64 = _Buffer.ReadUInt64();
                    Record("Unknown:Vengeance", unkInt64);
                }

                if (Package.LicenseeVersion >= 2)
                {
                    string vengeanceDefaultPropertiesText = _Buffer.ReadText();
                    Record(nameof(vengeanceDefaultPropertiesText), vengeanceDefaultPropertiesText);
                }

                if (Package.LicenseeVersion >= 6)
                {
                    string vengeanceClassFilePath = _Buffer.ReadText();
                    Record(nameof(vengeanceClassFilePath), vengeanceClassFilePath);
                }

                if (Package.LicenseeVersion >= 12)
                {
                    UArray<UName> names;
                    _Buffer.ReadArray(out names);
                    Record("Unknown:Vengeance", names);
                }

                if (Package.LicenseeVersion >= 15)
                {
                    _Buffer.ReadArray(out Vengeance_Implements);
                    Record(nameof(Vengeance_Implements), Vengeance_Implements);
                }

                if (Package.LicenseeVersion >= 20)
                {
                    UArray<UObject> unk;
                    _Buffer.ReadArray(out unk);
                    Record("Unknown:Vengeance", unk);
                }

                if (Package.LicenseeVersion >= 32)
                {
                    UArray<UObject> unk;
                    _Buffer.ReadArray(out unk);
                    Record("Unknown:Vengeance", unk);
                }

                if (Package.LicenseeVersion >= 28)
                {
                    UArray<UName> unk;
                    _Buffer.ReadArray(out unk);
                    Record("Unknown:Vengeance", unk);
                }

                if (Package.LicenseeVersion >= 30)
                {
                    int unkInt32A = _Buffer.ReadInt32();
                    Record("Unknown:Vengeance", unkInt32A);
                    int unkInt32B = _Buffer.ReadInt32();
                    Record("Unknown:Vengeance", unkInt32B);

                    // Lazy array?
                    int skipSize = _Buffer.ReadInt32();
                    Record("Unknown:Vengeance", skipSize);
                    // FIXME: Couldn't RE this code
                    int b = _Buffer.ReadLength();
                    Debug.Assert(b == 0, "Unknown data was not zero!");
                    Record("Unknown:Vengeance", b);
                }
            }
#endif
            // In later UE3 builds, defaultproperties are stored in separated objects named DEFAULT_namehere,
            // TODO: Corrigate Version
            if (Package.Version >= 322)
            {
                Default = _Buffer.ReadObject();
                Record(nameof(Default), Default);
            }
            else
            {
                DeserializeProperties();
            }
        }

        private void DeserializeInterfaces()
        {
            // See http://udn.epicgames.com/Three/UnrealScriptInterfaces.html
            int interfacesCount = _Buffer.ReadInt32();
            Record("Implements.Count", interfacesCount);
            if (interfacesCount <= 0)
                return;

            AssertEOS(interfacesCount * 8, "Implemented");
            ImplementedInterfaces = new List<int>(interfacesCount);
            for (var i = 0; i < interfacesCount; ++i)
            {
                int interfaceIndex = _Buffer.ReadInt32();
                Record("Implemented.InterfaceIndex", interfaceIndex);
                int typeIndex = _Buffer.ReadInt32();
                Record("Implemented.TypeIndex", typeIndex);
                ImplementedInterfaces.Add(interfaceIndex);
            }
        }

        private void DeserializeHideCategories()
        {
            HideCategories = DeserializeGroup("HideCategories");
        }

        private void DeserializeComponentsMap()
        {
            int componentsCount = _Buffer.ReadInt32();
            Record("Components.Count", componentsCount);
            if (componentsCount <= 0)
                return;

            // NameIndex/ObjectIndex
            int numBytes = componentsCount * 12;
            AssertEOS(numBytes, "Components");
            _Buffer.Skip(numBytes);
        }

        protected override void FindChildren()
        {
            base.FindChildren();
            States = new List<UState>();
            for (var child = Children; child != null; child = child.NextField)
                if (child.IsClassType("State"))
                    States.Insert(0, (UState)child);
        }

        #endregion

        #region Methods

        private IList<int> DeserializeGroup(string groupName = "List", int count = -1)
        {
            if (count == -1) count = _Buffer.ReadLength();

            Record($"{groupName}.Count", count);
            if (count <= 0)
                return null;

            var groupList = new List<int>(count);
            for (var i = 0; i < count; ++i)
            {
                int index = _Buffer.ReadNameIndex();
                groupList.Add(index);

                Record($"{groupName}({Package.GetIndexName(index)})", index);
            }

            return groupList;
        }

        public bool HasClassFlag(ClassFlags flag)
        {
            return (ClassFlags & (uint)flag) != 0;
        }

        public bool HasClassFlag(uint flag)
        {
            return (ClassFlags & flag) != 0;
        }

        public bool IsClassInterface()
        {
#if VENGEANCE
            if (HasClassFlag(Flags.ClassFlags.VG_Interface))
            {
                return true;
            }
#endif
            return (Super != null && string.Compare(Super.Name, "Interface", StringComparison.OrdinalIgnoreCase) == 0)
                   || string.Compare(Name, "Interface", StringComparison.OrdinalIgnoreCase) == 0;
        }

        public bool IsClassWithin()
        {
            return Within != null && !string.Equals(Within.Name, "Object", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}