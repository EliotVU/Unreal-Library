using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UELib.Branch;
using UELib.Flags;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UClass/Core.Class
    /// </summary>
    [UnrealRegisterClass]
    public partial class UClass : UState, IUnrealExportable
    {
        #region Serialized Members

        /// <summary>
        /// The class flags, reflecting various class specifiers as seen in a class declaration.
        /// </summary>
        public UnrealFlags<ClassFlag> ClassFlags
        {
            get => _ClassFlags;
            set => _ClassFlags = value;
        }

        /// <summary>
        /// The class guid, set using the class specifier 'guid' e.g. <example>class MyClass extends Object guid(A,B,C,D);</example>
        ///
        /// Possibly used to identify the class for use in COM (Component Object Model)
        /// </summary>
        public UGuid ClassGuid
        {
            get => _ClassGuid;
            set => _ClassGuid = value;
        }

        [Obsolete("Use ClassWithin instead")] public UClass? Within => ClassWithin;

        /// <summary>
        /// The class that this class belongs within, e.g. <example>class MyClass extends Object within PlayerController;</example>
        ///
        /// Presume <value>Class'Core.Object'</value> if null.
        /// </summary>
        public UClass? ClassWithin { get; set; }

        [Obsolete("Use ClassConfigName instead")] public UName ConfigName => ClassConfigName;

        /// <summary>
        /// Name of the configuration file that the class is associated with, e.g. <example>class MyClass extends Object config(MyConfig);</example>
        /// 
        /// Presume <value>'System'</value> if 'None'.
        /// </summary>
        public UName ClassConfigName { get; set; } = UnrealName.None;

        /// <summary>
        /// The DLL name to bind the class to, e.g. <example>class MyClass extends Object DLLBind(MyDLL);</example>
        /// </summary>
        public UName? DLLBindName { get; set; } = UnrealName.None;

        [Obsolete("Use NativeHeaderName instead")] public string NativeClassName => NativeHeaderName;

        /// <summary>
        /// The native header file name of the class, e.g. <example>class MaterialInterface extends Surface native(Material);</example> will give "Engine/Inc/EngineMaterialClasses.h".
        /// </summary>
        public string NativeHeaderName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the class properties should be forced to appear in script order, e.g. <example>class MyClass extends Object forcescriptorder(true);</example>
        /// </summary>
        public bool ForceScriptOrder { get; set; }

        /// <summary>
        /// Class dependencies that the class is dependent on; Including both Imports and Exports.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        public UArray<Dependency> ClassDependencies
        {
            get => _ClassDependencies;
            set => _ClassDependencies = value;
        }

        /// <summary>
        /// Names of packages that the class imports.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        public UArray<UName> PackageImportNames
        {
            get => _PackageImportNames;
            set => _PackageImportNames = value;
        }

        [Obsolete("Use PackageImportNames", true)] public IList<int> PackageImports;

        /// <summary>
        /// A map of default objects for the components that are instantiated by this class.
        ///
        /// The component objects are expected to be derivatives of class <see cref="UComponent"/>,
        /// however not all UComponent objects are known to UELib, so manual safe casting is required.
        ///
        /// Will be null if not deserialized.
        /// </summary>
        [BuildGeneration(BuildGeneration.UE3)]
        public UMap<UName, UObject> ComponentDefaultObjectMap
        {
            get => _ComponentDefaultObjectMap;
            set => _ComponentDefaultObjectMap = value;
        }

        [Obsolete("Use ComponentDefaultObjectMap", true)] public IList<int> Components = null;

        /// <summary>
        /// Category names to hide.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        public UArray<UName> HideCategories
        {
            get => _HideCategories;
            set => _HideCategories = value;
        }

        /// <summary>
        /// Category names that should not be sorted.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        [BuildGeneration(BuildGeneration.UE3)]
        public UArray<UName> DontSortCategories
        {
            get => _DontSortCategories;
            set => _DontSortCategories = value;
        }

        /// <summary>
        /// Category names to auto-expand.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        [BuildGeneration(BuildGeneration.UE3)]
        public UArray<UName> AutoExpandCategories
        {
            get => _AutoExpandCategories;
            set => _AutoExpandCategories = value;
        }

        /// <summary>
        /// Category names to auto-collapse.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        [BuildGeneration(BuildGeneration.UE3)]
        public UArray<UName> AutoCollapseCategories
        {
            get => _AutoCollapseCategories;
            set => _AutoCollapseCategories = value;
        }

        /// <summary>
        /// Class group names that the class is a member of.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        [BuildGeneration(BuildGeneration.UE3)]
        public UArray<UName> ClassGroups
        {
            get => _ClassGroups;
            set => _ClassGroups = value;
        }

        /// <summary>
        /// Interfaces that the class has implemented.
        /// 
        /// Will be null if not deserialized.
        /// </summary>
        [BuildGeneration(BuildGeneration.UE3)]
        public UArray<ImplementedInterface> ImplementedInterfaces
        {
            get => _ImplementedInterfaces;
            set => _ImplementedInterfaces = value;
        }

        [Obsolete("Use ImplementedInterfaces", true)] public UArray<UObject>? Vengeance_Implements;
        private UnrealFlags<ClassFlag> _ClassFlags;
        private UGuid _ClassGuid;
        private UArray<Dependency> _ClassDependencies;
        private UArray<UName> _PackageImportNames;
        private UArray<ImplementedInterface> _ImplementedInterfaces;
        private UArray<UName> _ClassGroups;
        private UArray<UName> _AutoCollapseCategories;
        private UArray<UName> _AutoExpandCategories;
        private UArray<UName> _DontSortCategories;
        private UArray<UName> _HideCategories;
        private UMap<UName, UObject> _ComponentDefaultObjectMap;

        #endregion

        public IEnumerable<UState> States => EnumerateFields<UState>();

        #region Constructors

        // TODO: Clean this mess up...
        protected override void Deserialize()
        {
#if UNREAL2 || DEVASTATION
            if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Unreal2 ||
                _Buffer.Build == UnrealPackage.GameBuild.BuildName.Devastation)
            {
                _Buffer.ReadArray(out UArray<UObject> u2NetProperties);
                Record(nameof(u2NetProperties), u2NetProperties);
            }
#endif
            base.Deserialize();
#if VENGEANCE
            if (_Buffer.Build == BuildGeneration.Vengeance &&
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
            if (_Buffer.Build == BuildGeneration.AGP)
            {
                uint unknownUInt32 = _Buffer.ReadUInt32();
                Record("Unknown:AA2", unknownUInt32);
            }
#endif
#if UE4
            if (_Buffer.UE4Version > 0)
            {
                _Buffer.ReadMap(out UMap<UName, UFunction> funcMap);
                FuncMap = funcMap;
                Record(nameof(FuncMap), FuncMap);
            }
#endif
            _Buffer.Read(out _ClassFlags);
            Record(nameof(ClassFlags), _ClassFlags);
#if ROCKETLEAGUE
            if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                _Buffer.LicenseeVersion >= 1)
            {
                uint v194 = _Buffer.ReadUInt32();
                Record(nameof(v194), v194);
            }
#endif
#if SPELLBORN
            if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
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
#if SPELLBORN || LEAD || ADVENT
                if (
                    Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn ||
                    Package.Build == BuildGeneration.Lead ||
                    (Package.Build == UnrealPackage.GameBuild.BuildName.Advent && _Buffer.Version >= 133)
                )
                {
                    // Deprecated with v139 (in Spellborn and UC2 (UE2X), and Advent Rising (v133))
                    goto skipClassGuid;
                }
#endif
                _Buffer.ReadStruct(out _ClassGuid);
                Record(nameof(ClassGuid), _ClassGuid);
            }
        skipClassGuid:
#if R6
            // No version check
            if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.R6Vegas)
            {
                _Buffer.ReadArray(out UArray<UName> v100);
                Record(nameof(v100), v100);
            }
#endif
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.ClassDependenciesDeprecated)
            {
                _Buffer.ReadArray(out _ClassDependencies);
                Record(nameof(ClassDependencies), ClassDependencies);
            }

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.PackageImportsDeprecated)
            {
                _Buffer.ReadArray(out _PackageImportNames);
                Record(nameof(PackageImportNames), PackageImportNames);
            }

        serializeWithin:
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.Release62)
            {
                ClassWithin = _Buffer.ReadObject<UClass>();
                Record(nameof(ClassWithin), ClassWithin);

                ClassConfigName = _Buffer.ReadName();
                Record(nameof(ClassConfigName), ClassConfigName);
#if DNF
                if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                    _Buffer.Version >= 102)
                {
                    _Buffer.Read(out _HideCategories);
                    Record(nameof(HideCategories), HideCategories);

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
                                                || _Buffer.Build == UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                                || _Buffer.Build == BuildGeneration.HMS
#endif
                    ;

                if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedHideCategoriesToUClass)
                {
                    // FIXME: Clean up
                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.DisplacedHideCategories
                        && isHideCategoriesOldOrder
                        && !Package.IsConsoleCooked()
                        && !_Buffer.Build.Flags.HasFlag(BuildFlags.XenonCooked)
                        && _Buffer.UE4Version < 117)
                    {
                        _Buffer.Read(out _HideCategories);
                        Record(nameof(HideCategories), HideCategories);
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
                        _Buffer.Read(out _ComponentDefaultObjectMap);
                        Record(nameof(ComponentDefaultObjectMap), ComponentDefaultObjectMap);
                    }

                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedInterfacesFeature &&
                        _Buffer.Version < (uint)PackageObjectLegacyVersion.InterfaceClassesDeprecated)
                    {
                        _Buffer.ReadArray(out UArray<UClass> interfaceClasses);
                        Record(nameof(interfaceClasses), interfaceClasses);

                        ImplementedInterfaces = new UArray<ImplementedInterface>(
                            interfaceClasses.Select(interfaceClass =>
                                new ImplementedInterface
                                {
                                    InterfaceClass = interfaceClass,
                                    // FIXME: Generate the property?
                                    VfTableProperty = null
                                }));
                    }

                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.InterfaceClassesDeprecated)
                    {
                        _Buffer.ReadArray(out _ImplementedInterfaces);
                        Record(nameof(ImplementedInterfaces), ImplementedInterfaces);
                    }
#if UE4
                    if (_Buffer.UE4Version > 0)
                    {
                        var classGeneratedBy = _Buffer.ReadObject();
                        Record(nameof(classGeneratedBy), classGeneratedBy);
                    }
#endif
#if AHIT
                    if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.AHIT && _Buffer.Version >= 878)
                    {
                        // AHIT auto-generates a list of unused function names for its optional interface functions.
                        // Seems to have been added in 878, during the modding beta between 1.Nov.17 and 6.Jan.18.
                        _Buffer.ReadArray(out UArray<UName> unusedOptionalInterfaceFunctions);
                        Record(nameof(unusedOptionalInterfaceFunctions), unusedOptionalInterfaceFunctions);
                    }
#endif
                    if (!Package.IsConsoleCooked() && !_Buffer.Build.Flags.HasFlag(BuildFlags.XenonCooked))
                    {
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedDontSortCategoriesToUClass &&
                            _Buffer.UE4Version < 113
#if TERA
                            && _Buffer.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                           )
                        {
                            _Buffer.Read(out _DontSortCategories);
                            Record(nameof(DontSortCategories), DontSortCategories);
                        }

                        // FIXME: Clean up
                        if (_Buffer.Version < (uint)PackageObjectLegacyVersion.DisplacedHideCategories || !isHideCategoriesOldOrder)
                        {
                            _Buffer.Read(out _HideCategories);
                            Record(nameof(HideCategories), HideCategories);
                        }
#if SPELLBORN
                        if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
                        {
                            uint replicationFlags = _Buffer.ReadUInt32();
                            Record(nameof(replicationFlags), replicationFlags);
                        }
#endif
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedAutoExpandCategoriesToUClass)
                        {
                            // 490:GoW1, 576:CrimeCraft
                            if (!HasClassFlag(ClassFlag.CollapseCategories)
                                || _Buffer.Version <= vHideCategoriesOldOrder || _Buffer.Version >= 576)
                                _Buffer.Read(out _AutoExpandCategories);
                        }
#if TRANSFORMERS
                        if (_Buffer.Build == BuildGeneration.HMS)
                        {
                            _Buffer.ReadArray(out UArray<UObject> hmsConstructors);
                            Record(nameof(hmsConstructors), hmsConstructors);
                        }
#endif
                        // FIXME: Wrong version, no version checks found in games that DO have checks for version 600+
                        if (_Buffer.Version > 670
#if BORDERLANDS
                            || _Buffer.Build == UnrealPackage.GameBuild.BuildName.Borderlands
#endif
                           )
                        {
                            _Buffer.Read(out _AutoCollapseCategories);
                            Record(nameof(AutoCollapseCategories), AutoCollapseCategories);
                        }
#if BATMAN
                        // Only attested in bm4 with no version check.
                        if (_Buffer.Build == BuildGeneration.RSS &&
                            _Buffer.Build == UnrealPackage.GameBuild.BuildName.Batman4)
                        {
                            _Buffer.ReadArray(out UArray<UName> bm4_v198);
                            Record(nameof(bm4_v198), bm4_v198);
                        }
#endif
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.ForceScriptOrderAddedToUClass
#if BIOSHOCK
                            // Partially upgraded
                            && _Buffer.Build != UnrealPackage.GameBuild.BuildName.Bioshock_Infinite
#endif
                           )
                        {
                            // bForceScriptOrder
                            ForceScriptOrder = _Buffer.ReadBool();
                            Record(nameof(ForceScriptOrder), ForceScriptOrder);
                        }
#if DD2
                        // DD2 doesn't use a LicenseeVersion, maybe a merged standard feature (bForceScriptOrder?).
                        if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.DD2 && _Buffer.Version >= 688)
                        {
                            int dd2UnkInt32 = _Buffer.ReadInt32();
                            Record(nameof(dd2UnkInt32), dd2UnkInt32);
                        }
#endif
#if DISHONORED
                        if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                        {
                            var unknownName = _Buffer.ReadName();
                            Record("Unknown:Dishonored", unknownName);
                        }
#endif
#if BATTLEBORN
                        if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
                        {
                            // Usually 0x03
                            byte unknownByte = _Buffer.ReadByte();
                            Record("Unknown:Battleborn", unknownByte);

                            NativeHeaderName = _Buffer.ReadString();
                            Record(nameof(NativeHeaderName), NativeHeaderName);

                            // not verified
                            _Buffer.Read(out _ClassGroups);
                            Record(nameof(ClassGroups), ClassGroups);

                            goto skipClassGroups;
                        }
#endif
#if DISHONORED
                        if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                        {
                            NativeHeaderName = _Buffer.ReadString();
                            Record(nameof(NativeHeaderName), NativeHeaderName);

                            goto skipClassGroups;
                        }
#endif
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedClassGroupsToUClass)
                        {
                            _Buffer.Read(out _ClassGroups);
                            Record(nameof(ClassGroups), ClassGroups);
                        }

                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedNativeClassNameToUClass)
                        {
                            NativeHeaderName = _Buffer.ReadString();
                            Record(nameof(NativeHeaderName), NativeHeaderName);
                        }

                    skipClassGroups:;

                        // FIXME: Found first in(V:655, DLLBind?), Definitely not in APB and GoW 2
                        // TODO: Corrigate Version
                        if (_Buffer.Version > 575 && _Buffer.Version < 673
#if TERA
                                                  && _Buffer.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                                  && _Buffer.Build != BuildGeneration.HMS
#endif
#if BORDERLANDS
                                                  && _Buffer.Build != UnrealPackage.GameBuild.BuildName.Borderlands
#endif
                           )
                        {
                            int unknownInt32 = _Buffer.ReadInt32();
                            Record("Unknown", unknownInt32);
                        }
#if SINGULARITY
                        if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Singularity)
                        {
                            _Buffer.Skip(8);
                            _Buffer.ConformRecordPosition();
                        }
#endif
                    }
#if BATMAN
                    if (_Buffer.Build == BuildGeneration.RSS)
                    {
                        if (_Buffer.LicenseeVersion >= 95)
                        {
                            int bm_v174 = _Buffer.ReadInt32();
                            Record(nameof(bm_v174), bm_v174);
                        }
                    }
#endif
#if ROCKETLEAGUE
                    if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
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
                        DLLBindName = _Buffer.ReadName();
                        Record(nameof(DLLBindName), DLLBindName);
                    }
#if MASS_EFFECT
                    if (_Buffer.Build == BuildGeneration.SFX)
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
                    if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.RememberMe)
                    {
                        var unknownName = _Buffer.ReadName();
                        Record("Unknown:RememberMe", unknownName);
                    }
#endif
#if DISHONORED
                    if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                    {
                        _Buffer.Read(out _ClassGroups);
                        Record(nameof(ClassGroups), ClassGroups);
                    }
#endif
#if BORDERLANDS2 || BATTLEBORN
                    if ((_Buffer.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                         _Buffer.Build == UnrealPackage.GameBuild.BuildName.Battleborn) &&
                        _Buffer.LicenseeVersion >= 45
                       )
                    {
                        _Buffer.Read(out byte v1cc); // usually 0x01, sometimes 0x02?
                        _Buffer.Record("v1cc", v1cc);
                    }
#endif
                }
            }
#if UNDYING
            if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                _Buffer.Version >= 70)
            {
                _Buffer.Read(out uint classCRC); // v4a8
                Record(nameof(classCRC), classCRC);
            }
#endif
#if THIEF_DS || DeusEx_IW
            if (_Buffer.Build == BuildGeneration.Flesh)
            {
                string thiefClassVisibleName = _Buffer.ReadString();
                Record(nameof(thiefClassVisibleName), thiefClassVisibleName);

                // Restore the human-readable name if possible
                if (!string.IsNullOrEmpty(thiefClassVisibleName)
                    && _Buffer.Build == UnrealPackage.GameBuild.BuildName.Thief_DS)
                {
                    Name = new UName(thiefClassVisibleName);
                }
            }
#endif
#if VENGEANCE
            if (_Buffer.Build == BuildGeneration.Vengeance)
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
                    _Buffer.ReadArray(out UArray<UClass> interfaceClasses);
                    Record(nameof(interfaceClasses), interfaceClasses);

                    ImplementedInterfaces = new UArray<ImplementedInterface>(
                        interfaceClasses.Select(interfaceClass =>
                            new ImplementedInterface
                            {
                                InterfaceClass = interfaceClass,
                                // FIXME: Generate the property?
                                VfTableProperty = null
                            }));
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
            if (_Buffer.Build == UnrealPackage.GameBuild.BuildName.RocketLeague)
            {
                // StateMap; Seems to keep track of all declared states in the class.
                _Buffer.Read(out UMap<UName, UObject> v368);
                Record(nameof(v368), v368);
            }
#endif
        }

        #endregion

        #region Methods

        [Obsolete("Use ClassFlags directly")]
        public bool HasClassFlag(ClassFlags flag)
        {
            return (ClassFlags & (uint)flag) != 0;
        }

        [Obsolete("Use ClassFlags directly")]
        public bool HasClassFlag(uint flag)
        {
            return (ClassFlags & flag) != 0;
        }

        internal bool HasClassFlag(ClassFlag flagIndex)
        {
            return ClassFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(ClassFlag)], flagIndex);
        }

        public bool IsClassInterface()
        {
#if VENGEANCE
            if (HasClassFlag(Flags.ClassFlags.VG_Interface))
            {
                return true;
            }
#endif
            return HasClassFlag(ClassFlag.Interface);
        }

        public bool IsClassWithin()
        {
            return ClassWithin != null && ClassWithin.Name != UnrealName.Object;
        }

        #endregion
    }
}
