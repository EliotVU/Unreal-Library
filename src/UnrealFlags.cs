using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace UELib.Flags
{
    public struct UnrealFlags<TEnum> : IEquatable<ulong>
        where TEnum : Enum
    {
        private readonly ulong _RawValue;

        public readonly ulong[] FlagsMap;

        public UnrealFlags(ulong rawValue)
        {
            _RawValue = rawValue;
            FlagsMap = null;
        }

        public UnrealFlags(ulong rawValue, ulong[] flagsMap)
        {
            Debug.Assert(flagsMap != null);

            _RawValue = rawValue;
            FlagsMap = flagsMap;
        }

        public UnrealFlags(ulong[] flagsMap, params TEnum[] flagIndices)
        {
            Debug.Assert(flagsMap != null);

            ulong flags = 0;

            foreach (var flagIndex in flagIndices)
            {
                var source = flagIndex;
                int index = Unsafe.As<TEnum, int>(ref source);
                flags |= flagsMap[index];
            }

            _RawValue = flags;
            FlagsMap = flagsMap;
        }

        /// <summary>
        /// Gets the flags at the specified flag index.
        /// </summary>
        /// <param name="flagIndex">the flag index that resolves to the actual flags.</param>
        /// <returns>the actual flags.</returns>
        public ulong GetFlag(TEnum flagIndex)
        {
            int index = Unsafe.As<TEnum, int>(ref flagIndex);
            return GetFlag(index);
        }

        private ulong GetFlag(int flagIndex)
        {
            return FlagsMap != null ? GetFlag(FlagsMap, flagIndex) : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetFlag(ulong[] flagsMap, int flagIndex)
        {
            Debug.Assert(flagsMap != null);

            ulong flag = flagsMap[flagIndex];
            return flag;
        }

        /// <summary>
        /// Gets the flags at the specified flag indices.
        /// </summary>
        /// <param name="flagIndices">the flag indices that resolve to the actual flags.</param>
        /// <returns>the actual flags.</returns>
        public ulong GetFlags(params TEnum[] flagIndices)
        {
            return GetFlags(FlagsMap, flagIndices);
        }

        public ulong GetFlags(ulong[] flagsMap, params TEnum[] flagIndices)
        {
            Debug.Assert(flagsMap != null);

            ulong flags = 0;

            foreach (var flagIndex in flagIndices)
            {
                var source = flagIndex;
                int index = Unsafe.As<TEnum, int>(ref source);
                flags |= flagsMap[index];
            }

            return flags;
        }

        private bool HasFlag(int flagIndex)
        {
            return FlagsMap != null && HasFlag(FlagsMap, flagIndex);
        }

        private bool HasFlag(ulong[] flagsMap, int flagIndex)
        {
            Debug.Assert(flagsMap != null);

            ulong flag = flagsMap[flagIndex];
            return (_RawValue & flag) != 0;
        }

        /// <summary>
        /// Checks if any of the flags at the specified flag index are set.
        /// </summary>
        /// <param name="flagIndex">the flag index that resolves to the actual flags.</param>
        public bool HasFlag(TEnum flagIndex)
        {
            return FlagsMap != null && HasFlag(FlagsMap, flagIndex);
        }

        public bool HasFlag(ulong[] flagsMap, TEnum flagIndex)
        {
            int index = Unsafe.As<TEnum, int>(ref flagIndex);
            return HasFlag(flagsMap, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlags(uint rawFlags)
        {
            return (_RawValue & rawFlags) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlags(ulong rawFlags)
        {
            return (_RawValue & rawFlags) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(UnrealFlags<TEnum> flags)
        {
            return (ushort)flags._RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(UnrealFlags<TEnum> flags)
        {
            return (int)flags._RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator uint(UnrealFlags<TEnum> flags)
        {
            return (uint)flags._RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(UnrealFlags<TEnum> flags)
        {
            return flags._RawValue;
        }

        /// <summary>
        /// Enumerates the flags that are set.
        /// </summary>
        /// <returns>the indices to the flags map.</returns>
        public IEnumerable<int> EnumerateFlags()
        {
            return FlagsMap != null ? EnumerateFlags(FlagsMap) : [];
        }

        public IEnumerable<int> EnumerateFlags(ulong[] flagsMap)
        {
            Debug.Assert(flagsMap != null);

            for (var flagIndex = 0; flagIndex < flagsMap.Length - 1; ++flagIndex)
            {
                if (flagsMap[flagIndex] != 0 && HasFlag(flagsMap, flagIndex))
                    yield return flagIndex;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ulong other)
        {
            return _RawValue == other;
        }

        public string ToString(ulong[] flagsMap)
        {
            Debug.Assert(flagsMap != null);

            var stringBuilder = new StringBuilder();
            var values = Enum.GetValues(typeof(TEnum));
            ulong flags = _RawValue;
            for (int i = 0; i < values.Length - 1; i++)
            {
                ulong flag = _RawValue & GetFlag(flagsMap, i);
                if (flag == 0)
                {
                    continue;
                }

                flags &= ~(_RawValue & flag); // remove all matching bits.
                string name = Enum.GetName(typeof(TEnum), i);
                stringBuilder.Append($"0x{flag:X}:{name};");
            }

            // Remaining flags
            for (int i = 0; flags != 0; ++i, flags >>= 1)
            {
                if ((flags & 1) == 0)
                {
                    continue;
                }

                ulong flag = (1ul << i) & _RawValue;
                stringBuilder.Append($"0x{flag:X};");
            }

            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            return FlagsMap != null ? ToString(FlagsMap) : "";
        }
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="UnrealPackage"/>.
    ///
    /// <see cref="Branch.DefaultEngineBranch.PackageFlagsDefault"/>
    ///
    /// <seealso cref="Branch.DefaultEngineBranch.PackageFlagsUE1"/>
    /// <seealso cref="Branch.DefaultEngineBranch.PackageFlagsUE2"/>
    /// <seealso cref="Branch.DefaultEngineBranch.PackageFlagsUE3"/>
    /// <seealso cref="Branch.UE4.EngineBranchUE4.PackageFlagsUE4"/>
    /// </summary>
    public enum PackageFlag
    {
        AllowDownload,
        ClientOptional,
        ServerSideOnly,

        /// <summary>
        /// UE1???
        /// </summary>
        Encrypted,
#if UT
        /// <summary>
        /// The package is official and cannot be overriden.
        ///
        /// Can be enabled in 'System\Official.ini' [Packages] SavePackagesAsOfficial=true
        /// or in 'Package\Classes\Package.UPKG' [Flags] Official=true
        ///
        /// Exclusive to UE2.5 (UT2004)
        /// </summary>
        Official,
#endif
        Cooked,
#if UE3
        ContainsMap,
        ContainsDebugData,
        ContainsScript,
        StrippedSource,
#endif
#if UE4
        EditorOnly,
        UnversionedProperties,
        ReloadingForCooker,
        FilterEditorOnly,
#endif
        Max,
    }

    [Obsolete("Use the normalized PackageFlag instead")]
    [Flags]
    public enum PackageFlags : uint
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

        BrokenLinks = 0x00000008U,      // @Redefined(UE3, Cooked)

        /// <summary>
        /// The package is cooked.
        /// </summary>
        Cooked = 0x00000008U,      // @Redefined

        /// <summary>
        /// ???
        /// <= UT
        /// </summary>
        Unsecure = 0x00000010U,

        /// <summary>
        /// The package is encrypted.
        /// <= UT
        /// Also attested in file UT2004/Packages.MD5 but it is not encrypted.
        /// </summary>
        Encrypted = 0x00000020U,

#if UE4
        EditorOnly = 0x00000040U,
        UnversionedProperties = 0x00002000U,
#endif

        /// <summary>
        /// Clients must download the package.
        /// </summary>
        Need = 0x00008000U,

        /// <summary>
        /// Unknown flags
        /// -   0x20000000  -- Probably means the package contains Content(Meshes, Textures)
        /// </summary>
        ///

        /// Package holds map data.
        ContainsMap = 0x00020000U,

        /// <summary>
        /// Package contains classes.
        /// </summary>
        ContainsScript = 0x00200000U,

        /// <summary>
        /// The package was build with -Debug
        /// </summary>
        ContainsDebugData = 0x00400000U,

        Imports = 0x00800000U,

        Compressed = 0x02000000U,
        FullyCompressed = 0x04000000U,

        /// <summary>
        /// Whether package has metadata exported(anything related to the editor).
        /// </summary>
        NoExportsData = 0x20000000U,

        /// <summary>
        /// Package's source is stripped.
        /// UE4: Same as ReloadingForCooker?
        /// </summary>
        Stripped = 0x40000000U,
#if UE4
        FilterEditorOnly = 0x80000000U,
#endif
        Protected = 0x80000000U,
#if TRANSFORMERS
        HMS_XmlFormat = 0x80000000U,
#endif
    }

    /// <summary>
    /// Flags that specify the compression algorithm used to compress any chunk in a package.
    /// </summary>
    [Flags]
    public enum CompressionFlags : uint
    {
        ZLIB                = 0x00000001U,
        ZLO                 = 0x00000002U,
        ZLX                 = 0x00000004U,
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="UExportTableItem"/>.
    ///
    /// Introduced with UE3 and replaced with individual booleans as of UE4
    /// </summary>
    [Flags]
    public enum ExportFlags : uint
    {
        ForcedExport        = 0x00000001U,
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="UObject"/>.
    /// </summary>
    public enum ObjectFlag
    {
        /// <summary>
        /// The object is transactional.
        /// </summary>
        Transactional,

        /// <summary>
        /// The object is standalone.
        /// </summary>
        Standalone,

        /// <summary>
        /// The object is public, or private if not set.
        /// </summary>
        Public,

        /// <summary>
        /// The object (UProperty) is protected.
        /// 
        /// Introduced at some point during UE2
        /// </summary>
        Protected,

        /// <summary>
        /// The object is final (private)
        /// 
        /// Introduced at some point during UE2, as for UE1 and UE3 a property can be considered private if <seealso cref="Public"/> is unset.
        ///
        /// Deprecated with UE3's expansion to 64 bit object flags.
        /// </summary>
        Final,

        /// <summary>
        /// The object should be loaded on a client.
        /// </summary>
        LoadForClient,

        /// <summary>
        /// The object should be loaded on a server.
        /// </summary>
        LoadForServer,

        /// <summary>
        /// The object should be loaded on an editor.
        /// </summary>
        LoadForEditor,

        /// <summary>
        /// The object should not be loaded on a client.
        ///
        /// If not set, the flag <see cref="LoadForClient"/> is expected to be set.
        /// </summary>
        NotForClient,

        /// <summary>
        /// The object should not be loaded on a server.
        ///
        /// If not set, the flag <see cref="LoadForServer"/> is expected to be set.
        /// </summary>
        NotForServer,

        /// <summary>
        /// The object should not be loaded on an editor.
        ///
        /// If not set, the flag <see cref="LoadForEditor"/> is expected to be set.
        /// </summary>
        NotForEditor,

        /// <summary>
        /// The object should not be saved.
        /// 
        /// <seealso cref="ClassFlags.Transient"/>
        /// <seealso cref="StructFlags.Transient"/>
        /// <seealso cref="PropertyFlagsLO.Transient"/>
        /// </summary>
        Transient,

        /// <summary>
        /// The object class is marked with the modifier 'Native'
        /// </summary>
        Native,

        /// <summary>
        /// The object has a script state frame.
        /// </summary>
        HasStack,

        /// <summary>
        /// The object should be localized by the instance name.
        ///
        /// Introduced at some point during UE2, and is usually applied to sub-objects with a <seealso cref="ClassFlags.Localized"/> class.
        /// Starting with UE3 it is also applied if the object class has the flag <seealso cref="ClassFlags.PerObjectLocalized"/>
        /// </summary>
        PerObjectLocalized,

        /// <summary>
        /// The object is a container for a class's default properties.
        /// </summary>
        ClassDefaultObject,

        /// <summary>
        /// The object is an archetype, and is used as a template for other objects.
        /// </summary>
        ArchetypeObject,

        /// <summary>
        /// The object is a template, true if one of <seealso cref="ArchetypeObject"/> and <seealso cref="ClassDefaultObject"/> are set.
        /// </summary>
        TemplateObject,

        Max
    }

    /// <summary>
    /// Lower order flags describing an instance of any <see cref="Core.UObject"/>.
    /// </summary>
    [Obsolete("Use the normalized ObjectFlag instead")]
    [Flags]
    public enum ObjectFlagsLO : ulong
    {
        Transactional       = 0x00000001U,

        [Obsolete("Of no use")]
        InSingularFunc      = 0x00000002U,

        Public              = 0x00000004U,

        [Obsolete("See Final")]
        Private             = 0x00000080U,

        /// <summary>
        /// Introduced at some point during UE2, as for UE1 a property can be considered private if <seealso cref="Public"/> is unset.
        /// </summary>
        Final               = 0x00000080U,

        [Obsolete("???")]
        Automated           = 0x00000100U,

        PerObjectLocalized  = 0x00000100U,

        /// <summary>
        /// Introduced at some point during UE2 and displaced to <seealso cref="ObjectFlagsHO.Protected"/> with UE3
        /// </summary>
        Protected           = 0x00000800U,

        Transient           = 0x00004000U,

        LoadForClient       = 0x00010000U,
        LoadForServer       = 0x00020000U,
        LoadForEdit         = 0x00040000U,
        Standalone          = 0x00080000U,
        NotForClient        = 0x00100000U,
        NotForServer        = 0x00200000U,
        NotForEdit          = 0x00400000U,

        HasStack            = 0x02000000U,
        Native              = 0x04000000U,

        [Obsolete("Of no use")]
        Marked              = 0x08000000U,
#if VENGEANCE
        // Used in Swat4 and BioShock constructor functions
        // const RF_Unnamed		= 0x08000000;
        VG_Unnamed          = 0x08000000U,
#endif
    }

    /// <summary>
    /// Higher order flags describing an instance of any <see cref="Core.UObject"/>.
    /// </summary>
    [Obsolete("Use the normalized ObjectFlag instead")]
    [Flags]
    public enum ObjectFlagsHO : ulong
    {
        [Obsolete("Of no use")]
        Obsolete                = 0x00000020U,

        /// <summary>
        /// 'Private', deprecated with UE3 64 bit flags, 0x80 may have a different meaning.
        /// </summary>
        [Obsolete("Of no use")]
        Final                   = 0x00000080U,

        PerObjectLocalized      = 0x00000100U,
        Protected               = 0x00000100U,
        PropertiesObject        = 0x00000200U,
        ArchetypeObject         = 0x00000400U,

        [Obsolete("Of no use")]
        RemappedName            = 0x00000800U,
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="Core.UFunction"/>.
    /// </summary>
    public enum FunctionFlag
    {
        Final,
        Defined,
        Iterator,
        Latent,
        PreOperator,
        Singular,
        Net,
        NetReliable,
        Simulated,
        Exec,
        Native,
        Event,
        Operator,
        Static,

        NoExport,
        Const,
        Invariant,

        Public,
        Private,
        Protected,

        Delegate,
        NetServer,
        NetClient,

        HasOutParms,
        HasOptionalParms,
        HasDefaults,

        DLLImport,
        K2Call,
        K2Override,
        K2Pure,

        Max,
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="Core.UFunction"/>.
    /// </summary>
    [Flags]
    public enum FunctionFlags : ulong
    {
        Final               = 0x00000001U,
        Defined             = 0x00000002U,
        Iterator            = 0x00000004U,
        Latent              = 0x00000008U,
        PreOperator         = 0x00000010U,
        Singular            = 0x00000020U,
        Net                 = 0x00000040U,
        NetReliable         = 0x00000080U,
        Simulated           = 0x00000100U,
        Exec                = 0x00000200U,
        Native              = 0x00000400U,
        Event               = 0x00000800U,
        Operator            = 0x00001000U,
        Static              = 0x00002000U,

        /// <summary>
        /// NoExport
        /// UE3 (~V300): Indicates whether we have optional parameters, including optional expression data.
        /// </summary>
        OptionalParameters  = 0x00004000U,
        NoExport            = 0x00004000U,

        Const               = 0x00008000U,
        Invariant           = 0x00010000U,

        // UE2 additions
        // =============

        Public              = 0x00020000U,
        Private             = 0x00040000U,
        Protected           = 0x00080000U,
        Delegate            = 0x00100000U,
#if VENGEANCE
        // Generated/Constructor?
        VG_Unk1             = 0x00200000U,
        VG_Overloaded       = 0x00800000U,
#endif
        /// <summary>
        /// UE2: Multicast (Replicated to all relevant clients)
        /// UE3: Function is replicated to relevant client.
        /// </summary>
        NetServer           = 0x00200000U,
#if UNREAL2
        Interface           = 0x00400000U,
#endif
        OutParameters       = 0x00400000U,
        ScriptStructs       = 0x00800000U,
        NetClient           = 0x01000000U,

        /// <summary>
        /// UE2: Unknown
        /// UE3 (V655)
        /// </summary>
        DLLImport           = 0x02000000U,
        // K2 Additions, late UDK, early implementation of Blueprints that were soon deprecated.
        K2Call              = 0x04000000U,
        K2Override          = 0x08000000U,
        K2Pure              = 0x10000000U,
#if AHIT
        AHIT_Multicast      = 0x04000000U,
        AHIT_NoOwnerRepl    = 0x08000000U,
        AHIT_Optional       = 0x10000000U,
        AHIT_EditorOnly     = 0x20000000U,
#endif
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="Core.UProperty"/>.
    /// </summary>
    public enum PropertyFlag
    {
        /// <summary>
        /// The property is a parameter of a function.
        /// </summary>
        Parm,

        /// <summary>
        /// The property is marked with the parameter modifier 'Optional'
        /// </summary>
        OptionalParm,

        /// <summary>
        /// The property is marked with the parameter modifier 'Out'
        /// </summary>
        OutParm,

        /// <summary>
        /// The property is marked with the parameter modifier 'Skip'
        /// </summary>
        SkipParm,

        /// <summary>
        /// The property is marked with the parameter modifier 'Coerce'
        /// </summary>
        CoerceParm,

        /// <summary>
        /// The property is the auto-generated return parameter of a function.
        /// </summary>
        ReturnParm,

        /// <summary>
        /// The property is marked with the modifier 'Const'
        /// </summary>
        Const,

        /// <summary>
        /// The property is marked with the modifier 'Input'
        /// </summary>
        Input,

        /// <summary>
        /// The property is marked with the modifier 'Init'
        /// </summary>
        AlwaysInit,

        /// <summary>
        /// The property needs a constructor link, and should be initialized in the constructor.
        ///
        /// Usually applied to property types String, Array or Map.
        /// </summary>
        CtorLink,

        /// <summary>
        /// The property is marked with the modifier 'Instanced' and the referenced object should be exported to T3D markup.
        ///
        /// Should also apply the flag <seealso cref="EditInline"/>
        /// </summary>
        ExportObject,

        /// <summary>
        /// The property is marked for replication, and has an associated script offset.
        /// </summary>
        Net,

        /// <summary>
        /// The property is marked with the modifier 'Native'
        /// </summary>
        Native,

        /// <summary>
        /// The property is marked with the modifier 'Config'
        /// </summary>
        Config,

        /// <summary>
        /// The property is marked with the modifier 'GlobalConfig'
        ///
        /// Should also apply the flag <seealso cref="Config"/>
        /// </summary>
        GlobalConfig,

        /// <summary>
        /// The property is marked with the modifier 'Localized'
        ///
        /// Should also apply the flag <seealso cref="Const"/> for UE3.
        /// </summary>
        Localized,

        /// <summary>
        /// The property is marked with the modifier 'Travel'
        /// </summary>
        Travel,

        /// <summary>
        /// The property is marked with the modifier 'Transient' and should not be serialized.
        /// </summary>
        Transient,

        /// <summary>
        /// The property is marked with the modifier 'DuplicateTransient' and the referenced object should be created anew when copied.
        /// 
        /// Introduced with UE3, (Not marked in UE2)
        /// </summary>
        DuplicateTransient,

        /// <summary>
        /// The property is marked with the modifier 'Deprecated' and should not be serialized.
        /// 
        /// Introduced with UE2
        /// </summary>
        Deprecated,

        /// <summary>
        /// The property is marked with the modifier 'NoExport' and should not be exported to the header file.
        /// </summary>
        NoExport,

        /// <summary>
        /// The property is marked with the modifier 'NoImport' and should not be import from a T3D markup.
        /// </summary>
        NoImport,

        /// <summary>
        /// The property is marked with the modifier 'NoClear' and should not be allowed to be cleared in the editor.
        /// </summary>
        NoClear,

        /// <summary>
        /// The property has a reference to a <see cref="Core.UComponent"/> derivative, or to a struct with components.
        /// In older builds this may have meant the property is marked with the modifier 'Component'.
        ///
        /// Should also apply the flag <seealso cref="ExportObject"/>
        /// 
        /// Introduced with UE3
        /// </summary>
        Component,

        /// <summary>
        /// The property is marked with the modifier 'Archetype' and has a reference to an archetype <see cref="Core.UComponent"/> derivative.
        ///
        /// Introduced with UE3
        /// </summary>
        Archetype,

        /// <summary>
        /// The property should have an associated button in the editor.
        /// var button MyButton;
        ///
        /// Deprecated with UE3
        /// </summary>
        Button,

        /// <summary>
        /// The property has an associated tooltip string.
        ///
        /// Introduced sporadically with UE2 and deprecated with UE3
        /// </summary>
        CommentString,

        /// <summary>
        /// The property is marked with the modifier 'DataBinding'
        /// 
        /// Introduced with UE3
        /// </summary>
        DataBinding,

        /// <summary>
        /// The property is marked with the modifier 'SerializeText'
        /// 
        /// Introduced with UE3
        /// </summary>
        SerializeText,

        /// <summary>
        /// The property is declared as an editable property in the editor and has an associated <see cref="Core.UProperty.CategoryName"/>
        /// </summary>
        Editable,

        /// <summary>
        /// The property is marked with the modifier 'EditConst'
        /// </summary>
        EditConst,

        /// <summary>
        /// The property is marked with the modifier 'EditConstArray'
        ///
        /// Introduced with UE2?
        /// </summary>
        EditConstArray,

        /// <summary>
        /// The property is marked with the modifier 'EditFixedSize'
        /// 
        /// Introduced with UE3
        /// </summary>
        EditFixedSize,

        /// <summary>
        /// The property is marked with the modifier 'EditInline'
        /// </summary>
        EditInline,

        /// <summary>
        /// The property is marked with the modifier 'EditInlineUse'
        ///
        /// Should also apply the flag <seealso cref="EditInline"/>
        /// </summary>
        EditInlineUse,

        /// <summary>
        /// The property is marked with the modifier 'EditInlineNotify'
        ///
        /// Should also apply the flag <seealso cref="EditInline"/>
        /// </summary>
        EditInlineNotify,

        /// <summary>
        /// The property is marked with the modifier 'EditTextBox'
        /// </summary>
        EditTextBox,

        /// <summary>
        /// The property is marked with the modifier 'EditHide'
        /// </summary>
        EditHide,

        /// <summary>
        /// The property is marked with the modifier 'EdFindable'
        /// </summary>
        EdFindable,

        /// <summary>
        /// The property is marked with the modifier 'EditorOnly'
        /// 
        /// Introduced with UE3
        /// </summary>
        EditorOnly,

        /// <summary>
        /// The property is marked with the modifier 'RepNotify'
        /// 
        /// Introduced with UE3
        /// </summary>
        RepNotify,

        /// <summary>
        /// The property is marked with the modifier 'RepRetry'
        /// 
        /// Introduced with UE3
        /// </summary>
        RepRetry,

        /// <summary>
        /// The property is marked with the modifier 'Interp'
        ///
        /// Should also apply the flag <seealso cref="Editable"/>
        /// 
        /// Introduced with UE3
        /// </summary>
        Interp,

        /// <summary>
        /// The property is marked with the modifier 'NonTransactional'
        /// 
        /// Introduced with UE3
        /// </summary>
        NonTransactional,

        /// <summary>
        /// The property is marked with the modifier 'NotForConsole'
        /// 
        /// Introduced with UE3
        /// </summary>
        NotForConsole,

        /// <summary>
        /// The property is marked with the modifier 'NotForFinalRelease'
        /// </summary>
        NotForFinalRelease,

        /// <summary>
        /// The property is marked with the modifier 'PrivateWrite'
        /// 
        /// Introduced with UE3
        /// </summary>
        PrivateWrite,

        /// <summary>
        /// The property is marked with the modifier 'ProtectedWrite'
        /// 
        /// Introduced with UE3
        /// </summary>
        ProtectedWrite,

        /// <summary>
        /// The property is marked with the modifier 'CrossLevelPassive'
        /// 
        /// Introduced with UE3
        /// </summary>
        CrossLevelPassive,

        /// <summary>
        /// The property is marked with the modifier 'CrossLevelActive'
        /// 
        /// Introduced with UE3
        /// </summary>
        CrossLevelActive,

        Max,
    }

    /// <summary>
    /// Lower order flags describing an instance of any <see cref="Core.UProperty"/>.
    /// </summary>
    [Flags]
    public enum PropertyFlagsLO : ulong // actually uint but were using ulong for UE2 and UE3 Compatibly
    {
        #region Parameters
        /// <summary>
        /// The parameter is optional.
        /// </summary>
        OptionalParm        = 0x00000010U,

        Parm                = 0x00000080U,      // Property is a part of the function parameters

        OutParm             = 0x00000100U,      // Reference(UE3) param

        SkipParm            = 0x00000200U,      // ???
        /// <summary>
        /// The property is a return type
        /// </summary>
        ReturnParm          = 0x00000400U,

        CoerceParm          = 0x00000800U,      // auto-cast
        #endregion

        Editable            = 0x00000001U,      // Can be set by UnrealEd users

        Const               = 0x00000002U,      // ReadOnly

        /// <summary>
        /// UE2
        /// </summary>
        Input               = 0x00000004U,      // Can be set with binds
        ExportObject        = 0x00000008U,      // Export sub-object properties to clipboard
        Net                 = 0x00000020U,      // Replicated

        EditConstArray      = 0x00000040U,      // Dynamic Array size cannot be changed by UnrealEd users
        EditFixedSize       = 0x00000040U,

        Native              = 0x00001000U,      // C++
        Transient           = 0x00002000U,      // Don't save
        Config              = 0x00004000U,      // Saved within .ini
        Localized           = 0x00008000U,      // Language ...
        Travel              = 0x00010000U,      // Keep value after travel
        EditConst           = 0x00020000U,      // ReadOnly in UnrealEd

        GlobalConfig        = 0x00040000U,
        NetAlways           = 0x00080000U,      // <= 61

        /// <summary>
        /// The property is a component.
        ///
        /// => UE3
        /// </summary>
        Component           = 0x00080000U,      // NetAlways in 61 <=
        OnDemand            = 0x00100000U,      // @Redefined(UE3, Init) Load on demand
        Init                = 0x00100000U,      //

        New                 = 0x00200000U,      // Inner object. @Removed(UE3)
        DuplicateTransient  = 0x00200000U,

        NeedCtorLink        = 0x00400000U,
        NoExport            = 0x00800000U,      // Don't export properties to clipboard
        NoImport            = 0x01000000U,
        /// <summary>
        /// ver button MyButton;
        /// </summary>
        Button              = 0x01000000U,
#if UT
        /// <summary>
        /// The property is marked with the modifier 'Cache' and can be exported to a .UCL cache file
        /// <seealso cref="ClassFlags.Cacheable"/>
        /// 
        /// Exclusive to UE2.5 (UT2004 and other derived games)
        /// </summary>
        Cache               = 0x01000000U,
#endif
#if VENGEANCE
        VG_NoCheckPoint     = 0x01000000U,
#endif
        EditorData          = 0x02000000U,      // @Redefined(UE3, NoClear)
        NoClear             = 0x02000000U,      // Don't permit reference clearing.

        EditInline          = 0x04000000U,
        EdFindable          = 0x08000000U,
#if AHIT
        AHIT_Bitwise        = 0x08000000U,
#endif
        EditInlineUse       = 0x10000000U,
        Deprecated          = 0x20000000U,

        EditInlineNotify    = 0x40000000U,      // Always set on Automated tagged properties (name is assumed!)
        DataBinding         = 0x40000000U,

        /// <summary>
        /// The property is marked with the modifier 'SerializeText'
        /// 
        /// Introduced with UE3
        /// </summary>
        SerializeText       = 0x80000000U,
#if UT
        /// <summary>
        /// The property is marked with the modifier 'Automated' and can be automated in a GUIComponent.
        /// Also enables <seealso cref="Editable"/>, <seealso cref="EditInline"/> and <seealso cref="EditInlineNotify"/>
        ///
        /// Exclusive to UE2.5? and UE2X
        /// For UE3 see <seealso cref="SerializeText"/>
        /// </summary>
        Automated           = 0x80000000U,
#endif
        EditInlineAll       = EditInline | EditInlineUse,
        Instanced           = ExportObject | EditInline,
    }

    /// <summary>
    /// Higher order flags describing an instance of any <see cref="Core.UProperty"/>.
    /// </summary>
    [Flags]
    public enum PropertyFlagsHO : ulong
    {
        RepNotify           = 0x00000001U,
        Interp              = 0x00000002U,
        NonTransactional    = 0x00000004U,
        EditorOnly          = 0x00000008U,
        NotForConsole       = 0x00000010U,
        RepRetry            = 0x00000020U,
        PrivateWrite        = 0x00000040U,
        ProtectedWrite      = 0x00000080U,
        Archetype           = 0x00000100U,
        EditHide            = 0x00000200U,
        EditTextBox         = 0x00000400U,
        // GAP!
        CrossLevelPassive   = 0x00001000U,
        CrossLevelActive    = 0x00002000U,
#if AHIT
        AHIT_Serialize      = 0x00004000U,
#endif
#if BIOSHOCK
        BIOINF_Unk1         = 0x00080000U,

        // DrawScale3D, DrawScale, PrePivot
        BIOINF_Unk2         = 0x00200000U,
        // XWeakReferenceProperty related.
        BIOINF_Unk3         = 0x01000000U,
#endif

        // Possible flags: CrossLevel, AllowAbstract
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="Core.UState"/>.
    /// </summary>
    public enum StateFlag
    {
        Editable,
        Auto,
        Simulated,

        HasLocalProps,

        Max,
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="Core.UState"/>.
    /// </summary>
    [Flags]
    public enum StateFlags : uint
    {
        Editable            = 0x00000001U,
        Auto                = 0x00000002U,
        Simulated           = 0x00000004U,
        HasLocalProps       = 0x00000008U,
    }

    /// <summary>  
    /// Flags describing an instance of any <see cref="Core.UClass"/>.  
    /// </summary>  
    public enum ClassFlag
    {
        /// <summary>
        /// The class is internal only (has no UnrealScript counter-part)
        /// </summary>
        Intrinsic,
        RuntimeStatic,
        Parsed,
        Compiled,

        HasInstancedProps,
        HasComponents,
        HasCrossLevelRefs,

        Abstract,
        SafeReplace,
        Config,
        Transient,
        Localized,
        Interface,

        NoExport,
        NoUserCreate,

        Placeable,
        NativeReplication,
        EditInlineNew,
        CollapseCategories,
        ExportStructs,
        HideDropDown,
        Hidden,
        Deprecated,
        Exported,
        NativeOnly,

        PerObjectConfig,
        PerObjectLocalized,

        Max
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="Core.UClass"/>.
    /// </summary>
    [Flags]
    public enum ClassFlags : ulong
    {
        [Obsolete]
        None                = 0x00000000U,
        Abstract            = 0x00000001U,
        Compiled            = 0x00000002U,
        Config              = 0x00000004U,
        Transient           = 0x00000008U,
        Parsed              = 0x00000010U,
        Localized           = 0x00000020U,
        SafeReplace         = 0x00000040U,

        /// <summary>
        /// True for some classes like <see cref="Engine.UPolys"/>
        /// </summary>
        RuntimeStatic       = 0x00000080U,

        NoExport            = 0x00000100U,
        NoUserCreate        = 0x00000200U,
        Placeable           = 0x00000200U,
        PerObjectConfig     = 0x00000400U,
        NativeReplication   = 0x00000800U,

        EditInlineNew       = 0x00001000U,
        CollapseCategories  = 0x00002000U,

        /// <summary>
        /// The compiler is instructed to export all struct declarations of the class to C++ header files.
        ///
        /// Replaced at some point during UE3 by <seealso cref="Interface"/>
        /// </summary>
        ExportStructs       = 0x00004000U,

        /// <summary>
        /// The class is declared as an interface.
        /// 
        /// Replaced legacy <seealso cref="ExportStructs"/> as of UE3
        /// </summary>
        Interface           = 0x00004000U,
#if AHIT
        AHIT_AlwaysLoaded   = 0x00008000U,
        AHIT_IterOptimized  = 0x00010000U,
#endif
        Instanced           = 0x00200000U,
        HasInstancedRefs    = 0x00200000,

        /// <summary>
        /// The class is marked with the modifier 'HideDropDown'
        /// 
        /// Displaced to UE3 <seealso cref="HideDropDown"/> and replaced by UE3 <seealso cref="HasComponents"/>
        /// </summary>
        HideDropDown        = 0x00400000U,

        /// <summary>
        /// Replaced legacy <seealso cref="HideDropDown"/> as of UE3
        /// </summary>
        HasComponents       = 0x00400000U,
#if UT
        /// <summary>
        /// The class is marked with the modifier 'CacheExempt' and is to be ignored when generating the class package .UCL file.
        ///
        /// Exclusive to UE2.5 and is overlapped by UE3 <seealso cref="Hidden"/>
        /// </summary>
        CacheExempt         = 0x00800000U,
#endif
        /// <summary>
        /// Hide the class in the Unreal Editor
        ///
        /// Introduced with UE3, overlaps <seealso cref="CacheExempt"/>
        /// </summary>
        Hidden              = 0x00800000U,

        /// <summary>
        /// The class is marked with the modifier 'ParseConfig' allowing the class config name to be configured in the commandline.
        /// 
        /// Replaced by UE3 <seealso cref="Deprecated"/>
        /// </summary>
        ParseConfig         = 0x01000000U,
#if VENGEANCE
        /// <summary>
        /// The class is marked with the modifier 'Interface'
        /// </summary>
        VG_Interface        = 0x01000000U,
#endif
        /// <summary>
        /// The class is marked with the modifier 'Deprecated' and won't be serialized.
        /// 
        /// Introduced with UE3 and replaced legacy <seealso cref="ParseConfig"/>
        /// </summary>
        Deprecated          = 0x01000000U,

#if UT
        /// <summary>
        /// The class can be cached because it contains properties with the flag <seealso cref="PropertyFlagsLO.Cache"/>
        /// 
        /// Exclusive to UE2.5 and is overlapped by UE3 <seealso cref="HideDropDown2"/>
        /// </summary>
        Cacheable           = 0x02000000U,
#endif
        /// <summary>
        /// The class is marked with the modifier 'HideDropDown' and will be hidden from any dropdown in the Unreal Editor
        /// 
        /// Displaced <seealso cref="HideDropDown"/> with UE3
        /// </summary>
        HideDropDown2       = 0x02000000U,

        /// <summary>
        /// Introduced with UE3
        /// </summary>
        Exported            = 0x04000000U,

        Intrinsic           = 0x10000000U,

        /// <summary>
        /// The class is marked with the modifier 'NativeOnly'
        /// 
        /// Introduced with UE3
        /// </summary>
        NativeOnly          = 0x20000000U,

        /// <summary>
        /// The class is marked with the modifier 'PerObjectLocalized', <seealso cref="ObjectFlagsHO.PerObjectLocalized"/> is also expected to be enabled.
        /// 
        /// Introduced at some point during UE3
        /// </summary>
        PerObjectLocalized  = 0x40000000U,
        HasCrossLevelRefs   = 0x80000000U,
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="Core.UStruct"/> and <see cref="Core.UScriptStruct"/>.
    /// </summary>
    public enum StructFlag
    {
        Native,
        Export,

        // UE2.5
        Long,
        Init,

        HasComponents,
        Transient,
        Atomic,
        Immutable,
        StrictConfig,
        ImmutableWhenCooked,
        AtomicWhenCooked,

        Max,
    }

    /// <summary>
    /// Flags describing an instance of any <see cref="Core.UScriptStruct"/>.
    /// </summary>
    [Flags]
    public enum StructFlags : uint
    {
        Native              = 0x00000001U,
        Export              = 0x00000002U,

        Long                = 0x00000004U,      // @Redefined(UE3, HasComponents)
        Init                = 0x00000008U,      // @Redefined(UE3, Transient)

        // UE3

        HasComponents       = 0x00000004U,      // @Redefined
        Transient           = 0x00000008U,      // @Redefined
        Atomic              = 0x00000010U,
        Immutable           = 0x00000020U,
        StrictConfig        = 0x00000040U,
        ImmutableWhenCooked = 0x00000080U,
        AtomicWhenCooked    = 0x00000100U,
    }
}
