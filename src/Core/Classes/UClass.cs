using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using UELib.Branch;
using UELib.Flags;
using UELib.IO;
using UELib.ObjectModel.Annotations;
using UELib.Services;

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
        ///     The class flags for this class, reflecting various class specifiers as seen in a class declaration.
        ///</summary>
        [StreamRecord]
        public UnrealFlags<ClassFlag> ClassFlags { get; set; }

        /// <summary>
        ///     The guid for this class, set using the class specifier 'guid' e.g.
        ///     <example>class MyClass extends Object guid(A,B,C,D);</example>
        ///     Possibly used to identify the class for use in COM (Component Object Model)
        /// </summary>
        [StreamRecord]
        public UGuid ClassGuid { get; set; }

        /// <summary>
        ///     The class that this class belongs within, e.g.
        ///     <example>class MyClass extends Object within PlayerController;</example>
        ///     Presume
        ///     <value>Class'Core.Object'</value>
        ///     if null.
        /// </summary>
        [StreamRecord]
        public UClass? ClassWithin { get; set; }

        /// <summary>
        ///     Name of the configuration file that this class is associated with, e.g.
        ///     <example>class MyClass extends Object config(MyConfig);</example>
        ///     Presume
        ///     <value>'System'</value>
        ///     if 'None'.
        /// </summary>
        [StreamRecord]
        public UName ClassConfigName { get; set; } = UnrealName.None;

        /// <summary>
        ///     The DLL name to bind this class to e.g.
        ///     <example>class MyClass extends Object DLLBind(MyDLL);</example>
        /// </summary>
        [StreamRecord]
        public UName DLLBindName { get; set; } = UnrealName.None;

        /// <summary>
        ///     The native header file name of this class, e.g.
        ///     <example>class MaterialInterface extends Surface native(Material);</example>
        ///     will give "Engine/Inc/EngineMaterialClasses.h".
        /// </summary>
        [StreamRecord]
        public string NativeHeaderName { get; set; } = string.Empty;

        /// <summary>
        ///     Whether this class properties should be forced to appear in script order, e.g.
        ///     <example>class MyClass extends Object forcescriptorder(true);</example>
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord]
        public bool? ForceScriptOrder { get; set; }

        /// <summary>
        ///     Class dependencies that this class is dependent on; Including both Imports and Exports.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord]
        public UArray<Dependency>? ClassDependencies { get; set; }

        /// <summary>
        ///     Names of packages that this class imports.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord]
        public UArray<UName>? PackageImportNames { get; set; }

        /// <summary>
        ///     A map of default objects for the components that are instantiated by this class.
        ///     The component objects are expected to be derivatives of class <see cref="UComponent" />,
        ///     however not all UComponent objects are known to UELib, so manual safe casting is required.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord, BuildGeneration(BuildGeneration.UE3)]
        public UMap<UName, UObject>? ComponentDefaultObjectMap { get; set; }

        /// <summary>
        ///     Interfaces that this class has implemented.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord, BuildGeneration(BuildGeneration.UE3)]
        public UArray<ImplementedInterface>? ImplementedInterfaces { get; set; }

        /// <summary>
        ///     Category names that should be hidden in the editor.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord]
        public UArray<UName>? HideCategories { get; set; }

        /// <summary>
        ///     Category names that should not be sorted in the editor.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord, BuildGeneration(BuildGeneration.UE3)]
        public UArray<UName>? DontSortCategories { get; set; }

        /// <summary>
        ///     Category names that should be auto-expanded in the editor.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord, BuildGeneration(BuildGeneration.UE3)]
        public UArray<UName>? AutoExpandCategories { get; set; }

        /// <summary>
        ///     Category names that should be auto-collapsed in the editor.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord, BuildGeneration(BuildGeneration.UE3)]
        public UArray<UName>? AutoCollapseCategories { get; set; }

        /// <summary>
        ///     Class group names that this class is a member of.
        ///     Will be null if not deserialized.
        /// </summary>
        [StreamRecord, BuildGeneration(BuildGeneration.UE3)]
        public UArray<UName>? ClassGroups { get; set; }

        [StreamRecord, BuildGeneration(BuildGeneration.UE4)]
        public UObject? ClassGeneratedBy { get; set; }

        [StreamRecord, BuildGeneration(BuildGeneration.UE4)]
        public bool? IsCooked { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
#if UNREAL2 || DEVASTATION
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2 ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Devastation)
            {
                stream.ReadArray(out UArray<UObject> u2NetProperties);
                stream.Record(nameof(u2NetProperties), u2NetProperties);
            }
#endif
            base.Deserialize(stream);
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance &&
                stream.LicenseeVersion >= 36)
            {
                var header = (2, 0);
                VengeanceDeserializeHeader(stream, ref header);
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.Release62)
            {
                int classRecordSize = stream.ReadInt32();
                stream.Record(nameof(classRecordSize), classRecordSize);
            }
#if AA2
            if (stream.Build == BuildGeneration.AGP &&
                stream.LicenseeVersion >= 8)
            {
                // hard-coded 0x9da3 (AAO 2.6)
                uint v94 = stream.ReadUInt32();
                stream.Record(nameof(v94), v94);
                if (v94 != 0) LibServices.Debug(GetReferencePath() + ":v94", v94);
            }
#endif
#if UE4
            if (stream.IsUE4())
            {
                FuncMap = stream.ReadMap(stream.ReadName, stream.ReadObject<UFunction>);
                stream.Record(nameof(FuncMap), FuncMap);
            }
#endif
            ClassFlags = stream.ReadFlags32<ClassFlag>();
            stream.Record(nameof(ClassFlags), ClassFlags);
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 1)
            {
                uint v194 = stream.ReadUInt32();
                stream.Record(nameof(v194), v194);
                if (v194 != 0) LibServices.Debug(GetReferencePath() + ":v194", v194);
            }
#endif
#if SPELLBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
            {
                goto skipClassGuid;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ClassGuidDeprecated)
            {
                if (stream.Version < (uint)PackageObjectLegacyVersion.ClassPlatformFlagsDeprecated)
                {
                    // platform specifier like 'platform(PC, Linux)'.
                    byte classPlatformFlags = stream.ReadByte();
                    stream.Record(nameof(classPlatformFlags), classPlatformFlags);
                    if (classPlatformFlags != 0) LibServices.Debug(GetReferencePath() + ":classPlatformFlags", classPlatformFlags);
                }
            }
            else
            {
#if SPELLBORN || LEAD || ADVENT
                if (
                    stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn ||
                    stream.Build == BuildGeneration.Lead ||
                    (stream.Build == UnrealPackage.GameBuild.BuildName.Advent && stream.Version >= 133)
                )
                {
                    // Deprecated with v139 (in Spellborn and UC2 (UE2X), and Advent Rising (v133))
                    goto skipClassGuid;
                }
#endif
                ClassGuid = stream.ReadStruct<UGuid>();
                stream.Record(nameof(ClassGuid), ClassGuid);
            }

        skipClassGuid:
#if R6
            // No version check
            if (stream.Build == UnrealPackage.GameBuild.BuildName.R6Vegas)
            {
                stream.ReadArray(out UArray<UName> v100);
                stream.Record(nameof(v100), v100);
                if (v100.Count != 0) LibServices.Debug(GetReferencePath() + ":v100", v100);
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.ClassDependenciesDeprecated)
            {
                ClassDependencies = stream.ReadArray<Dependency>();
                stream.Record(nameof(ClassDependencies), ClassDependencies);
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.PackageImportsDeprecated)
            {
                PackageImportNames = stream.ReadNameArray();
                stream.Record(nameof(PackageImportNames), PackageImportNames);
            }

        serializeWithin:
            if (stream.Version >= (uint)PackageObjectLegacyVersion.Release62)
            {
                ClassWithin = stream.ReadObject<UClass?>(); // ?? UObject
                stream.Record(nameof(ClassWithin), ClassWithin);

                ClassConfigName = stream.ReadName(); // ?? "System"
                stream.Record(nameof(ClassConfigName), ClassConfigName);
            }
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                stream.Version >= 102)
            {
                HideCategories = stream.ReadNameArray();
                stream.Record(nameof(HideCategories), HideCategories);

                if (stream.Version >= 137)
                {
                    stream.ReadArray(out UArray<string> dnfTags);
                    stream.Record(nameof(dnfTags), dnfTags);
                }

                if (stream.Version >= 113)
                {
                    // Unknown purpose, used to set a global variable to 0 (GIsATablesInitialized_exref) if it reads 0.
                    bool dnfBool = stream.ReadBool();
                    stream.Record(nameof(dnfBool), dnfBool);

                    // FBitArray data, not sure if this behavior is correct, always 0.
                    int dnfBitArrayLength = stream.ReadInt32();
                    stream.Skip(dnfBitArrayLength);
                    stream.Record(nameof(dnfBitArrayLength), dnfBitArrayLength);
                }

                goto scriptProperties;
            }
#endif
            const int vHideCategoriesOldOrder = 539;
            bool isHideCategoriesOldOrder = stream.Version <= vHideCategoriesOldOrder
#if TERA
                                            || stream.Build == UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                            || stream.Build == BuildGeneration.HMS
#endif
                ;

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedHideCategoriesToUClass)
            {
                // FIXME: Clean up
                if (stream.Version >= (uint)PackageObjectLegacyVersion.DisplacedHideCategories
                    && isHideCategoriesOldOrder
                    && stream.ContainsEditorOnlyData()
                    && !stream.Build.Flags.HasFlag(BuildFlags.XenonCooked)
                    && stream.UE4Version < 117)
                {
                    HideCategories = stream.ReadNameArray();
                    stream.Record(nameof(HideCategories), HideCategories);
                }
            }

            // FIXME: >= version
            if (stream.Version >= 178 &&
                stream.Version < (uint)PackageObjectLegacyVersion.ComponentClassBridgeMapDeprecated)
            {
                stream.ReadMap(out UMap<UObject, UName> componentClassBridgeMap);
                stream.Record(nameof(componentClassBridgeMap), componentClassBridgeMap);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedComponentTemplatesToUClass &&
                stream.Version < (uint)PackageObjectLegacyVersion.ComponentTemplatesDeprecated)
            {
                stream.ReadArray(out UArray<UObject> componentTemplates);
                stream.Record(nameof(componentTemplates), componentTemplates);
            }

            // FIXME: >= version
            if (stream.Version >= 178 && stream.UE4Version < 118)
            {
                ComponentDefaultObjectMap = stream.ReadMap(stream.ReadName, stream.ReadObject<UObject>);
                stream.Record(nameof(ComponentDefaultObjectMap), ComponentDefaultObjectMap);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedInterfacesFeature &&
                stream.Version < (uint)PackageObjectLegacyVersion.InterfaceClassesDeprecated)
            {
                stream.ReadArray(out UArray<UClass> interfaceClasses);
                stream.Record(nameof(interfaceClasses), interfaceClasses);

                ImplementedInterfaces = new UArray<ImplementedInterface>(
                    interfaceClasses.Select(interfaceClass => new ImplementedInterface
                    {
                        InterfaceClass = interfaceClass,
                        // FIXME: Generate the property?
                        VfTableProperty = null
                    }));
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.InterfaceClassesDeprecated)
            {
                ImplementedInterfaces = stream.ReadArray<ImplementedInterface>();
                stream.Record(nameof(ImplementedInterfaces), ImplementedInterfaces);
            }
#if UE4
            if (stream.IsUE4())
            {
                ClassGeneratedBy = stream.ReadObject();
                stream.Record(nameof(ClassGeneratedBy), ClassGeneratedBy);
            }
#endif
#if AHIT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.AHIT && stream.Version >= 878)
            {
                // AHIT auto-generates a list of unused function names for its optional interface functions.
                // Seems to have been added in 878, during the modding beta between 1.Nov.17 and 6.Jan.18.
                stream.ReadArray(out UArray<UName> unusedOptionalInterfaceFunctions);
                stream.Record(nameof(unusedOptionalInterfaceFunctions), unusedOptionalInterfaceFunctions);
            }
#endif
            if (stream.ContainsEditorOnlyData() && !stream.Build.Flags.HasFlag(BuildFlags.XenonCooked))
            {
                if (stream is
                    {
                        Version: >= (uint)PackageObjectLegacyVersion.AddedDontSortCategoriesToUClass,
                        UE4Version: < 113
                    }
#if TERA
                    && stream.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                   )
                {
                    DontSortCategories = stream.ReadNameArray();
                    stream.Record(nameof(DontSortCategories), DontSortCategories);
                }

                // FIXME: Clean up
                if ((stream.Version >= (uint)PackageObjectLegacyVersion.AddedHideCategoriesToUClass &&
                     stream.Version < (uint)PackageObjectLegacyVersion.DisplacedHideCategories) ||
                    !isHideCategoriesOldOrder)
                {
                    HideCategories = stream.ReadNameArray();
                    stream.Record(nameof(HideCategories), HideCategories);
                }
#if SPELLBORN
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
                {
                    uint replicationFlags = stream.ReadUInt32();
                    stream.Record(nameof(replicationFlags), replicationFlags);
                }
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedAutoExpandCategoriesToUClass)
                {
                    // FIXME: 490:GoW1, 576:CrimeCraft
                    if (!HasClassFlag(ClassFlag.CollapseCategories)
                        || stream.Version <= vHideCategoriesOldOrder
                        || stream.Version >= 576)
                    {
                        AutoExpandCategories = stream.ReadNameArray();
                        stream.Record(nameof(AutoExpandCategories), AutoExpandCategories);
                    }
                }
#if TRANSFORMERS
                if (stream.Build == BuildGeneration.HMS)
                {
                    stream.ReadArray(out UArray<UObject> hmsConstructors);
                    stream.Record(nameof(hmsConstructors), hmsConstructors);
                }
#endif
                // FIXME: Wrong version, no version checks found in games that DO have checks for version 600+
                if (stream.Version > 670
#if BORDERLANDS
                    || stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands
#endif
                   )
                {
                    AutoCollapseCategories = stream.ReadNameArray();
                    stream.Record(nameof(AutoCollapseCategories), AutoCollapseCategories);
                }
#if BATMAN
                // Only attested in bm4 with no version check.
                if (stream.Build == BuildGeneration.RSS &&
                    stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
                {
                    stream.ReadArray(out UArray<UName> bm4_v198);
                    stream.Record(nameof(bm4_v198), bm4_v198);
                    if (bm4_v198.Count != 0) LibServices.Debug(GetReferencePath() + ":bm4_v198", bm4_v198);
                }
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.ForceScriptOrderAddedToUClass
#if BIOSHOCK
                    // Partially upgraded
                    && stream.Build != UnrealPackage.GameBuild.BuildName.Bioshock_Infinite
#endif
                   )
                {
                    // bForceScriptOrder
                    ForceScriptOrder = stream.ReadBool();
                    stream.Record(nameof(ForceScriptOrder), ForceScriptOrder);
                }
#if DD2
                // DD2 doesn't use a LicenseeVersion, maybe a merged standard feature (bForceScriptOrder?).
                if (stream.Build == UnrealPackage.GameBuild.BuildName.DD2 && stream.Version >= 688)
                {
                    int dd2UnkInt32 = stream.ReadInt32();
                    stream.Record(nameof(dd2UnkInt32), dd2UnkInt32);
                    if (dd2UnkInt32 != 0) LibServices.Debug(GetReferencePath() + ":dd2UnkInt32", dd2UnkInt32);
                }
#endif
#if BATTLEBORN
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
                {
                    // Usually 0x03
                    byte unknownByte = stream.ReadByte();
                    stream.Record("Unknown:Battleborn", unknownByte);
                    if (unknownByte != 0) LibServices.Debug(GetReferencePath() + ":unknownByte", unknownByte);

                    NativeHeaderName = stream.ReadString();
                    stream.Record(nameof(NativeHeaderName), NativeHeaderName);

                    // not verified
                    ClassGroups = stream.ReadNameArray();
                    stream.Record(nameof(ClassGroups), ClassGroups);

                    goto skipClassGroups;
                }
#endif
#if DISHONORED
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                {
                    var unknownName = stream.ReadName();
                    stream.Record("Unknown:Dishonored", unknownName);
                    if (unknownName.IsNone() == false)
                    {
                        LibServices.Debug(GetReferencePath() + ":unknownName", unknownName);
                    }

                    NativeHeaderName = stream.ReadString();
                    stream.Record(nameof(NativeHeaderName), NativeHeaderName);

                    goto skipClassGroups;
                }
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedClassGroupsToUClass)
                {
                    ClassGroups = stream.ReadNameArray();
                    stream.Record(nameof(ClassGroups), ClassGroups);
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedNativeClassNameToUClass)
                {
                    NativeHeaderName = stream.ReadString();
                    stream.Record(nameof(NativeHeaderName), NativeHeaderName);
                }

            skipClassGroups:;

                // FIXME: Attested in many builds between 575 and 673 (UDK-2009), coincides with FPropertyTag bool change.
                // This is apparently 'AutoCollapseCategories' (Confirmed to exist in UDK-2009 which has a lower version than the one that we currently have)
                // However, need to run full tests on this for the special case 'builds'
                if (stream.Version > 575 && stream.Version < 673
#if TERA
                                         && stream.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                         && stream.Build != BuildGeneration.HMS
#endif
#if BORDERLANDS
                                         && stream.Build != UnrealPackage.GameBuild.BuildName.Borderlands
#endif
                   )
                {
                    AutoCollapseCategories = stream.ReadNameArray();
                }
#if SINGULARITY
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Singularity)
                {
                    stream.Skip(8);
                    stream.ConformRecordPosition();
                }
#endif
            }
#if BATMAN
            if (stream.Build == BuildGeneration.RSS)
            {
                if (stream.LicenseeVersion >= 95)
                {
                    int bm_v174 = stream.ReadInt32();
                    stream.Record(nameof(bm_v174), bm_v174);
                    if (bm_v174 != 0) LibServices.Debug(GetReferencePath() + ":bm_v174", bm_v174);
                }
            }
#endif
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 21)
            {
                string v298 = stream.ReadString();
                stream.Record(nameof(v298), v298);
                if (v298 != string.Empty) LibServices.Debug(GetReferencePath() + ":v298", v298);

                int v2a8 = stream.ReadInt32();
                stream.Record(nameof(v2a8), v2a8);
                if (v2a8 != 0) LibServices.Debug(GetReferencePath() + ":v2a8", v2a8);

                stream.Read(out UArray<UName> v2b0);
                stream.Record(nameof(v2b0), v2b0);
                if (v2b0.Count != 0) LibServices.Debug(GetReferencePath() + ":v2b0", v2b0);
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDLLBindFeature &&
                stream.UE4Version < 117)
            {
                DLLBindName = stream.ReadName();
                stream.Record(nameof(DLLBindName), DLLBindName);
            }
#if MASS_EFFECT
            if (stream.Build == BuildGeneration.SFX)
            {
                if (stream.LicenseeVersion - 138u < 15)
                {
                    stream.Read(out int v40);
                    stream.Record(nameof(v40), v40);
                    if (v40 != 0) LibServices.Debug(GetReferencePath() + ":v40", v40);
                }

                if (stream.LicenseeVersion >= 139)
                {
                    stream.Read(out int v1ec);
                    stream.Record(nameof(v1ec), v1ec);
                    if (v1ec != 0) LibServices.Debug(GetReferencePath() + ":v1ec", v1ec);
                }
            }
#endif
#if REMEMBERME
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RememberMe)
            {
                var v160 = stream.ReadName();
                stream.Record(nameof(v160), v160);
                if (v160 != UnrealName.None) LibServices.Debug(GetReferencePath() + ":v160", v160);
            }
#endif
#if DISHONORED
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
            {
                ClassGroups = stream.ReadNameArray();
                stream.Record(nameof(ClassGroups), ClassGroups);
            }
#endif
#if BORDERLANDS2 || BATTLEBORN
            if ((stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                 stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn) &&
                stream.LicenseeVersion >= 45
               )
            {
                stream.Read(out byte v1cc); // usually 0x01, sometimes 0x02?
                stream.Record("v1cc", v1cc);
                if (v1cc != 0) LibServices.Debug(GetReferencePath() + ":v1cc", v1cc);
            }
#endif
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                stream.Version >= 70)
            {
                stream.Read(out uint classCRC); // v4a8
                stream.Record(nameof(classCRC), classCRC);
            }
#endif
#if THIEF_DS || DeusEx_IW
            if (stream.Build == BuildGeneration.Flesh)
            {
                string thiefClassVisibleName = stream.ReadString();
                stream.Record(nameof(thiefClassVisibleName), thiefClassVisibleName);

                // Restore the human-readable name if possible
                if (!string.IsNullOrEmpty(thiefClassVisibleName)
                    && stream.Build == UnrealPackage.GameBuild.BuildName.Thief_DS)
                {
                    Name = new UName(thiefClassVisibleName);
                }
            }
#endif
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance)
            {
                if (stream.LicenseeVersion >= 2)
                {
                    ulong unkInt64 = stream.ReadUInt64();
                    stream.Record("Unknown:Vengeance", unkInt64);
                }

                if (stream.LicenseeVersion >= 3)
                {
                    ulong unkInt64 = stream.ReadUInt64();
                    stream.Record("Unknown:Vengeance", unkInt64);
                }

                if (stream.LicenseeVersion >= 2)
                {
                    string vengeanceDefaultPropertiesText = stream.ReadString();
                    stream.Record(nameof(vengeanceDefaultPropertiesText), vengeanceDefaultPropertiesText);
                }

                if (stream.LicenseeVersion >= 6)
                {
                    string vengeanceClassFilePath = stream.ReadString();
                    stream.Record(nameof(vengeanceClassFilePath), vengeanceClassFilePath);
                }

                if (stream.LicenseeVersion >= 12)
                {
                    stream.ReadArray(out UArray<UName> names);
                    stream.Record("Unknown:Vengeance", names);
                }

                if (stream.LicenseeVersion >= 15)
                {
                    stream.ReadArray(out UArray<UClass> interfaceClasses);
                    stream.Record(nameof(interfaceClasses), interfaceClasses);

                    ImplementedInterfaces = new UArray<ImplementedInterface>(
                        interfaceClasses.Select(interfaceClass => new ImplementedInterface
                        {
                            InterfaceClass = interfaceClass,
                            // FIXME: Generate the property?
                            VfTableProperty = null
                        }));
                }

                if (stream.LicenseeVersion >= 20)
                {
                    UArray<UObject> unk;
                    stream.ReadArray(out unk);
                    stream.Record("Unknown:Vengeance", unk);
                }

                if (stream.LicenseeVersion >= 32)
                {
                    UArray<UObject> unk;
                    stream.ReadArray(out unk);
                    stream.Record("Unknown:Vengeance", unk);
                }

                if (stream.LicenseeVersion >= 28)
                {
                    UArray<UName> unk;
                    stream.ReadArray(out unk);
                    stream.Record("Unknown:Vengeance", unk);
                }

                if (stream.LicenseeVersion >= 30)
                {
                    int unkInt32A = stream.ReadInt32();
                    stream.Record("Unknown:Vengeance", unkInt32A);
                    int unkInt32B = stream.ReadInt32();
                    stream.Record("Unknown:Vengeance", unkInt32B);

                    // Lazy array?
                    int skipSize = stream.ReadInt32();
                    stream.Record("Unknown:Vengeance", skipSize);
                    // FIXME: Couldn't RE this code
                    int b = stream.ReadLength();
                    Debug.Assert(b == 0, "Unknown data was not zero!");
                    stream.Record("Unknown:Vengeance", b);
                }
            }
#endif
#if UE4
            if (stream.IsUE4())
            {
                string dummy = stream.ReadName();
                stream.Record(nameof(dummy), dummy);

                IsCooked = stream.ReadBool();
                stream.Record(nameof(IsCooked), IsCooked);
            }
#endif
        scriptProperties:
            if (stream.Version >= (uint)PackageObjectLegacyVersion.DisplacedScriptPropertiesWithClassDefaultObject)
            {
                // Default__ClassName
                Default = stream.ReadObject();
                stream.Record(nameof(Default), Default);
            }
            else
            {
                Default = this;
                DefaultProperties = DeserializeScriptProperties(stream, this);
                Properties = DefaultProperties;
            }
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague)
            {
                // StateMap; Seems to keep track of all declared states in the class.
                stream.Read(out UMap<UName, UObject> v368);
                stream.Record(nameof(v368), v368);
                if (v368.Count != 0) LibServices.Debug(GetReferencePath() + ":v368", v368);
            }
#endif
            // version < 57
            //stream.ReadName();
            //stream.ReadName();
        }

        public override void Serialize(IUnrealStream stream)
        {
#if UNREAL2 || DEVASTATION
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2 ||
                stream.Build == UnrealPackage.GameBuild.BuildName.Devastation)
            {
                var netProperties = EnumerateFields<UProperty>()
                    .Where(property => property.HasPropertyFlag(PropertyFlag.Net));
                stream.WriteArray(new UArray<UProperty>(netProperties));
            }
#endif
            base.Serialize(stream);
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance &&
                stream.LicenseeVersion >= 36)
            {
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.Release62)
            {
                throw new NotSupportedException("This package version is not supported!");
                //stream.Write(int classRecordSize);
            }
#if AA2
            if (stream.Build == BuildGeneration.AGP &&
                stream.LicenseeVersion >= 8)
            {
                // hard-coded 0x9da3 (AAO 2.6)
                stream.Write((uint)0x9da3);
            }
#endif
#if UE4
            if (stream.IsUE4())
            {
                stream.WriteMap(FuncMap, key => stream.WriteName(key), stream.WriteObject);
            }
#endif
            stream.Write((uint)ClassFlags);
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 1)
            {
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
#if SPELLBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
            {
                goto skipClassGuid;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ClassGuidDeprecated)
            {
                if (stream.Version < (uint)PackageObjectLegacyVersion.ClassPlatformFlagsDeprecated)
                {
                    const byte classPlatformFlags = 0;
                    stream.Write(classPlatformFlags);
                }
            }
            else
            {
#if SPELLBORN || LEAD || ADVENT
                if (
                    stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn ||
                    stream.Build == BuildGeneration.Lead ||
                    (stream.Build == UnrealPackage.GameBuild.BuildName.Advent && stream.Version >= 133)
                )
                {
                    // Deprecated with v139 (in Spellborn and UC2 (UE2X), and Advent Rising (v133))
                    goto skipClassGuid;
                }
#endif
                stream.WriteStruct(ClassGuid);
            }

        skipClassGuid:
#if R6
            // No version check
            if (stream.Build == UnrealPackage.GameBuild.BuildName.R6Vegas)
            {
                throw new NotSupportedException("This package version is not supported!");
                //stream.WriteArray(UArray<UName> v100);
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.ClassDependenciesDeprecated)
            {
                stream.WriteArray(ClassDependencies);
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.PackageImportsDeprecated)
            {
                stream.WriteArray(PackageImportNames);
            }

        serializeWithin:
            if (stream.Version >= (uint)PackageObjectLegacyVersion.Release62)
            {
                stream.WriteObject(ClassWithin);
                stream.WriteName(ClassConfigName);
            }
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                stream.Version >= 102)
            {
                stream.WriteArray(HideCategories);

                if (stream.Version >= 137)
                {
                    throw new NotSupportedException("This package version is not supported!");
                    //stream.WriteArray(UArray<string> dnfTags);
                }

                if (stream.Version >= 113)
                {
                    // Unknown purpose, used to set a global variable to 0 (GIsATablesInitialized_exref) if it reads 0.
                    stream.Write(false);

                    // FBitArray data, not sure if this behavior is correct, always 0.
                    stream.Write(0);
                }

                goto scriptProperties;
            }
#endif
            const int vHideCategoriesOldOrder = 539;
            bool isHideCategoriesOldOrder = stream.Version <= vHideCategoriesOldOrder
#if TERA
                                            || stream.Build == UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                            || stream.Build == BuildGeneration.HMS
#endif
                ;

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedHideCategoriesToUClass)
            {
                // FIXME: Clean up
                if (stream.Version >= (uint)PackageObjectLegacyVersion.DisplacedHideCategories
                    && isHideCategoriesOldOrder
                    && stream.ContainsEditorOnlyData()
                    && !stream.Build.Flags.HasFlag(BuildFlags.XenonCooked)
                    && stream.UE4Version < 117)
                {
                    stream.WriteArray(HideCategories);
                }
            }

            // FIXME: >= version
            if (stream.Version >= 178 &&
                stream.Version < (uint)PackageObjectLegacyVersion.ComponentClassBridgeMapDeprecated)
            {
                throw new NotSupportedException("This package version is not supported!");
                //stream.WriteMap(UMap<UObject, UName> componentClassBridgeMap);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedComponentTemplatesToUClass &&
                stream.Version < (uint)PackageObjectLegacyVersion.ComponentTemplatesDeprecated)
            {
                throw new NotSupportedException("This package version is not supported!");
                //stream.WriteArray(UArray<UObject> componentTemplates);
            }

            // FIXME: >= version
            if (stream.Version >= 178 && stream.UE4Version < 118)
            {
                stream.WriteMap(ComponentDefaultObjectMap);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedInterfacesFeature &&
                stream.Version < (uint)PackageObjectLegacyVersion.InterfaceClassesDeprecated)
            {
                stream.WriteArray(ImplementedInterfaces != null
                    ? new UArray<UClass>(ImplementedInterfaces.Select(impl => impl.InterfaceClass))
                    : null);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.InterfaceClassesDeprecated)
            {
                stream.WriteArray(ImplementedInterfaces);
            }
#if UE4
            if (stream.IsUE4())
            {
                stream.WriteObject(ClassGeneratedBy);
            }
#endif
#if AHIT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.AHIT && stream.Version >= 878)
            {
                // AHIT auto-generates a list of unused function names for its optional interface functions.
                // Seems to have been added in 878, during the modding beta between 1.Nov.17 and 6.Jan.18.
                throw new NotSupportedException("This package version is not supported!");
                //stream.WriteArray(UArray<UName> unusedOptionalInterfaceFunctions);
            }
#endif
            if (stream.ContainsEditorOnlyData() && !stream.Build.Flags.HasFlag(BuildFlags.XenonCooked))
            {
                if (stream is
                    {
                        Version: >= (uint)PackageObjectLegacyVersion.AddedDontSortCategoriesToUClass,
                        UE4Version: < 113
                    }
#if TERA
                    && stream.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                   )
                {
                    stream.WriteArray(DontSortCategories);
                }

                // FIXME: Clean up
                if ((stream.Version >= (uint)PackageObjectLegacyVersion.AddedHideCategoriesToUClass &&
                     stream.Version < (uint)PackageObjectLegacyVersion.DisplacedHideCategories) ||
                    !isHideCategoriesOldOrder)
                {
                    stream.WriteArray(HideCategories);
                }
#if SPELLBORN
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
                {
                    throw new NotSupportedException("This package version is not supported!");
                    //stream.Write(uint replicationFlags);
                }
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedAutoExpandCategoriesToUClass)
                {
                    // FIXME: 490:GoW1, 576:CrimeCraft
                    if (!HasClassFlag(ClassFlag.CollapseCategories)
                        || stream.Version <= vHideCategoriesOldOrder
                        || stream.Version >= 576)
                    {
                        stream.WriteArray(AutoExpandCategories);
                    }
                }
#if TRANSFORMERS
                if (stream.Build == BuildGeneration.HMS)
                {
                    throw new NotSupportedException("This package version is not supported!");
                    //stream.WriteArray(UArray<UObject> hmsConstructors);
                }
#endif
                // FIXME: Wrong version, no version checks found in games that DO have checks for version 600+
                if (stream.Version > 670
#if BORDERLANDS
                    || stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands
#endif
                   )
                {
                    stream.WriteArray(AutoCollapseCategories);
                }
#if BATMAN
                // Only attested in bm4 with no version check.
                if (stream.Build == BuildGeneration.RSS &&
                    stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
                {
                    throw new NotSupportedException("This package version is not supported!");
                    //stream.WriteArray(out UArray<UName> bm4_v198);
                }
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.ForceScriptOrderAddedToUClass
#if BIOSHOCK
                    // Partially upgraded
                    && stream.Build != UnrealPackage.GameBuild.BuildName.Bioshock_Infinite
#endif
                   )
                {
                    // bForceScriptOrder
                    stream.Write(ForceScriptOrder ?? false);
                }
#if DD2
                // DD2 doesn't use a LicenseeVersion, maybe a merged standard feature (bForceScriptOrder?).
                if (stream.Build == UnrealPackage.GameBuild.BuildName.DD2 && stream.Version >= 688)
                {
                    throw new NotSupportedException("This package version is not supported!");
                    //stream.Write(int dd2UnkInt32);
                }
#endif
#if BATTLEBORN
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn)
                {
                    // Usually 0x03
                    stream.Write((byte)0x3);

                    stream.WriteString(NativeHeaderName);

                    // not verified
                    stream.WriteArray(ClassGroups);

                    goto skipClassGroups;
                }
#endif
#if DISHONORED
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
                {
                    stream.WriteName(UnrealName.None); // unknown value
                    stream.WriteString(NativeHeaderName);

                    goto skipClassGroups;
                }
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedClassGroupsToUClass)
                {
                    stream.WriteArray(ClassGroups);
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedNativeClassNameToUClass)
                {
                    stream.WriteString(NativeHeaderName);
                }

            skipClassGroups:;

                // FIXME: Attested in many builds between 575 and 673.
                if (stream.Version > 575 && stream.Version < 673
#if TERA
                                         && stream.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                         && stream.Build != BuildGeneration.HMS
#endif
#if BORDERLANDS
                                         && stream.Build != UnrealPackage.GameBuild.BuildName.Borderlands
#endif
                   )
                {
                    stream.WriteArray(AutoCollapseCategories);
                }
#if SINGULARITY
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Singularity)
                {
                    stream.Skip(8);
                    stream.ConformRecordPosition();
                }
#endif
            }
#if BATMAN
            if (stream.Build == BuildGeneration.RSS)
            {
                if (stream.LicenseeVersion >= 95)
                {
                    throw new NotSupportedException("This package version is not supported!");
                    //stream.Write(int bm_v174);
                }
            }
#endif
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 21)
            {
                throw new NotSupportedException("This package version is not supported!");
                //stream.WriteString(string v298);
                //stream.Write(int v2a8);
                //stream.WriteArray(UArray<UName> v2b0);
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDLLBindFeature &&
                stream.UE4Version < 117)
            {
                stream.WriteName(DLLBindName);
            }
#if MASS_EFFECT
            if (stream.Build == BuildGeneration.SFX)
            {
                if (stream.LicenseeVersion - 138u < 15)
                {
                    throw new NotSupportedException("This package version is not supported!");
                    //stream.Write(int v40);
                }

                if (stream.LicenseeVersion >= 139)
                {
                    throw new NotSupportedException("This package version is not supported!");
                    //stream.Write(int v1ec);
                }
            }
#endif
#if REMEMBERME
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RememberMe)
            {
                var v160 = UnrealName.None; // Never attested as anything but None.
                stream.WriteName(v160);
            }
#endif
#if DISHONORED
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Dishonored)
            {
                stream.WriteArray(ClassGroups);
            }
#endif
#if BORDERLANDS2 || BATTLEBORN
            if ((stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands2 ||
                 stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn) &&
                 stream.LicenseeVersion >= 45
               )
            {
                throw new NotSupportedException("This package version is not supported!");
                //stream.Write(byte v1cc); // usually 0x01, sometimes 0x02?
            }
#endif
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                stream.Version >= 70)
            {
                throw new NotSupportedException("This package version is not supported!");
                //stream.Write(uint classCRC); // v4a8
            }
#endif
#if THIEF_DS || DeusEx_IW
            if (stream.Build == BuildGeneration.Flesh)
            {
                throw new NotSupportedException("This package version is not supported!");
                //stream.WriteString(string thiefClassVisibleName);
            }
#endif
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance)
            {
                throw new NotSupportedException("This package version is not supported!");

                if (stream.LicenseeVersion >= 2)
                {
                    //stream.Write(ulong unkInt64);
                }

                if (stream.LicenseeVersion >= 3)
                {
                    //stream.Write(ulong unkInt64);
                }

                if (stream.LicenseeVersion >= 2)
                {
                    stream.WriteString(string.Empty);
                    //stream.WriteString(string vengeanceDefaultPropertiesText);
                }

                if (stream.LicenseeVersion >= 6)
                {
                    stream.WriteString(string.Empty);
                    //stream.WriteString(string vengeanceClassFilePath);
                }

                if (stream.LicenseeVersion >= 12)
                {
                    //stream.WriteArray(UArray<UName> names);
                }

                if (stream.LicenseeVersion >= 15)
                {
                    stream.WriteArray(ImplementedInterfaces != null
                        ? new UArray<UClass>(ImplementedInterfaces.Select(impl => impl.InterfaceClass))
                        : null);
                }

                if (stream.LicenseeVersion >= 20)
                {
                    UArray<UObject>? unk;
                    //stream.WriteArray(unk);
                }

                if (stream.LicenseeVersion >= 32)
                {
                    UArray<UObject>? unk;
                    //stream.WriteArray(unk);
                }

                if (stream.LicenseeVersion >= 28)
                {
                    UArray<UName>? unk;
                    //stream.WriteArray(unk);
                }

                if (stream.LicenseeVersion >= 30)
                {
                    //stream.Write(int unkInt32A);
                    //stream.Write(int unkInt32B);

                    //// Lazy array?
                    //stream.Write(int skipSize);
                    //// FIXME: Couldn't RE this code
                    //stream.Write(int b);
                }
            }
#endif
#if UE4
            if (stream.IsUE4())
            {
                stream.WriteName(UnrealName.None);
                stream.Write(IsCooked ?? false);
            }
#endif
        scriptProperties:
            if (stream.Version >= (uint)PackageObjectLegacyVersion.DisplacedScriptPropertiesWithClassDefaultObject)
            {
                // Default__ClassName
                stream.WriteObject(Default);
            }
            else
            {
                Default = this;
                DefaultProperties = DeserializeScriptProperties(stream, this);
                Properties = DefaultProperties;
            }
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague)
            {
                // StateMap; Seems to keep track of all declared states in the class.
                var v368 = new UMap<UName, UObject>();
                foreach (var state in EnumerateFields<UState>())
                {
                    v368.Add(state.Name, state);
                }

                stream.WriteMap(v368);
            }
#endif
            // version < 57
            //stream.WriteName();
            //stream.WriteName();
        }

        [Obsolete("Use ClassFlags directly")]
        public bool HasClassFlag(ClassFlags flag)
        {
            return (ClassFlags & (uint)flag) != 0;
        }

        [Obsolete("Use HasAnyClassFlags directly")]
        public bool HasClassFlag(uint flag)
        {
            return (ClassFlags & flag) != 0;
        }

        internal bool HasClassFlag(ClassFlag flagIndex)
        {
            return ClassFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(ClassFlag)], flagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnyClassFlags(ulong flag) => (ClassFlags & flag) != 0;

        public bool IsClassInterface()
        {
#if VENGEANCE
            // TODO: Move to EngineBranch
            if (HasAnyClassFlags((ulong)Flags.ClassFlags.VG_Interface))
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

        [Obsolete("Use ComponentDefaultObjectMap", true)]
        public IList<int> Components = null;

        [Obsolete("Use PackageImportNames", true)]
        public IList<int> PackageImports;

        [Obsolete("Use ImplementedInterfaces", true)]
        public UArray<UObject>? Vengeance_Implements;

        [Obsolete("Use EnumerateFields")]
        public IEnumerable<UState> States => EnumerateFields<UState>();

        [Obsolete("Use ClassWithin instead")]
        public UClass? Within => ClassWithin;

        [Obsolete("Use ClassConfigName instead")]
        public UName ConfigName => ClassConfigName;

        [Obsolete("Use NativeHeaderName instead")]
        public string NativeClassName => NativeHeaderName;
    }

    public sealed class UClassLicenseeAttachment : ObjectLicenseeAttachment;
}
