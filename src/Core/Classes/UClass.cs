﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using UELib.Annotations;
using UELib.Branch;
using UELib.Flags;

namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal class.
    /// </summary>
    [UnrealRegisterClass]
    public partial class UClass : UState, IUnrealExportable
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
#if DNF
                // No specified version
                if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                {
                    goto skipDeep;
                }
#endif
                stream.Write(IsDeep);
            skipDeep:
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

        public UGuid ClassGuid;
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
        /// A list of package names imported by this class.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        public UArray<UName> PackageImportNames;

        [Obsolete("Use PackageImportNames")] public IList<int> PackageImports;

        /// <summary>
        /// A map of default objects for the components that are instantiated by this class.
        ///
        /// The component objects are expected to be derivatives of class <see cref="UComponent"/>,
        /// however not all UComponent objects are known to UELib, so manual safe casting is required.
        ///
        /// Will be null if not deserialized.
        /// </summary>
        public UMap<UName, UObject> ComponentDefaultObjectMap;

        [Obsolete("Use ComponentDefaultObjectMap")]
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
#if UNREAL2 || DEVASTATION
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2 ||
                Package.Build == UnrealPackage.GameBuild.BuildName.Devastation)
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
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.Release62)
            {
                int classRecordSize = _Buffer.ReadInt32();
                Record(nameof(classRecordSize), classRecordSize);
            }
#if AA2
            if (Package.Build == BuildGeneration.AGP)
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
#if ROCKETLEAGUE
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                _Buffer.LicenseeVersion >= 1)
            {
                uint v194 = _Buffer.ReadUInt32();
                Record(nameof(v194), v194);
            }
#endif
#if SPELLBORN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
            {
                goto skipClassGuid;
            }
#endif
#if LEAD
            if (Package.Build == BuildGeneration.Lead)
            {
                goto skipClassGuid;
            }
#endif
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.ClassGuidDeprecated)
            {
                if (_Buffer.Version < (uint)PackageObjectLegacyVersion.ClassPlatformFlagsDeprecated)
                {
                    byte classPlatformFlags = _Buffer.ReadByte();
                    Record(nameof(classPlatformFlags), classPlatformFlags);
                }
            }
            else
            {
                _Buffer.ReadStruct(out ClassGuid);
                Record(nameof(ClassGuid), ClassGuid);
            }
#if R6
            // No version check
            if (Package.Build == UnrealPackage.GameBuild.BuildName.R6Vegas)
            {
                _Buffer.ReadArray(out UArray<UName> v100);
                Record(nameof(v100), v100);
            }
#endif
        skipClassGuid:

#if LEAD
            if (Package.Build == BuildGeneration.Lead)
            {
                var unk_0 = _Buffer.ReadIndex();
                Record(nameof(unk_0), unk_0);

                for (int i = 0; i < unk_0; i++) {
                    var objIdx = _Buffer.ReadObjectIndex();
                    var prop1 = _Buffer.ReadUInt32();
                    var prop2 = _Buffer.ReadUInt32();

                    Record("Lead:Unk_0", objIdx + ":" + prop1.ToString() + ":" + prop2.ToString());
                    Console.WriteLine(Package.GetIndexObjectName(objIdx) + " " + prop1.ToString() + " " + prop2.ToString());
                }

                ClassGroups = DeserializeGroup("ClassGroups");

                var unk_1 = _Buffer.ReadObjectIndex();
                var unk_2 = _Buffer.ReadUInt32();

                DeserializeGroup("UnknownGroup");

                DeserializeProperties(_Buffer);

                return;
            }
#endif

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.ClassDependenciesDeprecated)
            {
                _Buffer.ReadArray(out ClassDependencies);
                Record(nameof(ClassDependencies), ClassDependencies);
            }

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.PackageImportsDeprecated)
            {
                _Buffer.ReadArray(out PackageImportNames);
                Record(nameof(PackageImportNames), PackageImportNames);
            }

        serializeWithin:
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.Release62)
            {
                Within = _Buffer.ReadObject<UClass>();
                Record(nameof(Within), Within);

                ConfigName = _Buffer.ReadNameReference();
                Record(nameof(ConfigName), ConfigName);
#if DNF
                if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                    _Buffer.Version >= 102)
                {
                    HideCategories = DeserializeGroup("HideCategories");
                    if (_Buffer.Version >= 137)
                    {
                        _Buffer.ReadArray(out UArray<string> dnfTags);
                        Record(nameof(dnfTags), dnfTags);
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

                if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedHideCategoriesToUClass)
                {
                    // FIXME: Clean up
                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.DisplacedHideCategories
                        && isHideCategoriesOldOrder
                        && !Package.IsConsoleCooked()
                        && !Package.Build.Flags.HasFlag(BuildFlags.XenonCooked)
                        && _Buffer.UE4Version < 117)
                    {
                        HideCategories = DeserializeGroup("HideCategories");
                    }

                    // FIXME: >= version
                    if (_Buffer.Version >= 178 &&
                        _Buffer.Version < (uint)PackageObjectLegacyVersion.ComponentClassBridgeMapDeprecated)
                    {
                        _Buffer.ReadMap(out UMap<UObject, UName> componentClassBridgeMap);
                        Record(nameof(componentClassBridgeMap), componentClassBridgeMap);
                    }

                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedComponentTemplatesToUClass &&
                        _Buffer.Version < (uint)PackageObjectLegacyVersion.ComponentTemplatesDeprecated)
                    {
                        _Buffer.ReadArray(out UArray<UObject> componentTemplates);
                        Record(nameof(componentTemplates), componentTemplates);
                    }

                    // FIXME: >= version
                    if (_Buffer.Version >= 178 && _Buffer.UE4Version < 118)
                    {
                        _Buffer.Read(out ComponentDefaultObjectMap);
                        Record(nameof(ComponentDefaultObjectMap), ComponentDefaultObjectMap);
                    }

                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedInterfacesFeature &&
                        _Buffer.Version < (uint)PackageObjectLegacyVersion.InterfaceClassesDeprecated)
                    {
                        _Buffer.ReadArray(out UArray<UObject> interfaceClasses);
                        Record(nameof(interfaceClasses), interfaceClasses);
                    }

                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.InterfaceClassesDeprecated)
                    {
                        // See http://udn.epicgames.com/Three/UnrealScriptInterfaces.html
                        int interfacesCount = _Buffer.ReadInt32();
                        Record("Implements.Count", interfacesCount);
                        if (interfacesCount > 0)
                        {
                            AssertEOS(interfacesCount * 8, "Implemented");
                            ImplementedInterfaces = new List<int>(interfacesCount);
                            for (int i = 0; i < interfacesCount; ++i)
                            {
                                int interfaceIndex = _Buffer.ReadInt32();
                                Record("Implemented.InterfaceIndex", interfaceIndex);
                                int typeIndex = _Buffer.ReadInt32();
                                Record("Implemented.TypeIndex", typeIndex);
                                ImplementedInterfaces.Add(interfaceIndex);
#if UE4
                                if (_Buffer.UE4Version <= 0)
                                {
                                    continue;
                                }

                                bool isImplementedByK2 = _Buffer.ReadInt32() > 0;
                                Record("Implemented.isImplementedByK2", isImplementedByK2);
#endif
                            }
                        }
                    }
#if UE4
                    if (_Buffer.UE4Version > 0)
                    {
                        var classGeneratedBy = _Buffer.ReadObject();
                        Record(nameof(classGeneratedBy), classGeneratedBy);
                    }
#endif
#if AHIT
                    if (Package.Build == UnrealPackage.GameBuild.BuildName.AHIT && _Buffer.Version >= 878)
                    {
                        // AHIT auto-generates a list of unused function names for its optional interface functions.
                        // Seems to have been added in 878, during the modding beta between 1.Nov.17 and 6.Jan.18.
                        DeserializeGroup("UnusedOptionalInterfaceFunctions");
                    }
#endif

                    if (!Package.IsConsoleCooked() && !Package.Build.Flags.HasFlag(BuildFlags.XenonCooked))
                    {
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedDontSortCategoriesToUClass &&
                            _Buffer.UE4Version < 113
#if TERA
                            && Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                           )
                        {
                            DontSortCategories = DeserializeGroup("DontSortCategories");
                        }

                        // FIXME: Clean up
                        if (_Buffer.Version < (uint)PackageObjectLegacyVersion.DisplacedHideCategories || !isHideCategoriesOldOrder)
                        {
                            HideCategories = DeserializeGroup("HideCategories");
                        }
#if SPELLBORN
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
                        {
                            uint replicationFlags = _Buffer.ReadUInt32();
                            Record(nameof(replicationFlags), replicationFlags);
                        }
#endif
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedAutoExpandCategoriesToUClass)
                        {
                            // 490:GoW1, 576:CrimeCraft
                            if (!HasClassFlag(Flags.ClassFlags.CollapseCategories)
                                || _Buffer.Version <= vHideCategoriesOldOrder || _Buffer.Version >= 576)
                                AutoExpandCategories = DeserializeGroup("AutoExpandCategories");
                        }
#if TRANSFORMERS
                        if (Package.Build == BuildGeneration.HMS)
                        {
                            _Buffer.ReadArray(out UArray<UObject> hmsConstructors);
                            Record(nameof(hmsConstructors), hmsConstructors);
                        }
#endif
                        // FIXME: Wrong version, no version checks found in games that DO have checks for version 600+
                        if (_Buffer.Version > 670
#if BORDERLANDS
                            || Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands
#endif
                           )
                        {
                            AutoCollapseCategories = DeserializeGroup("AutoCollapseCategories");
                        }
#if BATMAN
                        // Only attested in bm4 with no version check.
                        if (_Buffer.Package.Build == BuildGeneration.RSS &&
                            _Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
                        {
                            IList<int> bm4_v198;
                            bm4_v198 = DeserializeGroup(nameof(bm4_v198));
                        }
#endif
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.ForceScriptOrderAddedToUClass
#if BIOSHOCK
                            // Partially upgraded
                            && Package.Build != UnrealPackage.GameBuild.BuildName.Bioshock_Infinite
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
#if BATTLEBORN
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
                        {
                            // Usually 0x03
                            byte unknownByte = _Buffer.ReadByte();
                            Record("Unknown:Battleborn", unknownByte);

                            NativeClassName = _Buffer.ReadString();
                            Record(nameof(NativeClassName), NativeClassName);

                            // not verified
                            ClassGroups = DeserializeGroup("ClassGroups");

                            goto skipClassGroups;
                        }
#endif
#if DISHONORED
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                        {
                            NativeClassName = _Buffer.ReadString();
                            Record(nameof(NativeClassName), NativeClassName);

                            goto skipClassGroups;
                        }
#endif
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedClassGroupsToUClass)
                        {
                            ClassGroups = DeserializeGroup("ClassGroups");
                        }

                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedNativeClassNameToUClass)
                        {
                            NativeClassName = _Buffer.ReadString();
                            Record(nameof(NativeClassName), NativeClassName);
                        }

                    skipClassGroups: ;

                        // FIXME: Found first in(V:655, DLLBind?), Definitely not in APB and GoW 2
                        // TODO: Corrigate Version
                        if (_Buffer.Version > 575 && _Buffer.Version < 673
#if TERA
                                                  && Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                                  && Package.Build != BuildGeneration.HMS
#endif
#if BORDERLANDS
                                                  && Package.Build != UnrealPackage.GameBuild.BuildName.Borderlands
#endif
                           )
                        {
                            int unknownInt32 = _Buffer.ReadInt32();
                            Record("Unknown", unknownInt32);
                        }
#if SINGULARITY
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Singularity)
                        {
                            _Buffer.Skip(8);
                            _Buffer.ConformRecordPosition();
                        }
#endif
                    }
#if BATMAN
                    if (Package.Build == BuildGeneration.RSS)
                    {
                        if (_Buffer.LicenseeVersion >= 95)
                        {
                            int bm_v174 = _Buffer.ReadInt32();
                            Record(nameof(bm_v174), bm_v174);
                        }
                    }
#endif
#if ROCKETLEAGUE
                    if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                        _Buffer.LicenseeVersion >= 21)
                    {
                        string v298 = _Buffer.ReadString();
                        Record(nameof(v298), v298);

                        int v2a8 = _Buffer.ReadInt32();
                        Record(nameof(v2a8), v2a8);

                        _Buffer.Read(out UArray<UName> v2b0);
                        Record(nameof(v2b0), v2b0);
                    }
#endif
                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedDLLBindFeature &&
                        _Buffer.UE4Version < 117)
                    {
                        DLLBindName = _Buffer.ReadNameReference();
                        Record(nameof(DLLBindName), DLLBindName);
                    }
#if MASS_EFFECT
                    if (Package.Build == BuildGeneration.SFX)
                    {
                        if (_Buffer.LicenseeVersion - 138u < 15)
                        {
                            _Buffer.Read(out int v40);
                            Record(nameof(v40), v40);
                        }

                        if (_Buffer.LicenseeVersion >= 139)
                        {
                            _Buffer.Read(out int v1ec);
                            Record(nameof(v1ec), v1ec);
                        }
                    }
#endif
#if REMEMBERME
                    if (Package.Build == UnrealPackage.GameBuild.BuildName.RememberMe)
                    {
                        var unknownName = _Buffer.ReadNameReference();
                        Record("Unknown:RememberMe", unknownName);
                    }
#endif
#if DISHONORED
                    if (Package.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                    {
                        ClassGroups = DeserializeGroup("ClassGroups");
                    }
#endif
#if BORDERLANDS2 || BATTLEBORN
                    if (Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                        Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn
                       )
                    {
                        byte unknownByte = _Buffer.ReadByte();
                        Record("Unknown:Borderlands2", unknownByte);
                    }
#endif
                }
            }
#if UNDYING
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                _Buffer.Version >= 70)
            {
                _Buffer.Read(out uint classCRC); // v4a8
                Record(nameof(classCRC), classCRC);
            }
#endif
#if THIEF_DS || DeusEx_IW
            if (Package.Build == BuildGeneration.Flesh)
            {
                string thiefClassVisibleName = _Buffer.ReadString();
                Record(nameof(thiefClassVisibleName), thiefClassVisibleName);

                // Restore the human-readable name if possible
                if (!string.IsNullOrEmpty(thiefClassVisibleName)
                    && Package.Build == UnrealPackage.GameBuild.BuildName.Thief_DS)
                {
                    var nameEntry = new UNameTableItem() { Name = thiefClassVisibleName };
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
                    string vengeanceDefaultPropertiesText = _Buffer.ReadString();
                    Record(nameof(vengeanceDefaultPropertiesText), vengeanceDefaultPropertiesText);
                }

                if (_Buffer.LicenseeVersion >= 6)
                {
                    string vengeanceClassFilePath = _Buffer.ReadString();
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
#if UE4
            if (_Buffer.UE4Version > 0)
            {
                string dummy = _Buffer.ReadName();
                Record("dummy", dummy);
                bool isCooked = _Buffer.ReadBool();
                Record("isCooked", isCooked);
            }
#endif
        scriptProperties:
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.DisplacedScriptPropertiesWithClassDefaultObject)
            {
                // Default__ClassName
                Default = _Buffer.ReadObject();
                Record(nameof(Default), Default);
            }
            else
            {
                DeserializeProperties(_Buffer);
            }
#if ROCKETLEAGUE
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague)
            {
                // StateMap; Seems to keep track of all declared states in the class.
                _Buffer.Read(out UMap<UName, UObject> v368);
                Record(nameof(v368), v368);
            }
#endif
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
