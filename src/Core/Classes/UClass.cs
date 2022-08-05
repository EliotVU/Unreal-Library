using System;
using System.Collections;
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
        /// <summary>
        /// Implements FDependency.
        /// 
        /// A legacy dependency struct that was used for incremental compilation (UnrealEd).
        /// </summary>
        public struct Dependency : IUnrealSerializableClass
        {
            [NotNull] public UClass Class;
            public bool IsDeep;
            public uint ScriptTextCRC;
            
            public void Serialize(IUnrealStream stream)
            {
                stream.Write(Class);
                stream.Write(IsDeep);
                stream.Write(ScriptTextCRC);
            }

            public void Deserialize(IUnrealStream stream)
            {
                Class = stream.ReadObject<UClass>();
#if DNF
                // No specified version
                if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                {
                    goto skipDeep;
                }
#endif
                IsDeep = stream.ReadBool();
                skipDeep:
                ScriptTextCRC = stream.ReadUInt32();
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
            if (Package.Build == BuildGeneration.Vengeance &&
                _Buffer.LicenseeVersion >= 36)
            {
                var header = (2, 0);
                VengeanceDeserializeHeader(_Buffer, ref header);
            }
#endif
            if (_Buffer.Version < 62)
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
#if UE4
            if (_Buffer.UE4Version > 0)
            {
                _Buffer.ReadMap(out FuncMap);
                Record(nameof(FuncMap), FuncMap);
            }
#endif
            ClassFlags = _Buffer.ReadUInt32();
            Record(nameof(ClassFlags), (ClassFlags)ClassFlags);
#if SPELLBORN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
            {
                goto skipClassGuid;
            }
#endif
            if (_Buffer.Version >= 276)
            {
                if (_Buffer.Version < 547)
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

        skipClassGuid:
            if (_Buffer.Version < 248)
            {
                _Buffer.ReadArray(out ClassDependencies);
                Record(nameof(ClassDependencies), ClassDependencies);
                PackageImports = DeserializeGroup(nameof(PackageImports));
            }

            if (_Buffer.Version >= 62)
            {
                // Class Name Extends Super.Name Within _WithinIndex
                //      Config(_ConfigIndex);
                Within = _Buffer.ReadObject<UClass>();
                Record(nameof(Within), Within);
                ConfigName = _Buffer.ReadNameReference();
                Record(nameof(ConfigName), ConfigName);
#if DNF
                if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                    _Buffer.Version >= 102)
                {
                    DeserializeHideCategories();
                    if (_Buffer.Version >= 137)
                    {
                        _Buffer.ReadArray(out UArray<string> dnfStringArray);
                        Record(nameof(dnfStringArray), dnfStringArray);
                    }

                    if (_Buffer.Version >= 113)
                    {
                        // Unknown purpose, used to set a global variable to 0 (GIsATablesInitialized_exref) if it reads 0.
                        bool dnfBool = _Buffer.ReadBool();
                        Record(nameof(dnfBool), dnfBool);
                        
                        // FBitArray data, not sure if this behavior is correct, always 0.
                        int dnfBitArrayLength = _Buffer.ReadInt32();
                        _Buffer.Skip(dnfBitArrayLength);
                        Record(nameof(dnfBitArrayLength), dnfBitArrayLength);
                    }

                    goto scriptProperties;
                }
#endif
                const int vHideCategoriesOldOrder = 539;
                bool isHideCategoriesOldOrder = _Buffer.Version <= vHideCategoriesOldOrder
#if TERA
                                                || Package.Build == UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                                || Package.Build == BuildGeneration.HMS
#endif
                    ;

                // +HideCategories
                if (_Buffer.Version >= 99)
                {
                    // TODO: Corrigate Version
                    if (_Buffer.Version >= 220)
                    {
                        // TODO: Corrigate Version
                        if (isHideCategoriesOldOrder && !Package.IsConsoleCooked() &&
                            !Package.Build.Flags.HasFlag(BuildFlags.XenonCooked) &&
                            _Buffer.UE4Version < 117)
                            DeserializeHideCategories();

                        // Seems to have been removed in transformer packages
                        if (_Buffer.UE4Version < 118) DeserializeComponentsMap();
                    }

                    // RoboBlitz(369)
                    // TODO: Corrigate Version
                    if (_Buffer.Version >= VInterfaceClass) DeserializeInterfaces();
#if UE4
                    if (_Buffer.UE4Version > 0)
                    {
                        var classGeneratedBy = _Buffer.ReadObject();
                        Record(nameof(classGeneratedBy), classGeneratedBy);
                    }
#endif
                    if (!Package.IsConsoleCooked() && !Package.Build.Flags.HasFlag(BuildFlags.XenonCooked))
                    {
                        if (_Buffer.Version >= 603 && _Buffer.UE4Version < 113
#if TERA
                                                   && Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                           )
                            DontSortCategories = DeserializeGroup("DontSortCategories");

                        // FIXME: Added in v99, removed in ~220?
                        if (_Buffer.Version < 220 || !isHideCategoriesOldOrder)
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
                        if (_Buffer.Version >= 185)
                        {
                            // 490:GoW1, 576:CrimeCraft
                            if (!HasClassFlag(Flags.ClassFlags.CollapseCategories)
                                || _Buffer.Version <= vHideCategoriesOldOrder || _Buffer.Version >= 576)
                                AutoExpandCategories = DeserializeGroup("AutoExpandCategories");
#if TRANSFORMERS
                            if (Package.Build == BuildGeneration.HMS)
                            {
                                _Buffer.ReadArray(out UArray<UObject> hmsConstructors);
                                Record(nameof(hmsConstructors), hmsConstructors);
                            }
#endif
                        }

                        if (_Buffer.Version > 670)
                        {
                            AutoCollapseCategories = DeserializeGroup("AutoCollapseCategories");
                        }

                        if (_Buffer.Version >= 749
#if SPECIALFORCE2
                            && Package.Build != UnrealPackage.GameBuild.BuildName.SpecialForce2
#endif
                           )
                        {
                            // bForceScriptOrder
                            ForceScriptOrder = _Buffer.ReadBool();
                            Record(nameof(ForceScriptOrder), ForceScriptOrder);
                        }
#if DD2
                        // DD2 doesn't use a LicenseeVersion, maybe a merged standard feature (bForceScriptOrder?).
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.DD2 && _Buffer.Version >= 688)
                        {
                            int dd2UnkInt32 = _Buffer.ReadInt32();
                            Record(nameof(dd2UnkInt32), dd2UnkInt32);
                        }
#endif
#if DISHONORED
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                        {
                            var unknownName = _Buffer.ReadNameReference();
                            Record("Unknown:Dishonored", unknownName);
                        }
#endif
                        if (_Buffer.Version >= UnrealPackage.VCLASSGROUP)
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
                            if (_Buffer.Version >= 813)
                            {
                                NativeClassName = _Buffer.ReadText();
                                Record(nameof(NativeClassName), NativeClassName);
                            }
                        }
#if DISHONORED
                    skipClassGroups: ;
#endif

                        // FIXME: Found first in(V:655, DLLBind?), Definitely not in APB and GoW 2
                        // TODO: Corrigate Version
                        if (_Buffer.Version > 575 && _Buffer.Version < 673
#if TERA
                                                  && Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                                  && Package.Build != BuildGeneration.HMS
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
#if BATMAN
                    if (Package.Build == BuildGeneration.RSS)
                    {
                        _Buffer.Skip(sizeof(int));
                    }
#endif
                    if (_Buffer.Version >= UnrealPackage.VDLLBIND && _Buffer.UE4Version < 117)
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
#if UE4
            if (_Buffer.UE4Version > 0)
            {
                string dummy = _Buffer.ReadName();
                Record("dummy", dummy);
                bool isCooked = _Buffer.ReadBool();
                Record("isCooked", isCooked);
            }
#endif
#if THIEF_DS || DeusEx_IW
            if (Package.Build == BuildGeneration.Flesh)
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
            if (Package.Build == BuildGeneration.Vengeance)
            {
                if (_Buffer.LicenseeVersion >= 2)
                {
                    ulong unkInt64 = _Buffer.ReadUInt64();
                    Record("Unknown:Vengeance", unkInt64);
                }

                if (_Buffer.LicenseeVersion >= 3)
                {
                    ulong unkInt64 = _Buffer.ReadUInt64();
                    Record("Unknown:Vengeance", unkInt64);
                }

                if (_Buffer.LicenseeVersion >= 2)
                {
                    string vengeanceDefaultPropertiesText = _Buffer.ReadText();
                    Record(nameof(vengeanceDefaultPropertiesText), vengeanceDefaultPropertiesText);
                }

                if (_Buffer.LicenseeVersion >= 6)
                {
                    string vengeanceClassFilePath = _Buffer.ReadText();
                    Record(nameof(vengeanceClassFilePath), vengeanceClassFilePath);
                }

                if (_Buffer.LicenseeVersion >= 12)
                {
                    UArray<UName> names;
                    _Buffer.ReadArray(out names);
                    Record("Unknown:Vengeance", names);
                }

                if (_Buffer.LicenseeVersion >= 15)
                {
                    _Buffer.ReadArray(out Vengeance_Implements);
                    Record(nameof(Vengeance_Implements), Vengeance_Implements);
                }

                if (_Buffer.LicenseeVersion >= 20)
                {
                    UArray<UObject> unk;
                    _Buffer.ReadArray(out unk);
                    Record("Unknown:Vengeance", unk);
                }

                if (_Buffer.LicenseeVersion >= 32)
                {
                    UArray<UObject> unk;
                    _Buffer.ReadArray(out unk);
                    Record("Unknown:Vengeance", unk);
                }

                if (_Buffer.LicenseeVersion >= 28)
                {
                    UArray<UName> unk;
                    _Buffer.ReadArray(out unk);
                    Record("Unknown:Vengeance", unk);
                }

                if (_Buffer.LicenseeVersion >= 30)
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
        scriptProperties:
            // In later UE3 builds, defaultproperties are stored in separated objects named DEFAULT_namehere,
            // TODO: Corrigate Version
            if (_Buffer.Version >= 322)
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
#if UE4
                if (_Buffer.UE4Version > 0)
                {
                    var isImplementedByK2 = _Buffer.ReadInt32() > 0;
                    Record("Implemented.isImplementedByK2", isImplementedByK2);
                }
#endif
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

    [UnrealRegisterClass]
    public class UBlueprint : UField
    {
    }

    [UnrealRegisterClass]
    public class UBlueprintGeneratedClass : UClass
    {
    }
}