using System;
using System.Text;

namespace UELib.Flags
{
    public class UnrealFlags<TEnum>
        where TEnum : Enum
    {
        private ulong _Flags;
        private readonly ulong[] _FlagsMap;
        
        public ulong Flags
        {
            get => _Flags;
            set => _Flags = value;
        }

        public UnrealFlags(ulong flags, ulong[] flagsMap)
        {
            _Flags = flags;
            _FlagsMap = flagsMap;
        }

        private bool HasFlag(int flagIndex)
        {
            ulong flag = _FlagsMap[flagIndex];
            return flag != 0 && (_Flags & flag) != 0;
        }

        public bool HasFlag(TEnum flagIndex)
        {
            ulong flag = _FlagsMap[(int)(object)flagIndex];
            return flag != 0 && (_Flags & _FlagsMap[(int)(object)flagIndex]) != 0;
        }
        
        public static explicit operator uint(UnrealFlags<TEnum> flags)
        {
            return (uint)flags._Flags;
        }

        public static explicit operator ulong(UnrealFlags<TEnum> flags)
        {
            return flags._Flags;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            var values = Enum.GetValues(typeof(TEnum));
            for (var i = 0; i < values.Length - 1 /* Max */; i++)
            {
                if (!HasFlag(i)) continue;
                string n = Enum.GetName(typeof(TEnum), i);
                stringBuilder.Append($"{n};");
            }
            return stringBuilder.ToString();
        }
    }

    /// <summary>
    /// <see cref="Branch.DefaultEngineBranch.PackageFlagsDefault"/>
    /// 
    /// <seealso cref="Branch.DefaultEngineBranch.PackageFlagsUE1"/>
    /// <seealso cref="Branch.DefaultEngineBranch.PackageFlagsUE2"/>
    /// <seealso cref="Branch.DefaultEngineBranch.PackageFlagsUE3"/>
    /// <seealso cref="UELib.Branch.UE4.EngineBranchUE4.PackageFlagsUE4"/>
    /// </summary>
    public enum PackageFlags
    {
        AllowDownload,
        ClientOptional,
        ServerSideOnly,

        /// <summary>
        /// UE1???
        /// </summary>
        Encrypted,
        
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

    [Flags]
    public enum CompressionFlags : uint
    {
        ZLIB                = 0x00000001U,
        ZLO                 = 0x00000002U,
        ZLX                 = 0x00000004U,
    }

    [Flags]
    public enum ExportFlags : uint
    {
        ForcedExport        = 0x00000001U,
    }

    /// <summary>
    /// Flags describing an object instance.
    ///
    /// Note:
    ///     This is valid for UE3 as well unless otherwise noted.
    ///
    /// @Redefined( Version, Clone )
    ///     The flag is redefined in (Version) as (Clone)
    ///
    /// @Removed( Version )
    ///     The flag is removed in (Version)
    /// </summary>
    [Flags]
    public enum ObjectFlagsLO : ulong
    {
        Transactional       = 0x00000001U,
        InSingularFunc      = 0x00000002U,
        Public              = 0x00000004U,

        Private             = 0x00000080U,
        Automated           = 0x00000100U,
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
        Marked              = 0x08000000U,
#if VENGEANCE
        // Used in Swat4 and BioShock constructor functions
        // const RF_Unnamed		= 0x08000000;
        VG_Unnamed          = 0x08000000U,
#endif
    }

    /// <summary>
    /// Flags describing an object instance(32-64 part) (In Unreal Engine 3 2006+ builds only).
    ///
    /// Note:
    ///     This is valid for UE3 as well unless otherwise noted.
    ///
    /// @Redefined( Version, Clone )
    ///     The flag is redefined in (Version) as (Clone)
    ///
    /// @Removed( Version )
    ///     The flag is removed in (Version)
    /// </summary>
    // FIXME: Merge with ObjectFlagsLO
    [Flags]
    public enum ObjectFlagsHO : ulong
    {
        Obsolete                = 0x00000020U,
        Final                   = 0x00000080U,
        PerObjectLocalized      = 0x00000100U,
        Protected               = 0x00000100U,
        PropertiesObject        = 0x00000200U,
        ArchetypeObject         = 0x00000400U,
        RemappedName            = 0x00000800U,
    }

    /// <summary>
    /// Flags describing a function instance.
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
        
        Interface           = 0x00400000U,
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
    }

    /// <summary>
    /// Flags describing an property instance.
    ///
    /// Note:
    ///     This is valid for UE3 as well unless otherwise noted.
    ///
    /// @Redefined( Version, Clone )
    ///     The flag is redefined in (Version) as (Clone)
    ///
    /// @Removed( Version )
    ///     The flag is removed in (Version)
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
        ExportObject        = 0x00000008U,      // Export suboject properties to clipboard
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
#if VENGEANCE
        VG_NoCheckPoint     = 0x01000000U,
#endif
        EditorData          = 0x02000000U,      // @Redefined(UE3, NoClear)
        NoClear             = 0x02000000U,      // Don't permit reference clearing.

        EditInline          = 0x04000000U,
        EdFindable          = 0x08000000U,
        EditInlineUse       = 0x10000000U,
        Deprecated          = 0x20000000U,

        EditInlineNotify    = 0x40000000U,      // Always set on Automated tagged properties (name is assumed!)
        DataBinding         = 0x40000000U,

        SerializeText       = 0x80000000U,
        #region UT2004 Flags
        Cache               = 0x01000000U,      // @Removed(UE3) Generate cache file: .ucl
        NoImport            = 0x01000000U,
        Automated           = 0x80000000U,      // @Removed(UE3)
        #endregion

        #region Combinations
        EditInlineAll       = (EditInline | EditInlineUse),
        Instanced           = ExportObject | EditInline,
        #endregion
    }

    /// <summary>
    /// Flags describing an property instance.
    ///
    /// Note:
    ///     This is valid for UE3 as well unless otherwise noted.
    ///
    /// @Redefined( Version, Clone )
    ///     The flag is redefined in (Version) as (Clone)
    ///
    /// @Removed( Version )
    ///     The flag is removed in (Version)
    /// </summary>
    [Flags]
    public enum PropertyFlagsHO : ulong // actually uint but were using ulong for UE2 and UE3 Compatably
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
    /// Flags describing an state instance.
    /// </summary>
    [Flags]
    public enum StateFlags : uint
    {
        Editable            = 0x00000001U,
        Auto                = 0x00000002U,
        Simulated           = 0x00000004U,
    }

    /// <summary>
    /// Flags describing an class instance.
    ///
    /// Note:
    ///     This is valid for UE3 as well unless otherwise noted.
    ///
    /// @Redefined( Version, Clone )
    ///     The flag is redefined in (Version) as (Clone)
    ///
    /// @Removed( Version )
    ///     The flag is removed in (Version)
    ///
    /// @Moved( Version, New )
    ///     The flag was moved since (Version) to a different value (New)
    /// </summary>
    [Flags]
    public enum ClassFlags : ulong // actually uint but were using ulong for UE2 and UE3+ Compatably
    {
        None                = 0x00000000U,
        Abstract            = 0x00000001U,
        Compiled            = 0x00000002U,
        Config              = 0x00000004U,
        Transient           = 0x00000008U,
        Parsed              = 0x00000010U,
        Localized           = 0x00000020U,
        SafeReplace         = 0x00000040U,

        NoExport            = 0x00000100U,
        Placeable           = 0x00000200U,
        PerObjectConfig     = 0x00000400U,
        NativeReplication   = 0x00000800U,
        EditInlineNew       = 0x00001000U,
        CollapseCategories  = 0x00002000U,
        ExportStructs       = 0x00004000U,      // @Removed(UE3 in early but not latest)

        Instanced           = 0x00200000U,      // @Removed(UE3)
        HideDropDown        = 0x00400000U,      // @Redefined(UE3, HasComponents), @Moved(UE3, HideDropDown2)
        ParseConfig         = 0x01000000U,      // @Redefined(UE3, Deprecated)
#if VENGEANCE
        VG_Interface        = 0x01000000U,
#endif
        #region Unique UT2004 Flags
        CacheExempt         = 0x00800000U,      // @Redefined(UE3, Hidden)
        #endregion

        #region Unique UE3 Flags    // New or Redefined Unreal Engine 3 Flags.
        HasComponents       = 0x00400000U,      // @Redefined Class has component properties.
        Hidden              = 0x00800000U,      // @Redefined Don't show this class in the editor class browser or edit inline new menus.
        Deprecated          = 0x01000000U,      // @Redefined Don't save objects of this class when serializing
        HideDropDown2       = 0x02000000U,
        Exported            = 0x04000000U,
        NativeOnly          = 0x20000000U,
        #endregion
    }

    /// <summary>
    /// Flags describing an struct instance.
    ///
    /// Note:
    ///     This is valid for UE3 as well unless otherwise noted.
    ///
    /// @Redefined( Version, Clone )
    ///     The flag is redefined in (Version) as (Clone)
    ///
    /// @Removed( Version )
    ///     The flag is removed in (Version)
    /// </summary>
    [Flags]
    public enum StructFlags : uint
    {
        Native              = 0x00000001U,
        Export              = 0x00000002U,
        Long                = 0x00000004U,      // @Redefined(UE3, HasComponents)
        Init                = 0x00000008U,      // @Redefined(UE3, Transient)

        #region Unique UE3 Flags    // New or Redefined Unreal Engine 3 Flags.
        HasComponents       = 0x00000004U,      // @Redefined
        Transient           = 0x00000008U,      // @Redefined
        Atomic              = 0x00000010U,
        Immutable           = 0x00000020U,
        StrictConfig        = 0x00000040U,
        ImmutableWhenCooked = 0x00000080U,
        AtomicWhenCooked    = 0x00000100U,
        #endregion
    }
}