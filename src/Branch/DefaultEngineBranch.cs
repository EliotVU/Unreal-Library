using System;
using UELib.Branch.UE2.VG.Tokens;
using UELib.Branch.UE3.Willow.Tokens;
using UELib.Core;
using UELib.Core.Tokens;
using UELib.Flags;
using UELib.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch
{
    /// <summary>
    /// The default EngineBranch handles UE1, 2, and 3 packages, this helps us separate the UE3 or less specific code away
    /// from the <see cref="UE4.EngineBranchUE4" /> implementation.
    /// Note: The current implementation is incomplete, i.e. it does not map any other enum flags other than PackageFlags
    /// yet.
    /// Its main focus is to split UE3 and UE4 logic.
    /// </summary>
    public class DefaultEngineBranch : EngineBranch
    {
        [Flags]
        public enum PackageFlagsDefault : uint
        {
            /// <summary>
            /// UEX: Whether clients are allowed to download the package from the server.
            /// UE4: Displaced by "NewlyCreated"
            /// </summary>
            AllowDownload = 0x00000001U,

            /// <summary>
            /// Whether clients can skip downloading the package but still able to join the server.
            /// </summary>
            ClientOptional = 0x00000002U,

            /// <summary>
            /// Only necessary to load on the server.
            /// </summary>
            ServerSideOnly = 0x00000004U,

            /// <summary>
            /// UE4: Displaced by "CompiledIn"
            /// </summary>
            Unsecure = 0x00000010U,

            Protected = 0x80000000U
        }
#if TRANSFORMERS
        [Flags]
        public enum PackageFlagsHMS : uint
        {
            XmlFormat = 0x80000000U
        }
#endif
        [Flags]
        public enum PackageFlagsUE1 : uint
        {
            /// <summary>
            /// Whether the package has broken links.
            /// Runtime-only;not-serialized
            /// </summary>
            [Obsolete] BrokenLinks = 0x00000008U,

            /// <summary>
            /// Whether the client needs to download the package.
            /// Runtime-only;not-serialized
            /// </summary>
            [Obsolete] Need = 0x00008000U,

            /// <summary>
            /// The package is encrypted.
            /// <= UT
            /// </summary>
            Encrypted = 0x00000020U
        }

        [Flags]
        public enum PackageFlagsUE2 : uint
        {
            // UE2.5
            Official = 0x00000020U,
        }

        [Flags]
        public enum PackageFlagsUE3 : uint
        {
            /// <summary>
            /// Whether the package has been cooked.
            /// </summary>
            Cooked = 0x00000008U,

            /// <summary>
            /// Whether the package contains a ULevel or UWorld object.
            /// </summary>
            ContainsMap = 0x00020000U,

            [Obsolete] Trash = 0x00040000,

            /// <summary>
            /// Whether the package contains a UClass or any UnrealScript types.
            /// </summary>
            ContainsScript = 0x00200000U,

            /// <summary>
            /// Whether the package contains debug info i.e. it was built with -Debug.
            /// </summary>
            ContainsDebugData = 0x00400000U,

            Imports = 0x00800000U,

            StoreCompressed = 0x02000000U,
            StoreFullyCompressed = 0x04000000U,

            /// <summary>
            /// Whether package has metadata exported(anything related to the editor).
            /// </summary>
            NoExportsData = 0x20000000U,

            /// <summary>
            /// Whether the package TextBuffers' have been stripped of its content.
            /// </summary>
            StrippedSource = 0x40000000U
        }

        public DefaultEngineBranch(BuildGeneration generation) : base(generation)
        {
        }

        public override void Setup(UnrealPackage linker)
        {
            SetupEnumPackageFlags(linker);
            SetupEnumObjectFlags(linker);
            SetupEnumPropertyFlags(linker);
            SetupEnumStructFlags(linker);
            SetupEnumFunctionFlags(linker);
            SetupEnumStateFlags(linker);
            SetupEnumClassFlags(linker);

            EnumFlagsMap.Add(typeof(PackageFlag), PackageFlags);
            EnumFlagsMap.Add(typeof(ObjectFlag), ObjectFlags);
            EnumFlagsMap.Add(typeof(PropertyFlag), PropertyFlags);
            EnumFlagsMap.Add(typeof(StructFlag), StructFlags);
            EnumFlagsMap.Add(typeof(FunctionFlag), FunctionFlags);
            EnumFlagsMap.Add(typeof(StateFlag), StateFlags);
            EnumFlagsMap.Add(typeof(ClassFlag), ClassFlags);
        }

        protected virtual void SetupEnumPackageFlags(UnrealPackage linker)
        {
            PackageFlags[(int)PackageFlag.AllowDownload] = (uint)PackageFlagsDefault.AllowDownload;
            PackageFlags[(int)PackageFlag.ClientOptional] = (uint)PackageFlagsDefault.ClientOptional;
            PackageFlags[(int)PackageFlag.ServerSideOnly] = (uint)PackageFlagsDefault.ServerSideOnly;
#if UE1
            // FIXME: Version
            if (linker.Version > 61 && linker.Version <= 69) // <= UT99
                PackageFlags[(int)PackageFlag.Encrypted] = (uint)PackageFlagsUE1.Encrypted;
#endif
#if UE2
            if (linker.Build == BuildGeneration.UE2_5)
                PackageFlags[(int)PackageFlag.Official] = (uint)PackageFlagsUE2.Official;
#endif
#if UE3
            // Map the new PackageFlags, but the version is nothing but a guess!
            if (linker.Version >= 180)
            {
                if (linker.Version >= (uint)PackageObjectLegacyVersion.AddedCookerVersion)
                    PackageFlags[(int)PackageFlag.Cooked] = (uint)PackageFlagsUE3.Cooked;

                PackageFlags[(int)PackageFlag.ContainsMap] = (uint)PackageFlagsUE3.ContainsMap;
                PackageFlags[(int)PackageFlag.ContainsDebugData] = (uint)PackageFlagsUE3.ContainsDebugData;
                PackageFlags[(int)PackageFlag.ContainsScript] = (uint)PackageFlagsUE3.ContainsScript;
                PackageFlags[(int)PackageFlag.StrippedSource] = (uint)PackageFlagsUE3.StrippedSource;
            }
#endif
        }

        protected virtual void SetupEnumObjectFlags(UnrealPackage linker)
        {
            ObjectFlags[(int)ObjectFlag.Transactional] = (uint)ObjectFlagsLO.Transactional;

            // version >= 48
            ObjectFlags[(int)ObjectFlag.HasStack] = (uint)ObjectFlagsLO.HasStack;

            if (linker.Version >= (uint)PackageObjectLegacyVersion.Release62)
            {
                ObjectFlags[(int)ObjectFlag.Standalone] = (uint)ObjectFlagsLO.Standalone;
                ObjectFlags[(int)ObjectFlag.Public] = (uint)ObjectFlagsLO.Public;
            }

            ObjectFlags[(int)ObjectFlag.LoadForClient] = (uint)ObjectFlagsLO.LoadForClient;
            ObjectFlags[(int)ObjectFlag.LoadForServer] = (uint)ObjectFlagsLO.LoadForServer;
            ObjectFlags[(int)ObjectFlag.LoadForEditor] = (uint)ObjectFlagsLO.LoadForEdit;
            ObjectFlags[(int)ObjectFlag.NotForClient] = (uint)ObjectFlagsLO.NotForClient;
            ObjectFlags[(int)ObjectFlag.NotForServer] = (uint)ObjectFlagsLO.NotForServer;
            ObjectFlags[(int)ObjectFlag.NotForEditor] = (uint)ObjectFlagsLO.NotForEdit;

            ObjectFlags[(int)ObjectFlag.Native] = (uint)ObjectFlagsLO.Native;
            ObjectFlags[(int)ObjectFlag.Transient] = (uint)ObjectFlagsLO.Transient;

            // UE2? Let's just restrict it to anything after UT99
            if (linker.Version > 69)
            {
                // New flags with UE2
                ObjectFlags[(int)ObjectFlag.Protected] = (uint)ObjectFlagsLO.Protected;
                ObjectFlags[(int)ObjectFlag.Final] = (uint)ObjectFlagsLO.Final;
                ObjectFlags[(int)ObjectFlag.PerObjectLocalized] = (uint)ObjectFlagsLO.PerObjectLocalized;
            }

            if (linker.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                // Shifted from 0x800 to 0x100 and moved to higher bits.
                ObjectFlags[(int)ObjectFlag.Protected] = (ulong)ObjectFlagsHO.Protected << 32;
                // Deprecated
                ObjectFlags[(int)ObjectFlag.Final] = 0x00;
                // Same bits but moved to higher bits.
                ObjectFlags[(int)ObjectFlag.PerObjectLocalized] = (ulong)ObjectFlagsHO.PerObjectLocalized << 32;
            }

            // Could be earlier, but we'll just assume it's introduced with the separating of a class's defaults.
            if (linker.Version >= (uint)PackageObjectLegacyVersion.DisplacedScriptPropertiesWithClassDefaultObject)
            {
                ObjectFlags[(int)ObjectFlag.ClassDefaultObject] = (ulong)ObjectFlagsHO.PropertiesObject << 32;
                ObjectFlags[(int)ObjectFlag.TemplateObject] = ObjectFlags[(int)ObjectFlag.ClassDefaultObject];
            }

            // Assumption
            if (linker.Version >= (uint)PackageObjectLegacyVersion.ArchetypeAddedToExports)
            {
                ObjectFlags[(int)ObjectFlag.ArchetypeObject] = (ulong)ObjectFlagsHO.ArchetypeObject << 32;
                // FIXME: The flag check was added later (Not checked for in GoW), no known version.
                ObjectFlags[(int)ObjectFlag.TemplateObject] |= ObjectFlags[(int)ObjectFlag.ArchetypeObject];
            }
#if BULLETSTORM
            // FIXME: Figure out if this is only specific to Bulletstorm or if it is a general UE3 thing.
            if (linker.Build == UnrealPackage.GameBuild.BuildName.Bulletstorm)
            {
                ObjectFlags[(int)ObjectFlag.ClassDefaultObject] = 0x80UL << 32; // (same bit as Batman Ak)
                ObjectFlags[(int)ObjectFlag.ArchetypeObject] = 0x100UL << 32; // assumed to come next after ClassDefaultObject.
                ObjectFlags[(int)ObjectFlag.TemplateObject] = ObjectFlags[(int)ObjectFlag.ClassDefaultObject] | ObjectFlags[(int)ObjectFlag.ArchetypeObject];
            }
#endif
        }

        protected virtual void SetupEnumPropertyFlags(UnrealPackage linker)
        {
            PropertyFlags[(int)PropertyFlag.Editable] = (ulong)PropertyFlagsLO.Editable;
            PropertyFlags[(int)PropertyFlag.Input] = (ulong)PropertyFlagsLO.Input;
            PropertyFlags[(int)PropertyFlag.ExportObject] = (ulong)PropertyFlagsLO.ExportObject;
            PropertyFlags[(int)PropertyFlag.OptionalParm] = (ulong)PropertyFlagsLO.OptionalParm;
            PropertyFlags[(int)PropertyFlag.Parm] = (ulong)PropertyFlagsLO.Parm;
            PropertyFlags[(int)PropertyFlag.OutParm] = (ulong)PropertyFlagsLO.OutParm;
            PropertyFlags[(int)PropertyFlag.SkipParm] = (ulong)PropertyFlagsLO.SkipParm;
            PropertyFlags[(int)PropertyFlag.ReturnParm] = (ulong)PropertyFlagsLO.ReturnParm;
            PropertyFlags[(int)PropertyFlag.CoerceParm] = (ulong)PropertyFlagsLO.CoerceParm;
            PropertyFlags[(int)PropertyFlag.Net] = (ulong)PropertyFlagsLO.Net;
            PropertyFlags[(int)PropertyFlag.Const] = (ulong)PropertyFlagsLO.Const;

            PropertyFlags[(int)PropertyFlag.Native] = (ulong)PropertyFlagsLO.Native;
            PropertyFlags[(int)PropertyFlag.Transient] = (ulong)PropertyFlagsLO.Transient;
            PropertyFlags[(int)PropertyFlag.Config] = (ulong)PropertyFlagsLO.Config;
            PropertyFlags[(int)PropertyFlag.Localized] = (ulong)PropertyFlagsLO.Localized;
            PropertyFlags[(int)PropertyFlag.Travel] = (ulong)PropertyFlagsLO.Travel;
            PropertyFlags[(int)PropertyFlag.GlobalConfig] = (ulong)PropertyFlagsLO.GlobalConfig;
            // Not functional
            PropertyFlags[(int)PropertyFlag.DuplicateTransient] = (ulong)PropertyFlagsLO.New;

            if (linker.Version > 68)
            {
                PropertyFlags[(int)PropertyFlag.NoExport] = (ulong)PropertyFlagsLO.NoExport;
                PropertyFlags[(int)PropertyFlag.EditConst] = (ulong)PropertyFlagsLO.EditConst;
                PropertyFlags[(int)PropertyFlag.EditInline] = (ulong)PropertyFlagsLO.EditInline;
                PropertyFlags[(int)PropertyFlag.EdFindable] = (ulong)PropertyFlagsLO.EdFindable;
                PropertyFlags[(int)PropertyFlag.EditInlineUse] = (ulong)PropertyFlagsLO.EditInlineUse;
                PropertyFlags[(int)PropertyFlag.Deprecated] = (ulong)PropertyFlagsLO.Deprecated;
            }

            if (linker.Version > 68)
            {
                // between GoW and UT3
                if (linker.Version > 225)
                {
                    // Displaced EditConstArray
                    PropertyFlags[(int)PropertyFlag.EditFixedSize] = (ulong)PropertyFlagsLO.EditFixedSize;
                }
                else
                {
                    // UE2? ConstRef in earlier versions
                    PropertyFlags[(int)PropertyFlag.EditConstArray] = (ulong)PropertyFlagsLO.EditConstArray;
                }
            }

            if (linker.Version > 68 && linker.Version < (uint)PackageObjectLegacyVersion.UE3)
            {
                // Overlaps with 'Cache' ( > 120)
                PropertyFlags[(int)PropertyFlag.Button] = (ulong)PropertyFlagsLO.Button;

                // UE1, UE2, removed with UC2?
                PropertyFlags[(int)PropertyFlag.CommentString] = (ulong)PropertyFlagsLO.EditorData;

                // << OnDemand?

                // UE2
                PropertyFlags[(int)PropertyFlag.EditInlineNotify] = (ulong)PropertyFlagsLO.EditInlineNotify;
            }

            // > UT2003, added between 121-128 removed between 159-178
            if (linker.Version > 120 && linker.Version < (uint)PackageObjectLegacyVersion.UE3)
            {
                // Automated
            }

            if (linker.Version >= (uint)PackageObjectLegacyVersion.UE3)
            {
                PropertyFlags[(int)PropertyFlag.Component] = (ulong)PropertyFlagsLO.Component;

                // Replaced OnDemand
                PropertyFlags[(int)PropertyFlag.AlwaysInit] = (ulong)PropertyFlagsLO.Init;
                // Replaced CommentString
                PropertyFlags[(int)PropertyFlag.NoClear] = (ulong)PropertyFlagsLO.NoClear;
            }

            if (linker.Version >= (uint)PackageObjectLegacyVersion.PropertyFlagsSizeExpandedTo64Bits)
            {
                PropertyFlags[(int)PropertyFlag.RepNotify] = (ulong)PropertyFlagsHO.RepNotify << 32;
                PropertyFlags[(int)PropertyFlag.Interp] = (ulong)PropertyFlagsHO.Interp << 32;
                PropertyFlags[(int)PropertyFlag.NonTransactional] = (ulong)PropertyFlagsHO.NonTransactional << 32;
            }

            // Most Gow-UT3
            if (linker.Version > 225)
            {
                PropertyFlags[(int)PropertyFlag.EditorOnly] = (ulong)PropertyFlagsHO.EditorOnly << 32;
                PropertyFlags[(int)PropertyFlag.NotForConsole] = (ulong)PropertyFlagsHO.NotForConsole << 32;
                PropertyFlags[(int)PropertyFlag.RepRetry] = (ulong)PropertyFlagsHO.RepRetry << 32;

                PropertyFlags[(int)PropertyFlag.NoImport] = (ulong)PropertyFlagsLO.NoImport;

                // Replaced New
                PropertyFlags[(int)PropertyFlag.DuplicateTransient] = (ulong)PropertyFlagsLO.DuplicateTransient;

                // Replaced EditInlineNotify
                PropertyFlags[(int)PropertyFlag.DataBinding] = (ulong)PropertyFlagsLO.DataBinding;

                PropertyFlags[(int)PropertyFlag.SerializeText] = (ulong)PropertyFlagsLO.SerializeText;

                PropertyFlags[(int)PropertyFlag.PrivateWrite] = (ulong)PropertyFlagsHO.PrivateWrite << 32;
                PropertyFlags[(int)PropertyFlag.ProtectedWrite] = (ulong)PropertyFlagsHO.ProtectedWrite << 32;

                // Maybe post UT3 (512), need to double-check.

                PropertyFlags[(int)PropertyFlag.Archetype] = (ulong)PropertyFlagsHO.Archetype << 32;

                PropertyFlags[(int)PropertyFlag.EditHide] = (ulong)PropertyFlagsHO.EditHide << 32;
                PropertyFlags[(int)PropertyFlag.EditTextBox] = (ulong)PropertyFlagsHO.EditTextBox << 32;
            }

            if (linker.Version >= (uint)PackageObjectLegacyVersion.AddedImportExportGuidsTable)
            {
                PropertyFlags[(int)PropertyFlag.CrossLevelPassive] = (ulong)PropertyFlagsHO.CrossLevelPassive << 32;
                PropertyFlags[(int)PropertyFlag.CrossLevelActive] = (ulong)PropertyFlagsHO.CrossLevelActive << 32;
            }
        }

        protected virtual void SetupEnumStructFlags(UnrealPackage linker)
        {
            StructFlags[(int)StructFlag.Native] = (ulong)Flags.StructFlags.Native;
            StructFlags[(int)StructFlag.Export] = (ulong)Flags.StructFlags.Export;

            // FIXME: Just an estimation.
            if (linker.Version >= (uint)PackageObjectLegacyVersion.AddedComponentMapToExports)
            {
                StructFlags[(int)StructFlag.HasComponents] = (ulong)Flags.StructFlags.HasComponents;
            }

            // FIXME: Just an estimation.
            if (linker.Version >= (uint)PackageObjectLegacyVersion.AddedStructFlagsToScriptStruct)
            {
                StructFlags[(int)StructFlag.Transient] = (ulong)Flags.StructFlags.Transient;
            }

            // FIXME: Just a guess, it's sort of related.
            if (linker.Version >= (uint)PackageObjectLegacyVersion.DisplacedScriptPropertiesWithClassDefaultObject)
            {
                StructFlags[(int)StructFlag.Atomic] = (ulong)Flags.StructFlags.Atomic;
            }

            // Assuming that the serialization logic is added together with the introduction of the flag.
            if (linker.Version >= (uint)PackageObjectLegacyVersion.AddedImmutableStructs)
            {
                StructFlags[(int)StructFlag.Immutable] = (ulong)Flags.StructFlags.Immutable;
            }

            // FIXME: Added way after cooking was introduced...
            if (linker.Version > 375)
            {
                StructFlags[(int)StructFlag.AtomicWhenCooked] = (ulong)Flags.StructFlags.AtomicWhenCooked;
                StructFlags[(int)StructFlag.ImmutableWhenCooked] = (ulong)Flags.StructFlags.ImmutableWhenCooked;
            }
        }

        protected virtual void SetupEnumFunctionFlags(UnrealPackage linker)
        {
            // UE1+ Flags
            FunctionFlags[(int)FunctionFlag.Final] = (ulong)Flags.FunctionFlags.Final;
            FunctionFlags[(int)FunctionFlag.Defined] = (ulong)Flags.FunctionFlags.Defined;
            FunctionFlags[(int)FunctionFlag.Iterator] = (ulong)Flags.FunctionFlags.Iterator;
            FunctionFlags[(int)FunctionFlag.Latent] = (ulong)Flags.FunctionFlags.Latent;
            FunctionFlags[(int)FunctionFlag.PreOperator] = (ulong)Flags.FunctionFlags.PreOperator;
            FunctionFlags[(int)FunctionFlag.Singular] = (ulong)Flags.FunctionFlags.Singular;
            FunctionFlags[(int)FunctionFlag.Net] = (ulong)Flags.FunctionFlags.Net;
            FunctionFlags[(int)FunctionFlag.NetReliable] = (ulong)Flags.FunctionFlags.NetReliable;
            FunctionFlags[(int)FunctionFlag.Simulated] = (ulong)Flags.FunctionFlags.Simulated;
            FunctionFlags[(int)FunctionFlag.Exec] = (ulong)Flags.FunctionFlags.Exec;
            FunctionFlags[(int)FunctionFlag.Native] = (ulong)Flags.FunctionFlags.Native;
            FunctionFlags[(int)FunctionFlag.Event] = (ulong)Flags.FunctionFlags.Event;
            FunctionFlags[(int)FunctionFlag.Operator] = (ulong)Flags.FunctionFlags.Operator;
            FunctionFlags[(int)FunctionFlag.Static] = (ulong)Flags.FunctionFlags.Static;
            // <= 61

            if (linker.Version > 61 && linker.Version < 187)
            {
                // Deprecated, replaced later by OptionalParms
                FunctionFlags[(int)FunctionFlag.NoExport] = (ulong)Flags.FunctionFlags.NoExport;
            }

            // 62-68 Flags
            if (linker.Version > 61)
            {
                FunctionFlags[(int)FunctionFlag.Const] = (ulong)Flags.FunctionFlags.Const; // Modifier in UE3
            }

            if (linker.Version > 61 && linker.Version < 187)
            {
                FunctionFlags[(int)FunctionFlag.Invariant] = (ulong)Flags.FunctionFlags.Invariant;
            }

            // UE2+? skip ahead of at least UT99
            if (linker.Version > 69)
            {
                // Missing in U1, UT99
                FunctionFlags[(int)FunctionFlag.Public] = (ulong)Flags.FunctionFlags.Public;
                FunctionFlags[(int)FunctionFlag.Private] = (ulong)Flags.FunctionFlags.Private;
                FunctionFlags[(int)FunctionFlag.Protected] = (ulong)Flags.FunctionFlags.Protected;

                FunctionFlags[(int)FunctionFlag.Delegate] = (ulong)Flags.FunctionFlags.Delegate;
            }

            // Missing in UT2003, appears with UT2004.
            if (linker.Version > 120)
            {
                // DebugOnly in UC2 (seen in other games as well, like XII)
                // UC2 has this flag as 0x00400000
                FunctionFlags[(int)FunctionFlag.NetServer] = (ulong)Flags.FunctionFlags.NetServer;
            }

            if (linker.Version > 225)
            {
                FunctionFlags[(int)FunctionFlag.HasOptionalParms] = (ulong)Flags.FunctionFlags.OptionalParameters;
            }

            if (linker.Version > 186)
            {
                // UC2 has this flag as 0x00800000
                FunctionFlags[(int)FunctionFlag.HasOutParms] = (ulong)Flags.FunctionFlags.OutParameters;
                FunctionFlags[(int)FunctionFlag.HasDefaults] = (ulong)Flags.FunctionFlags.ScriptStructs;
                FunctionFlags[(int)FunctionFlag.NetClient] = (ulong)Flags.FunctionFlags.NetClient;
            }

            if (linker.Version >= (uint)PackageObjectLegacyVersion.AddedDLLBindFeature)
            {
                FunctionFlags[(int)FunctionFlag.DLLImport] = (ulong)Flags.FunctionFlags.DLLImport;

                // Added with late UDK
                FunctionFlags[(int)FunctionFlag.K2Call] = (ulong)Flags.FunctionFlags.K2Call;
                FunctionFlags[(int)FunctionFlag.K2Override] = (ulong)Flags.FunctionFlags.K2Override;
                FunctionFlags[(int)FunctionFlag.K2Pure] = (ulong)Flags.FunctionFlags.K2Pure;
            }
        }

        protected virtual void SetupEnumStateFlags(UnrealPackage linker)
        {
            // p.s . Seems like these may be unnecessary.
            StateFlags[(int)StateFlag.Auto] = (ulong)Flags.StateFlags.Auto;
            StateFlags[(int)StateFlag.Editable] = (ulong)Flags.StateFlags.Editable;
            StateFlags[(int)StateFlag.Simulated] = (ulong)Flags.StateFlags.Simulated;
        }

        protected virtual void SetupEnumClassFlags(UnrealPackage linker)
        {
            ClassFlags[(int)ClassFlag.Abstract] = (ulong)Flags.ClassFlags.Abstract;
            ClassFlags[(int)ClassFlag.Compiled] = (ulong)Flags.ClassFlags.Compiled;
            ClassFlags[(int)ClassFlag.Config] = (ulong)Flags.ClassFlags.Config;
            ClassFlags[(int)ClassFlag.Transient] = (ulong)Flags.ClassFlags.Transient;
            ClassFlags[(int)ClassFlag.Parsed] = (ulong)Flags.ClassFlags.Parsed;
            ClassFlags[(int)ClassFlag.Localized] = (ulong)Flags.ClassFlags.Localized;
            ClassFlags[(int)ClassFlag.SafeReplace] = (ulong)Flags.ClassFlags.SafeReplace;

            if (linker.Version > 61)
            {
                if (linker.Version < 225)
                {
                    ClassFlags[(int)ClassFlag.RuntimeStatic] = (ulong)Flags.ClassFlags.RuntimeStatic;
                }

                ClassFlags[(int)ClassFlag.NoExport] = (ulong)Flags.ClassFlags.NoExport;
                // << NoUserCreate
                ClassFlags[(int)ClassFlag.PerObjectConfig] = (ulong)Flags.ClassFlags.PerObjectConfig;
                ClassFlags[(int)ClassFlag.NativeReplication] = (ulong)Flags.ClassFlags.NativeReplication;
            }

            if (linker.Version > 68)
            {
                // Replaced NoUserCreate
                ClassFlags[(int)ClassFlag.Placeable] = (ulong)Flags.ClassFlags.Placeable;

                ClassFlags[(int)ClassFlag.EditInlineNew] = (ulong)Flags.ClassFlags.EditInlineNew;
                ClassFlags[(int)ClassFlag.CollapseCategories] = (ulong)Flags.ClassFlags.CollapseCategories;
            }

            if (linker.Version >= (uint)PackageObjectLegacyVersion.AddedInterfacesFeature)
            {
                ClassFlags[(int)ClassFlag.Interface] = (ulong)Flags.ClassFlags.Interface;
            }
            else if (linker.Version > 68)
            {
                // See also Exported below, but that has existed before the interface feature.
                ClassFlags[(int)ClassFlag.ExportStructs] = (ulong)Flags.ClassFlags.ExportStructs;
            }

            if (linker.Version > 69)
            {
                // UE2+ Doesn't appear in early UE3, but re-appears in later UE3 builds.
                ClassFlags[(int)ClassFlag.HasInstancedProps] = (ulong)Flags.ClassFlags.Instanced;
            }
            else if (linker.Version > 61)
            {
                ClassFlags[(int)ClassFlag.NoUserCreate] = (ulong)Flags.ClassFlags.NoUserCreate;
            }

            // > UT2003
            if (linker.Version > 120 && linker.Version < (uint)PackageObjectLegacyVersion.UE3)
            {
                // Might also be UT2 only...
                ClassFlags[(int)ClassFlag.HideDropDown] = (ulong)Flags.ClassFlags.HideDropDown;

                // << UT2 CacheExempt
                // << UT2 ParseConfig
                // << UT2 Cache
            }

            if (linker.Version >= (uint)PackageObjectLegacyVersion.UE3)
            {
                ClassFlags[(int)ClassFlag.HasComponents] = (ulong)Flags.ClassFlags.HasComponents;
                ClassFlags[(int)ClassFlag.Hidden] = (ulong)Flags.ClassFlags.Hidden;
                // UC2 has this flag as 0x00400000
                ClassFlags[(int)ClassFlag.Deprecated] = (ulong)Flags.ClassFlags.Deprecated;
                ClassFlags[(int)ClassFlag.HideDropDown] = (ulong)Flags.ClassFlags.HideDropDown2;
                ClassFlags[(int)ClassFlag.Exported] = (ulong)Flags.ClassFlags.Exported;
            }

            if (linker.Version > 186)
            {
                ClassFlags[(int)ClassFlag.Intrinsic] = (ulong)Flags.ClassFlags.Intrinsic;
                // << ComponentClass, displaced with ArchetypeObject and ClassDefaultObject flags.
            }

            if (linker.Version > 225)
            {
                ClassFlags[(int)ClassFlag.NativeOnly] = (ulong)Flags.ClassFlags.NativeOnly;
                ClassFlags[(int)ClassFlag.PerObjectLocalized] = (ulong)Flags.ClassFlags.PerObjectLocalized;
            }

            if (linker.Version >= (uint)PackageObjectLegacyVersion.AddedImportExportGuidsTable)
            {
                ClassFlags[(int)ClassFlag.HasCrossLevelRefs] = (ulong)Flags.ClassFlags.HasCrossLevelRefs;
            }
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            SetupSerializer<DefaultPackageSerializer>();
        }

        /// <summary>
        /// Builds a tokens map for UE1, 2, and 3.
        /// The default byte-codes are correct for UE2 and are adjusted accordingly for UE1, and UE3.
        ///
        /// FYI: Any version is not actually correct, in most cases changes that have been made to the UnrealScript byte-code are not versioned.
        /// -- Any version here is an approximation that works best for most packages.
        /// </summary>
        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = new TokenMap((byte)ExprToken.ExtendedNative)
            {
                { 0x00, typeof(LocalVariableToken) },
                { 0x01, typeof(InstanceVariableToken) },
                { 0x02, typeof(DefaultVariableToken) },
                { 0x03, typeof(BadToken) },
                { 0x04, typeof(ReturnToken) },
                { 0x05, typeof(SwitchToken) },
                { 0x06, typeof(JumpToken) },
                { 0x07, typeof(JumpIfNotToken) },
                { 0x08, typeof(StopToken) },
                { 0x09, typeof(AssertToken) },
                { 0x0A, typeof(CaseToken) },
                { 0x0B, typeof(NothingToken) },
                { 0x0C, typeof(LabelTableToken) },
                { 0x0D, typeof(GotoLabelToken) },
                {
                    0x0E, linker.Version < 62
                        // Serialized but never emitted, must have been a really old expression.
                        ? typeof(ValidateObjectToken)
                        : typeof(EatStringToken)
                },
                { 0x0F, typeof(LetToken) },
                { 0x10, typeof(BadToken) },

                // Bad expr in UE1 v61
                {
                    0x11, linker.Version < 62
                        ? typeof(BadToken)
                        : typeof(NewToken)
                },
                { 0x12, typeof(ClassContextToken) },
                { 0x13, typeof(MetaClassCastToken) },
                {
                    0x14, linker.Version < 62
                        ? typeof(BeginFunctionToken)
                        : typeof(LetBoolToken)
                },
                {
                    0x15, linker.Version < 62
                        ? typeof(EndOfScriptToken)
                        // Attested in UE2 builds such as Unreal2 and Unreal2XMP, but not in any UE1 or UE2.5 builds, nor RS3 (UE2)
                        : linker.Version < (uint)PackageObjectLegacyVersion.UE3
                            ? typeof(LineNumberToken)
                            : typeof(BadToken)
                },
                { 0x16, typeof(EndFunctionParmsToken) },
                { 0x17, typeof(SelfToken) },
                { 0x18, typeof(SkipToken) },
                { 0x19, typeof(ContextToken) },
                { 0x1A, typeof(ArrayElementToken) },
                { 0x1B, typeof(VirtualFunctionToken) },
                { 0x1C, typeof(FinalFunctionToken) },
                { 0x1D, typeof(IntConstToken) },
                { 0x1E, typeof(FloatConstToken) },
                { 0x1F, typeof(StringConstToken) },
                { 0x20, typeof(ObjectConstToken) },
                { 0x21, typeof(NameConstToken) },
                { 0x22, typeof(RotationConstToken) },
                { 0x23, typeof(VectorConstToken) },
                { 0x24, typeof(ByteConstToken) },
                { 0x25, typeof(IntZeroToken) },
                { 0x26, typeof(IntOneToken) },
                { 0x27, typeof(TrueToken) },
                { 0x28, typeof(FalseToken) },
                { 0x29, typeof(NativeParameterToken) },
                { 0x2A, typeof(NoObjectToken) },
                {
                    0x2B, linker.Version < (uint)PackageObjectLegacyVersion.CastStringSizeTokenDeprecated
                        ? typeof(ResizeStringToken)
                        : typeof(BadToken)
                },
                { 0x2C, typeof(IntConstByteToken) },
                { 0x2D, typeof(BoolVariableToken) },
                { 0x2E, typeof(DynamicCastToken) },
                { 0x2F, typeof(IteratorToken) },
                { 0x30, typeof(IteratorPopToken) },
                { 0x31, typeof(IteratorNextToken) },
                { 0x32, typeof(StructCmpEqToken) },
                { 0x33, typeof(StructCmpNeToken) },
                {
                    0x34, linker.Version < 62
                        // Actually a StructConstToken but is not implemented in the VM.
                        ? typeof(BadToken)
                        : typeof(UnicodeStringConstToken)
                },
                {
                    0x35, linker.Version < 62
                        ? typeof(BadToken)
                        // Defined and emitted but ignored by the VM in UE2,
                        // -- however some builds do serialize this token, so we'll keep it
                        : linker.Build == BuildGeneration.UE2
                            ? typeof(RangeConstToken)
                            : typeof(BadToken)
                },
                { 0x36, typeof(StructMemberToken) },
                { 0x37, typeof(BadToken) },
                { 0x38, typeof(GlobalFunctionToken) },

                // PrimitiveCast:MinConversion/RotationToVector (UE1)
                { 0x39, typeof(PrimitiveCastToken) },

                // PrimitiveCast:ByteToInt (UE1)
                {
                    0x3A, linker.Version < (uint)PackageObjectLegacyVersion.PrimitiveCastTokenAdded
                        ? typeof(BadToken) // will be overridden down if UE1
                        : typeof(ReturnNothingToken)
                },

                // Added with UE2 (FIXME: version)
                // FIXME: Bad expr in GoW
                { 0x3B, typeof(DelegateCmpEqToken) },
                // FIXME: Bad expr in GoW
                { 0x3C, typeof(DelegateCmpNeToken) },
                // FIXME: Bad expr in GoW
                { 0x3D, typeof(DelegateFunctionCmpEqToken) },
                // FIXME: Bad expr in GoW
                { 0x3E, typeof(DelegateFunctionCmpNeToken) },
                // FIXME: Bad expr in GoW
                { 0x3F, typeof(EmptyDelegateToken) },
                { 0x40, typeof(BadToken) },
                // FIXME: Valid in GoW, no bytes
                { 0x41, typeof(BadToken) },
                { 0x42, typeof(BadToken) },
                { 0x43, typeof(BadToken) },
                { 0x44, typeof(BadToken) },
                { 0x45, typeof(BadToken) },
                // Unused PrimitiveCast (UE1)
                { 0x46, typeof(BadToken) },
                // PrimitiveCast:ObjectToTool (UE1)
                { 0x47, typeof(EndOfScriptToken) },
                // PrimitiveCast:NameToBool (UE1)
                { 0x48, typeof(ConditionalToken) },
                { 0x49, typeof(BadToken) },
                { 0x4A, typeof(BadToken) },
                { 0x4B, typeof(BadToken) },
                { 0x4C, typeof(BadToken) },
                { 0x4D, typeof(BadToken) },
                { 0x4E, typeof(BadToken) },
                { 0x4F, typeof(BadToken) },
                { 0x50, typeof(BadToken) },
                { 0x51, typeof(BadToken) },
                { 0x52, typeof(BadToken) },
                { 0x53, typeof(BadToken) },
                { 0x54, typeof(BadToken) },
                { 0x55, typeof(BadToken) },
                { 0x56, typeof(BadToken) },
                { 0x57, typeof(BadToken) },
                { 0x58, typeof(BadToken) },
                { 0x59, typeof(BadToken) },
                // PrimitiveCast:MaxConversion (UE1)
                { 0x5A, typeof(BadToken) },
                { 0x5B, typeof(BadToken) },
                { 0x5C, typeof(BadToken) },
                { 0x5D, typeof(BadToken) },
                { 0x5E, typeof(BadToken) },
                { 0x5F, typeof(BadToken) },
            };

            if (linker.Version >= (uint)PackageObjectLegacyVersion.DynamicArrayTokensAdded)
            {
                tokenMap[0x10] = typeof(DynamicArrayElementToken);
                // Added in a later engine build, but some UE1 games (or special builds) do allow this token.
                tokenMap[0x37] = typeof(DynamicArrayLengthToken);
            }

            if (linker.Version < (uint)PackageObjectLegacyVersion.PrimitiveCastTokenAdded)
            {
                DowngradePrimitiveCasts(tokenMap);
            }
            else
            {
                if (linker.Version >= (uint)PackageObjectLegacyVersion.DynamicArrayInsertTokenAdded)
                {
                    // Beware! these will be shifted down, see UnshiftTokens3
                    tokenMap[0x40] = typeof(DynamicArrayInsertToken);
                    tokenMap[0x41] = typeof(DynamicArrayRemoveToken);
                }

                tokenMap[0x42] = typeof(DebugInfoToken);
                tokenMap[0x43] = typeof(DelegateFunctionToken);
                tokenMap[0x44] = typeof(DelegatePropertyToken);
                tokenMap[0x45] = typeof(LetDelegateToken);
            }
#if UE3
            // RangeConst was deprecated to add new tokens, and as a result all op codes past it were shifted around.
            if (linker.Version >= (uint)PackageObjectLegacyVersion.RangeConstTokenDeprecated)
                UnshiftTokens3(tokenMap);
#endif
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (linker.Build.Name)
            {
#if BIOSHOCK
                case UnrealPackage.GameBuild.BuildName.BioShock:
                    tokenMap[0x49] = typeof(LogFunctionToken);
                    break;
#endif
#if MIRRORSEDGE
                case UnrealPackage.GameBuild.BuildName.MirrorsEdge:
                    tokenMap[0x4F] = typeof(UnresolvedToken);
                    break;
#endif
            }

            return tokenMap;
        }

        // TODO: Confirm if these are correct for UE1
        /// <summary>
        /// Downgrades any UE2+ byte codes to their UE1 counterpart.
        /// In UE1 primitive casts were on the same level as any other token expression.
        /// In UE2 or earlier these were displaced and inlined within a new "PrimitiveCast" token.
        /// </summary>
        protected void DowngradePrimitiveCasts(TokenMap tokenMap)
        {
            var primitiveCastTokenType = typeof(PrimitiveInlineCastToken);
            // Functions as the "MinConversion" (UE1) and also "RotatorToVector"
            tokenMap[(byte)CastToken.RotatorToVector] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ByteToInt] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ByteToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ByteToFloat] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.IntToByte] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.IntToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.IntToFloat] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.BoolToByte] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.BoolToInt] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.BoolToFloat] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.FloatToByte] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.FloatToInt] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.FloatToBool] = primitiveCastTokenType;

            // "StringToName" seen in some builds, otherwise a bad token but we cannot assume any version boundaries.
            tokenMap[(byte)CastToken.ObjectToInterface] = primitiveCastTokenType;

            tokenMap[(byte)CastToken.ObjectToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.NameToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToByte] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToInt] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToFloat] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToVector] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToRotator] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.VectorToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.VectorToRotator] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.RotatorToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ByteToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.IntToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.BoolToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.FloatToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ObjectToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.NameToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.VectorToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.RotatorToString] = primitiveCastTokenType;

            // Represents the "MaxConversion" (UE1)
            // "DelegateToString" (UE2+), later deprecated
            tokenMap[(byte)CastToken.DelegateToString] = typeof(BadToken);
        }
#if UE3
        protected void UnshiftTokens3(TokenMap tokenMap)
        {
            // EatString -> EatReturnValueToken (also in UC2)
            tokenMap[0x0E] = typeof(EatReturnValueToken);

            tokenMap[0x15] = typeof(EndParmValueToken);

            tokenMap[0x35] = typeof(StructMemberToken);
            tokenMap[0x36] = typeof(DynamicArrayLengthToken);
            tokenMap[0x37] = typeof(GlobalFunctionToken);
            tokenMap[0x38] = typeof(PrimitiveCastToken);
            tokenMap[0x39] = typeof(DynamicArrayInsertToken);

            tokenMap[0x3A] = typeof(ReturnNothingToken);

            // 0x3B to 0x3F were not shifted.

            // These as well.
            tokenMap[0x40] = typeof(DynamicArrayRemoveToken);
            tokenMap[0x41] = typeof(DebugInfoToken);
            tokenMap[0x42] = typeof(DelegateFunctionToken);
            tokenMap[0x43] = typeof(DelegatePropertyToken);
            tokenMap[0x44] = typeof(LetDelegateToken);
            tokenMap[0x45] = typeof(ConditionalToken);
            tokenMap[0x46] = typeof(DynamicArrayFindToken);
            tokenMap[0x47] = typeof(DynamicArrayFindStructToken);
            // UC2 has this bytecode as 0x49
            tokenMap[0x48] = typeof(OutVariableToken);
            tokenMap[0x49] = typeof(DefaultParameterToken);
            // FIXME: added post GoW
            tokenMap[0x4A] = typeof(EmptyParmToken);
            // FIXME: added post GoW
            tokenMap[0x4B] = typeof(InstanceDelegateToken);
            // Attested in GoW
            tokenMap[0x50] = typeof(UndefinedVariableToken);

            tokenMap[0x51] = typeof(InterfaceContextToken);
            tokenMap[0x52] = typeof(InterfaceCastToken);
            tokenMap[0x53] = typeof(EndOfScriptToken);
            tokenMap[0x54] = typeof(DynamicArrayAddToken);
            tokenMap[0x55] = typeof(DynamicArrayAddItemToken);
            tokenMap[0x56] = typeof(DynamicArrayRemoveItemToken);
            tokenMap[0x57] = typeof(DynamicArrayInsertItemToken);
            tokenMap[0x58] = typeof(DynamicArrayIteratorToken);
            // FIXME: added post GoW
            tokenMap[0x59] = typeof(DynamicArraySortToken);

            // Added with a late UDK build.
            tokenMap[0x03] = typeof(StateVariableToken);
            tokenMap[0x5A] = typeof(FilterEditorOnlyToken);
        }
#endif
    }
}
